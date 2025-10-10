namespace PocGestorExpectativas.Models;

public class PaymentMessage
{
    public string IdentificationField { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public DateTime? DueDate { get; set; }
    public string BeneficiaryName { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}


