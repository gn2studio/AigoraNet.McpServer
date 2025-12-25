using System.Text;

namespace AigoraNet.Common.Configurations;

public record RedisConfigurations : CurrentSiteConfiguration
{
    public string Password { get; set; } = string.Empty;

    public int TimeoutMilliseconds { get; set; } = 5000;

    public string ConnectionString
    {
        get
        {
            StringBuilder sb = new StringBuilder(200);
            sb.Append(this.BaseUrl);
            sb.Append($",password={this.Password},syncTimeout={TimeoutMilliseconds},abortConnect=false");
            return sb.ToString();
        }
    }
}