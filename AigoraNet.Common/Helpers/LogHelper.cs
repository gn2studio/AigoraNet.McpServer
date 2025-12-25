using Serilog;
using System.Text;
using Newtonsoft.Json;

namespace AigoraNet.Common.Helpers;

public class LogHelper
{
    private static Serilog.ILogger logger = default!;

    private static readonly Lazy<LogHelper> lazy = new Lazy<LogHelper>(() => new LogHelper());
    public static LogHelper Current { get { return lazy.Value; } }

    public LogHelper()
    {
        logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                path: $"Logs/log.txt",
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                encoding: Encoding.UTF8
            )
            .CreateLogger();
    }

    public void Debug(string msg)
    {
        logger.Debug(msg);
    }

    public void Debug<T>(T target)
    {
        logger.Debug(JsonConvert.SerializeObject(target));
    }

    public void Error(string msg)
    {
        logger.Error(msg);
    }

    public void Error(Exception ex)
    {
        logger.Error(ex, ex.Message);
    }

    public void Warn(string msg)
    {
        logger.Warning(msg);
    }

    public void Fatal(string msg)
    {
        logger.Fatal(msg);
    }

    public void Info(string msg)
    {
        logger.Information(msg);
    }

    public void Info<T>(T target)
    {
        logger.Information(JsonConvert.SerializeObject(target));
    }
}