namespace AigoraNet.Common.Models;

public class ReturnValues<T> : ReturnValue
{
    public T? Data { get; set; } = default!;
    public ReturnValues() : base()
    {
        this.Data = default(T);
    }

    public ReturnValues(T data) : base()
    {
    }

    public virtual void SetSuccess(int count, T obj)
    {
        this.Count = count;
        this.Data = obj;
        this.Success = true;
    }

    public virtual void SetSuccess(int count, T obj, string value)
    {
        this.Count = count;
        this.Data = obj;
        this.Value = value;
        this.Success = true;
    }

    public virtual void SetSuccess(int count, T obj, string value, string msg)
    {
        this.Count = count;
        this.Data = obj;
        this.Value = value;
        this.Message = msg;
        this.Success = true;
    }
}