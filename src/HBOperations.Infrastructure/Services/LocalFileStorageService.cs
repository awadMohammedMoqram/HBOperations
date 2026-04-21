using HBOperations.Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace HBOperations.Infrastructure.Services;

public class LocalFileStorageService(IWebHostEnvironment environment) : IFileStorageService
{
    private const string UploadFolder = "uploads";
    private const string ArchiveFolder = "archive";

    public async Task<FileUploadResult> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        var date = DateTime.UtcNow;
        var relativePath = Path.Combine(UploadFolder, date.Year.ToString(), date.Month.ToString("D2"));
        var fullPath = Path.Combine(environment.WebRootPath, relativePath);
        Directory.CreateDirectory(fullPath);

        var storedFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
        var filePath = Path.Combine(fullPath, storedFileName);

        using (var output = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write))
        {
            await stream.CopyToAsync(output, ct);
        }

        // Compute SHA-256 checksum from the saved file (stream may not be seekable)
        string checksum;
        using (var fileRead = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var hash = await sha.ComputeHashAsync(fileRead, ct);
            checksum = Convert.ToHexStringLower(hash);
        }

        var fileInfo = new FileInfo(filePath);
        var storagePath = Path.Combine(relativePath, storedFileName).Replace('\\', '/');

        return new FileUploadResult(storagePath, storedFileName, fileInfo.Length, checksum);
    }

    public Task<Stream> DownloadAsync(string storagePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(environment.WebRootPath, storagePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("File not found", storagePath);

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return Task.FromResult(stream);
    }

    public Task<bool> DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(environment.WebRootPath, storagePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
            return Task.FromResult(false);

        File.Delete(fullPath);
        return Task.FromResult(true);
    }

    public Task MoveToArchiveAsync(string storagePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(environment.WebRootPath, storagePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("File not found", storagePath);

        var archivePath = Path.Combine(environment.WebRootPath, ArchiveFolder, storagePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(archivePath)!);
        File.Move(fullPath, archivePath);

        return Task.CompletedTask;
    }
}
