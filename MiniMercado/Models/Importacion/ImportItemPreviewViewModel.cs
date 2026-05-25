namespace MiniMercado.Models.Importacion
{
    public class ImportItemPreviewViewModel
    {
        public string CodigoBarra { get; set; }
        public string Nombre { get; set; }
        public string Rubro { get; set; }
        public decimal Precio { get; set; }
        public decimal Stock { get; set; }
        public bool EsValido { get; set; }
        public string MensajeError { get; set; }
    }
}
