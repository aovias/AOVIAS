using ComplianceService.Application.DTOs;
using ComplianceService.Domain.Policies;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Domain.Enums;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.V1_1
{
	[ApiController]
	[ApiVersion("1.1")]
	[Route("api/v{version:apiVersion}/frameworks")]
	[Authorize(Policy = Shared.Infrastructure.Auth.AuthConstants.Policies.ComplianceScope)]
	public class FrameworkReadController : ControllerBase
	{
		private readonly ComplianceDbContext _db;
		private readonly IOperationalModeClassificationPolicy _operationalModeClassification;

		public FrameworkReadController(
			ComplianceDbContext db,
			IOperationalModeClassificationPolicy operationalModeClassification)
		{
			_db = db;
			_operationalModeClassification = operationalModeClassification;
		}

		/// <summary>
		/// Returns Framework → Chapter → ComplianceStandard → Objective Element structure (NABH hierarchy).
		/// Each <see cref="ComplianceStandardDto"/> includes <see cref="ComplianceStandardDto.ChapterId"/>.
		/// </summary>
		[HttpGet("{frameworkId}/structure")]
		public async Task<ActionResult<ApiResponse<FrameworkStructureDto>>> GetFrameworkStructure(
			Guid frameworkId,
			[FromQuery] OperationalMode? operationalMode = null,
			[FromQuery] string? classifications = null,
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = 100,
			CancellationToken ct = default)
		{
			var traceId = HttpContext.TraceIdentifier;

			var framework = await _db.ComplianceFrameworks.FirstOrDefaultAsync(f => f.Id == frameworkId, ct);
			if (framework == null)
			{
				var notFound = ApiResponseFactory.Failure<FrameworkStructureDto>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
				var meta404 = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
				return StatusCode(meta404.HttpStatus, notFound);
			}

			// Load chapters
			var chapters = await _db.ComplianceChapters
				.Where(ch => ch.FrameworkId == frameworkId)
				.OrderBy(ch => ch.Sequence)
				.ToListAsync(ct);

			// Load standards and objective elements for all chapters
			var chapterIds = chapters.Select(ch => ch.Id).ToList();

			var standards = await _db.ComplianceStandards
				.Where(s => chapterIds.Contains(s.ChapterId))
				.OrderBy(s => s.Sequence)
				.ToListAsync(ct);

			var standardIds = standards.Select(s => s.Id).ToList();

			// Determine included classifications
			var included = new List<Domain.ObjectiveElementClassification>();
			if (operationalMode.HasValue)
			{
				included = _operationalModeClassification.GetClassificationsForWorkspace(operationalMode.Value).ToList();
			}
			else if (!string.IsNullOrWhiteSpace(classifications))
			{
				var parts = classifications.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				foreach (var p in parts)
				{
					if (Enum.TryParse<Domain.ObjectiveElementClassification>(p, true, out var cls))
					{
						if (!included.Contains(cls)) included.Add(cls);
					}
				}
			}

			IQueryable<Domain.ObjectiveElement> oeQuery = _db.ObjectiveElements
				.Where(oe => standardIds.Contains(oe.ComplianceStandardId));

			if (included.Count > 0)
			{
				oeQuery = oeQuery.Where(oe => included.Contains(oe.Classification));
			}

			// Apply per-standard paging later; first materialize all filtered OEs for mapping
			var oes = await oeQuery
				.OrderBy(oe => oe.Code)
				.ToListAsync(ct);

			// Map to DTOs
			var oesByStandard = oes.GroupBy(o => o.ComplianceStandardId).ToDictionary(g => g.Key, g => g.ToList());
			var standardsByChapter = standards.GroupBy(s => s.ChapterId).ToDictionary(g => g.Key, g => g.ToList());

			var chapterDtos = new List<ChapterWithStandardsDto>(chapters.Count);
			foreach (var ch in chapters)
			{
				var stForChapter = standardsByChapter.GetValueOrDefault(ch.Id, new List<Domain.ComplianceStandard>());
				var stdDtos = new List<ComplianceStandardDto>(stForChapter.Count);

				foreach (var std in stForChapter)
				{
					var oeForStd = oesByStandard.GetValueOrDefault(std.Id, new List<Domain.ObjectiveElement>());

					// Per-standard paging
					var p = Math.Max(1, page);
					var ps = Math.Max(1, Math.Min(pageSize, 1000));
					var paged = oeForStd.Skip((p - 1) * ps).Take(ps).ToList();

					var oeDtos = paged.Select(oe => new ObjectiveElementDto
					{
						Id = oe.Id,
						Code = oe.Code,
						Title = oe.Title,
						Description = oe.Description,
						Classification = oe.Classification,
						IsMandatory = oe.IsMandatory,
						IsScorable = oe.IsScorable,
						IsApplicable = oe.IsApplicable,
						IsDocumentationRequired = oe.IsDocumentationRequired,
						Weight = oe.Weight,
						Tags = oe.Tags,
						Department = oe.Department,
						ResponsibleRole = oe.ResponsibleRole,
						Frequency = oe.Frequency,
						EvidenceCategory = oe.EvidenceCategory
					}).ToList();

					stdDtos.Add(new ComplianceStandardDto
					{
						Id = std.Id,
						ChapterId = std.ChapterId,
						Code = std.Code,
						Name = std.Name,
						Sequence = std.Sequence,
						Weight = std.Weight,
						ObjectiveElements = oeDtos
					});
				}

				chapterDtos.Add(new ChapterWithStandardsDto
				{
					Id = ch.Id,
					Code = ch.Code,
					Name = ch.Name,
					Sequence = ch.Sequence,
					Weight = ch.Weight,
					Standards = stdDtos
				});
			}

			var payload = new FrameworkStructureDto
			{
				FrameworkId = framework.Id,
				FrameworkName = framework.Name,
				Version = framework.Version,
				Chapters = chapterDtos
			};

			var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, payload, traceId);
			var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
			return StatusCode(meta.HttpStatus, response);
		}
	}
}

