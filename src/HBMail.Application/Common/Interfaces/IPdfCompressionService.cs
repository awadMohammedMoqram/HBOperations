namespace HBMail.Application.Common.Interfaces;

/// <summary>
/// Compresses PDF streams to reduce storage footprint.
/// Uses PdfSharpCore (MIT license) — safe for commercial/banking use.
/// </summary>
public interface IPdfCompressionService
{
    /// <summary>
    /// Returns a compressed copy of the input PDF.
    /// If compression fails or produces a larger file, the original bytes are returned unchanged.
    /// The returned stream is positioned at 0 and owned by the caller.
    /// </summary>
    Task<PdfCompressionResult> CompressAsync(Stream input, CancellationToken ct = default);
}

public record PdfCompressionResult(
    Stream OutputStream,
    long OriginalSizeBytes,
    long CompressedSizeBytes,
    bool WasCompressed);
