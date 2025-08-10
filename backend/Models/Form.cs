namespace SupportBot.Models;

public enum FormCategory
{
    General,
    Technical,
    Billing,
    Feedback,
    Account,
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
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public FormCategory category { get; set; } = FormCategory.General;
    public FormUrgency urgency { get; set; } = FormUrgency.Low;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}