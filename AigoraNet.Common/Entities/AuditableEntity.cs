namespace AigoraNet.Common;

public enum ConditionStatus : byte
{
    None = 0,
    Active = 1,
    Disabled = 2,
    Hidden = 3,
    Out = 4
}

public class AuditableEntity
{
    public string CreatedBy { get; set; } = string.Empty;

    public string? UpdatedBy { get; set; }

    public string? DeletedBy { get; set; }

    public DateTime RegistDate { get; set; }

    public DateTime? LastUpdate { get; set; }

    public DateTime? DeletedDate { get; set; }

    public ConditionStatus Status { get; set; }

    public bool IsEnabled { get; set; }

    public DateTime RecentDate
    {
        get
        {
            return LastUpdate ?? RegistDate;
        }
    }

    public AuditableEntity()
    {
        this.CreatedBy = string.Empty;
        this.IsEnabled = true;
        this.Status = ConditionStatus.Active;
    }
}