using System.ComponentModel.DataAnnotations;

namespace PocGestorExpectativas.Models;

public class Payment
{
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string IdentificationField { get; set; } = string.Empty; // linha digitável
    
    [Required]
    public decimal Value { get; set; }
    
    public DateTime? DueDate { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string BeneficiaryName { get; set; } = string.Empty; // Nome original da fatura
    
    [MaxLength(200)]
    public string? NormalizedBeneficiary { get; set; } // Nome normalizado via RAG
    
    public bool Pago { get; set; } = false; // Status principal
    
    public DateTime? PaidAt { get; set; } // Quando foi pago
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
