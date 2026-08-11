using Microsoft.EntityFrameworkCore;
using ProfessionalService.Domain.Entities;
using ProfessionalService.Infrastructure.Persistence;

namespace ProfessionalService.Application.Training;

public sealed class TrainingAppService : ITrainingAppService
{
    private readonly ProfessionalDbContext _db;

    public TrainingAppService(ProfessionalDbContext db) => _db = db;

    public async Task<IReadOnlyList<TrainingProgramDto>> ListProgramsAsync(
        Guid? organizationId, bool? mandatoryOnly, CancellationToken ct = default)
    {
        var q = _db.TrainingPrograms.AsNoTracking().Where(p => !p.IsDeleted);
        if (organizationId.HasValue) q = q.Where(p => p.OrganizationId == organizationId);
        if (mandatoryOnly == true) q = q.Where(p => p.IsMandatory);

        var rows = await q.OrderBy(p => p.Title)
            .Select(p => new
            {
                Program = p,
                SessionCount = p.Sessions.Count(s => !s.IsDeleted)
            })
            .ToListAsync(ct);

        return rows.Select(r => MapProgram(r.Program, r.SessionCount)).ToList();
    }

    public async Task<TrainingProgramDto?> GetProgramAsync(Guid id, CancellationToken ct = default)
    {
        var p = await _db.TrainingPrograms.AsNoTracking()
            .Include(x => x.Sessions)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        return p == null ? null : MapProgram(p, p.Sessions.Count(s => !s.IsDeleted));
    }

