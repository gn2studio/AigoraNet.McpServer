using AigoraNet.Common.Configurations;

namespace AigoraNet.Common.Abstracts;

public record BlobUploadResult(string BlobName, string Url, long Length, string ContentType);

public interface IAzureBlobFileService
{
    Task<BlobUploadResult> UploadAsync(string fileName, byte[] content, string contentType, CancellationToken ct);
    Task DeleteAsync(string blobName, CancellationToken ct);
}
