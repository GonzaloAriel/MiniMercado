using System.ComponentModel.DataAnnotations;

namespace MiniMercado.Models.ViewModels
{
    public class CrearUsuarioVM
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        public string Rol { get; set; }
    }
}
