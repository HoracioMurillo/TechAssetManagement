using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TechAssetManagement.Core.Entities;
using TechAssetManagement.Infraestructure;
using TechAssetManagement.Web.Filters;

namespace TechAssetManagement.Web.Controllers
{
    [AuthorizeUser] // Cualquier usuario logueado puede ver, pero restringiremos acciones
    public class AssetsController : Controller
    {
        private readonly AppDbContext _context;

        public AssetsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Assets (Listado)
        public async Task<IActionResult> Index()
        {
            return View(await _context.Assets.ToListAsync());
        }

        // GET: Assets/Create
        public IActionResult Create()
        {
            ViewData["AssetTypeId"] = new SelectList(_context.AssetTypes, "Id", "Name");
            return View();
        }

        // POST: Assets/Create
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Create(Asset asset)
        {
            // ... validaciones de serial ...
            if (await _context.Assets.AnyAsync(a => a.SerialNumber == asset.SerialNumber))
            {
                ModelState.AddModelError("SerialNumber", "Ya existe un equipo con este número de serie.");
            }

            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(asset.Status)) asset.Status = "Available";

                _context.Add(asset);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Equipo registrado correctamente.";
                return RedirectToAction(nameof(Index));
            }

            // --- CORRECCIÓN CRÍTICA AQUÍ ---
            // Si el modelo NO es válido (ej. falta nombre), volvemos a la vista.
            // PERO debemos volver a llenar el SelectList, si no, ¡el Select desaparece!
            ViewData["AssetTypeId"] = new SelectList(_context.AssetTypes, "Id", "Name", asset.AssetTypeId);
            // -------------------------------

            return View(asset);
        }

        // POST: CreateTypeJson (Para el Modal)
        [HttpPost]
        public async Task<IActionResult> CreateTypeJson([FromBody] AssetType assetType) // <--- [FromBody] es vital
        {
            // Verificamos que venga el nombre
            if (assetType == null || string.IsNullOrWhiteSpace(assetType.Name))
            {
                return Json(new { success = false, message = "El nombre es obligatorio" });
            }

            // Verificar duplicados
            if (await _context.AssetTypes.AnyAsync(t => t.Name == assetType.Name))
            {
                return Json(new { success = false, message = "Este tipo ya existe" });
            }

            _context.Add(assetType);
            await _context.SaveChangesAsync();

            // Retornamos los datos. OJO: .NET Core por defecto convierte a camelCase (id, name) en el JSON
            return Json(new { success = true, data = assetType });
        }

        // GET: Assets/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var asset = await _context.Assets.FindAsync(id);
            if (asset == null) return NotFound();

            return View(asset);
        }

        // POST: Assets/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Asset asset)
        {
            if (id != asset.Id) return NotFound();

            // Validar Serial único excluyendo el actual
            if (await _context.Assets.AnyAsync(a => a.SerialNumber == asset.SerialNumber && a.Id != id))
            {
                ModelState.AddModelError("SerialNumber", "Este número de serie pertenece a otro equipo.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(asset);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Equipo actualizado correctamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Assets.Any(e => e.Id == asset.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(asset);
        }

        // POST: Assets/Delete/5
        // Usaremos un método POST directo llamado desde JS para borrar
        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null)
            {
                return Json(new { success = false, message = "Equipo no encontrado" });
            }

            // Regla de Negocio: No borrar si tiene tickets históricos
            var hasTickets = await _context.Tickets.AnyAsync(t => t.AssetId == id);
            if (hasTickets)
            {
                return Json(new { success = false, message = "No se puede eliminar: El equipo tiene historial de tickets. Considere cambiar su estado a 'Retirado'." });
            }

            _context.Assets.Remove(asset);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Equipo eliminado correctamente." });
        }

   

    }
}