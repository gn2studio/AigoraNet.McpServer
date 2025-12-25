using AigoraNet.Common.Abstracts;
using System.ComponentModel.DataAnnotations;

namespace AigoraNet.Common.Models;

public class PagingBase : IPagingBase
{
    private int _curPage = 1;
    private int _pageSize = 10;

    [Range(1, 10000)]
    public int CurPage
    {
        get => _curPage;
        set => _curPage = value < 1 ? 1 : value;
    }

    [Range(2, 100)]
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 1 : value;
    }
}