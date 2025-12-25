namespace AigoraNet.Common.Filters;

public class StringValueAttribute : Attribute
{
    public string enumStringValue { get; set; }

    public StringValueAttribute(string Value) : base()
    {
        this.enumStringValue = Value;
    }

    public string Value { get { return enumStringValue; } }
}
