using System.Collections.Generic;
using System.Linq;

namespace MiniMercado.Models.Importacion
{
    public class ImportPreviewViewModel
    {
        public List<ImportItemPreviewViewModel> Items { get; set; } = new List<ImportItemPreviewViewModel>();
        public int TotalValidos => Items.Count(x => x.EsValido);
        public int TotalErrores => Items.Count(x => !x.EsValido);
    }
}
