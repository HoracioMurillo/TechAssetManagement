using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechAssetManagement.Core.Entities
{
    public class Ticket
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string Priority { get; set; }

        [Required]
        public string Status { get; set; } = "Open";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Relaciones existentes
        public int AssetId { get; set; }
        [ForeignKey("AssetId")]
        public Asset Asset { get; set; }

        public int RequestedByUserId { get; set; }
        [ForeignKey("RequestedByUserId")]
        public User RequestedByUser { get; set; }

        // Agregamos esto recientemente
        public int? AssignedToUserId { get; set; }
        [ForeignKey("AssignedToUserId")]
        public User? AssignedToUser { get; set; }

        // --- AGREGA ESTO (La pieza que falta) ---
        // Relación 1 a Muchos: Un Ticket tiene muchos Historiales
        public ICollection<TicketHistory> TicketHistories { get; set; } = new List<TicketHistory>();
    }
}