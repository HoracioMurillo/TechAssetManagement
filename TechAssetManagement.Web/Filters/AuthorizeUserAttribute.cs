using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TechAssetManagement.Web.Filters
{
    // Este atributo se usará así: [AuthorizeUser] o [AuthorizeUser("Admin")]
    public class AuthorizeUserAttribute : ActionFilterAttribute
    {
        private readonly string[] _roles;

        public AuthorizeUserAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // 1. Verificar si hay sesión
            var userId = context.HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                // Si no hay sesión, mandar al Login
                context.Result = new RedirectToActionResult("Login", "Access", null);
                return;
            }

            // 2. Verificar Roles (Si se especificaron)
            if (_roles.Length > 0)
            {
                var userRole = context.HttpContext.Session.GetString("UserRole");
                if (string.IsNullOrEmpty(userRole) || !_roles.Contains(userRole))
                {
                    // Si está logueado pero no tiene permiso -> Acceso Denegado
                    context.Result = new RedirectToActionResult("AccessDenied", "Access", null);
                }
            }

            base.OnActionExecuting(context);
        }
    }
}