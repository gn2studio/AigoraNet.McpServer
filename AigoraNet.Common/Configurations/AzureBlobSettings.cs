namespace AigoraNet.Common.Configurations;

public class AzureBlobSettings
{
    /// <summary>
    /// Azure Storage 계정의 연결 문자열입니다. 
    /// (예: DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...)
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// 데이터를 저장할 Blob 컨테이너의 이름입니다. (S3의 BucketName에 해당)
    /// </summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// Blob에 직접 접근할 때 사용되는 Public 기본 URL입니다.
    /// (예: https://[StorageAccountName].blob.core.windows.net/[ContainerName])
    /// </summary>
    public string PublicBaseUrl { get; set; } = string.Empty;
}