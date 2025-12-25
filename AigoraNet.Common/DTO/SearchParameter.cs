using AigoraNet.Common.Models;
using System.Text;
using System.Web;

namespace AigoraNet.Common.DTO;


public class SearchParameter : PagingBase
{
    public string? Keyword { get; set; }

    public SearchParameter()
    {
        this.CurPage = 1;
        this.PageSize = 10;
    }

    public SearchParameter(int curPage, int pageSize = 10)
    {
        this.CurPage = curPage;
        this.PageSize = pageSize;
    }

    public virtual string toSerialize()
    {
        StringBuilder builder = new StringBuilder(200);
        builder.Append($"CurPage={this.CurPage}");
        if (this.PageSize != 10)
        {
            builder.Append($"&PageSize={this.PageSize}");
        }
        if (!string.IsNullOrEmpty(this.Keyword))
        {
            builder.Append($"&Keyword={HttpUtility.UrlEncode(this.Keyword)}");
        }
        return builder.ToString();
    }

    public string toFormat()
    {
        StringBuilder builder = new StringBuilder(200);
        builder.Append("CurPage={0}");
        if (this.PageSize != 10)
        {
            builder.Append($"&PageSize={this.PageSize}");
        }
        if (!string.IsNullOrEmpty(this.Keyword))
        {
            builder.Append($"&Keyword={HttpUtility.UrlEncode(this.Keyword)}");
        }
        return builder.ToString();
    }
}
