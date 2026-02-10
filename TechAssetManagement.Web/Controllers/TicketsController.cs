using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TechAssetManagement.Core.Entities;
using TechAssetManagement.Core.Interfaces;
using TechAssetManagement.Infraestructure;
using TechAssetManagement.Web.Filters;

//Controlador para manejar CRUD de Tickets (Incidencias)
namespace TechAssetManagement.Web.Controllers
{
    [AuthorizeUser]
    public class TicketsController : Controller
    {
        private readonly AppDbContext _context;

        private readonly IEmailService _emailService;

        public TicketsController(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }
        // GET: Tickets
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            var query = _context.Tickets
                .Include(t => t.Asset)
                .Include(t => t.RequestedByUser)
                .Include(t => t.AssignedToUser)
                .AsQueryable();

            if (userRole == "Technician")
            {
                // Técnicos ven lo asignado a ellos
                query = query.Where(t => t.AssignedToUserId == userId);
            }
            else if (userRole != "Admin")
            {
                // Usuarios normales ven solo lo suyo
                query = query.Where(t => t.RequestedByUserId == userId);
            }

            return View(await query.OrderByDescending(t => t.CreatedAt).ToListAsync());
        }

        // GET: Tickets/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Tickets/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Ticket ticket)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Access");

            ticket.RequestedByUserId = userId.Value;
            ticket.CreatedAt = DateTime.Now;
            ticket.Status = "Open";

            ModelState.Remove("Asset");
            ModelState.Remove("RequestedByUser");
            ModelState.Remove("AssignedToUser");

            if (ticket.AssetId <= 0)
            {
                ModelState.AddModelError("AssetId", "Debes buscar y seleccionar un equipo válido.");
            }

            if (ModelState.IsValid)
            {
                _context.Add(ticket);
                await _context.SaveChangesAsync();

                // Historial Inicial
                _context.TicketHistories.Add(new TicketHistory
                {
                    TicketId = ticket.Id,
                    ChangedByUserId = userId.Value,
                    Action = "Ticket Creado",
                    NewValue = "Open",
                    ChangedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();
                string adminMsg = $@"
            <h2 style='color: #198754;'>¡Tienes una nueva tarea!</h2>
            <p>Hola <b>{ticket.RequestedByUser.FirstName}</b>, se te ha asignado el siguiente caso:</p>
            
            <div style='background-color: #fff3cd; border-left: 5px solid #ffc107; padding: 15px; margin: 20px 0;'>
                <p style='margin: 0; font-size: 18px;'><b>{ticket.Title}</b></p>
                <p style='margin: 5px 0 0; color: #666;'>{ticket.Description}</p>
            </div>

            <table style='width: 100%; border-collapse: collapse;'>
                <tr><td><b>Estado Actual:</b></td><td><span style='background-color: #0dcaf0; padding: 2px 8px; border-radius: 4px;'>{ticket.Status}</span></td></tr>
                <tr><td><b>Prioridad:</b></td><td>{ticket.Priority}</td></tr>
                <tr><td><b>Equipo:</b></td><td>{(ticket.Asset != null ? ticket.Asset.Name : "N/A")}</td></tr>
            </table>
            
            <br>
    
        ";
                await NotifyAdmins("Nuevo Ticket Registrado", adminMsg);

                TempData["Success"] = "Ticket creado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            return View(ticket);
        }

        // GET: Tickets/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.Tickets
                .Include(t => t.Asset)
                .Include(t => t.RequestedByUser)
                .Include(t => t.AssignedToUser)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null) return NotFound();

            // Cargar listas
            CargarListasEdit(ticket);

            return View(ticket);
        }

        // POST: Tickets/Edit/5 (VERSIÓN FIX DEFINITIVA)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Ticket modelForm)
        {
            if (id != modelForm.Id) return NotFound();

            var currentUserId = HttpContext.Session.GetInt32("UserId").Value;
            var userRole = HttpContext.Session.GetString("UserRole");

            // 1. Cargar el ticket REAL de la BD
            var ticketInDb = await _context.Tickets
                .Include(t => t.Asset)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticketInDb == null) return NotFound();

            // Variables para controlar el envío de correos
            bool hasChanges = false;
            bool isAssignedNewTech = false;
            int? newTechId = null;

            // 2. DETECCIÓN DE CAMBIOS

            // -- Cambio de Estado --
            if (ticketInDb.Status != modelForm.Status)
            {
                _context.TicketHistories.Add(new TicketHistory
                {
                    TicketId = id,
                    ChangedByUserId = currentUserId,
                    Action = "Cambio de Estado",
                    OldValue = ticketInDb.Status,
                    NewValue = modelForm.Status,
                    ChangedAt = DateTime.Now
                });
                ticketInDb.Status = modelForm.Status;
                hasChanges = true;
            }

            // -- Cambio de Prioridad (Solo Admin) --
            if (userRole == "Admin" && ticketInDb.Priority != modelForm.Priority)
            {
                _context.TicketHistories.Add(new TicketHistory
                {
                    TicketId = id,
                    ChangedByUserId = currentUserId,
                    Action = "Cambio de Prioridad",
                    OldValue = ticketInDb.Priority,
                    NewValue = modelForm.Priority,
                    ChangedAt = DateTime.Now
                });
                ticketInDb.Priority = modelForm.Priority;
                hasChanges = true;
            }

