using Microsoft.EntityFrameworkCore;
using System;
using TechAssetManagement.Core.Interfaces;
using TechAssetManagement.Infraestructure;
using TechAssetManagement.Infraestructure.Data;
using TechAssetManagement.Infraestructure.Services;
using TechAssetManagement.Core.Interfaces;
using TechAssetManagement.Infraestructure.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar conexión a SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Habilitar Session (Fundamental para tu auth manual)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // La sesión muere en 30 min inactiva
    options.Cookie.HttpOnly = true; // Seguridad: JS no puede leer la cookie
    options.Cookie.IsEssential = true;
});

// Add services to the container.
builder.Services.AddControllersWithViews();

// ...
builder.Services.AddScoped<IEmailService, EmailService>();
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        // Asegura que la DB exista (y aplica migraciones pendientes si las hay)
        context.Database.Migrate();
        // Ejecuta el sembrador
        DbInitializer.Seed(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al sembrar la base de datos.");
    }
}
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 3. Activar Session antes de Authorization
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Access}/{action=Login}/{id?}"); // Cambiaremos Home por Access/Login pronto

app.Run();