using System.ComponentModel.DataAnnotations;

namespace TechAssetManagement.Core.Entities
{
    public class AssetType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } // Ej: Laptop, Monitor, Licencia
    }
}