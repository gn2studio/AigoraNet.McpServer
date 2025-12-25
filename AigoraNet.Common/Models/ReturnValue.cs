namespace AigoraNet.Common.Models;

public class ReturnValue
{
    public bool Success { get; set; } = false;
    public int ErrorCode { get; set; } = 0;
    public int Count { get; set; } = -1;
    public string Value { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public ReturnValue()
    {
    }

    public virtual void SetSuccess()
    {
        this.Success = true;
    }

    public virtual void SetSuccess(int count)
    {
        this.Count = count;
        this.Success = true;
    }

    public virtual void SetSuccess(string value)
    {
        this.Value = value;
        this.Success = true;
    }

    public virtual void SetSuccess(int count, string value)
    {
        this.Count = count;
        this.Value = value;
        this.Success = true;
    }

    public virtual void SetError(string msg)
    {
        this.Message = msg;
        this.Success = false;
    }

    public virtual void SetError(string msg, int code)
    {
        this.Message = msg;
        this.Success = false;
        this.ErrorCode = code;
    }

    public virtual void SetError(Exception ex)
    {
        if (ex.InnerException != null)
        {
            this.Message = $"{ex.InnerException.Message}({ex.Message}){Environment.NewLine}{ex.InnerException.StackTrace}";
        }
        else
        {
            this.Message = $"{ex.Message}{Environment.NewLine}{ex.StackTrace}";
        }
        this.Success = false;
    }
}