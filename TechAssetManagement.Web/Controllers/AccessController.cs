using BCrypt.Net; // Asegúrate de usar BCrypt.Net-Next
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechAssetManagement.Core.Entities;
using TechAssetManagement.Core.Interfaces;
using TechAssetManagement.Infraestructure;
using TechAssetManagement.Web.Models;
//Controlador para manejar Login, Logout, Recuperación de Contraseña
namespace TechAssetManagement.Web.Controllers
{

    public class AccessController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        public AccessController(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("UserEmail") != null)
            {
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);

                if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    if (!user.IsActive)
                    {
                        TempData["Error"] = "Tu cuenta está desactivada. Contacta al Administrador del Sistema."; // SweetAlert
                        return View();
                    }

                    // Crear Sesión
                    HttpContext.Session.SetInt32("UserId", user.Id);
                    HttpContext.Session.SetString("UserEmail", user.Email);
                    HttpContext.Session.SetString("UserRole", user.Role);
                    HttpContext.Session.SetString("UserName", user.FirstName);

                    // ALERTA DE BIENVENIDA
                    TempData["Success"] = $"¡Bienvenido de nuevo, {user.FirstName}!";
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    TempData["Error"] = "Usuario o contraseña incorrectos."; 
                }
            }
            return View();
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Success"] = "Sesión cerrada correctamente."; 
            return RedirectToAction("Login");
        }

        public IActionResult ForgotPassword() => View();

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                // Por seguridad, no decimos si existe o no, pero aquí simulamos éxito
                TempData["Success"] = "Si el correo existe, recibirás instrucciones para la recuperacion";
                return View();
            }

          
            string token = Guid.NewGuid().ToString();
            user.ResetToken = token;
            user.ResetTokenExpiry = DateTime.Now.AddHours(1); 
            await _context.SaveChangesAsync();

            // Crear Link
            var resetLink = Url.Action("ResetPassword", "Access", new { token = token }, Request.Scheme);

            string body = $@"
    <h2>Recuperación de Acceso</h2>
    <p>Hola <b>{user.FirstName}</b>,</p>
    <p>Hemos recibido una solicitud para restablecer tu contraseña. Si no fuiste tú, ignora este mensaje.</p>
    <div style='text-align: center; margin: 30px 0;'>
        <a href='{resetLink}' style='background-color: #dc3545; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; font-size: 16px;'>Restablecer mi Contraseña</a>
    </div>
    <p><small>Este enlace expira en 1 hora.</small></p>
";
            await _emailService.SendEmailAsync(user.Email, "Recuperación de Contraseña - TechAsset", body);

            TempData["Success"] = "Revisa tu correo electrónico.";
            return View();
        }

        // GET: ResetPassword
        public async Task<IActionResult> ResetPassword(string token)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.ResetToken == token && u.ResetTokenExpiry > DateTime.Now);
            if (user == null) return Content("El enlace es inválido o ha expirado.");
            return View(new ResetPasswordViewModel { Token = token }); // Crea este ViewModel simple
        }

        // POST: ResetPassword
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.ResetToken == model.Token && u.ResetTokenExpiry > DateTime.Now);
            if (user == null) return RedirectToAction("Login");

            // Cambiar clave
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
            user.ResetToken = null; // Quemar token
            user.ResetTokenExpiry = null;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Contraseña restablecida. Inicia sesión.";
            return RedirectToAction("Login");
        }
    }
}