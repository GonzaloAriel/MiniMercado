using System.ComponentModel.DataAnnotations;

namespace MiniMercado.Models.ViewModels
{
    public class CambiarPasswordVM
    {
        public string Id { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva contraseña")]
        [MinLength(6)]
        public string NuevaPassword { get; set; }
    }
}