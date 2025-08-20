using SupportBot.Models;

namespace SupportBot.Dtos;

public class FormDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    
    public string Category { get; set; } = FormCategory.General.ToString();
    
    public string Urgency { get; set; } = FormUrgency.Low.ToString();
    
    public string State { get; set; } = FormState.Open.ToString();
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Username { get; set; } = string.Empty;

}