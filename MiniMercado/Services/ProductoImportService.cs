using ClosedXML.Excel;
using MiniMercado.Models.Importacion;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MiniMercado.Services
{
    public class ProductoImportService
    {
        private readonly string[] _aliasCodigo = { "codigo", "código", "codbarra", "sku", "ean", "barras", "barcode" };
        private readonly string[] _aliasNombre = { "nombre", "producto", "descripcion", "artículo", "articulo", "detalle" };
        private readonly string[] _aliasPrecio = { "precio", "p.venta", "precioventa", "pventa", "venta", "costo" };
        private readonly string[] _aliasStock = { "stock", "cantidad", "existencia", "disponible", "cant" };

        private string Normalizar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "";
            
            // Quitar tildes, caracteres especiales y normalizar a minúsculas sin espacios
            var b = new System.Text.StringBuilder();
            foreach (var c in texto.ToLower().Trim())
            {
                if (c == 'á') b.Append('a');
                else if (c == 'é') b.Append('e');
                else if (c == 'í') b.Append('i');
                else if (c == 'ó') b.Append('o');
                else if (c == 'ú') b.Append('u');
                else if (char.IsLetterOrDigit(c)) b.Append(c);
            }
            return b.ToString();
        }

        private decimal? ParseDecimalSeguro(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;

            // Limpiar el string: quitar $, espacios, y palabras como "unid", "unidad", "unidades"
            // Conservar números, comas, puntos y el signo menos
            string limpio = valor.ToLower().Trim();
            limpio = limpio.Replace("$", "").Replace(" ", "");
            
            // Eliminar sufijos de unidades comunes
            limpio = limpio.Replace("unidades", "").Replace("unidad", "").Replace("unid.", "").Replace("unid", "");
            
            // Usar regex para dejar solo lo que pertenece a un número en formato AR (0-9, punto, coma, menos)
            limpio = Regex.Replace(limpio, @"[^0-9,\.\-]", "");

            if (string.IsNullOrWhiteSpace(limpio)) return null;

            var culture = new CultureInfo("es-AR");
            if (decimal.TryParse(limpio, NumberStyles.Any, culture, out decimal resultado))
            {
                return resultado;
            }

            return null;
        }

        private int? BuscarColumna(IXLRow headerRow, string[] aliases)
        {
            if (headerRow == null) return null;
            var cells = headerRow.CellsUsed();
            foreach (var cell in cells)
            {
                var valorOriginal = cell.GetString();
                var valorNormalizado = Normalizar(valorOriginal);
                
                foreach (var alias in aliases)
                {
                    if (Normalizar(alias) == valorNormalizado)
                    {
                        return cell.Address.ColumnNumber;
                    }
                }
            }
            return null;
        }

        private IXLRow DetectarFilaEncabezado(IXLWorksheet worksheet)
        {
            IXLRow mejorFila = null;
            int maxCoincidencias = 0;

            // Recorrer las primeras 10 filas usadas
            var filas = worksheet.RowsUsed().Take(10);

            foreach (var fila in filas)
            {
                int coincidencias = 0;

                if (BuscarColumna(fila, _aliasCodigo) != null) coincidencias++;
                if (BuscarColumna(fila, _aliasNombre) != null) coincidencias++;
                if (BuscarColumna(fila, _aliasPrecio) != null) coincidencias++;
                if (BuscarColumna(fila, _aliasStock) != null) coincidencias++;

                if (coincidencias > maxCoincidencias)
                {
                    maxCoincidencias = coincidencias;
                    mejorFila = fila;
                }
            }

            return maxCoincidencias > 0 ? mejorFila : null;
        }

        public List<ProductoImportDto> LeerExcel(Stream stream)
        {
            var productos = new List<ProductoImportDto>();

            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);
            var headerRow = DetectarFilaEncabezado(worksheet);
            if (headerRow == null) return productos;

            int? colCod = BuscarColumna(headerRow, _aliasCodigo);
            int? colNom = BuscarColumna(headerRow, _aliasNombre);
            int? colPre = BuscarColumna(headerRow, _aliasPrecio);
            int? colSto = BuscarColumna(headerRow, _aliasStock);

            // Empezar desde la fila siguiente a la de encabezados
            var rows = worksheet.RowsUsed(r => r.RowNumber() > headerRow.RowNumber());

            foreach (var row in rows)
            {
                try
                {
                    var producto = new ProductoImportDto
                    {
                        CodigoBarra = colCod.HasValue ? row.Cell(colCod.Value).GetString() : "",
                        Nombre = colNom.HasValue ? row.Cell(colNom.Value).GetString() : "",
                        Precio = colPre.HasValue ? ParseDecimalSeguro(row.Cell(colPre.Value).GetString()) ?? 0 : 0,
                        Stock = colSto.HasValue ? ParseDecimalSeguro(row.Cell(colSto.Value).GetString()) ?? 0 : 0
                    };

                    productos.Add(producto);
                }
                catch
                {
                }
            }

            return productos;
        }

        public List<ImportItemPreviewViewModel> ObtenerPreview(Stream stream)
        {
            var items = new List<ImportItemPreviewViewModel>();

            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);
            
            var headerRow = DetectarFilaEncabezado(worksheet);
            if (headerRow == null)
            {
                throw new Exception("No se pudieron detectar encabezados válidos en el Excel.");
            }

            int? colCod = BuscarColumna(headerRow, _aliasCodigo);
            int? colNom = BuscarColumna(headerRow, _aliasNombre);
            int? colPre = BuscarColumna(headerRow, _aliasPrecio);
            int? colSto = BuscarColumna(headerRow, _aliasStock);

            // Validar columnas obligatorias
            if (colCod == null || colNom == null || colPre == null)
            {
                var faltantes = new List<string>();
                if (colCod == null) faltantes.Add("Código");
                if (colNom == null) faltantes.Add("Nombre");
                if (colPre == null) faltantes.Add("Precio");

                throw new Exception($"Faltan columnas obligatorias en el Excel: {string.Join(", ", faltantes)}");
            }

            // Empezar desde la fila siguiente a la de encabezados
            var rows = worksheet.RowsUsed(r => r.RowNumber() > headerRow.RowNumber());

            foreach (var row in rows)
            {
                var codigo = row.Cell(colCod.Value).GetString()?.Trim();
                var nombre = row.Cell(colNom.Value).GetString()?.Trim();
                var precioStr = row.Cell(colPre.Value).GetString()?.Trim();
                var stockStr = colSto.HasValue ? row.Cell(colSto.Value).GetString()?.Trim() : "0";

                // Ignorar filas totalmente vacías
                if (string.IsNullOrEmpty(codigo) && string.IsNullOrEmpty(nombre) && 
                    string.IsNullOrEmpty(precioStr) && string.IsNullOrEmpty(stockStr))
                    continue;

                var item = new ImportItemPreviewViewModel
                {
                    CodigoBarra = codigo,
                    Nombre = nombre,
                    EsValido = true,
                    MensajeError = ""
                };

                // Validaciones
                if (string.IsNullOrEmpty(codigo))
                {
                    item.EsValido = false;
                    item.MensajeError += "Código obligatorio. ";
                }

                if (string.IsNullOrEmpty(nombre))
                {
                    item.EsValido = false;
                    item.MensajeError += "Nombre obligatorio. ";
                }

                var precioParseado = ParseDecimalSeguro(precioStr);
                if (precioParseado.HasValue)
                {
                    item.Precio = precioParseado.Value;
                    if (item.Precio <= 0)
                    {
                        item.EsValido = false;
                        item.MensajeError += "Precio debe ser mayor a 0. ";
                    }
                }
                else
                {
                    item.EsValido = false;
                    item.MensajeError += "Precio inválido. ";
                }

                var stockParseado = ParseDecimalSeguro(stockStr);
                if (stockParseado.HasValue)
                {
                    item.Stock = stockParseado.Value;
                    if (item.Stock < 0)
                    {
                        item.EsValido = false;
                        item.MensajeError += "Stock debe ser mayor o igual a 0. ";
                    }
                }
                else
                {
                    item.Stock = 0; // Default si no existe la columna
                    if (colSto.HasValue && !string.IsNullOrEmpty(stockStr))
                    {
                        item.EsValido = false;
                        item.MensajeError += "Stock inválido. ";
                    }
                }

                items.Add(item);
            }

            return items;
        }
    }
}