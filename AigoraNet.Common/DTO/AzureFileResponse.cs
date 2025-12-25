namespace AigoraNet.Common.DTO;

public class AzureFileResponse
{
    /// <summary>
    /// 파일 내용이 담긴 스트림입니다. (파일을 닫을 책임은 이 스트림을 소비하는 쪽에 있습니다.)
    /// </summary>
    public Stream? Content { get; set; }

    /// <summary>
    /// 파일의 MIME 타입 (예: image/jpeg, application/pdf)입니다.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// 파일의 길이(바이트)입니다.
    /// </summary>
    public long ContentLength { get; set; } = 0;

    /// <summary>
    /// Azure Blob Storage 내에 저장된 파일의 키/경로입니다.
    /// </summary>
    public string Key { get; set; } = string.Empty;
}