namespace AigoraNet.Common.Abstracts;

public interface IPagingBase
{
    int CurPage { get; set; }

    int PageSize { get; set; }
}