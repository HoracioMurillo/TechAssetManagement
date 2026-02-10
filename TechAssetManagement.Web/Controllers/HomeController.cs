using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechAssetManagement.Infraestructure;
using TechAssetManagement.Web.Filters;
using TechAssetManagement.Web.Models;

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
            // Lógica de Negocio: Obtener Métricas
            // Usamos AsNoTracking() porque solo vamos a leer, es más rápido.

            var metrics = new DashboardViewModel
            {
                TotalAssets = await _context.Assets.CountAsync(),
                AvailableAssets = await _context.Assets.CountAsync(a => a.Status == "Available"),
                AssignedAssets = await _context.Assets.CountAsync(a => a.Status == "Assigned"),
                AssetsInMaintenance = await _context.Assets.CountAsync(a => a.Status == "Maintenance"),

                OpenTickets = await _context.Tickets.CountAsync(t => t.Status == "Open"),
                CriticalTickets = await _context.Tickets.CountAsync(t => t.Priority == "Critical" && t.Status != "Closed"),

                // Traemos los últimos 5 tickets incluyendo quién los creó
                RecentTickets = await _context.Tickets
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