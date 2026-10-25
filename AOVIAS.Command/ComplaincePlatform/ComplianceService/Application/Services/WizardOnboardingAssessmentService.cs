using System.Text.Json;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Services;

public sealed class WizardOnboardingAssessmentService
{
    private readonly ComplianceDbContext _db;
    private readonly ILogger<WizardOnboardingAssessmentService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public WizardOnboardingAssessmentService(
        ComplianceDbContext db,
        ILogger<WizardOnboardingAssessmentService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<WizardOnboardingAssessmentResponseDto> SaveAsync(SaveWizardAssessmentRequestDto request, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(request.AssessmentAnswers ?? new List<WizardAssessmentAnswerItemDto>(), JsonOptions);

        var existing = await _db.WizardOnboardingAssessments
            .AsNoTracking()
            .Where(x => x.HospitalId == request.HospitalId && x.WizardVersion == request.WizardVersion)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing != null)
        {
            _logger.LogInformation(
                "Wizard onboarding assessment already exists for HospitalId={HospitalId}, WizardVersion={WizardVersion}; returning {AssessmentId} (idempotent)",
                request.HospitalId, request.WizardVersion, existing.Id);
            return Map(existing);
        }

        var row = new WizardOnboardingAssessment
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            HospitalId = request.HospitalId,
            WizardVersion = request.WizardVersion,
            AnswersJson = json,
            CreatedAt = DateTime.UtcNow
        };

        _db.WizardOnboardingAssessments.Add(row);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Saved wizard onboarding assessment {AssessmentId} for HospitalId={HospitalId}, WizardVersion={WizardVersion}",
            row.Id, row.HospitalId, row.WizardVersion);

        return Map(row);
    }

    public async Task<WizardOnboardingAssessmentResponseDto?> GetLatestForHospitalAsync(Guid hospitalId, CancellationToken cancellationToken)
    {
        var row = await _db.WizardOnboardingAssessments
            .AsNoTracking()
            .Where(x => x.HospitalId == hospitalId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return row == null ? null : Map(row);
    }

    private static WizardOnboardingAssessmentResponseDto Map(WizardOnboardingAssessment row)
    {
        List<WizardAssessmentAnswerItemDto> answers;
        try
        {
            answers = JsonSerializer.Deserialize<List<WizardAssessmentAnswerItemDto>>(row.AnswersJson, JsonOptions)
                      ?? new List<WizardAssessmentAnswerItemDto>();
        }
        catch
        {
            answers = new List<WizardAssessmentAnswerItemDto>();
        }

        return new WizardOnboardingAssessmentResponseDto
        {
            Id = row.Id,
            OrganizationId = row.OrganizationId,
            HospitalId = row.HospitalId,
            WizardVersion = row.WizardVersion,
            AssessmentAnswers = answers,
            CreatedAt = row.CreatedAt
        };
    }
}
