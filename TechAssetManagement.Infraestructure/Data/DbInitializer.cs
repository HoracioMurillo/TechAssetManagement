using BCrypt.Net; // Asegúrate de tener el using correcto
using TechAssetManagement.Core.Entities;

namespace TechAssetManagement.Infraestructure.Data
{
    public static class DbInitializer
    {
        public static void Seed(AppDbContext context)
        {
            // Verificar si ya existen usuarios
            if (context.Users.Any())
            {
                return; // La base de datos ya tiene datos
            }

            // Crear el Usuario Admin por defecto
            var adminUser = new User
            {
                FirstName = "Admin",
                LastName = "Principal",
                Email = "horacio89@hotmail.es",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.Now,
                // Contraseña por defecto: "Admin123!"
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!")
            };

            context.Users.Add(adminUser);
            context.SaveChanges();
        }
    }
}