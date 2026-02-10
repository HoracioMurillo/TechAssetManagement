using Microsoft.EntityFrameworkCore;
using TechAssetManagement.Core.Entities;

namespace TechAssetManagement.Infraestructure
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<AssetType> AssetTypes { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<TicketHistory> TicketHistories { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Ticket>()
    .HasOne(t => t.AssignedToUser)
    .WithMany()
    .HasForeignKey(t => t.AssignedToUserId)
    .OnDelete(DeleteBehavior.Restrict);
            // ---------------------------------------------------------
            // SOLUCIÓN AL ERROR DE CICLOS O CASCADA MÚLTIPLE
            // ---------------------------------------------------------

            // 1. Configuración para Ticket -> RequestedByUser
            // Si se borra un usuario, NO borrar sus tickets en cascada (para mantener registro)
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.RequestedByUser)
                .WithMany()
                .HasForeignKey(t => t.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // 2. Configuración para TicketHistory -> ChangedByUser
            // Esta es la que te dio el error específico.
            // Si se borra un usuario, NO borrar el historial donde él hizo cambios.
            modelBuilder.Entity<TicketHistory>()
                .HasOne(th => th.ChangedByUser)
                .WithMany()
                .HasForeignKey(th => th.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // 3. Configuración para TicketHistory -> Ticket
            // Si se borra el Ticket, está bien que se borre su historial (Cascada es default),
            // pero a veces es mejor restringirlo también. Dejémoslo por defecto o Restrict.
            // Para este ejercicio, con arreglar los usuarios es suficiente.

            modelBuilder.Entity<AssetType>().HasData(
    new AssetType { Id = 1, Name = "Laptop" },
    new AssetType { Id = 2, Name = "Desktop" },
    new AssetType { Id = 3, Name = "Monitor" },
    new AssetType { Id = 4, Name = "Periférico" }
);
        }
     
    }
}