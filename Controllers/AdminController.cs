using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PocGestorExpectativas.Data;

namespace PocGestorExpectativas.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminController> _logger;

    public AdminController(AppDbContext context, ILogger<AdminController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Obtém estatísticas gerais do sistema
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<object>> GetStats()
    {
        var totalPayments = await _context.Payments.CountAsync();
        var paidPayments = await _context.Payments.CountAsync(p => p.Pago);
        var totalExpectations = await _context.Expectations.CountAsync();
        var totalAuditLogs = await _context.AuditLogs.CountAsync();

        var avgPaymentValue = await _context.Payments
            .Where(p => p.Pago)
            .AverageAsync(p => p.Value);

        var avgConfidence = await _context.Expectations
            .AverageAsync(e => e.ConfidenceScore);

        var recentPayments = await _context.Payments
            .Where(p => p.CreatedAt >= DateTime.UtcNow.AddDays(-7))
            .CountAsync();

        return Ok(new
        {
            Payments = new
            {
                Total = totalPayments,
                Paid = paidPayments,
                Unpaid = totalPayments - paidPayments,
                AverageValue = Math.Round(avgPaymentValue, 2),
                RecentPayments = recentPayments
            },
            Expectations = new
            {
                Total = totalExpectations,
                AverageConfidence = Math.Round(avgConfidence, 2)
            },
            Audit = new
            {
                TotalLogs = totalAuditLogs
            },
            System = new
            {
                DatabaseConnected = true,
                Timestamp = DateTime.UtcNow
            }
        });
    }

    /// <summary>
    /// Obtém status da fila RabbitMQ (simulado por enquanto)
    /// </summary>
    [HttpGet("queue-status")]
    public ActionResult<object> GetQueueStatus()
    {
        // Por enquanto retorna status simulado
        // Na Fase 2 implementaremos a conexão real com RabbitMQ
        return Ok(new
        {
            Status = "Connected",
            QueueName = "faturas",
            MessagesInQueue = 0, // Será implementado na Fase 2
            ConsumerStatus = "Active",
            LastProcessed = DateTime.UtcNow.AddMinutes(-5),
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Obtém logs de auditoria recentes
    /// </summary>
    [HttpGet("audit-logs")]
    public async Task<ActionResult<IEnumerable<object>>> GetAuditLogs([FromQuery] int limit = 50)
    {
        var logs = await _context.AuditLogs
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .Select(a => new
            {
                a.Id,
                a.PaymentId,
                a.ExpectationId,
                a.Action,
                a.Details,
                a.Timestamp
            })
            .ToListAsync();

        return Ok(logs);
    }

    /// <summary>
    /// Obtém estatísticas por beneficiário
    /// </summary>
    [HttpGet("beneficiary-stats")]
    public async Task<ActionResult<IEnumerable<object>>> GetBeneficiaryStats()
    {
        var stats = await _context.Payments
            .Where(p => p.Pago && !string.IsNullOrEmpty(p.NormalizedBeneficiary))
            .GroupBy(p => p.NormalizedBeneficiary)
            .Select(g => new
            {
                Beneficiary = g.Key,
                PaymentCount = g.Count(),
                TotalValue = g.Sum(p => p.Value),
                AverageValue = g.Average(p => p.Value),
                LastPayment = g.Max(p => p.PaidAt),
                HasExpectation = _context.Expectations.Any(e => e.NormalizedBeneficiary == g.Key)
            })
            .OrderByDescending(s => s.TotalValue)
            .ToListAsync();

        return Ok(stats);
    }
}
