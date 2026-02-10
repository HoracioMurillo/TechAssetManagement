using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechAssetManagement.Infraestructure;
using TechAssetManagement.Web.Filters;
using TechAssetManagement.Web.Models;

//Controlador para manejar la Página Principal (Dashboard) y otras páginas generales como Privacy, Error, etc.

namespace TechAssetManagement.Web.Controllers
{
    [AuthorizeUser] // <--- ¡Seguridad aplicada a todo el controlador!
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Obtener contexto del usuario
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            // 2. Preparar consultas base
            var ticketsQuery = _context.Tickets.AsQueryable();
            var assetsQuery = _context.Assets.AsQueryable(); // Los activos suelen ser globales, pero puedes filtrar si quieres

            // 3. APLICAR FILTROS POR ROL
            if (userRole == "Technician")
            {
                // Técnico ve: Sus tickets asignados y Tickets sin asignar (para tomarlos)
                ticketsQuery = ticketsQuery.Where(t => t.AssignedToUserId == userId || t.AssignedToUserId == null);
            }
            else if (userRole == "User") // Usuario Normal
            {
                // Usuario ve: Solo lo que él creó
                ticketsQuery = ticketsQuery.Where(t => t.RequestedByUserId == userId);
            }
            // Admin ve TODO (no entra en los if)

            // 4. Calcular Métricas con los filtros aplicados
            var metrics = new DashboardViewModel
            {
                // Activos (Generalmente todos ven el inventario, o puedes restringirlo)
                TotalAssets = await assetsQuery.CountAsync(),
                AvailableAssets = await assetsQuery.CountAsync(a => a.Status == "Available"),
                AssetsInMaintenance = await assetsQuery.CountAsync(a => a.Status == "Maintenance"),

                // Tickets (Filtrados por Rol)
                OpenTickets = await ticketsQuery.CountAsync(t => t.Status == "Open"),
                // Para Técnico/User "Crítico" significa sus tickets críticos. Para Admin, todos.
                CriticalTickets = await ticketsQuery.CountAsync(t => t.Priority == "Critical" && t.Status != "Closed"),

                // Tabla de Recientes (Filtrada)
                RecentTickets = await ticketsQuery
                    .Include(t => t.RequestedByUser)
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(5)
                    .AsNoTracking()
                    .ToListAsync()
            };

            return View(metrics);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(); // Simplificado
        }
    }
}