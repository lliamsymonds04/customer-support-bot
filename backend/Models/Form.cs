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

public class Form
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public FormCategory category { get; set; } = FormCategory.General;
    public FormUrgency urgency { get; set; } = FormUrgency.Low;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool Completed { get; set; } = false; 
}