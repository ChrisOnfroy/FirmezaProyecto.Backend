using ClosedXML.Excel;
using Firmeza.Application.DTOs.Products;
using Firmeza.Application.Interfaces.Services;

namespace Firmeza.Infrastructure.Services;

public class ExcelExportService : IExcelExportService
{
    /// <summary>
    /// Genera la lista de precios oficial para clientes.
    /// Únicamente muestra categorías, nombres de productos, descripciones y precios.
    /// NO revela cantidades de stock, valorización económica de bodega ni datos sensibles del negocio.
    /// </summary>
    public Task<byte[]> ExportPriceListToExcelAsync(IReadOnlyList<ProductDto> products, CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Lista de Precios");

        worksheet.ShowGridLines = true;

        // Paleta Corporativa Firmeza S.A.S.
        var headerColor = XLColor.FromArgb(30, 41, 59);      // Slate 800 (#1E293B)
        var accentColor = XLColor.FromArgb(37, 99, 235);     // Blue 600 (#2563EB)
        var zebraColor = XLColor.FromArgb(248, 250, 252);    // Slate 50 (#F8FAFC)
        var borderColor = XLColor.FromArgb(203, 213, 225);   // Slate 300 (#CBD5E1)
        var noteColor = XLColor.FromArgb(100, 116, 139);     // Slate 500

        // 1. Encabezado Institucional
        worksheet.Cell("B2").Value = "FIRMEZA S.A.S. - MATERIALES & MAQUINARIA";
        worksheet.Cell("B2").Style.Font.Bold = true;
        worksheet.Cell("B2").Style.Font.FontSize = 16;
        worksheet.Cell("B2").Style.Font.FontColor = headerColor;

        worksheet.Cell("B3").Value = "LISTA OFICIAL DE PRECIOS AL PÚBLICO";
        worksheet.Cell("B3").Style.Font.Bold = true;
        worksheet.Cell("B3").Style.Font.FontSize = 12;
        worksheet.Cell("B3").Style.Font.FontColor = accentColor;

        worksheet.Cell("B4").Value = $"Fecha de Emisión: {DateTime.Now:dd/MM/yyyy} | Moneda: Pesos Colombianos (COP) | Precios sujetos a cambio sin previo aviso";
        worksheet.Cell("B4").Style.Font.Italic = true;
        worksheet.Cell("B4").Style.Font.FontSize = 9;
        worksheet.Cell("B4").Style.Font.FontColor = noteColor;

        // 2. Cabeceras de la Tabla (Fila 6) - Solo datos de cara al cliente
        const int startRow = 6;
        var headers = new[]
        {
            ("A", "#", XLAlignmentHorizontalValues.Center),
            ("B", "CATEGORÍA", XLAlignmentHorizontalValues.Left),
            ("C", "PRODUCTO", XLAlignmentHorizontalValues.Left),
            ("D", "DESCRIPCIÓN COMERCIAL", XLAlignmentHorizontalValues.Left),
            ("E", "PRECIO UNITARIO (COP)", XLAlignmentHorizontalValues.Right),
            ("F", "DISPONIBILIDAD", XLAlignmentHorizontalValues.Center)
        };

        foreach (var (col, title, align) in headers)
        {
            var cell = worksheet.Cell($"{col}{startRow}");
            cell.Value = title;
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 10;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = headerColor;
            cell.Style.Alignment.Horizontal = align;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = headerColor;
        }
        worksheet.Row(startRow).Height = 26;

        // 3. Filas de Datos
        var currentRow = startRow + 1;
        var itemNumber = 1;

        foreach (var p in products)
        {
            worksheet.Cell($"A{currentRow}").Value = itemNumber;
            worksheet.Cell($"A{currentRow}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell($"B{currentRow}").Value = p.CategoryName ?? "General";
            worksheet.Cell($"B{currentRow}").Style.Font.Bold = true;

            worksheet.Cell($"C{currentRow}").Value = p.Name;
            worksheet.Cell($"C{currentRow}").Style.Font.Bold = true;

            worksheet.Cell($"D{currentRow}").Value = string.IsNullOrWhiteSpace(p.Description) ? "Estándar" : p.Description;
            worksheet.Cell($"D{currentRow}").Style.Alignment.WrapText = true;

            worksheet.Cell($"E{currentRow}").Value = p.Price;
            worksheet.Cell($"E{currentRow}").Style.NumberFormat.Format = "$ #,##0.00";
            worksheet.Cell($"E{currentRow}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            worksheet.Cell($"E{currentRow}").Style.Font.Bold = true;

            // En lugar de números de stock, se muestra disponibilidad comercial
            var availCell = worksheet.Cell($"F{currentRow}");
            var isAvailable = p.Stock > 0;
            availCell.Value = isAvailable ? "Disponible" : "Bajo Pedido";
            availCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            availCell.Style.Font.Bold = true;
            availCell.Style.Font.FontSize = 9;

            if (isAvailable)
            {
                availCell.Style.Font.FontColor = XLColor.FromArgb(22, 101, 52);   // Green 800
                availCell.Style.Fill.BackgroundColor = XLColor.FromArgb(220, 252, 231); // Green 100
            }
            else
            {
                availCell.Style.Font.FontColor = XLColor.FromArgb(146, 64, 14);  // Amber 800
                availCell.Style.Fill.BackgroundColor = XLColor.FromArgb(254, 243, 199); // Amber 100
            }

            // Alternancia de colores cebra y bordes
            var rowRange = worksheet.Range($"A{currentRow}:F{currentRow}");
            if (itemNumber % 2 == 0)
            {
                worksheet.Range($"A{currentRow}:E{currentRow}").Style.Fill.BackgroundColor = zebraColor;
            }

            rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            rowRange.Style.Border.OutsideBorderColor = borderColor;
            rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            rowRange.Style.Border.InsideBorderColor = borderColor;
            worksheet.Row(currentRow).Height = 22;

            currentRow++;
            itemNumber++;
        }

        // 4. Nota de pie comercial para clientes
        var noteRow = currentRow + 1;
        worksheet.Range($"A{noteRow}:F{noteRow}").Merge();
        var clientNote = worksheet.Cell($"A{noteRow}");
        clientNote.Value = "* Para cotizaciones formales por volumen, fletes y despachos a obra, contáctenos en ventas@firmeza.com o a través de nuestros canales oficiales.";
        clientNote.Style.Font.Italic = true;
        clientNote.Style.Font.FontSize = 9;
        clientNote.Style.Font.FontColor = noteColor;
        clientNote.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        // 5. Ajuste de anchos de columna
        worksheet.Columns().AdjustToContents();
        worksheet.Column("A").Width = 6;
        worksheet.Column("B").Width = 24;
        worksheet.Column("C").Width = 38;
        worksheet.Column("D").Width = 48;
        worksheet.Column("E").Width = 22;
        worksheet.Column("F").Width = 18;

        // Congelar filas de encabezado (Panel fijo en fila 6)
        worksheet.SheetView.FreezeRows(startRow);

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        return Task.FromResult(memoryStream.ToArray());
    }

    /// <summary>
    /// Genera el reporte de inventario administrativo interno con existencias y valorización para gestión de la empresa.
    /// Exclusivo para administradores y dueños del negocio.
    /// </summary>
    public Task<byte[]> ExportInventoryReportToExcelAsync(IReadOnlyList<ProductDto> products, CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Auditoría de Inventario");

        worksheet.ShowGridLines = true;

        var headerColor = XLColor.FromArgb(30, 41, 59);      // Slate 800 (#1E293B)
        var accentColor = XLColor.FromArgb(37, 99, 235);     // Blue 600 (#2563EB)
        var zebraColor = XLColor.FromArgb(248, 250, 252);    // Slate 50 (#F8FAFC)
        var borderColor = XLColor.FromArgb(203, 213, 225);   // Slate 300 (#CBD5E1)
        var totalsColor = XLColor.FromArgb(241, 245, 249);   // Slate 100 (#F1F5F9)

        // 1. Encabezado Institucional
        worksheet.Cell("B2").Value = "FIRMEZA S.A.S. - MATERIALES & MAQUINARIA";
        worksheet.Cell("B2").Style.Font.Bold = true;
        worksheet.Cell("B2").Style.Font.FontSize = 16;
        worksheet.Cell("B2").Style.Font.FontColor = headerColor;

        worksheet.Cell("B3").Value = "REPORTE INTERNO DE AUDITORÍA Y VALORIZACIÓN DE INVENTARIO";
        worksheet.Cell("B3").Style.Font.Bold = true;
        worksheet.Cell("B3").Style.Font.FontSize = 12;
        worksheet.Cell("B3").Style.Font.FontColor = accentColor;

        worksheet.Cell("B4").Value = $"Fecha de Emisión: {DateTime.Now:dd/MM/yyyy HH:mm} | Total de Referencias: {products.Count} | Moneda: COP | USO CONFIDENCIAL";
        worksheet.Cell("B4").Style.Font.Italic = true;
        worksheet.Cell("B4").Style.Font.FontSize = 9;
        worksheet.Cell("B4").Style.Font.FontColor = XLColor.FromArgb(100, 116, 139);

        // 2. Cabeceras de la Tabla (Fila 6)
        const int startRow = 6;
        var headers = new[]
        {
            ("A", "#", XLAlignmentHorizontalValues.Center),
            ("B", "ID REF", XLAlignmentHorizontalValues.Center),
            ("C", "CATEGORÍA", XLAlignmentHorizontalValues.Left),
            ("D", "NOMBRE DEL PRODUCTO", XLAlignmentHorizontalValues.Left),
            ("E", "PRECIO UNITARIO (COP)", XLAlignmentHorizontalValues.Right),
            ("F", "STOCK FÍSICO", XLAlignmentHorizontalValues.Right),
            ("G", "VALOR EN INVENTARIO (COP)", XLAlignmentHorizontalValues.Right),
            ("H", "ESTADO", XLAlignmentHorizontalValues.Center)
        };

        foreach (var (col, title, align) in headers)
        {
            var cell = worksheet.Cell($"{col}{startRow}");
            cell.Value = title;
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 10;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = headerColor;
            cell.Style.Alignment.Horizontal = align;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = headerColor;
        }
        worksheet.Row(startRow).Height = 26;

        // 3. Filas de Datos
        var currentRow = startRow + 1;
        var itemNumber = 1;

        foreach (var p in products)
        {
            worksheet.Cell($"A{currentRow}").Value = itemNumber;
            worksheet.Cell($"A{currentRow}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            worksheet.Cell($"B{currentRow}").Value = p.Id.ToString()[..8].ToUpperInvariant();
            worksheet.Cell($"B{currentRow}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell($"B{currentRow}").Style.Font.FontName = "Courier New";
            worksheet.Cell($"B{currentRow}").Style.Font.FontSize = 9;

            worksheet.Cell($"C{currentRow}").Value = p.CategoryName ?? "Sin Categoría";
            worksheet.Cell($"C{currentRow}").Style.Font.Bold = true;

            worksheet.Cell($"D{currentRow}").Value = p.Name;

            worksheet.Cell($"E{currentRow}").Value = p.Price;
            worksheet.Cell($"E{currentRow}").Style.NumberFormat.Format = "$ #,##0.00";
            worksheet.Cell($"E{currentRow}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            worksheet.Cell($"F{currentRow}").Value = p.Stock;
            worksheet.Cell($"F{currentRow}").Style.NumberFormat.Format = "#,##0";
            worksheet.Cell($"F{currentRow}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            // Fórmula: Precio * Stock
            worksheet.Cell($"G{currentRow}").FormulaA1 = $"=E{currentRow}*F{currentRow}";
            worksheet.Cell($"G{currentRow}").Style.NumberFormat.Format = "$ #,##0.00";
            worksheet.Cell($"G{currentRow}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            var statusCell = worksheet.Cell($"H{currentRow}");
            statusCell.Value = p.IsActive ? "ACTIVO" : "INACTIVO";
            statusCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            statusCell.Style.Font.Bold = true;
            statusCell.Style.Font.FontSize = 9;
            if (p.IsActive)
            {
                statusCell.Style.Font.FontColor = XLColor.FromArgb(22, 101, 52);   // Green 800
                statusCell.Style.Fill.BackgroundColor = XLColor.FromArgb(220, 252, 231); // Green 100
            }
            else
            {
                statusCell.Style.Font.FontColor = XLColor.FromArgb(153, 27, 27);  // Red 800
                statusCell.Style.Fill.BackgroundColor = XLColor.FromArgb(254, 226, 226); // Red 100
            }

            var rowRange = worksheet.Range($"A{currentRow}:H{currentRow}");
            if (itemNumber % 2 == 0)
            {
                worksheet.Range($"A{currentRow}:G{currentRow}").Style.Fill.BackgroundColor = zebraColor;
            }

            rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            rowRange.Style.Border.OutsideBorderColor = borderColor;
            rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            rowRange.Style.Border.InsideBorderColor = borderColor;
            worksheet.Row(currentRow).Height = 22;

            currentRow++;
            itemNumber++;
        }

        // 4. Fila de Totales Consolidados (Exclusivo Auditoría Interna)
        var totalRow = currentRow;
        worksheet.Range($"A{totalRow}:E{totalRow}").Merge();
        var totalLabel = worksheet.Cell($"A{totalRow}");
        totalLabel.Value = "TOTALES DE INVENTARIO EN BODEGA:";
        totalLabel.Style.Font.Bold = true;
        totalLabel.Style.Font.FontSize = 10;
        totalLabel.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        totalLabel.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        var totalStockCell = worksheet.Cell($"F{totalRow}");
        totalStockCell.FormulaA1 = $"=SUM(F{startRow + 1}:F{totalRow - 1})";
        totalStockCell.Style.Font.Bold = true;
        totalStockCell.Style.NumberFormat.Format = "#,##0";
        totalStockCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        var totalValuationCell = worksheet.Cell($"G{totalRow}");
        totalValuationCell.FormulaA1 = $"=SUM(G{startRow + 1}:G{totalRow - 1})";
        totalValuationCell.Style.Font.Bold = true;
        totalValuationCell.Style.NumberFormat.Format = "$ #,##0.00";
        totalValuationCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        var totalRange = worksheet.Range($"A{totalRow}:H{totalRow}");
        totalRange.Style.Fill.BackgroundColor = totalsColor;
        totalRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
        totalRange.Style.Border.TopBorderColor = headerColor;
        totalRange.Style.Border.BottomBorder = XLBorderStyleValues.Double;
        totalRange.Style.Border.BottomBorderColor = headerColor;
        worksheet.Row(totalRow).Height = 24;

        // 5. Ajuste automático de anchos de columna
        worksheet.Columns().AdjustToContents();
        worksheet.Column("A").Width = 6;
        worksheet.Column("B").Width = 14;
        worksheet.Column("C").Width = 24;
        worksheet.Column("D").Width = 38;
        worksheet.Column("E").Width = 22;
        worksheet.Column("F").Width = 18;
        worksheet.Column("G").Width = 26;
        worksheet.Column("H").Width = 14;

        worksheet.SheetView.FreezeRows(startRow);

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        return Task.FromResult(memoryStream.ToArray());
    }

    /// <summary>
    /// Método por compatibilidad: por defecto genera la lista de precios segura de clientes.
    /// </summary>
    public Task<byte[]> ExportProductsToExcelAsync(IReadOnlyList<ProductDto> products, CancellationToken cancellationToken = default)
    {
        return ExportPriceListToExcelAsync(products, cancellationToken);
    }
}
