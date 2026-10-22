using ClosedXML.Excel;
using Firmeza.Application.DTOs.Products;
using Firmeza.Application.Interfaces.Services;

namespace Firmeza.Infrastructure.Services;

public class ExcelExportService : IExcelExportService
{
    public Task<byte[]> ExportProductsToExcelAsync(IReadOnlyList<ProductDto> products, CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Catálogo de Productos");

        worksheet.ShowGridLines = true;

        // Paleta Corporativa Firmeza S.A.S.
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

        worksheet.Cell("B3").Value = "CATÁLOGO OFICIAL DE PRODUCTOS E INVENTARIO";
        worksheet.Cell("B3").Style.Font.Bold = true;
        worksheet.Cell("B3").Style.Font.FontSize = 12;
        worksheet.Cell("B3").Style.Font.FontColor = accentColor;

        worksheet.Cell("B4").Value = $"Fecha de Emisión: {DateTime.Now:dd/MM/yyyy HH:mm} | Total de Referencias: {products.Count} | Moneda: COP";
        worksheet.Cell("B4").Style.Font.Italic = true;
        worksheet.Cell("B4").Style.Font.FontSize = 9;
        worksheet.Cell("B4").Style.Font.FontColor = XLColor.FromArgb(100, 116, 139);

        // 2. Cabeceras de la Tabla (Fila 6)
        const int startRow = 6;
        var headers = new[]
        {
            ("A", "#", XLAlignmentHorizontalValues.Center),
            ("B", "ID REFERENCIA", XLAlignmentHorizontalValues.Center),
            ("C", "CATEGORÍA", XLAlignmentHorizontalValues.Left),
            ("D", "NOMBRE DEL PRODUCTO", XLAlignmentHorizontalValues.Left),
            ("E", "DESCRIPCIÓN TÉCNICA", XLAlignmentHorizontalValues.Left),
            ("F", "PRECIO UNITARIO (COP)", XLAlignmentHorizontalValues.Right),
            ("G", "STOCK DISPONIBLE", XLAlignmentHorizontalValues.Right),
            ("H", "VALOR EN INVENTARIO (COP)", XLAlignmentHorizontalValues.Right),
            ("I", "ESTADO", XLAlignmentHorizontalValues.Center)
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

            worksheet.Cell($"E{currentRow}").Value = p.Description ?? "N/A";
            worksheet.Cell($"E{currentRow}").Style.Alignment.WrapText = true;

            worksheet.Cell($"F{currentRow}").Value = p.Price;
            worksheet.Cell($"F{currentRow}").Style.NumberFormat.Format = "$ #,##0.00";
            worksheet.Cell($"F{currentRow}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            worksheet.Cell($"G{currentRow}").Value = p.Stock;
            worksheet.Cell($"G{currentRow}").Style.NumberFormat.Format = "#,##0";
            worksheet.Cell($"G{currentRow}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            // Fórmula dinámica de Excel: Precio * Stock
            worksheet.Cell($"H{currentRow}").FormulaA1 = $"=F{currentRow}*G{currentRow}";
            worksheet.Cell($"H{currentRow}").Style.NumberFormat.Format = "$ #,##0.00";
            worksheet.Cell($"H{currentRow}").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            var statusCell = worksheet.Cell($"I{currentRow}");
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

            // Cebra y bordes
            var rowRange = worksheet.Range($"A{currentRow}:I{currentRow}");
            if (itemNumber % 2 == 0)
            {
                // Solo pintar el fondo si no es la celda de estado que ya tiene color
                worksheet.Range($"A{currentRow}:H{currentRow}").Style.Fill.BackgroundColor = zebraColor;
            }

            rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            rowRange.Style.Border.OutsideBorderColor = borderColor;
            rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            rowRange.Style.Border.InsideBorderColor = borderColor;
            worksheet.Row(currentRow).Height = 22;

            currentRow++;
            itemNumber++;
        }

        // 4. Fila de Totales Consolidados
        var totalRow = currentRow;
        worksheet.Range($"A{totalRow}:E{totalRow}").Merge();
        var totalLabel = worksheet.Cell($"A{totalRow}");
        totalLabel.Value = "TOTALES CONSOLIDADOS:";
        totalLabel.Style.Font.Bold = true;
        totalLabel.Style.Font.FontSize = 10;
        totalLabel.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        totalLabel.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        var totalStockCell = worksheet.Cell($"G{totalRow}");
        totalStockCell.FormulaA1 = $"=SUM(G{startRow + 1}:G{totalRow - 1})";
        totalStockCell.Style.Font.Bold = true;
        totalStockCell.Style.NumberFormat.Format = "#,##0";
        totalStockCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        var totalValuationCell = worksheet.Cell($"H{totalRow}");
        totalValuationCell.FormulaA1 = $"=SUM(H{startRow + 1}:H{totalRow - 1})";
        totalValuationCell.Style.Font.Bold = true;
        totalValuationCell.Style.NumberFormat.Format = "$ #,##0.00";
        totalValuationCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        var totalRange = worksheet.Range($"A{totalRow}:I{totalRow}");
        totalRange.Style.Fill.BackgroundColor = totalsColor;
        totalRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
        totalRange.Style.Border.TopBorderColor = headerColor;
        totalRange.Style.Border.BottomBorder = XLBorderStyleValues.Double;
        totalRange.Style.Border.BottomBorderColor = headerColor;
        worksheet.Row(totalRow).Height = 24;

        // 5. Ajuste automático de anchos de columna
        worksheet.Columns().AdjustToContents();
        worksheet.Column("A").Width = 6;
        worksheet.Column("B").Width = 16;
        worksheet.Column("C").Width = 24;
        worksheet.Column("D").Width = 36;
        worksheet.Column("E").Width = 45;
        worksheet.Column("F").Width = 22;
        worksheet.Column("G").Width = 18;
        worksheet.Column("H").Width = 26;
        worksheet.Column("I").Width = 14;

        // Congelar filas de encabezado (Panel fijo en fila 6)
        worksheet.SheetView.FreezeRows(startRow);

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        return Task.FromResult(memoryStream.ToArray());
    }
}
