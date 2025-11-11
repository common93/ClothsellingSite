using ClothingStore.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
public class OrderController : Controller
{
    private readonly AppDbContext _context;

    public OrderController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        return View(order);
    }
    public async Task<IActionResult> Invoice(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        using (var stream = new MemoryStream())
        {
            Document pdfDoc = new Document(PageSize.A4, 25, 25, 25, 25);
            PdfWriter.GetInstance(pdfDoc, stream);
            pdfDoc.Open();

            // --- Fonts ---
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 22);
            var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
            var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 11);
            var smallFont = FontFactory.GetFont(FontFactory.HELVETICA, 9);

            // --- Store Title ---
            pdfDoc.Add(new Paragraph("CLOTHING STORE", titleFont) { Alignment = Element.ALIGN_CENTER });
            pdfDoc.Add(new Paragraph("Invoice", boldFont) { Alignment = Element.ALIGN_CENTER });
            pdfDoc.Add(new Paragraph("\n"));

            // --- Order Info Box ---
            PdfPTable infoTable = new PdfPTable(2);
            infoTable.WidthPercentage = 100;
            infoTable.DefaultCell.Border = 0;

            infoTable.AddCell(new Phrase($"Order ID: #{order.Id}", normalFont));
            infoTable.AddCell(new Phrase($"Order Date: {order.OrderDate:dd MMM yyyy}", normalFont));
            infoTable.AddCell(new Phrase($"Customer: {order.CustomerName}", normalFont));
            infoTable.AddCell(new Phrase($"Payment: {order.PaymentMethod}", normalFont));
            infoTable.AddCell(new Phrase($"Email: {order.Email}", normalFont));
            infoTable.AddCell(new Phrase(""));

            pdfDoc.Add(infoTable);
            pdfDoc.Add(new Paragraph("\n"));

            // --- Address Section ---
            PdfPTable addressTable = new PdfPTable(1);
            addressTable.WidthPercentage = 100;

            PdfPCell addrHeader = new PdfPCell(new Phrase("Shipping Address", boldFont));
            addrHeader.BackgroundColor = new BaseColor(230, 230, 230);
            addrHeader.Padding = 6;
            addressTable.AddCell(addrHeader);

            PdfPCell addrCell = new PdfPCell(new Phrase(order.Address, normalFont));
            addrCell.Padding = 8;
            addressTable.AddCell(addrCell);

            pdfDoc.Add(addressTable);
            pdfDoc.Add(new Paragraph("\n"));

            // --- Items Table ---
            PdfPTable table = new PdfPTable(4);
            table.WidthPercentage = 100;
            table.SetWidths(new float[] { 45f, 15f, 10f, 20f });

            string[] headers = { "Product", "Price", "Qty", "Total" };
            foreach (var h in headers)
            {
                PdfPCell headerCell = new PdfPCell(new Phrase(h, boldFont));
                headerCell.BackgroundColor = new BaseColor(240, 240, 240);
                headerCell.HorizontalAlignment = Element.ALIGN_CENTER;
                headerCell.Padding = 6;
                table.AddCell(headerCell);
            }

            bool alt = false;
            foreach (var item in order.Items)
            {
                BaseColor bg = alt ? new BaseColor(250, 250, 250) : BaseColor.White;
                alt = !alt;

                table.AddCell(new PdfPCell(new Phrase(item.ProductName, normalFont)) { BackgroundColor = bg });
                table.AddCell(new PdfPCell(new Phrase($"₹{item.Price:N2}", normalFont)) { BackgroundColor = bg, HorizontalAlignment = Element.ALIGN_RIGHT });
                table.AddCell(new PdfPCell(new Phrase(item.Quantity.ToString(), normalFont)) { BackgroundColor = bg, HorizontalAlignment = Element.ALIGN_CENTER });
                table.AddCell(new PdfPCell(new Phrase($"₹{(item.Price * item.Quantity):N2}", normalFont)) { BackgroundColor = bg, HorizontalAlignment = Element.ALIGN_RIGHT });
            }

            pdfDoc.Add(table);
            pdfDoc.Add(new Paragraph("\n"));

            // --- Totals ---
            PdfPTable totalTable = new PdfPTable(2);
            totalTable.WidthPercentage = 50;
            totalTable.HorizontalAlignment = Element.ALIGN_RIGHT;

            totalTable.AddCell(new PdfPCell(new Phrase("Subtotal:", boldFont)) { Border = 0 });
            totalTable.AddCell(new PdfPCell(new Phrase($"₹{order.TotalAmount:N2}", boldFont)) { Border = 0, HorizontalAlignment = Element.ALIGN_RIGHT });

            totalTable.AddCell(new PdfPCell(new Phrase("Shipping:", boldFont)) { Border = 0 });
            totalTable.AddCell(new PdfPCell(new Phrase("FREE", boldFont)) { Border = 0, HorizontalAlignment = Element.ALIGN_RIGHT });

            totalTable.AddCell(new PdfPCell(new Phrase("Grand Total:", boldFont)) { Border = 0 });
            totalTable.AddCell(new PdfPCell(new Phrase($"₹{order.TotalAmount:N2}", boldFont)) { Border = 0, HorizontalAlignment = Element.ALIGN_RIGHT });

            pdfDoc.Add(totalTable);

            // --- Footer ---
            pdfDoc.Add(new Paragraph("\nThank you for shopping with us!", boldFont));
            pdfDoc.Add(new Paragraph("For support: support@clothingstore.com", smallFont) { Alignment = Element.ALIGN_CENTER });

            pdfDoc.Close();

            return File(stream.ToArray(), "application/pdf", $"Invoice_Order_{order.Id}.pdf");
        }
    }



}
