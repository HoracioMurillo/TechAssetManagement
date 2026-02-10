using System.ComponentModel.DataAnnotations;

namespace TechAssetManagement.Web.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string Nombre { get; set; }
        public string Apellido{ get; set; }


        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress]
        public string Email { get; set; }
     
        [DataType(DataType.Password)]
        public string Password { get; set; }

     
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
    }
}