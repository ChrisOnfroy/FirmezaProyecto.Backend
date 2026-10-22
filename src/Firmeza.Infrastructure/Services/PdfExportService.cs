using System.Globalization;
using Firmeza.Application.DTOs.Sales;
using Firmeza.Application.Interfaces.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Firmeza.Infrastructure.Services;

public class PdfExportService : IPdfExportService
{
    private static readonly CultureInfo CoCulture = new("es-CO");

    static PdfExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = true;
        QuestPDF.Settings.ThrowOnMissingFontFamilies = false;
    }

    public Task<byte[]> ExportSalesReportToPdfAsync(IReadOnlyList<SaleDto> sales, CancellationToken cancellationToken = default)
    {
        var totalAmount = sales.Sum(s => s.TotalAmount);
        var completedCount = sales.Count(s => s.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase));
        var pendingCount = sales.Count(s => s.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase));

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(25);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Lato"));

                // 1. Encabezado del Reporte
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem(2).Column(c =>
                        {
                            c.Item().Text("FIRMEZA S.A.S.").FontSize(18).Bold().FontColor("#1E3A8A");
                            c.Item().Text("Materiales & Maquinaria de Construcción • NIT: 901.458.789-2").FontSize(9).FontColor("#64748B");
                            c.Item().Text("REPORTE EJECUTIVO CONSOLIDADO DE VENTAS").FontSize(12).Bold().FontColor("#0F172A");
                        });

                        row.RelativeItem(1).AlignRight().Column(c =>
                        {
                            c.Item().Text($"Fecha de Emisión: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor("#64748B");
                            c.Item().Text($"Total Registros: {sales.Count}").FontSize(8).SemiBold();
                            c.Item().Text($"Moneda: COP ($)").FontSize(8).FontColor("#64748B");
                        });
                    });

                    col.Item().PaddingVertical(8).LineHorizontal(1).LineColor("#CBD5E1");

                    // Tarjetas de Resumen KPI
                    col.Item().PaddingBottom(10).Row(kpiRow =>
                    {
                        kpiRow.RelativeItem().Border(1).BorderColor("#E2E8F0").Background("#F8FAFC").Padding(8).Column(c =>
                        {
                            c.Item().Text("TOTAL FACTURADO").FontSize(7.5f).Bold().FontColor("#64748B");
                            c.Item().Text(totalAmount.ToString("C0", CoCulture)).FontSize(12).Bold().FontColor("#1E3A8A");
                        });

                        kpiRow.ConstantItem(10);

                        kpiRow.RelativeItem().Border(1).BorderColor("#E2E8F0").Background("#F8FAFC").Padding(8).Column(c =>
                        {
                            c.Item().Text("TOTAL TRANSACCIONES").FontSize(7.5f).Bold().FontColor("#64748B");
                            c.Item().Text($"{sales.Count} ventas").FontSize(12).Bold().FontColor("#0F172A");
                        });

                        kpiRow.ConstantItem(10);

                        kpiRow.RelativeItem().Border(1).BorderColor("#E2E8F0").Background("#F0FDF4").Padding(8).Column(c =>
                        {
                            c.Item().Text("VENTAS COMPLETADAS").FontSize(7.5f).Bold().FontColor("#166534");
                            c.Item().Text($"{completedCount}").FontSize(12).Bold().FontColor("#15803D");
                        });

                        kpiRow.ConstantItem(10);

                        kpiRow.RelativeItem().Border(1).BorderColor("#E2E8F0").Background("#FFFBEB").Padding(8).Column(c =>
                        {
                            c.Item().Text("VENTAS PENDIENTES").FontSize(7.5f).Bold().FontColor("#92400E");
                            c.Item().Text($"{pendingCount}").FontSize(12).Bold().FontColor("#B45309");
                        });
                    });
                });

                // 2. Tabla Principal de Ventas
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(25);  // #
                        columns.ConstantColumn(85);  // Factura / ID
                        columns.ConstantColumn(75);  // Fecha
                        columns.RelativeColumn(3);   // Cliente
                        columns.RelativeColumn(2);   // Identificación
                        columns.ConstantColumn(80);  // Estado
                        columns.ConstantColumn(45);  // Ítems
                        columns.RelativeColumn(2);   // Total (COP)
                    });

                    // Encabezados de Columna
                    table.Header(header =>
                    {
                        header.Cell().Background("#1E293B").Padding(6).AlignCenter().Text("#").FontColor(Colors.White).Bold();
                        header.Cell().Background("#1E293B").Padding(6).AlignLeft().Text("ID VENTA").FontColor(Colors.White).Bold();
                        header.Cell().Background("#1E293B").Padding(6).AlignLeft().Text("FECHA").FontColor(Colors.White).Bold();
                        header.Cell().Background("#1E293B").Padding(6).AlignLeft().Text("CLIENTE / RAZÓN SOCIAL").FontColor(Colors.White).Bold();
                        header.Cell().Background("#1E293B").Padding(6).AlignLeft().Text("IDENTIFICACIÓN").FontColor(Colors.White).Bold();
                        header.Cell().Background("#1E293B").Padding(6).AlignCenter().Text("ESTADO").FontColor(Colors.White).Bold();
                        header.Cell().Background("#1E293B").Padding(6).AlignCenter().Text("ÍTEMS").FontColor(Colors.White).Bold();
                        header.Cell().Background("#1E293B").Padding(6).AlignRight().Text("TOTAL (COP)").FontColor(Colors.White).Bold();
                    });

                    var index = 1;
                    foreach (var sale in sales)
                    {
                        var bg = index % 2 == 0 ? "#F8FAFC" : "#FFFFFF";

                        table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(5).AlignCenter().Text(index.ToString());
                        table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(5).AlignLeft().Text(sale.Id.ToString()[..8].ToUpperInvariant()).FontSize(8).SemiBold();
                        table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(5).AlignLeft().Text(sale.SaleDate.ToString("dd/MM/yyyy"));
                        table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(5).AlignLeft().Text(string.IsNullOrWhiteSpace(sale.CustomerName) ? "Cliente General" : sale.CustomerName).Bold();
                        table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(5).AlignLeft().Text(sale.CustomerIdentification ?? "N/A").FontSize(8).FontColor("#475569");

                        // Chip de estado
                        var (statusBg, statusText) = sale.Status.ToLowerInvariant() switch
                        {
                            "completed" => ("#DCFCE7", "#166534"),
                            "pending" => ("#FEF3C7", "#92400E"),
                            _ => ("#FEE2E2", "#991B1B")
                        };

                        table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(4).AlignCenter()
                            .Background(statusBg).Padding(2).Text(sale.Status.ToUpperInvariant()).FontSize(7.5f).Bold().FontColor(statusText);

                        table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(5).AlignCenter().Text(sale.Details.Count.ToString());
                        table.Cell().Background(bg).BorderBottom(1).BorderColor("#E2E8F0").Padding(5).AlignRight().Text(sale.TotalAmount.ToString("C2", CoCulture)).Bold();

                        index++;
                    }

                    // Fila de Totalización
                    table.Cell().ColumnSpan(7).Background("#F1F5F9").BorderTop(1).BorderColor("#0F172A").Padding(6).AlignRight().Text("TOTAL GENERAL FACTURADO:").Bold();
                    table.Cell().Background("#F1F5F9").BorderTop(1).BorderColor("#0F172A").Padding(6).AlignRight().Text(totalAmount.ToString("C2", CoCulture)).Bold().FontColor("#1E3A8A");
                });

                // 3. Pie de Página
                page.Footer().Column(col =>
                {
                    col.Item().LineHorizontal(1).LineColor("#E2E8F0");
                    col.Item().PaddingTop(4).Row(row =>
                    {
                        row.RelativeItem().Text("Firmeza S.A.S. - Sistema de Gestión Empresarial • Documento de Control Interno")
                            .FontSize(7.5f).FontColor("#94A3B8");

                        row.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Página ");
                            x.CurrentPageNumber();
                            x.Span(" de ");
                            x.TotalPages();
                        });
                    });
                });
            });
        });

        var pdfBytes = document.GeneratePdf();
        return Task.FromResult(pdfBytes);
    }

    public Task<byte[]> ExportSaleInvoiceToPdfAsync(SaleDto sale, CancellationToken cancellationToken = default)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(35);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontFamily("Lato"));

                // 1. Encabezado de Factura / Comprobante
                page.Header().Column(header =>
                {
                    header.Item().Row(row =>
                    {
                        // Datos Empresa
                        row.RelativeItem(3).Column(col =>
                        {
                            col.Item().Text("FIRMEZA S.A.S.").FontSize(22).Bold().FontColor("#1E3A8A");
                            col.Item().Text("Materiales & Maquinaria de Construcción").FontSize(10).SemiBold().FontColor("#475569");
                            col.Item().Text("NIT: 901.458.789-2 • Régimen Responsable de IVA").FontSize(8.5f).FontColor("#64748B");
                            col.Item().Text("Avenida El Dorado # 68C-61, Edificio Firmeza, Bogotá D.C.").FontSize(8.5f).FontColor("#64748B");
                            col.Item().Text("PBX: +57 (601) 745-8800 • Correo: facturacion@firmeza.com").FontSize(8.5f).FontColor("#64748B");
                        });

                        // Cuadro de Número de Factura
                        row.RelativeItem(2).Border(1.5f).BorderColor("#1E3A8A").Background("#F8FAFC").Padding(10).Column(col =>
                        {
                            col.Item().AlignCenter().Text("FACTURA DE VENTA").FontSize(11).Bold().FontColor("#1E3A8A");
                            col.Item().AlignCenter().Text($"No. FZ-{sale.Id.ToString()[..8].ToUpperInvariant()}").FontSize(14).Bold().FontColor("#0F172A");
                            col.Item().PaddingTop(4).AlignCenter().Text($"Fecha: {sale.SaleDate:dd/MM/yyyy HH:mm}").FontSize(8.5f);

                            var (statusBg, statusText) = sale.Status.ToLowerInvariant() switch
                            {
                                "completed" => ("#DCFCE7", "#166534"),
                                "pending" => ("#FEF3C7", "#92400E"),
                                _ => ("#FEE2E2", "#991B1B")
                            };

                            col.Item().PaddingTop(4).AlignCenter().Background(statusBg).Padding(3)
                                .Text(sale.Status.ToUpperInvariant()).FontSize(8).Bold().FontColor(statusText);
                        });
                    });

                    header.Item().PaddingVertical(12).LineHorizontal(1).LineColor("#E2E8F0");

                    // 2. Información del Cliente
                    header.Item().Border(1).BorderColor("#CBD5E1").Background("#F8FAFC").Padding(10).Row(clientRow =>
                    {
                        clientRow.RelativeItem().Column(c =>
                        {
                            c.Item().Text("DATOS DEL CLIENTE").FontSize(8).Bold().FontColor("#64748B");
                            c.Item().PaddingTop(2).Text(sale.CustomerName).FontSize(11).Bold().FontColor("#0F172A");
                            c.Item().Text($"Documento / NIT: {sale.CustomerIdentification ?? "Consumidor Final"}").FontSize(9).FontColor("#334155");
                            c.Item().Text($"Correo: {sale.CustomerEmail ?? "N/A"}").FontSize(8.5f).FontColor("#334155");
                        });

                        clientRow.RelativeItem().Column(c =>
                        {
                            c.Item().Text("DATOS DE ENTREGA & CONTACTO").FontSize(8).Bold().FontColor("#64748B");
                            c.Item().PaddingTop(2).Text($"Dirección: {sale.CustomerAddress ?? "Retiro en Bodega Principal"}").FontSize(9).FontColor("#334155");
                            c.Item().Text($"Teléfono: {sale.CustomerPhone ?? "N/A"}").FontSize(8.5f).FontColor("#334155");
                            c.Item().Text("Forma de Pago: Contado / Transferencia").FontSize(8.5f).FontColor("#334155");
                        });
                    });

                    header.Item().PaddingBottom(12);
                });

                // 3. Detalle de Ítems / Productos
                page.Content().Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);   // #
                            columns.RelativeColumn(5);    // Descripción del Producto
                            columns.ConstantColumn(50);   // Cantidad
                            columns.RelativeColumn(2);    // Precio Unitario
                            columns.RelativeColumn(2);    // Subtotal
                        });

                        table.Header(h =>
                        {
                            h.Cell().Background("#1E293B").Padding(6).AlignCenter().Text("#").FontColor(Colors.White).Bold();
                            h.Cell().Background("#1E293B").Padding(6).AlignLeft().Text("DESCRIPCIÓN DEL MATERIAL / EQUIPO").FontColor(Colors.White).Bold();
                            h.Cell().Background("#1E293B").Padding(6).AlignCenter().Text("CANT.").FontColor(Colors.White).Bold();
                            h.Cell().Background("#1E293B").Padding(6).AlignRight().Text("VR. UNITARIO").FontColor(Colors.White).Bold();
                            h.Cell().Background("#1E293B").Padding(6).AlignRight().Text("SUBTOTAL").FontColor(Colors.White).Bold();
                        });

                        var i = 1;
                        foreach (var detail in sale.Details)
                        {
                            var rowBg = i % 2 == 0 ? "#F8FAFC" : "#FFFFFF";

                            table.Cell().Background(rowBg).BorderBottom(1).BorderColor("#E2E8F0").Padding(6).AlignCenter().Text(i.ToString());
                            table.Cell().Background(rowBg).BorderBottom(1).BorderColor("#E2E8F0").Padding(6).AlignLeft().Text(detail.ProductName).SemiBold();
                            table.Cell().Background(rowBg).BorderBottom(1).BorderColor("#E2E8F0").Padding(6).AlignCenter().Text(detail.Quantity.ToString());
                            table.Cell().Background(rowBg).BorderBottom(1).BorderColor("#E2E8F0").Padding(6).AlignRight().Text(detail.UnitPrice.ToString("C2", CoCulture));
                            table.Cell().Background(rowBg).BorderBottom(1).BorderColor("#E2E8F0").Padding(6).AlignRight().Text(detail.Subtotal.ToString("C2", CoCulture)).Bold();

                            i++;
                        }
                    });

                    // 4. Bloque de Totales y Observaciones
                    col.Item().PaddingTop(12).Row(totalsRow =>
                    {
                        // Observaciones y Medios de Pago
                        totalsRow.RelativeItem(3).Column(obs =>
                        {
                            obs.Item().Text("NOTAS Y CONDICIONES COMERCIALES").FontSize(8).Bold().FontColor("#64748B");
                            obs.Item().PaddingTop(2).Text("• Esta factura se asimila a una letra de cambio según Art. 774 del Código de Comercio.")
                                .FontSize(7.5f).FontColor("#64748B");
                            obs.Item().Text("• Todo material debe ser revisado al momento de la entrega en obra.")
                                .FontSize(7.5f).FontColor("#64748B");
                            obs.Item().Text("• Consignaciones: Bancolombia Cta Corriente # 108-456789-21 a nombre de Firmeza S.A.S.")
                                .FontSize(7.5f).FontColor("#64748B");
                        });

                        totalsRow.ConstantItem(15);

                        // Resumen Financiero
                        totalsRow.RelativeItem(2).Border(1).BorderColor("#CBD5E1").Background("#F8FAFC").Padding(8).Column(summary =>
                        {
                            summary.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Subtotal:").FontSize(9);
                                r.RelativeItem().AlignRight().Text(sale.TotalAmount.ToString("C2", CoCulture)).FontSize(9);
                            });

                            summary.Item().PaddingVertical(2).Row(r =>
                            {
                                r.RelativeItem().Text("IVA (Incluido):").FontSize(8.5f).FontColor("#64748B");
                                r.RelativeItem().AlignRight().Text("$ 0.00").FontSize(8.5f).FontColor("#64748B");
                            });

                            summary.Item().LineHorizontal(1).LineColor("#CBD5E1");

                            summary.Item().PaddingTop(4).Background("#1E3A8A").Padding(6).Row(r =>
                            {
                                r.RelativeItem().Text("TOTAL A PAGAR:").FontSize(10.5f).Bold().FontColor(Colors.White);
                                r.RelativeItem().AlignRight().Text(sale.TotalAmount.ToString("C2", CoCulture)).FontSize(11).Bold().FontColor(Colors.White);
                            });
                        });
                    });
                });

                // 5. Pie de Página y Firmas
                page.Footer().Column(footer =>
                {
                    footer.Item().PaddingTop(20).Row(signatures =>
                    {
                        signatures.RelativeItem().Column(c =>
                        {
                            c.Item().LineHorizontal(1).LineColor("#94A3B8");
                            c.Item().PaddingTop(2).AlignCenter().Text("ENTREGADO POR / FIRMEZA S.A.S.").FontSize(7.5f).Bold().FontColor("#475569");
                        });

                        signatures.ConstantItem(40);

                        signatures.RelativeItem().Column(c =>
                        {
                            c.Item().LineHorizontal(1).LineColor("#94A3B8");
                            c.Item().PaddingTop(2).AlignCenter().Text("RECIBIDO A CONFORMIDAD / CLIENTE").FontSize(7.5f).Bold().FontColor("#475569");
                        });
                    });

                    footer.Item().PaddingTop(15).LineHorizontal(1).LineColor("#E2E8F0");

                    footer.Item().PaddingTop(4).Row(row =>
                    {
                        row.RelativeItem().Text("Factura Electrónica generada por Firmeza ERP v1.0 • Software Certificado")
                            .FontSize(7.5f).FontColor("#94A3B8");

                        row.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Página ");
                            x.CurrentPageNumber();
                            x.Span(" de ");
                            x.TotalPages();
                        });
                    });
                });
            });
        });

        var pdfBytes = document.GeneratePdf();
        return Task.FromResult(pdfBytes);
    }
}
