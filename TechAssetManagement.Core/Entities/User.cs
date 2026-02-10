using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechAssetManagement.Core.Entities
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

    
        public string PasswordHash { get; set; } // Nunca guardamos la contraseña plana

        [Required]
        [StringLength(20)]
        public string Role { get; set; } // "Admin", "User"

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Propiedad calculada (no se guarda en DB) para mostrar nombre completo
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";

        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }
    }
}