using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Services;

public sealed class AddFindingCommentCommandHandler
{
    private readonly ComplianceDbContext _db;
    private readonly IFindingAuditTrailService _auditTrail;

    public AddFindingCommentCommandHandler(ComplianceDbContext db, IFindingAuditTrailService auditTrail)
    {
        _db = db;
        _auditTrail = auditTrail;
    }

    public async Task HandleAsync(Guid organizationId, Guid findingId, Guid userId, string body, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Comment body is required.");

        var finding = await _db.Findings
            .FirstOrDefaultAsync(f => f.Id == findingId && f.TenantId == organizationId, ct)
            ?? throw new InvalidOperationException("Finding not found.");

        var comment = new FindingComment
        {
            Id = Guid.NewGuid(),
            FindingId = findingId,
            AuthorUserId = userId,
            Body = body.Trim(),
            CreatedAt = DateTime.UtcNow,
        };

        finding.LastUpdatedAt = DateTime.UtcNow;
        finding.LastUpdatedBy = userId;

        await _db.FindingComments.AddAsync(comment, ct);
        await _db.SaveChangesAsync(ct);

        await _auditTrail.LogAsync(findingId, "CommentAdded", userId, "User", null, body.Trim(), ct);
    }
}

public sealed class AddFindingAttachmentCommandHandler
{
    private readonly ComplianceDbContext _db;
    private readonly IFindingAuditTrailService _auditTrail;

    public AddFindingAttachmentCommandHandler(ComplianceDbContext db, IFindingAuditTrailService auditTrail)
    {
        _db = db;
        _auditTrail = auditTrail;
    }

    public async Task HandleAsync(
        Guid organizationId,
        Guid findingId,
        Guid userId,
        string fileName,
        string fileUrl,
        string? contentType,
        string attachmentType,
        long? fileSizeBytes,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(fileUrl))
            throw new ArgumentException("FileName and FileUrl are required.");

        var finding = await _db.Findings
            .FirstOrDefaultAsync(f => f.Id == findingId && f.TenantId == organizationId, ct)
            ?? throw new InvalidOperationException("Finding not found.");

        if (!Enum.TryParse<FindingAttachmentType>(attachmentType, ignoreCase: true, out var parsedType))
            parsedType = FindingAttachmentType.Document;

        var attachment = new FindingAttachment
        {
            Id = Guid.NewGuid(),
            FindingId = findingId,
            FileName = fileName.Trim(),
            FileUrl = fileUrl.Trim(),
            ContentType = contentType,
            AttachmentType = parsedType,
            FileSizeBytes = fileSizeBytes,
            UploadedByUserId = userId,
            UploadedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };

        finding.LastUpdatedAt = DateTime.UtcNow;
        finding.LastUpdatedBy = userId;

        await _db.FindingAttachments.AddAsync(attachment, ct);
        await _db.SaveChangesAsync(ct);

        await _auditTrail.LogAsync(
            findingId, "AttachmentAdded", userId, "User", null,
            System.Text.Json.JsonSerializer.Serialize(new { fileName, attachmentType = parsedType.ToString() }),
            ct);
    }
}