            // -- Reasignación (Solo Admin) --
            // Aquí comparamos ticketInDb (Base de datos) vs modelForm (Formulario)
            if (userRole == "Admin" && ticketInDb.AssignedToUserId != modelForm.AssignedToUserId)
            {
                // Guardamos bandera para enviar correo luego
                if (modelForm.AssignedToUserId != null)
                {
                    isAssignedNewTech = true;
                    newTechId = modelForm.AssignedToUserId;
                }

                var oldTechName = ticketInDb.AssignedToUserId.HasValue ? "ID " + ticketInDb.AssignedToUserId : "Sin Asignar";
                var newTechName = modelForm.AssignedToUserId.HasValue ? "ID " + modelForm.AssignedToUserId : "Sin Asignar";

                _context.TicketHistories.Add(new TicketHistory
                {
                    TicketId = id,
                    ChangedByUserId = currentUserId,
                    Action = "Reasignación",
                    OldValue = oldTechName,
                    NewValue = newTechName,
                    ChangedAt = DateTime.Now
                });

                ticketInDb.AssignedToUserId = modelForm.AssignedToUserId;
                hasChanges = true;
            }

            // 3. GUARDAR Y NOTIFICAR
            if (hasChanges)
            {
                try
                {
                    await _context.SaveChangesAsync();

                    // --- BLOQUE DE CORREOS (Aquí usamos ticketInDb ya actualizado) ---

                    // A. Notificar al Técnico (Si hubo reasignación)
                    if (isAssignedNewTech && newTechId.HasValue)
                    {
                        // Buscamos el correo del técnico (porque en el ticket solo tenemos el ID)
                        var techUser = await _context.Users.FindAsync(newTechId.Value);
                        if (techUser != null && !string.IsNullOrEmpty(techUser.Email))
                        {
                            string subject = $"Asignación de Ticket # {ticketInDb.Id}";
                            string msg = $@"Hola {techUser.FirstName},<br><br>
                                  Se te ha asignado un nuevo ticket:<br>
                                  <b>Título:</b> {ticketInDb.Title}<br>
                                  <b>Prioridad:</b> {ticketInDb.Priority}<br>
                                  <a href='tu-url/Tickets/Details/{id}'>Ver Ticket</a>";

                            // Envolvemos en try-catch para que si falla el correo NO falle el guardado
                            try
                            {
                                await _emailService.SendEmailAsync(techUser.Email, subject, msg);
                            }
                            catch { /* Log error de correo */ }
                        }
                    }

                    // B. Notificar a Admins (Resumen de actividad)
                    // Solo si cambió el estado a Closed o algo crítico, para no hacer spam
                    if (ticketInDb.Status == "Closed" || ticketInDb.Priority == "Critical")
                    {
                        string adminMsg = $"El ticket #{id} fue actualizado a estado: {ticketInDb.Status} por {HttpContext.Session.GetString("UserName")}.";
                        try
                        {
                            await NotifyAdmins($"Actualización Ticket #{id}", adminMsg);
                        }
                        catch { /* Log error de correo */ }
                    }

                    TempData["Success"] = "Ticket actualizado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error al guardar: " + ex.Message;
                }
            }
            else
            {
                TempData["Info"] = "No se detectaron cambios.";
                return RedirectToAction(nameof(Index));
            }

            // Recargar listas si falla
            CargarListasEdit(ticketInDb);
            return View(ticketInDb);
        }

        // GET: Tickets/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.Tickets
                .Include(t => t.Asset)
                .Include(t => t.RequestedByUser)
                .Include(t => t.AssignedToUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ticket == null) return NotFound();

            ViewBag.History = await _context.TicketHistories
                .Include(h => h.ChangedByUser)
                .Where(h => h.TicketId == id)
                .OrderByDescending(h => h.ChangedAt)
                .ToListAsync();

            return View(ticket);
        }

        // Método auxiliar para buscar activos por AJAX
        [HttpGet]
        public async Task<IActionResult> GetAssetBySerial(string serial)
        {
            if (string.IsNullOrEmpty(serial)) return Json(new { success = false, message = "Escribe un serial" });

            var asset = await _context.Assets
                .Where(a => a.SerialNumber == serial && a.Status != "Retired")
                .Select(a => new { a.Id, a.Name, a.Status })
                .FirstOrDefaultAsync();

            if (asset == null) return Json(new { success = false, message = "Equipo no encontrado." });

            return Json(new { success = true, data = asset });
        }

        // Método privado para no repetir código de listas
        private void CargarListasEdit(Ticket ticket)
        {
            var techUsers = _context.Users
                .Where(u => u.Role == "Technician" || u.Role == "Admin")
                .Select(u => new { Id = u.Id, FullName = $"{u.FirstName} {u.LastName} ({u.Role})" })
                .ToList();

            ViewData["Technicians"] = new SelectList(techUsers, "Id", "FullName", ticket.AssignedToUserId);
            ViewData["Assets"] = new SelectList(_context.Assets, "Id", "Name", ticket.AssetId);
        }
        private async Task NotifyAdmins(string subject, string message)
        {
            var admins = await _context.Users.Where(u => u.Role == "Admin" && u.IsActive).ToListAsync();
            foreach (var admin in admins)
            {
                // En un entorno real, esto se hace en background (Hangfire) para no lentear la app
                await _emailService.SendEmailAsync(admin.Email, subject, message);
            }
        }
    }
}