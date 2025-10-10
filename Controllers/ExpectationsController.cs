using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PocGestorExpectativas.Data;
using PocGestorExpectativas.Models;

namespace PocGestorExpectativas.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpectationsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ExpectationsController> _logger;

    public ExpectationsController(AppDbContext context, ILogger<ExpectationsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Lista todas as expectativas
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Expectation>>> GetExpectations([FromQuery] string? beneficiary = null)
    {
        var query = _context.Expectations.AsQueryable();

        if (!string.IsNullOrEmpty(beneficiary))
        {
            query = query.Where(e => e.NormalizedBeneficiary.Contains(beneficiary));
        }

        var expectations = await query
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        return Ok(expectations);
    }

    /// <summary>
    /// Obtém uma expectativa específica por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Expectation>> GetExpectation(Guid id)
    {
        var expectation = await _context.Expectations.FindAsync(id);

        if (expectation == null)
        {
            return NotFound();
        }

        return Ok(expectation);
    }

    /// <summary>
    /// Cria uma nova expectativa
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Expectation>> CreateExpectation(Expectation expectation)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        expectation.Id = Guid.NewGuid();
        expectation.CreatedAt = DateTime.UtcNow;
        expectation.UpdatedAt = DateTime.UtcNow;

        _context.Expectations.Add(expectation);
        await _context.SaveChangesAsync();

        // Log de auditoria
        await _context.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            ExpectationId = expectation.Id,
            Action = "expectation_created",
            Details = $"Expectativa criada para {expectation.NormalizedBeneficiary} - Data esperada: {expectation.NextExpectedPaymentDate:yyyy-MM-dd} - Valor: R$ {expectation.NextExpectedAmount:F2}",
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        _logger.LogInformation("Expectativa criada: {ExpectationId} para {Beneficiary}", expectation.Id, expectation.NormalizedBeneficiary);

        return CreatedAtAction(nameof(GetExpectation), new { id = expectation.Id }, expectation);
    }

    /// <summary>
    /// Atualiza uma expectativa
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateExpectation(Guid id, Expectation expectation)
    {
        if (id != expectation.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var existingExpectation = await _context.Expectations.FindAsync(id);
        if (existingExpectation == null)
        {
            return NotFound();
        }

        // Atualizar campos
        existingExpectation.NormalizedBeneficiary = expectation.NormalizedBeneficiary;
        existingExpectation.NextExpectedPaymentDate = expectation.NextExpectedPaymentDate;
        existingExpectation.NextExpectedAmount = expectation.NextExpectedAmount;
        existingExpectation.ConfidenceScore = expectation.ConfidenceScore;
        existingExpectation.Rationale = expectation.Rationale;
        existingExpectation.AnalysisMethod = expectation.AnalysisMethod;
        existingExpectation.HistoryCount = expectation.HistoryCount;
        existingExpectation.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Log de auditoria
        await _context.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            ExpectationId = expectation.Id,
            Action = "expectation_updated",
            Details = $"Expectativa atualizada para {expectation.NormalizedBeneficiary} - Data esperada: {expectation.NextExpectedPaymentDate:yyyy-MM-dd}",
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        _logger.LogInformation("Expectativa atualizada: {ExpectationId}", expectation.Id);

        return NoContent();
    }

    /// <summary>
    /// Deleta uma expectativa
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteExpectation(Guid id)
    {
        var expectation = await _context.Expectations.FindAsync(id);
        if (expectation == null)
        {
            return NotFound();
        }

        _context.Expectations.Remove(expectation);
        await _context.SaveChangesAsync();

        // Log de auditoria
        await _context.AuditLogs.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            ExpectationId = expectation.Id,
            Action = "expectation_deleted",
            Details = $"Expectativa deletada para {expectation.NormalizedBeneficiary}",
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        _logger.LogInformation("Expectativa deletada: {ExpectationId}", expectation.Id);

        return NoContent();
    }

    /// <summary>
    /// Obtém estatísticas das expectativas
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<object>> GetStats()
    {
        var totalExpectations = await _context.Expectations.CountAsync();
        var avgConfidence = await _context.Expectations.AverageAsync(e => e.ConfidenceScore);
        var beneficiariesCount = await _context.Expectations
            .Select(e => e.NormalizedBeneficiary)
            .Distinct()
            .CountAsync();

        return Ok(new
        {
            TotalExpectations = totalExpectations,
            AverageConfidence = Math.Round(avgConfidence, 2),
            UniqueBeneficiaries = beneficiariesCount
        });
    }
}
