using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechAssetManagement.Core.Entities;
using TechAssetManagement.Infraestructure;
using TechAssetManagement.Web.Filters;
using TechAssetManagement.Web.Models; // Asumiendo que moverás RegisterViewModel aquí o crearás UserCreateViewModel

namespace TechAssetManagement.Web.Controllers
{
    // SOLO los administradores pueden entrar a CUALQUIER acción de este controlador
    [AuthorizeUser("Admin")]
    public class UsersController : Controller
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // Listar usuarios
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // GET: Crear Usuario
        public IActionResult Create()
        {
            return View();
        }

        // POST: Crear Usuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user, string password)
        {
            // 1. SOLUCIÓN AL ERROR: Remover la validación del Hash porque se genera aquí
            ModelState.Remove("PasswordHash");

            // 2. Validar correo duplicado
            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
            {
                // Usamos SweetAlert en lugar de ModelState para el error principal
                TempData["Error"] = "El correo electrónico ya está registrado.";
                return View(user);
            }

            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(password))
                {
                    TempData["Error"] = "La contraseña es obligatoria.";
                    return View(user);
                }

                // Hashear password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                user.CreatedAt = DateTime.Now;
                user.IsActive = true;

                _context.Add(user);
                await _context.SaveChangesAsync();

                // ALERTA DE ÉXITO
                TempData["Success"] = $"Usuario {user.FirstName} creado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                // Si falla la validación (ej. campos vacíos), mostramos error genérico
                TempData["Error"] = "Por favor verifica los datos del formulario.";
            }

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return Json(new { success = false, message = "Usuario no encontrado" });

            // Invertir estado (Si es true pasa a false, y viceversa)
            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = user.IsActive ? "Usuario activado" : "Usuario desactivado" });
        }
    }
}