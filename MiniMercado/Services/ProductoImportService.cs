using ClosedXML.Excel;
using MiniMercado.Models.Importacion;

namespace MiniMercado.Services
{
    public class ProductoImportService
    {
        public List<ProductoImportDto> LeerExcel(Stream stream)
        {
            var productos = new List<ProductoImportDto>();

            using var workbook = new XLWorkbook(stream);

            var worksheet = workbook.Worksheet(1);

            var rows = worksheet.RowsUsed().Skip(1);

            foreach (var row in rows)
            {
                try
                {
                    var producto = new ProductoImportDto
                    {
                        CodigoBarra = row.Cell(1).GetString(),
                        Nombre = row.Cell(2).GetString(),
                        Precio = decimal.Parse(row.Cell(3).GetString()),
                        Stock = decimal.Parse(row.Cell(4).GetString())
                    };

                    productos.Add(producto);
                }
                catch
                {
                    // después agregamos manejo elegante
                }
            }

            return productos;
        }
    }
}