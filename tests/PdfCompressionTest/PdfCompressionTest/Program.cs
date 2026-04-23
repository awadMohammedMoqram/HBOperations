using HBOperations.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

var samplePath = Path.Combine(Path.GetTempPath(), "hb_sample_input.pdf");
var compressedPath = Path.Combine(Path.GetTempPath(), "hb_sample_output.pdf");

using (var doc = new PdfDocument())
{
    doc.Options.CompressContentStreams = false;
    doc.Options.NoCompression = true;
    doc.Options.FlateEncodeMode = PdfFlateEncodeMode.BestSpeed;

    var font = new XFont("Arial", 11, XFontStyle.Regular);
    var line = "Hadhramout Bank - transaction document - repeated test text. ";
    for (int p = 0; p < 8; p++)
    {
        var page = doc.AddPage();
        using var gfx = XGraphics.FromPdfPage(page);
        var y = 40.0;
        for (int i = 0; i < 50; i++)
        {
            gfx.DrawString(string.Concat(Enumerable.Repeat(line, 3)), font,
                XBrushes.Black, new XRect(20, y, page.Width - 40, 20), XStringFormats.TopLeft);
            y += 14;
        }
    }
    doc.Save(samplePath);
}

var originalSize = new FileInfo(samplePath).Length;
Console.WriteLine($"Sample PDF created: {samplePath}");
Console.WriteLine($"Original size: {originalSize:N0} bytes ({originalSize / 1024.0:F1} KB)");
Console.WriteLine();

using var loggerFactory = LoggerFactory.Create(b => b.AddSimpleConsole(o => o.SingleLine = true).SetMinimumLevel(LogLevel.Information));
var logger = loggerFactory.CreateLogger<PdfCompressionService>();
var service = new PdfCompressionService(logger);

await using var input = File.OpenRead(samplePath);
var result = await service.CompressAsync(input);

await using (var output = File.Create(compressedPath))
{
    result.OutputStream.Position = 0;
    await result.OutputStream.CopyToAsync(output);
}
result.OutputStream.Dispose();

var compressedSize = new FileInfo(compressedPath).Length;
var savedPct = (1.0 - (double)compressedSize / originalSize) * 100.0;

Console.WriteLine();
Console.WriteLine("=== RESULTS ===");
Console.WriteLine($"Original   : {originalSize,10:N0} bytes  ({originalSize / 1024.0:F1} KB)");
Console.WriteLine($"Compressed : {compressedSize,10:N0} bytes  ({compressedSize / 1024.0:F1} KB)");
Console.WriteLine($"Saved      : {savedPct,10:F1} %");
Console.WriteLine($"WasCompressed reported by service: {result.WasCompressed}");
Console.WriteLine($"Output: {compressedPath}");

try
{
    using var verify = PdfSharpCore.Pdf.IO.PdfReader.Open(compressedPath, PdfSharpCore.Pdf.IO.PdfDocumentOpenMode.Import);
    Console.WriteLine($"OK: Compressed PDF re-opens successfully ({verify.PageCount} pages)");
}
catch (Exception ex)
{
    Console.WriteLine($"FAIL: Compressed PDF failed to re-open: {ex.Message}");
    Environment.Exit(1);
}

Environment.Exit(savedPct > 0 ? 0 : 2);
// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
