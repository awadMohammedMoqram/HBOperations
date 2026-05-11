using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using HBMail.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HBMail.Infrastructure.Services;

public class AzureBlobStorageService : IFileStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<AzureBlobStorageService> _logger;
    private const string ArchivePrefix = "archive/";

    public AzureBlobStorageService(IConfiguration configuration, ILogger<AzureBlobStorageService> logger)
    {
        _logger = logger;
        var connectionString = configuration["AzureBlobStorage:ConnectionString"]
            ?? throw new InvalidOperationException("AzureBlobStorage:ConnectionString is not configured");
        var containerName = configuration["AzureBlobStorage:ContainerName"] ?? "documents";

        var serviceClient = new BlobServiceClient(connectionString);
        _containerClient = serviceClient.GetBlobContainerClient(containerName);
        _containerClient.CreateIfNotExists(PublicAccessType.None);
    }

    public async Task<FileUploadResult> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        var date = DateTime.UtcNow;
        var blobName = $"{date:yyyy}/{date:MM}/{Guid.NewGuid()}{Path.GetExtension(fileName)}";

        var blobClient = _containerClient.GetBlobClient(blobName);

        // Compute checksum before upload
        string checksum;
        long fileSize;
        using (var ms = new MemoryStream())
        {
            await stream.CopyToAsync(ms, ct);
            fileSize = ms.Length;
            ms.Position = 0;

            using var sha = System.Security.Cryptography.SHA256.Create();
            var hash = await sha.ComputeHashAsync(ms, ct);
            checksum = Convert.ToHexStringLower(hash);

            ms.Position = 0;
            await blobClient.UploadAsync(ms, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);
        }

        _logger.LogInformation("Uploaded blob {BlobName} ({Size} bytes) to Azure", blobName, fileSize);

        return new FileUploadResult(blobName, Path.GetFileName(blobName), fileSize, checksum);
    }

    public async Task<Stream> DownloadAsync(string storagePath, CancellationToken ct = default)
    {
        var blobClient = _containerClient.GetBlobClient(storagePath);

        if (!await blobClient.ExistsAsync(ct))
            throw new FileNotFoundException("Blob not found", storagePath);

        var ms = new MemoryStream();
        await blobClient.DownloadToAsync(ms, ct);
        ms.Position = 0;
        return ms;
    }

    public async Task<bool> DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        var blobClient = _containerClient.GetBlobClient(storagePath);
        var response = await blobClient.DeleteIfExistsAsync(cancellationToken: ct);

        if (response.Value)
            _logger.LogInformation("Deleted blob {BlobName} from Azure", storagePath);

        return response.Value;
    }

    public async Task MoveToArchiveAsync(string storagePath, CancellationToken ct = default)
    {
        var sourceBlob = _containerClient.GetBlobClient(storagePath);
        if (!await sourceBlob.ExistsAsync(ct))
            throw new FileNotFoundException("Blob not found", storagePath);

        var archiveBlobName = $"{ArchivePrefix}{storagePath}";
        var destBlob = _containerClient.GetBlobClient(archiveBlobName);

        // Copy to archive location
        await destBlob.StartCopyFromUriAsync(sourceBlob.Uri, cancellationToken: ct);

        // Wait for copy to complete
        var props = await destBlob.GetPropertiesAsync(cancellationToken: ct);
        while (props.Value.BlobCopyStatus == CopyStatus.Pending)
        {
            await Task.Delay(200, ct);
            props = await destBlob.GetPropertiesAsync(cancellationToken: ct);
        }

        // Delete original
        await sourceBlob.DeleteIfExistsAsync(cancellationToken: ct);

        _logger.LogInformation("Moved blob {Source} to archive {Dest}", storagePath, archiveBlobName);
    }
}
