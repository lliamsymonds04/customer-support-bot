namespace SupportBot.Models;

public enum FormCategory
{
    General,
    Technical,
    Billing,
    Feedback,
    Account,
    Request,
}

public enum FormUrgency
{
    Low,
    Medium,
    High,
    Critical
}

public enum FormState
{
    Open,
    InProgress,
    Closed,
}

public class Form
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public FormCategory Category { get; set; } = FormCategory.General;
    public FormUrgency Urgency { get; set; } = FormUrgency.Low;
    public FormState State { get; set; } = FormState.Open;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;


    // Foreign key to User
    public int? UserId { get; set; }
    public User? User { get; set; }
}