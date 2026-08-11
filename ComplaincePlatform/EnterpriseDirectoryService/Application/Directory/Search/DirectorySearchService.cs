using System.Diagnostics;
using System.Text.Json;
using EnterpriseDirectoryService.Application.Directory.Utilities;
using EnterpriseDirectoryService.Application.Contracts;
using EnterpriseDirectoryService.Application.Directory.Configuration;
using EnterpriseDirectoryService.Application.Directory.Platform;
using EnterpriseDirectoryService.Domain.ReadModels;
using EnterpriseDirectoryService.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnterpriseDirectoryService.Application.Directory.Search;

public sealed class DirectorySearchService(
    EnterpriseDirectoryDbContext db,
    IOptions<DirectoryOptions> options,
    ILogger<DirectorySearchService> logger) : IDirectorySearchEngine
{
    public async Task<DirectorySearchResultDto> SearchAsync(DirectoryQuery query, GovernedDirectoryContext auth, CancellationToken ct = default)
    {
        if (!auth.SearchAllowed)
            throw new UnauthorizedAccessException("Directory search not permitted.");

        var sw = Stopwatch.StartNew();
        var pageSize = Math.Clamp(query.PageSize <= 0 ? options.Value.DefaultPageSize : query.PageSize, 1, options.Value.MaxPageSize);
        var page = Math.Max(1, query.Page);
        var orgId = query.Filters.OrganizationId ?? auth.RowLevelFilter.OrganizationId;

        IQueryable<DirectoryIndex> baseQuery = db.DirectoryIndexes.AsNoTracking().Where(x => x.IsActive);
        baseQuery = ApplyRls(baseQuery, auth.RowLevelFilter);
        if (orgId.HasValue && orgId != Guid.Empty)
            baseQuery = baseQuery.Where(x => x.OrganizationId == orgId);

        baseQuery = ApplyFilters(baseQuery, query.Filters);

        var terms = ExpandSynonyms(await ExpandQueryTermsAsync(query.Text, ct));
        List<ScoredRow> scored;

        if (!string.IsNullOrWhiteSpace(query.Text))
        {
            var text = query.Text.Trim().ToLowerInvariant();
            if (await CanUseFtsAsync(ct))
                scored = await FtsSearchAsync(baseQuery, text, query, ct);
            else
                scored = await FallbackSearchAsync(baseQuery, terms, query, ct);
        }
        else
            scored = await baseQuery.Select(x => new ScoredRow(x, 1.0)).ToListAsync(ct);

        var total = scored.Count;
        var pageItems = scored
            .OrderByDescending(s => s.Score)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => MapHit(s.Row, s.Score, auth))
            .ToList();

        var facets = await GetFacetsInternalAsync(auth, orgId ?? Guid.Empty, ct);
        sw.Stop();
        return new DirectorySearchResultDto
        {
            Items = pageItems,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            ExecutionMs = sw.ElapsedMilliseconds,
            Facets = facets
        };
    }

    public async Task<IReadOnlyList<DirectorySuggestionDto>> SuggestAsync(string prefix, GovernedDirectoryContext auth, Guid organizationId, CancellationToken ct = default)
    {
        if (!auth.SearchAllowed || string.IsNullOrWhiteSpace(prefix)) return Array.Empty<DirectorySuggestionDto>();
        var q = db.DirectoryIndexes.AsNoTracking().Where(x => x.IsActive && x.OrganizationId == organizationId);
        q = ApplyRls(q, auth.RowLevelFilter);
        var p = prefix.Trim().ToLowerInvariant();
        var items = await q.Where(x => x.DisplayName.ToLower().StartsWith(p) || x.SearchBlob.Contains(p))
            .OrderBy(x => x.DisplayName)
            .Take(10)
            .Select(x => new DirectorySuggestionDto { Text = x.DisplayName, Subtitle = x.DepartmentName, PersonId = x.PersonId, EmploymentId = x.EmploymentId })
            .ToListAsync(ct);
        return items;
    }

    public Task<IReadOnlyDictionary<string, IReadOnlyList<FacetValueDto>>> GetFacetsAsync(GovernedDirectoryContext auth, Guid organizationId, CancellationToken ct = default)
        => GetFacetsInternalAsync(auth, organizationId, ct);

    private async Task<IReadOnlyDictionary<string, IReadOnlyList<FacetValueDto>>> GetFacetsInternalAsync(GovernedDirectoryContext auth, Guid organizationId, CancellationToken ct)
    {
        if (!auth.SearchAllowed) return new Dictionary<string, IReadOnlyList<FacetValueDto>>();
        var facets = await db.DirectoryFacets.AsNoTracking()
            .Where(f => f.OrganizationId == organizationId)
            .ToListAsync(ct);
        return facets.GroupBy(f => f.FacetType)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<FacetValueDto>)g.Select(f => new FacetValueDto { Value = f.FacetValue, Count = f.Count }).ToList());
    }

    private static IQueryable<DirectoryIndex> ApplyRls(IQueryable<DirectoryIndex> q, RowLevelFilterScope filter)
    {
        if (filter.OrganizationId.HasValue) q = q.Where(x => x.OrganizationId == filter.OrganizationId);
        if (filter.HospitalId.HasValue) q = q.Where(x => x.HospitalId == filter.HospitalId);
        if (filter.DepartmentId.HasValue) q = q.Where(x => x.DepartmentId == filter.DepartmentId);
        return q;
    }

    private static IQueryable<DirectoryIndex> ApplyFilters(IQueryable<DirectoryIndex> q, DirectoryQueryFilters f)
    {
        if (f.HospitalId.HasValue) q = q.Where(x => x.HospitalId == f.HospitalId);
        if (f.DepartmentId.HasValue) q = q.Where(x => x.DepartmentId == f.DepartmentId);
        if (!string.IsNullOrWhiteSpace(f.EmploymentStatus)) q = q.Where(x => x.EmploymentStatus == f.EmploymentStatus);
        if (!string.IsNullOrWhiteSpace(f.EmploymentType)) q = q.Where(x => x.EmploymentType == f.EmploymentType);
        if (!string.IsNullOrWhiteSpace(f.ProfessionalCategory)) q = q.Where(x => x.ProfessionalCategory == f.ProfessionalCategory);
        if (!string.IsNullOrWhiteSpace(f.Skill)) q = q.Where(x => x.SkillsJson.Contains(f.Skill));
        if (!string.IsNullOrWhiteSpace(f.Certification)) q = q.Where(x => x.CertificationsJson.Contains(f.Certification));
        return q;
    }

    private async Task<bool> CanUseFtsAsync(CancellationToken ct)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync("SELECT 1 WHERE FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') = 1", ct);
            return db.Database.IsSqlServer();
        }
        catch { return false; }
    }

    private async Task<List<ScoredRow>> FtsSearchAsync(IQueryable<DirectoryIndex> baseQuery, string text, DirectoryQuery query, CancellationToken ct)
    {
        var ids = await baseQuery.Select(x => x.Id).ToListAsync(ct);
        if (ids.Count == 0) return [];
        var idTable = string.Join(",", ids.Select(id => $"'{id}'"));
        var sql = $"""
            SELECT di.Id, RANK as FtsRank
            FROM DirectoryIndex di
            INNER JOIN CONTAINSTABLE(DirectoryIndex, SearchBlob, @term) AS ft ON di.Id = ft.[KEY]
            WHERE di.Id IN ({idTable})
            """;
        try
        {
            var conn = db.Database.GetDbConnection();
            await conn.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.Add(new SqlParameter("@term", $"\"{text}*\""));
            var results = new List<ScoredRow>();
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            var rankIdx = reader.GetOrdinal("FtsRank");
            while (await reader.ReadAsync(ct))
            {
                var id = reader.GetGuid(0);
                var rank = reader.GetInt32(rankIdx);
                var row = await db.DirectoryIndexes.AsNoTracking().FirstAsync(x => x.Id == id, ct);
                var score = rank * 10.0 + (query.Phonetic && row.PhoneticKey == PhoneticHelper.ComputeKey(text) ? 5 : 0);
                results.Add(new ScoredRow(row, score));
            }
            return results;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "FTS search failed; falling back to LIKE");
            var terms = ExpandSynonyms(text.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            return await FallbackSearchAsync(baseQuery, terms, query, ct);
        }
    }

    private static async Task<List<ScoredRow>> FallbackSearchAsync(IQueryable<DirectoryIndex> baseQuery, IReadOnlyList<string> terms, DirectoryQuery query, CancellationToken ct)
    {
        var rows = await baseQuery.ToListAsync(ct);
        var phoneticKey = query.Phonetic ? PhoneticHelper.ComputeKey(query.Text) : null;
        return rows.Select(row =>
        {
            var score = 0.0;
            foreach (var term in terms)
            {
                if (row.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase)) score += 10;
                if (row.SearchBlob.Contains(term, StringComparison.OrdinalIgnoreCase)) score += 5;
                if (query.Fuzzy && Levenshtein(row.DisplayName, term) <= 2) score += 3;
            }
            if (phoneticKey != null && row.PhoneticKey == phoneticKey) score += 4;
            if (row.EmploymentStatus?.Equals("Active", StringComparison.OrdinalIgnoreCase) == true) score += 1;
            return new ScoredRow(row, score);
        }).Where(s => s.Score > 0 || string.IsNullOrWhiteSpace(query.Text)).ToList();
    }

    private async Task<IReadOnlyList<string>> ExpandQueryTermsAsync(string text, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
        var terms = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(t => t.ToLowerInvariant()).ToList();
        var synonyms = await db.DirectorySynonyms.AsNoTracking()
            .Where(s => terms.Contains(s.Token) || terms.Contains(s.Synonym))
            .ToListAsync(ct);
        foreach (var syn in synonyms)
        {
            terms.Add(syn.Token);
            terms.Add(syn.Synonym);
        }
        return terms.Distinct().ToList();
    }

    private static IReadOnlyList<string> ExpandSynonyms(IReadOnlyList<string> terms) => terms;

    private static DirectorySearchHitDto MapHit(DirectoryIndex row, double score, GovernedDirectoryContext auth) => new()
    {
        PersonId = row.PersonId,
        EmploymentId = row.EmploymentId,
        DisplayName = row.DisplayName,
        PreferredName = row.PreferredName,
        PlatformId = row.PlatformId,
        EmployeeNumber = row.EmployeeNumber,
        Email = auth.ContactReadable ? row.EmailNormalized : Mask(row.EmailNormalized),
        Phone = auth.ContactReadable ? row.PhoneNormalized : Mask(row.PhoneNormalized),
        DepartmentName = row.DepartmentName,
        HospitalName = row.HospitalName,
        Designation = row.Designation,
        EmploymentStatus = row.EmploymentStatus,
        ProfessionalCategory = row.ProfessionalCategory,
        Score = score
    };

    private static string? Mask(string? value) => string.IsNullOrEmpty(value) ? value : "***";

    private static int Levenshtein(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) return b?.Length ?? 0;
        if (string.IsNullOrEmpty(b)) return a.Length;
        var d = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) d[0, j] = j;
        for (var i = 1; i <= a.Length; i++)
        for (var j = 1; j <= b.Length; j++)
        {
            var cost = a[i - 1] == b[j - 1] ? 0 : 1;
            d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
        }
        return d[a.Length, b.Length];
    }

    private sealed record ScoredRow(DirectoryIndex Row, double Score);
}
