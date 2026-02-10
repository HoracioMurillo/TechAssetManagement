using System.ComponentModel.DataAnnotations;

namespace TechAssetManagement.Web.Models
{
    // ViewModel para la página de Login. Solo necesitamos Email y Password.
    public class LoginViewModel
    {
        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}