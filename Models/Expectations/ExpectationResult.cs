namespace PocGestorExpectativas.Models.Expectations;

public class ExpectationResult
{
    public DateTime? NextExpectedPaymentDate { get; set; }
    public decimal? NextExpectedAmount { get; set; }
    public double ConfidenceScore { get; set; }
    public string Rationale { get; set; } = string.Empty;
}