    public async Task<TrainingProgramDto> CreateProgramAsync(CreateTrainingProgramRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code)) throw new InvalidOperationException("Program code is required.");
        if (string.IsNullOrWhiteSpace(request.Title)) throw new InvalidOperationException("Program title is required.");

        var exists = await _db.TrainingPrograms.AnyAsync(
            p => p.OrganizationId == request.OrganizationId && p.Code == request.Code.Trim() && !p.IsDeleted, ct);
        if (exists) throw new InvalidOperationException("A program with this code already exists for the organization.");

        var entity = new TrainingProgram
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            HospitalId = request.HospitalId,
            Code = request.Code.Trim(),
            Title = request.Title.Trim(),
            Description = request.Description,
            ControlledDocumentId = request.ControlledDocumentId,
            RoleCode = request.RoleCode?.Trim(),
            DepartmentId = request.DepartmentId,
            IsMandatory = request.IsMandatory,
            RenewalMonths = request.RenewalMonths,
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = request.CreatedByUserId,
            RowVersion = new byte[8],
        };
        _db.TrainingPrograms.Add(entity);
        await _db.SaveChangesAsync(ct);
        return MapProgram(entity, 0);
    }

    public async Task<TrainingProgramDto?> UpdateProgramAsync(Guid id, UpdateTrainingProgramRequest request, CancellationToken ct = default)
    {
        var entity = await _db.TrainingPrograms.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
        if (entity == null) return null;

        if (request.Title != null) entity.Title = request.Title.Trim();
        if (request.Description != null) entity.Description = request.Description;
        if (request.ControlledDocumentId.HasValue) entity.ControlledDocumentId = request.ControlledDocumentId;
        if (request.RoleCode != null) entity.RoleCode = request.RoleCode.Trim();
        if (request.DepartmentId.HasValue) entity.DepartmentId = request.DepartmentId;
        if (request.IsMandatory.HasValue) entity.IsMandatory = request.IsMandatory.Value;
        if (request.RenewalMonths.HasValue) entity.RenewalMonths = request.RenewalMonths;
        if (!string.IsNullOrWhiteSpace(request.Status)) entity.Status = request.Status.Trim();
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        var sessionCount = await _db.TrainingSessions.CountAsync(s => s.TrainingProgramId == id && !s.IsDeleted, ct);
        return MapProgram(entity, sessionCount);
    }

    public async Task<bool> DeleteProgramAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.TrainingPrograms.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
        if (entity == null) return false;
        entity.IsDeleted = true;
        entity.Status = "Retired";
        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<TrainingSessionDto>> ListSessionsAsync(
        Guid? programId, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var q = _db.TrainingSessions.AsNoTracking()
            .Include(s => s.Program)
            .Include(s => s.Attendances)
            .Where(s => !s.IsDeleted);
        if (programId.HasValue) q = q.Where(s => s.TrainingProgramId == programId);
        if (from.HasValue) q = q.Where(s => s.StartsAt >= from);
        if (to.HasValue) q = q.Where(s => s.StartsAt <= to);

        var sessions = await q.OrderBy(s => s.StartsAt).ToListAsync(ct);
        return sessions.Select(MapSession).ToList();
    }

    public async Task<TrainingSessionDto?> GetSessionAsync(Guid id, CancellationToken ct = default)
    {
        var s = await _db.TrainingSessions.AsNoTracking()
            .Include(x => x.Program)
            .Include(x => x.Attendances).ThenInclude(a => a.Assessment)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        return s == null ? null : MapSession(s);
    }

    public async Task<TrainingSessionDto> CreateSessionAsync(CreateTrainingSessionRequest request, CancellationToken ct = default)
    {
        var program = await _db.TrainingPrograms.FirstOrDefaultAsync(p => p.Id == request.TrainingProgramId && !p.IsDeleted, ct)
            ?? throw new InvalidOperationException("Training program not found.");
        if (string.IsNullOrWhiteSpace(request.Title)) throw new InvalidOperationException("Session title is required.");

        var entity = new TrainingSession
        {
            Id = Guid.NewGuid(),
            TrainingProgramId = program.Id,
            Title = request.Title.Trim(),
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            Location = request.Location,
            Facilitator = request.Facilitator,
            Capacity = request.Capacity,
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = request.CreatedByUserId,
            RowVersion = new byte[8],
        };
        _db.TrainingSessions.Add(entity);
        await _db.SaveChangesAsync(ct);
        entity.Program = program;
        return MapSession(entity);
    }

    public async Task<TrainingSessionDto?> UpdateSessionAsync(Guid id, UpdateTrainingSessionRequest request, CancellationToken ct = default)
    {
        var entity = await _db.TrainingSessions
            .Include(s => s.Program)
            .Include(s => s.Attendances)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, ct);
        if (entity == null) return null;

        if (request.Title != null) entity.Title = request.Title.Trim();
        if (request.StartsAt.HasValue) entity.StartsAt = request.StartsAt.Value;
        if (request.EndsAt.HasValue) entity.EndsAt = request.EndsAt;
        if (request.Location != null) entity.Location = request.Location;
        if (request.Facilitator != null) entity.Facilitator = request.Facilitator;
        if (request.Capacity.HasValue) entity.Capacity = request.Capacity;
        if (!string.IsNullOrWhiteSpace(request.Status)) entity.Status = request.Status.Trim();

        await _db.SaveChangesAsync(ct);
        return MapSession(entity);
    }

    public async Task<bool> DeleteSessionAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.TrainingSessions.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, ct);
        if (entity == null) return false;
        entity.IsDeleted = true;
        entity.Status = "Cancelled";
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<TrainingAttendanceDto>> AssignParticipantsAsync(
        Guid sessionId, AssignParticipantsRequest request, CancellationToken ct = default)
    {
        var session = await _db.TrainingSessions
            .Include(s => s.Attendances)
            .FirstOrDefaultAsync(s => s.Id == sessionId && !s.IsDeleted, ct)
            ?? throw new InvalidOperationException("Training session not found.");

        var ids = request.ProfessionalProfileIds.Distinct().ToList();
        foreach (var profileId in ids)
        {
            var exists = session.Attendances.Any(a => a.ProfessionalProfileId == profileId && !a.IsDeleted);
            if (exists) continue;

            var profileExists = await _db.ProfessionalProfiles.AnyAsync(p => p.Id == profileId && !p.IsDeleted, ct);
            if (!profileExists) continue;

            session.Attendances.Add(new TrainingAttendance
            {
                Id = Guid.NewGuid(),
                TrainingSessionId = sessionId,
                ProfessionalProfileId = profileId,
                AttendanceStatus = TrainingAttendanceCompletion.StatusAssigned,
                CreatedAt = DateTime.UtcNow,
                RowVersion = new byte[8],
            });
        }

        await _db.SaveChangesAsync(ct);
        return await ListAttendancesAsync(sessionId, ct);
    }

    public async Task<IReadOnlyList<TrainingAttendanceDto>> ListAttendancesAsync(Guid sessionId, CancellationToken ct = default)
    {
        var rows = await _db.TrainingAttendances.AsNoTracking()
            .Include(a => a.Assessment)
            .Where(a => a.TrainingSessionId == sessionId && !a.IsDeleted)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(ct);
        return rows.Select(MapAttendance).ToList();
    }

    public async Task<TrainingAttendanceDto?> RecordAttendanceAsync(
        Guid attendanceId, RecordAttendanceRequest request, CancellationToken ct = default)
    {
        var entity = await _db.TrainingAttendances
            .Include(a => a.Assessment)
            .Include(a => a.Session)!.ThenInclude(s => s!.Program)
            .FirstOrDefaultAsync(a => a.Id == attendanceId && !a.IsDeleted, ct);
        if (entity == null) return null;

        var status = string.IsNullOrWhiteSpace(request.AttendanceStatus)
            ? TrainingAttendanceCompletion.StatusAttended
            : request.AttendanceStatus.Trim();
        entity.AttendanceStatus = status;
        if (string.Equals(status, TrainingAttendanceCompletion.StatusAttended, StringComparison.OrdinalIgnoreCase))
            entity.CheckedInAt = request.CheckedInAt ?? DateTime.UtcNow;

        if (entity.Session?.Program is { RenewalMonths: > 0 } program
            && string.Equals(status, TrainingAttendanceCompletion.StatusAttended, StringComparison.OrdinalIgnoreCase)
            && entity.ExpiryDate == null)
        {
            entity.ExpiryDate = DateTime.UtcNow.Date.AddMonths(program.RenewalMonths.Value);
        }

        await _db.SaveChangesAsync(ct);
        return MapAttendance(entity);
    }

    public async Task<TrainingAssessmentDto?> RecordAssessmentAsync(
        Guid attendanceId, RecordAssessmentRequest request, CancellationToken ct = default)
    {
        var attendance = await _db.TrainingAttendances
            .Include(a => a.Assessment)
            .FirstOrDefaultAsync(a => a.Id == attendanceId && !a.IsDeleted, ct);
        if (attendance == null) return null;

        var assessment = attendance.Assessment;
        if (assessment == null)
        {
            assessment = new TrainingAssessment
            {
                Id = Guid.NewGuid(),
                TrainingAttendanceId = attendanceId,
                CreatedAt = DateTime.UtcNow,
                RowVersion = new byte[8],
            };
            _db.TrainingAssessments.Add(assessment);
            attendance.Assessment = assessment;
        }

        assessment.Score = request.Score;
        assessment.PassMark = request.PassMark;
        assessment.Passed = request.Passed ?? (request.Score.HasValue && request.PassMark.HasValue
            ? request.Score >= request.PassMark
            : request.Passed);
        assessment.AssessorName = request.AssessorName;
        assessment.AssessedAt = request.AssessedAt ?? DateTime.UtcNow;
        assessment.Notes = request.Notes;

        await _db.SaveChangesAsync(ct);
        return MapAssessment(assessment);
    }

    public async Task<TrainingAttendanceDto?> SetCertificateUrlAsync(
        Guid attendanceId, SetCertificateUrlRequest request, CancellationToken ct = default)
    {
        var entity = await _db.TrainingAttendances
            .Include(a => a.Assessment)
            .FirstOrDefaultAsync(a => a.Id == attendanceId && !a.IsDeleted, ct);
        if (entity == null) return null;
        if (string.IsNullOrWhiteSpace(request.CertificateUrl))
            throw new InvalidOperationException("CertificateUrl is required.");

        entity.CertificateUrl = request.CertificateUrl.Trim();
        entity.CertificateIssuedAt = DateTime.UtcNow;
        if (request.ExpiryDate.HasValue) entity.ExpiryDate = request.ExpiryDate;

        await _db.SaveChangesAsync(ct);
        return MapAttendance(entity);
    }

    public async Task<IReadOnlyList<TrainingMatrixItemDto>> GetMatrixAsync(
        Guid? organizationId, string? roleCode, Guid? departmentId, CancellationToken ct = default)
    {
        var q = _db.TrainingPrograms.AsNoTracking()
            .Include(p => p.Sessions).ThenInclude(s => s.Attendances).ThenInclude(a => a.Assessment)
            .Where(p => !p.IsDeleted && p.Status == "Active");
        if (organizationId.HasValue) q = q.Where(p => p.OrganizationId == organizationId);
        if (!string.IsNullOrWhiteSpace(roleCode)) q = q.Where(p => p.RoleCode == roleCode);
        if (departmentId.HasValue) q = q.Where(p => p.DepartmentId == departmentId);

        var programs = await q.OrderBy(p => p.Title).ToListAsync(ct);
        return programs.Select(p =>
        {
            var rows = p.Sessions.Where(s => !s.IsDeleted)
                .SelectMany(s => s.Attendances.Where(a => !a.IsDeleted))
                .Select(a => (a.AttendanceStatus, a.Assessment?.Passed))
                .ToList();
            var completed = rows.Count(r => TrainingAttendanceCompletion.IsCompleted(r.AttendanceStatus, r.Passed));
            return new TrainingMatrixItemDto
            {
                ProgramId = p.Id,
                ProgramCode = p.Code,
                ProgramTitle = p.Title,
                IsMandatory = p.IsMandatory,
                RoleCode = p.RoleCode,
                DepartmentId = p.DepartmentId,
                ControlledDocumentId = p.ControlledDocumentId,
                AssignedCount = rows.Count,
                CompletedCount = completed,
                CompletionPercent = TrainingAttendanceCompletion.CalculatePercent(rows.Count, completed),
            };
        }).ToList();
    }

    private static TrainingProgramDto MapProgram(TrainingProgram p, int sessionCount) => new()
    {
        Id = p.Id,
        OrganizationId = p.OrganizationId,
        HospitalId = p.HospitalId,
        Code = p.Code,
        Title = p.Title,
        Description = p.Description,
        ControlledDocumentId = p.ControlledDocumentId,
        RoleCode = p.RoleCode,
        DepartmentId = p.DepartmentId,
        IsMandatory = p.IsMandatory,
        RenewalMonths = p.RenewalMonths,
        Status = p.Status,
        CreatedAt = p.CreatedAt,
        SessionCount = sessionCount,
    };

    private static TrainingSessionDto MapSession(TrainingSession s)
    {
        var attendances = s.Attendances?.Where(a => !a.IsDeleted).ToList() ?? new List<TrainingAttendance>();
        var attended = attendances.Count(a =>
            TrainingAttendanceCompletion.IsCompleted(a.AttendanceStatus, a.Assessment?.Passed));
        return new TrainingSessionDto
        {
            Id = s.Id,
            TrainingProgramId = s.TrainingProgramId,
            ProgramTitle = s.Program?.Title ?? string.Empty,
            Title = s.Title,
            StartsAt = s.StartsAt,
            EndsAt = s.EndsAt,
            Location = s.Location,
            Facilitator = s.Facilitator,
            Capacity = s.Capacity,
            Status = s.Status,
            AssignedCount = attendances.Count,
            AttendedCount = attended,
            CompletionPercent = TrainingAttendanceCompletion.CalculatePercent(attendances.Count, attended),
            ControlledDocumentId = s.Program?.ControlledDocumentId,
        };
    }

    private static TrainingAttendanceDto MapAttendance(TrainingAttendance a) => new()
    {
        Id = a.Id,
        TrainingSessionId = a.TrainingSessionId,
        ProfessionalProfileId = a.ProfessionalProfileId,
        AttendanceStatus = a.AttendanceStatus,
        CheckedInAt = a.CheckedInAt,
        CertificateUrl = a.CertificateUrl,
        CertificateIssuedAt = a.CertificateIssuedAt,
        ExpiryDate = a.ExpiryDate,
        Assessment = a.Assessment == null ? null : MapAssessment(a.Assessment),
    };

    private static TrainingAssessmentDto MapAssessment(TrainingAssessment a) => new()
    {
        Id = a.Id,
        TrainingAttendanceId = a.TrainingAttendanceId,
        Score = a.Score,
        PassMark = a.PassMark,
        Passed = a.Passed,
        AssessorName = a.AssessorName,
        AssessedAt = a.AssessedAt,
        Notes = a.Notes,
    };
}
