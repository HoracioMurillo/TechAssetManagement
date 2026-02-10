using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechAssetManagement.Core.Entities
{
    public class Asset
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } // Ej: Laptop Dell XPS

        [StringLength(50)]
        public string SerialNumber { get; set; }

        public DateTime PurchaseDate { get; set; }

     
        public decimal Price { get; set; }

        [Required]
        public string Status { get; set; } = "Available"; // Available, Assigned, Maintenance, Retired

        // ... propiedades anteriores ...

        // Relación con AssetType
        [Required(ErrorMessage = "El tipo de activo es obligatorio")] // Validación importante
        public int AssetTypeId { get; set; }

        [ForeignKey("AssetTypeId")]
        public AssetType? AssetType { get; set; } // El ? evita validación circular en el ModelState

    }
}