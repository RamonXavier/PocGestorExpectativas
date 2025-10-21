using PocGestorExpectativas.Data;
using PocGestorExpectativas.Models;
using PocGestorExpectativas.Services.Interfaces;

namespace PocGestorExpectativas.Services;

public class ExpectationAnalyzer
{
    private readonly ILlmClient _llmClient;
    private readonly PaymentHistoryService _historyService;
    private readonly AppDbContext _context;
    private readonly ILogger<ExpectationAnalyzer> _logger;

    public ExpectationAnalyzer(
        ILlmClient llmClient,
        PaymentHistoryService historyService,
        AppDbContext context,
        ILogger<ExpectationAnalyzer> logger)
    {
        _llmClient = llmClient;
        _historyService = historyService;
        _context = context;
        _logger = logger;
    }

    public async Task AnalyzeAndCreateExpectationAsync(Payment payment)
    {
        try
        {
            _logger.LogInformation("Iniciando análise de expectativa para pagamento {PaymentId} - {Beneficiary}", 
                payment.Id, payment.BeneficiaryName);

            // 1. Normalizar beneficiário via RAG
            var normalizedBeneficiary = await _llmClient.NormalizeBeneficiaryAsync(payment.BeneficiaryName);
            
            // Atualizar o pagamento com o nome normalizado
            payment.NormalizedBeneficiary = normalizedBeneficiary;
            _context.Payments.Update(payment);

            _logger.LogInformation("Beneficiário normalizado: {Original} -> {Normalized}", 
                payment.BeneficiaryName, normalizedBeneficiary);

            // 2. Buscar histórico agrupado
            var history = await _historyService.GetBeneficiaryHistoryAsync(normalizedBeneficiary);

            if (history.Count < 1)
            {
                _logger.LogWarning("Histórico insuficiente para análise: {Count} registros", history.Count);
                await CreateBasicExpectation(normalizedBeneficiary, payment, history.Count);
                return;
            }

            // 3. Analisar com IA
            var result = await _llmClient.AnalyzePaymentHistoryAsync(normalizedBeneficiary, history);

            // 4. Criar expectativa
            var expectation = new Expectation
            {
                Id = Guid.NewGuid(),
                NormalizedBeneficiary = normalizedBeneficiary,
                NextExpectedPaymentDate = result.NextExpectedPaymentDate,
                NextExpectedAmount = result.NextExpectedAmount,
                ConfidenceScore = result.ConfidenceScore,
                Rationale = result.Rationale,
                AnalysisMethod = "llm",
                HistoryCount = history.Count,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Expectations.Add(expectation);
            await _context.SaveChangesAsync();

            // 5. Log de auditoria
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                PaymentId = payment.Id,
                ExpectationId = expectation.Id,
                Action = "expectation_generated",
                Details = $"IA analisou {history.Count} pagamentos históricos. Confiança: {result.ConfidenceScore:P}",
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Expectativa criada com sucesso: {ExpectationId} - Confiança: {Confidence:P}", 
                expectation.Id, result.ConfidenceScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao analisar expectativa para pagamento {PaymentId}", payment.Id);
            await CreateErrorExpectation(payment.NormalizedBeneficiary ?? payment.BeneficiaryName, ex.Message);
        }
    }

    private async Task CreateBasicExpectation(string normalizedBeneficiary, Payment payment, int historyCount)
    {
        var expectation = new Expectation
        {
            Id = Guid.NewGuid(),
            NormalizedBeneficiary = normalizedBeneficiary,
            NextExpectedPaymentDate = DateTime.UtcNow.AddDays(30), // Fallback: 30 dias
            NextExpectedAmount = payment.Value,
            ConfidenceScore = 0.3, // Baixa confiança
            Rationale = $"Histórico insuficiente ({historyCount} registros). Estimativa baseada no último pagamento.",
            AnalysisMethod = "rule-based",
            HistoryCount = historyCount
        };

        _context.Expectations.Add(expectation);
        await _context.SaveChangesAsync();
    }

    private async Task CreateErrorExpectation(string beneficiary, string errorMessage)
    {
        var expectation = new Expectation
        {
            Id = Guid.NewGuid(),
            NormalizedBeneficiary = beneficiary,
            ConfidenceScore = 0.0,
            Rationale = $"Erro na análise: {errorMessage}",
            AnalysisMethod = "error",
            HistoryCount = 0
        };

        _context.Expectations.Add(expectation);
        await _context.SaveChangesAsync();
    }
}