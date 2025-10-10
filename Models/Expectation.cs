using System.ComponentModel.DataAnnotations;

namespace PocGestorExpectativas.Models;

public class Expectation
{
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string NormalizedBeneficiary { get; set; } = string.Empty; // Beneficiário normalizado via RAG
    
    public DateTime? NextExpectedPaymentDate { get; set; }
    
    public decimal? NextExpectedAmount { get; set; }
    
    [Range(0.0, 1.0)]
    public double ConfidenceScore { get; set; } // 0..1
    
    [MaxLength(1000)]
    public string? Rationale { get; set; } // Explicação da IA
    
    [MaxLength(50)]
    public string AnalysisMethod { get; set; } = "llm"; // "llm", "ml", "rule-based"
    
    public int HistoryCount { get; set; } // Quantos pagamentos foram analisados
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
