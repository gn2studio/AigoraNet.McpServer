using AigoraNet.Common.Abstracts;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using GN2.Core.Configurations;
using Microsoft.Extensions.Options;

namespace AigoraNet.Common.Services;

public class AzureBlobFileService : IAzureBlobFileService
{
    private readonly BlobContainerClient _container;

    public AzureBlobFileService(IOptions<AzureBlobSettings> options)
    {
        var settings = options.Value;
        var service = new BlobServiceClient(settings.ConnectionString);
        _container = service.GetBlobContainerClient(settings.ContainerName);
        _container.CreateIfNotExists(PublicAccessType.None);
    }

    public async Task<BlobUploadResult> UploadAsync(string fileName, byte[] content, string contentType, CancellationToken ct)
    {
        var blobName = $"{Guid.NewGuid():N}_{fileName}";
        var blobClient = _container.GetBlobClient(blobName);
        using var stream = new MemoryStream(content, writable: false);
        var headers = new BlobHttpHeaders { ContentType = contentType };
        await blobClient.UploadAsync(stream, headers, cancellationToken: ct);
        return new BlobUploadResult(blobName, blobClient.Uri.AbsoluteUri, content.LongLength, contentType);
    }

    public async Task DeleteAsync(string blobName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(blobName)) return;
        var blobClient = _container.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: ct);
    }
}
