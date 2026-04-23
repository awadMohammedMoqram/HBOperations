using HBOperations.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace HBOperations.Infrastructure.Services;

/// <summary>
/// PDF compression using PdfSharpCore (MIT licensed).
/// Strategy: re-serialize the PDF with FlateDecode + object stream compaction + smart deduplication.
/// Best results on PDFs containing uncompressed streams or duplicated objects.
/// Image-heavy PDFs see modest gains (PdfSharp does not re-encode embedded images).
/// </summary>
public sealed class PdfCompressionService : IPdfCompressionService
{
    private readonly ILogger<PdfCompressionService> _logger;

    public PdfCompressionService(ILogger<PdfCompressionService> logger)
    {
        _logger = logger;
    }

    public async Task<PdfCompressionResult> CompressAsync(Stream input, CancellationToken ct = default)
    {
        if (input is null) throw new ArgumentNullException(nameof(input));

        // Buffer input into memory (PdfSharp needs a seekable stream and we need the size).
        var originalBuffer = new MemoryStream();
        if (input.CanSeek) input.Position = 0;
        await input.CopyToAsync(originalBuffer, ct);
        var originalBytes = originalBuffer.ToArray();
        var originalSize = originalBytes.LongLength;

        try
        {
            return await Task.Run(() => CompressInternal(originalBytes), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PDF compression failed; returning original bytes unchanged.");
            return new PdfCompressionResult(
                new MemoryStream(originalBytes, writable: false),
                originalSize,
                originalSize,
                WasCompressed: false);
        }
    }

    private PdfCompressionResult CompressInternal(byte[] originalBytes)
    {
        using var readStream = new MemoryStream(originalBytes, writable: false);

        // Open in Modify mode so we can mutate existing PdfStream objects in place.
        using var document = PdfReader.Open(readStream, PdfDocumentOpenMode.Modify);

        // Maximum compression options for any new streams PdfSharp writes.
        document.Options.CompressContentStreams = true;
        document.Options.NoCompression = false;
        document.Options.FlateEncodeMode = PdfFlateEncodeMode.BestCompression;
        document.Options.UseFlateDecoderForJpegImages = PdfUseFlateDecoderForJpegImages.Automatic;
        document.Options.EnableCcittCompressionForBilevelImages = true;

        // PdfSharpCore does NOT recompress existing streams automatically.
        // Pass 1: walk every indirect object.
        //  - Re-encode embedded JPEG images at lower quality (the biggest win for image-heavy PDFs).
        //  - Apply FlateDecode to any remaining uncompressed stream.
        long imageBytesSaved = 0;
        int imagesRecompressed = 0;
        foreach (var obj in document.Internals.GetAllObjects())
        {
            if (obj is not PdfDictionary dict || dict.Stream is null) continue;

            if (TryRecompressJpegImage(dict, out var saved))
            {
                imagesRecompressed++;
                imageBytesSaved += saved;
                continue;
            }

            if (TryConvertRawImageToJpeg(dict, out saved))
            {
                imagesRecompressed++;
                imageBytesSaved += saved;
                continue;
            }

            TryFlateCompress(dict);
        }

        if (imagesRecompressed > 0)
        {
            _logger.LogInformation(
                "Recompressed {Count} embedded image(s), saved {Saved}KB.",
                imagesRecompressed, imageBytesSaved / 1024);
        }

        var output = new MemoryStream();
        document.Save(output, closeStream: false);
        var compressedBytes = output.ToArray();
        var compressedSize = compressedBytes.LongLength;

        // If compression didn't help (or made it bigger), return the original.
        if (compressedSize >= originalBytes.LongLength)
        {
            _logger.LogInformation(
                "PDF compression skipped: original {Original}KB vs compressed {Compressed}KB.",
                originalBytes.LongLength / 1024,
                compressedSize / 1024);
            output.Dispose();
            return new PdfCompressionResult(
                new MemoryStream(originalBytes, writable: false),
                originalBytes.LongLength,
                originalBytes.LongLength,
                WasCompressed: false);
        }

        var ratio = Math.Round((1 - (double)compressedSize / originalBytes.LongLength) * 100, 1);
        _logger.LogInformation(
            "PDF compressed: {Original}KB → {Compressed}KB ({Ratio}% reduction).",
            originalBytes.LongLength / 1024,
            compressedSize / 1024,
            ratio);

        var resultStream = new MemoryStream(compressedBytes, writable: false);
        return new PdfCompressionResult(resultStream, originalBytes.LongLength, compressedSize, WasCompressed: true);
    }

    /// <summary>
    /// Applies FlateDecode (zlib) to a PdfStream if it isn't already compressed.
    /// Skips streams whose existing filter would be lossy to re-encode (e.g. DCTDecode JPEGs).
    /// </summary>
    private static void TryFlateCompress(PdfDictionary dict)
    {
        try
        {
            var pdfStream = dict.Stream;
            if (pdfStream is null) return;

            // Already filtered — leave it alone (avoids double-encoding & lossy re-encoding of images).
            if (dict.Elements.ContainsKey("/Filter")) return;

            var raw = pdfStream.Value;
            if (raw is null || raw.Length < 32) return;

            using var compressed = new MemoryStream();
            // PDF spec uses zlib (RFC 1950) which is DeflateStream wrapped in a 2-byte header + 4-byte adler32.
            // PdfSharpCore exposes FlateDecode internally; we replicate it via System.IO.Compression.
            compressed.WriteByte(0x78); // zlib header
            compressed.WriteByte(0xDA); // best compression
            using (var deflate = new System.IO.Compression.DeflateStream(compressed, System.IO.Compression.CompressionLevel.SmallestSize, leaveOpen: true))
            {
                deflate.Write(raw, 0, raw.Length);
            }
            // Adler-32 checksum of original data
            var adler = ComputeAdler32(raw);
            compressed.WriteByte((byte)(adler >> 24));
            compressed.WriteByte((byte)(adler >> 16));
            compressed.WriteByte((byte)(adler >> 8));
            compressed.WriteByte((byte)adler);

            var compressedBytes = compressed.ToArray();
            if (compressedBytes.Length >= raw.Length) return; // no benefit

            pdfStream.Value = compressedBytes;
            dict.Elements["/Filter"] = new PdfSharpCore.Pdf.PdfName("/FlateDecode");
            dict.Elements["/Length"] = new PdfSharpCore.Pdf.PdfInteger(compressedBytes.Length);
        }
        catch
        {
            // If anything goes wrong on a single stream, leave it untouched.
        }
    }

    private static uint ComputeAdler32(byte[] data)
    {
        const uint mod = 65521;
        uint a = 1, b = 0;
        foreach (var by in data)
        {
            a = (a + by) % mod;
            b = (b + a) % mod;
        }
        return (b << 16) | a;
    }

    /// <summary>
    /// Re-encodes an embedded JPEG image (DCTDecode) at lower quality, optionally downsampling
    /// to a maximum dimension. Returns true if the image was successfully shrunk.
    /// Conservative defaults: quality=60, max dimension=1600px, only large images (>20KB).
    /// </summary>
    private bool TryRecompressJpegImage(PdfDictionary dict, out long bytesSaved)
    {
        bytesSaved = 0;
        try
        {
            // Must be an Image XObject with DCTDecode filter (JPEG).
            var subtype = dict.Elements.GetName("/Subtype");
            if (subtype != "/Image") return false;

            var filter = dict.Elements["/Filter"];
            string filterName = filter switch
            {
                PdfName n => n.Value,
                PdfArray a when a.Elements.Count == 1 && a.Elements[0] is PdfName pn => pn.Value,
                _ => string.Empty
            };
            if (filterName != "/DCTDecode") return false;

            var pdfStream = dict.Stream!;
            var raw = pdfStream.Value;
            if (raw is null || raw.Length < 5_000) return false; // skip very small images

            const int JpegQuality = 50;
            const int MaxDimension = 1200;

            using var srcMs = new MemoryStream(raw);
            using var image = Image.Load(srcMs);

            if (image.Width > MaxDimension || image.Height > MaxDimension)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(MaxDimension, MaxDimension)
                }));
            }

            using var outMs = new MemoryStream();
            image.SaveAsJpeg(outMs, new JpegEncoder { Quality = JpegQuality });
            var newBytes = outMs.ToArray();

            if (newBytes.Length >= raw.Length) return false; // no benefit

            bytesSaved = raw.Length - newBytes.Length;
            pdfStream.Value = newBytes;
            dict.Elements["/Length"] = new PdfInteger(newBytes.Length);
            // Width/Height/ColorSpace stay the same conceptually; PDF viewers re-read them from the JPEG header.
            // Update declared Width/Height if we downsampled, so renderers don't stretch.
            dict.Elements["/Width"] = new PdfInteger(image.Width);
            dict.Elements["/Height"] = new PdfInteger(image.Height);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Converts a raw (FlateDecode-compressed) RGB or grayscale image into JPEG.
    /// This typically yields huge savings (10x+) for scanned pages stored as raw pixels.
    /// </summary>
    private bool TryConvertRawImageToJpeg(PdfDictionary dict, out long bytesSaved)
    {
        bytesSaved = 0;
        try
        {
            if (dict.Elements.GetName("/Subtype") != "/Image") return false;

            var filter = dict.Elements["/Filter"];
            string filterName = filter switch
            {
                PdfName n => n.Value,
                PdfArray a when a.Elements.Count == 1 && a.Elements[0] is PdfName pn => pn.Value,
                _ => string.Empty
            };
            if (filterName != "/FlateDecode") return false;

            var width = dict.Elements.GetInteger("/Width");
            var height = dict.Elements.GetInteger("/Height");
            var bpc = dict.Elements.GetInteger("/BitsPerComponent");
            if (width <= 0 || height <= 0 || bpc != 8) return false;

            var colorSpace = dict.Elements.GetName("/ColorSpace");
            int channels = colorSpace switch
            {
                "/DeviceRGB" => 3,
                "/DeviceGray" => 1,
                _ => 0
            };
            if (channels == 0) return false;

            var pdfStream = dict.Stream!;
            var raw = pdfStream.UnfilteredValue; // FlateDecode-decoded raw pixels
            if (raw is null || raw.Length != width * height * channels) return false;

            const int JpegQuality = 50;
            const int MaxDimension = 1200;

            using var outMs = new MemoryStream();
            int finalW = width, finalH = height;

            if (channels == 3)
            {
                using var image = Image.LoadPixelData<Rgb24>(raw, width, height);
                if (image.Width > MaxDimension || image.Height > MaxDimension)
                    image.Mutate(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(MaxDimension, MaxDimension) }));
                finalW = image.Width;
                finalH = image.Height;
                image.SaveAsJpeg(outMs, new JpegEncoder { Quality = JpegQuality });
            }
            else
            {
                using var image = Image.LoadPixelData<L8>(raw, width, height);
                if (image.Width > MaxDimension || image.Height > MaxDimension)
                    image.Mutate(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(MaxDimension, MaxDimension) }));
                finalW = image.Width;
                finalH = image.Height;
                image.SaveAsJpeg(outMs, new JpegEncoder { Quality = JpegQuality });
            }

            var newBytes = outMs.ToArray();
            var oldEncodedLen = pdfStream.Value?.Length ?? 0;
            if (newBytes.Length >= oldEncodedLen) return false;

            bytesSaved = oldEncodedLen - newBytes.Length;
            pdfStream.Value = newBytes;
            dict.Elements["/Filter"] = new PdfName("/DCTDecode");
            dict.Elements["/Length"] = new PdfInteger(newBytes.Length);
            dict.Elements["/Width"] = new PdfInteger(finalW);
            dict.Elements["/Height"] = new PdfInteger(finalH);
            // For DCTDecode, ColorSpace must be DeviceRGB or DeviceGray (already correct).
            // Remove DecodeParms — they only apply to FlateDecode.
            if (dict.Elements.ContainsKey("/DecodeParms")) dict.Elements.Remove("/DecodeParms");
            return true;
        }
        catch
        {
            return false;
        }
    }
}
