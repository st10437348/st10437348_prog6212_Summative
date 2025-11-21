using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using CMCSSummative.Data;

namespace CMCSSummative.Services
{
    public class PdfReportService
    {
        private readonly AppDbContext _db;
        public PdfReportService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<byte[]> GenerateClaimsReportAsync(DateTime from, DateTime to)
        {
            var claims = _db.Claims
                .Where(c => c.SubmittedAt >= from.ToUniversalTime() &&
                            c.SubmittedAt <= to.ToUniversalTime())
                .OrderBy(c => c.SubmittedAt)
                .ToList();

            using var doc = new PdfDocument();
            var page = doc.AddPage();
            var gfx = XGraphics.FromPdfPage(page);

            var headerFont = new XFont("Arial", 14, XFontStyle.Bold);
            var textFont = new XFont("Arial", 11);

            double marginLeft = 40;
            double marginTop = 40;
            double marginBottom = 40;
            double marginRight = 40;

            double y = marginTop;

            string header = $"Claims Report: {from:yyyy-MM-dd} to {to:yyyy-MM-dd}";
            gfx.DrawString(header, headerFont, XBrushes.Black,
                           new XPoint(marginLeft, y),
                           XStringFormats.TopLeft);

            y += gfx.MeasureString(header, headerFont).Height + 20;

            double usableWidth = page.Width - marginLeft - marginRight;

            foreach (var c in claims)
            {
                var lecturer = _db.Lecturers.FirstOrDefault(l => l.LecturerId == c.LecturerId);
                var lecturerName = lecturer?.Name ?? $"Lecturer {c.LecturerId}";

                string text =
                    $"Claim #{c.ClaimId} | {lecturerName} | Hours: {c.HoursWorked} | " +
                    $"Rate: {c.HourlyRate:C} | Total: {c.TotalAmount:C} | Status: {c.Status} | " +
                    $"Submitted: {c.SubmittedAt:yyyy-MM-dd}";

                var wrappedLines = WrapText(text, textFont, gfx, usableWidth);

                foreach (var line in wrappedLines)
                {
                    double lineHeight = gfx.MeasureString(line, textFont).Height;

                    if (y + lineHeight > page.Height - marginBottom)
                    {
                        page = doc.AddPage();
                        gfx = XGraphics.FromPdfPage(page);
                        y = marginTop;
                    }

                    gfx.DrawString(line, textFont, XBrushes.Black,
                                   new XRect(marginLeft, y, usableWidth, lineHeight),
                                   XStringFormats.TopLeft);

                    y += lineHeight + 4;
                }

                y += 6; 
            }

            using var ms = new MemoryStream();
            doc.Save(ms);
            return ms.ToArray();
        }

        private List<string> WrapText(string text, XFont font, XGraphics gfx, double maxWidth)
        {
            var result = new List<string>();
            var words = text.Split(' ');

            string current = "";

            foreach (var w in words)
            {
                string test = (current.Length == 0) ? w : current + " " + w;
                var size = gfx.MeasureString(test, font);

                if (size.Width > maxWidth)
                {
                    result.Add(current);
                    current = w;
                }
                else
                {
                    current = test;
                }
            }

            if (current.Length > 0)
                result.Add(current);

            return result;
        }
    }
}

