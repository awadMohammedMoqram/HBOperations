using HBOperations.Application.Common.Interfaces;

namespace HBOperations.Infrastructure.Services;

public class FileValidationService : IFileValidationService
{
    // PDF Magic Bytes: %PDF (0x25 0x50 0x44 0x46)
    private static readonly byte[] PdfMagicBytes = [0x25, 0x50, 0x44, 0x46];

    private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50 MB
    private const int MaxFilesPerTransaction = 10;

    private static readonly HashSet<string> AllowedExtensions = [".pdf"];
    private static readonly HashSet<string> AllowedContentTypes = ["application/pdf"];

    public FileValidationResult Validate(Stream stream, string fileName, string contentType, long fileSize)
    {
        // 1. Check file size
        if (fileSize <= 0)
            return new FileValidationResult(false, "الملف فارغ");

        if (fileSize > MaxFileSizeBytes)
            return new FileValidationResult(false, $"حجم الملف يتجاوز الحد المسموح ({MaxFileSizeBytes / (1024 * 1024)} ميجابايت)");

        // 2. Check extension
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            return new FileValidationResult(false, "نوع الملف غير مسموح. يُقبل فقط ملفات PDF");

        // 3. Check Content-Type
        if (!AllowedContentTypes.Contains(contentType.ToLowerInvariant()))
            return new FileValidationResult(false, "نوع المحتوى غير صالح. يُقبل فقط application/pdf");

        // 4. Check Magic Bytes
        if (!ValidateMagicBytes(stream))
            return new FileValidationResult(false, "محتوى الملف لا يتطابق مع ملف PDF صالح");

        return new FileValidationResult(true);
    }

    private static bool ValidateMagicBytes(Stream stream)
    {
        if (!stream.CanRead || stream.Length < PdfMagicBytes.Length)
            return false;

        var originalPosition = stream.Position;
        stream.Position = 0;

        Span<byte> header = stackalloc byte[PdfMagicBytes.Length];
        var bytesRead = stream.Read(header);

        stream.Position = originalPosition;

        if (bytesRead < PdfMagicBytes.Length)
            return false;

        return header.SequenceEqual(PdfMagicBytes);
    }
}
