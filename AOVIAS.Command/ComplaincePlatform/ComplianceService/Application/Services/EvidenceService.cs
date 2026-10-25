using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.EvidenceReview;
using ComplianceService.Application.Http;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Services
{
	public class EvidenceService : IEvidenceService
	{
		private readonly ComplianceDbContext _db;
		private readonly IFileStorageService _fileStorage;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IObligationProjectionService _obligationProjection;
		private readonly IDeficiencyTaskLifecycleService _deficiencyLifecycle;
		private readonly IComplianceEventEmitter _eventEmitter;
		private readonly IEvidenceReviewService _evidenceReview;
		private readonly ILogger<EvidenceService> _logger;

		public EvidenceService(
			ComplianceDbContext db,
			IFileStorageService fileStorage,
			IHttpContextAccessor httpContextAccessor,
			IObligationProjectionService obligationProjection,
			IDeficiencyTaskLifecycleService deficiencyLifecycle,
			IComplianceEventEmitter eventEmitter,
			IEvidenceReviewService evidenceReview,
			ILogger<EvidenceService> logger)
		{
			_db = db;
			_fileStorage = fileStorage;
			_httpContextAccessor = httpContextAccessor;
			_obligationProjection = obligationProjection;
			_deficiencyLifecycle = deficiencyLifecycle;
			_eventEmitter = eventEmitter;
			_evidenceReview = evidenceReview;
			_logger = logger;
		}

		public async Task<Evidence> CreateEvidenceAsync(Evidence evidence, CancellationToken cancellationToken = default)
		{
			evidence.Id = evidence.Id == Guid.Empty ? Guid.NewGuid() : evidence.Id;
			evidence.UploadedOn = evidence.UploadedOn == default ? DateTime.UtcNow : evidence.UploadedOn;
			await _db.Evidences.AddAsync(evidence, cancellationToken);

			// Seed version v1 if not provided
			_db.EvidenceVersions.Add(new EvidenceVersion
			{
				Id = Guid.NewGuid(),
				EvidenceId = evidence.Id,
				VersionNumber = Math.Max(1, evidence.CurrentVersion),
				FileUrl = evidence.FileUrl,
				Hash = evidence.Hash,
				CreatedBy = Guid.Empty,
				CreatedAt = evidence.UploadedOn,
				Notes = "v1"
			});

			await _db.SaveChangesAsync(cancellationToken);
			return evidence;
		}

		public async Task<IReadOnlyList<Evidence>> GetEvidenceForObjectiveElementAsync(Guid objectiveElementId, Evidence.EvidenceArtifactType? artifactType = null, string? department = null, Evidence.EvidenceLifecycleStatus? status = null, CancellationToken cancellationToken = default)
		{
			var query =
				from link in _db.ObjectiveElementEvidences.AsNoTracking()
				join ev in _db.Evidences.AsNoTracking() on link.EvidenceId equals ev.Id
				where link.ObjectiveElementId == objectiveElementId
				select ev;

			if (artifactType.HasValue)
			{
				query = query.Where(e => e.ArtifactType == artifactType.Value);
			}

			if (!string.IsNullOrWhiteSpace(department))
			{
				query = query.Where(e => e.Department == department);
			}

			if (status.HasValue)
			{
				query = query.Where(e => e.LifecycleStatus == status.Value);
			}

			return await query.Distinct().ToListAsync(cancellationToken);
		}

		public Task SetVerificationAsync(
			Guid objectiveElementId,
			Guid evidenceId,
			Guid auditorId,
			bool accepted,
			string? remarks,
			CancellationToken cancellationToken = default)
		{
			var action = accepted ? EvidenceApprovalAction.Approve : EvidenceApprovalAction.Reject;
			return ApplyApprovalAsync(
				evidenceId,
				auditorId,
				action,
				remarks,
				expectedRowVersionBase64: null,
				objectiveElementId: objectiveElementId,
				cancellationToken: cancellationToken);
		}

		public Task<EvidenceApprovalResultDto> ApplyApprovalAsync(
			Guid evidenceId,
			Guid auditorId,
			EvidenceApprovalAction action,
			string? reviewComment,
			string? expectedRowVersionBase64 = null,
			Guid? objectiveElementId = null,
			CancellationToken cancellationToken = default)
		{
			var decision = action switch
			{
				EvidenceApprovalAction.Approve => EvidenceReviewDecision.Approve,
				EvidenceApprovalAction.Reject => EvidenceReviewDecision.Reject,
				EvidenceApprovalAction.RequestMoreEvidence => EvidenceReviewDecision.RequestMoreEvidence,
				_ => throw new ArgumentException("Action must be approve, reject, or requestMoreEvidence.", nameof(action))
			};

			return _evidenceReview.ApplyReviewAsync(
				evidenceId,
				auditorId,
				decision,
				reviewComment,
				recommendation: null,
				reviewerRole: null,
				expectedRowVersionBase64,
				objectiveElementId,
				cancellationToken);
		}

		public async Task<EvidenceDetailDto?> GetEvidenceDetailAsync(
			Guid objectiveElementId,
			Guid evidenceId,
			CancellationToken cancellationToken = default)
		{
			var link = await _db.ObjectiveElementEvidences
				.AsNoTracking()
				.FirstOrDefaultAsync(
					x => x.ObjectiveElementId == objectiveElementId && x.EvidenceId == evidenceId,
					cancellationToken);
			if (link == null)
			{
				return null;
			}

			var ev = await _db.Evidences.AsNoTracking().FirstOrDefaultAsync(e => e.Id == evidenceId, cancellationToken);
			if (ev == null)
			{
				return null;
			}

			var versions = await _db.EvidenceVersions.AsNoTracking()
				.Where(v => v.EvidenceId == evidenceId)
				.OrderBy(v => v.VersionNumber)
				.ToListAsync(cancellationToken);

			return new EvidenceDetailDto
			{
				Id = ev.Id,
				Title = ev.Title ?? string.Empty,
				EvidenceType = ev.EvidenceType,
				FileUrl = ev.FileUrl,
				UploadedBy = ev.UploadedBy,
				UploadedOn = ev.UploadedOn,
				ExpiresAt = ev.ExpiresAt,
				ArtifactType = ev.ArtifactType.ToString(),
				LifecycleStatus = ev.LifecycleStatus.ToString(),
				Metadata = ev.Metadata ?? string.Empty,
				Verification = new EvidenceVerificationDetailDto
				{
					Status = MapStatusLabel(link.VerificationStatus),
					IsApproved = ToIsApproved(link.VerificationStatus),
					VerifiedBy = link.VerifiedBy?.ToString(),
					VerifiedAt = link.VerifiedAt,
					ReviewComment = link.ReviewComment ?? link.Remarks,
					Remarks = link.ReviewComment ?? link.Remarks,
					RowVersion = EncodeRowVersion(link.RowVersion)
				},
				Versions = versions.Select(v => new EvidenceVersionItemDto
				{
					VersionNumber = v.VersionNumber,
					FileUrl = v.FileUrl,
					CreatedAt = v.CreatedAt,
					Notes = v.Notes
				}).ToList()
			};
		}

		public async Task<Evidence> AppendEvidenceVersionAsync(
			Guid evidenceId,
			string newFileUrl,
			Guid userId,
			string? notes,
			CancellationToken cancellationToken = default)
		{
			var ev = await _db.Evidences.FirstOrDefaultAsync(e => e.Id == evidenceId, cancellationToken)
				?? throw new InvalidOperationException("Evidence not found.");

			var maxVer = await _db.EvidenceVersions
				.Where(v => v.EvidenceId == evidenceId)
				.Select(v => v.VersionNumber)
				.DefaultIfEmpty(0)
				.MaxAsync(cancellationToken);
			var newVersionNumber = Math.Max(maxVer, ev.CurrentVersion) + 1;

			var verId = Guid.NewGuid();
			var now = DateTime.UtcNow;
			_db.EvidenceVersions.Add(new EvidenceVersion
			{
				Id = verId,
				CreatedAt = now,
				EvidenceId = evidenceId,
				VersionNumber = newVersionNumber,
				FileUrl = newFileUrl,
				Hash = string.Empty,
				CreatedBy = userId,
				Notes = notes ?? $"v{newVersionNumber}"
			});

			ev.FileUrl = newFileUrl;
			ev.CurrentVersion = newVersionNumber;

			await _db.SaveChangesAsync(cancellationToken);
			return ev;
		}

		public async Task RemoveEvidenceFromObjectiveElementAsync(
			Guid objectiveElementId,
			Guid evidenceId,
			CancellationToken cancellationToken = default)
		{
			var link = await _db.ObjectiveElementEvidences
				.FirstOrDefaultAsync(
					x => x.ObjectiveElementId == objectiveElementId && x.EvidenceId == evidenceId,
					cancellationToken);

			if (link == null)
			{
				throw new InvalidOperationException("Evidence is not linked to this objective element.");
			}

			var evidenceRow = await _db.Evidences.AsNoTracking()
				.FirstOrDefaultAsync(e => e.Id == evidenceId, cancellationToken);

			_db.ObjectiveElementEvidences.Remove(link);

			var otherLinks = await _db.ObjectiveElementEvidences
				.AnyAsync(x => x.EvidenceId == evidenceId && x.ObjectiveElementId != objectiveElementId, cancellationToken);

			if (!otherLinks)
			{
				var ev = await _db.Evidences.FirstOrDefaultAsync(e => e.Id == evidenceId, cancellationToken);
				if (ev != null)
				{
					var versionRows = await _db.EvidenceVersions
						.Where(v => v.EvidenceId == evidenceId)
						.ToListAsync(cancellationToken);

					var urls = new HashSet<string>(StringComparer.Ordinal);
					foreach (var row in versionRows)
					{
						if (!string.IsNullOrWhiteSpace(row.FileUrl))
						{
							urls.Add(row.FileUrl);
						}
					}

					if (!string.IsNullOrWhiteSpace(ev.FileUrl))
					{
						urls.Add(ev.FileUrl);
					}

					var deletedFileCount = 0;
					foreach (var url in urls)
					{
						if (!url.StartsWith("local://", StringComparison.OrdinalIgnoreCase))
						{
							continue;
						}

						try
						{
							await _fileStorage.DeleteAsync(url, cancellationToken);
							deletedFileCount++;
						}
						catch (Exception ex)
						{
							_logger.LogWarning(
								ex,
								"Failed to delete stored evidence file {FileUrl} for evidence {EvidenceId}; correlation {CorrelationId}",
								url,
								evidenceId,
								CorrelationIdAccessor.FromHttpContext(_httpContextAccessor.HttpContext) ?? "(none)");
						}
					}

					_logger.LogInformation(
						"Removing evidence aggregate {EvidenceId}: deleted {DeletedFileCount} local storage file(s); correlation {CorrelationId}",
						evidenceId,
						deletedFileCount,
						CorrelationIdAccessor.FromHttpContext(_httpContextAccessor.HttpContext) ?? "(none)");

					_db.Evidences.Remove(ev);
				}
			}

			await _db.SaveChangesAsync(cancellationToken);

			if (evidenceRow != null)
			{
				await _obligationProjection.ProjectByObjectiveElementAsync(
					evidenceRow.HospitalId,
					objectiveElementId,
					ObligationProjectionTriggers.EvidenceDeleted,
					cancellationToken);
			}
		}

		private static bool? ToIsApproved(EvidenceVerificationStatus status) =>
			status switch
			{
				EvidenceVerificationStatus.Verified => true,
				EvidenceVerificationStatus.Rejected => false,
				_ => null
			};

		private static string MapStatusLabel(EvidenceVerificationStatus status) =>
			status switch
			{
				EvidenceVerificationStatus.Verified => "approved",
				EvidenceVerificationStatus.Rejected => "rejected",
				_ => "pendingReview"
			};

		private static string? NormalizeComment(string? comment)
		{
			if (string.IsNullOrWhiteSpace(comment))
			{
				return null;
			}

			return comment.Trim();
		}

		private static byte[]? ParseRowVersion(string? value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return null;
			}

			try
			{
				return Convert.FromBase64String(value);
			}
			catch
			{
				return null;
			}
		}

		private static string? EncodeRowVersion(byte[]? rowVersion) =>
			rowVersion == null || rowVersion.Length == 0
				? null
				: Convert.ToBase64String(rowVersion);
	}
}
