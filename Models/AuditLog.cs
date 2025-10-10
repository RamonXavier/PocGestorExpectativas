using System.ComponentModel.DataAnnotations;

namespace PocGestorExpectativas.Models;

public class AuditLog
{
    public Guid Id { get; set; }
    
    public Guid? PaymentId { get; set; }
    
    public Guid? ExpectationId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty; // "payment_received", "expectation_generated", "error"
    
    [MaxLength(2000)]
    public string? Details { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
