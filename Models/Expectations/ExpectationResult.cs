using System.Text.Json.Serialization;

namespace PocGestorExpectativas.Models.Expectations;

public class ExpectationResult
{
    [JsonPropertyName("nextExpectedPaymentDate")]
    public DateTime? NextExpectedPaymentDate { get; set; }

    [JsonPropertyName("nextExpectedAmount")]
    public decimal? NextExpectedAmount { get; set; }

    [JsonPropertyName("confidenceScore")]
    public double ConfidenceScore { get; set; }

    [JsonPropertyName("rationale")]
    public string Rationale { get; set; } = string.Empty;
}