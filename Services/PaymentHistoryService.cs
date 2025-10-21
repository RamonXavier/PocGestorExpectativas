using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PocGestorExpectativas.Data;
using PocGestorExpectativas.Models;

namespace PocGestorExpectativas.Services;

public class PaymentHistoryService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PaymentHistoryService> _logger;

    public PaymentHistoryService(AppDbContext context, ILogger<PaymentHistoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Payment>> GetBeneficiaryHistoryAsync(string normalizedBeneficiary, int maxRecords = 20)
    {
        var history = await _context.Payments
            .Where(p => p.NormalizedBeneficiary == normalizedBeneficiary && p.Pago)
            .OrderByDescending(p => p.PaidAt)
            .Take(maxRecords)
            .ToListAsync();

        _logger.LogInformation("Found {Count} historical payments for {Beneficiary}", history.Count, normalizedBeneficiary);

        return history;
    }
}