using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechAssetManagement.Core.Entities
{
    public class TicketHistory
    {
        [Key]
        public int Id { get; set; }

        public int TicketId { get; set; }
        [ForeignKey("TicketId")]
        public Ticket Ticket { get; set; }

        public int ChangedByUserId { get; set; }
        [ForeignKey("ChangedByUserId")]
        public User ChangedByUser { get; set; }

        [Required]
        public string Action { get; set; } // Ej: "Cambio de Estado", "Comentario Agregado"

        public string? OldValue { get; set; } // Ej: "Open"
        public string? NewValue { get; set; } // Ej: "In Progress"

        public DateTime ChangedAt { get; set; } = DateTime.Now;
    }
}