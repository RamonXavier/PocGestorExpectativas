using PocGestorExpectativas.Models;
using PocGestorExpectativas.Models.Expectations;

namespace PocGestorExpectativas.Services.Interfaces;

public interface ILlmClient
{
    Task<string> NormalizeBeneficiaryAsync(string rawBeneficiaryName, CancellationToken cancellationToken = default);
    Task<ExpectationResult> AnalyzePaymentHistoryAsync(string normalizedBeneficiary, List<Payment> history, CancellationToken cancellationToken = default);
}