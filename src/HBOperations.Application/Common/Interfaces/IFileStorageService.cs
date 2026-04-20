namespace HBOperations.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<FileUploadResult> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string storagePath, CancellationToken ct = default);
    Task<bool> DeleteAsync(string storagePath, CancellationToken ct = default);
    Task MoveToArchiveAsync(string storagePath, CancellationToken ct = default);
}

public record FileUploadResult(
    string StoragePath,
    string FileName,
    long FileSizeBytes,
    string Checksum);
