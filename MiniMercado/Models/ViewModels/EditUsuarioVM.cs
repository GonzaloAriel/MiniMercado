using System.ComponentModel.DataAnnotations;

namespace MiniMercado.Models.ViewModels
{
    public class EditUsuarioVM
    {
        public string Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Rol { get; set; }
    }
}