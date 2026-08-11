using ComplianceService.Application.Contracts;

using ComplianceService.Domain;

using ComplianceService.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;



namespace ComplianceService.Infrastructure.Repositories;



public class HospitalComplianceRepository : IHospitalComplianceRepository

{

    private readonly ComplianceDbContext _dbContext;



    public HospitalComplianceRepository(ComplianceDbContext dbContext)

    {

        _dbContext = dbContext;

    }



    public async Task<HospitalComplianceAggregate> LoadAsync(

        Guid hospitalId,

        Guid frameworkId,

        CancellationToken ct)

    {

        var chapters = await _dbContext.ComplianceChapters

            .AsNoTracking()

            .Where(c => c.FrameworkId == frameworkId)

            .ToListAsync(ct);



        if (!chapters.Any())

        {

            throw new InvalidOperationException(

                $"No compliance chapters found for FrameworkId={frameworkId}.");

        }



        var chapterIds = chapters.Select(c => c.Id).ToList();



        var objectiveElementIds = await (

            from oe in _dbContext.ObjectiveElements.AsNoTracking()

            join s in _dbContext.ComplianceStandards.AsNoTracking() on oe.ComplianceStandardId equals s.Id

            where chapterIds.Contains(s.ChapterId)

            select oe.Id).ToListAsync(ct);



        var tasks = await _dbContext.ComplianceTasks

            .Include(t => t.ObjectiveElement)

                .ThenInclude(oe => oe.ComplianceStandard)

                .ThenInclude(s => s!.Chapter)

            .Where(t =>

                t.HospitalId == hospitalId &&

                objectiveElementIds.Contains(t.ObjectiveElementId))

            .ToListAsync(ct);



        var risks = await _dbContext.Risks

            .Include(r => r.ObjectiveElement)!

            .ThenInclude(oe => oe!.ComplianceStandard)

            .Where(r => r.HospitalId == hospitalId)

            .ToListAsync(ct);



        var aggregate = new HospitalComplianceAggregate(hospitalId, frameworkId);



        foreach (var chapter in chapters)

        {

            aggregate.AddChapter(chapter);

        }



        foreach (var task in tasks)

        {

            var oe = task.ObjectiveElement;

            if (oe?.ComplianceStandard == null)

            {

                throw new InvalidOperationException(

                    $"Task {task.Id} does not have a related objective element for framework mapping.");

            }



            aggregate.AddTask(task, oe.ComplianceStandard.ChapterId);

        }



        foreach (var risk in risks)

        {

            aggregate.AddRisk(risk);

        }



        return aggregate;

    }



    public async Task SaveAsync(HospitalComplianceAggregate aggregate, CancellationToken ct)

    {

        await _dbContext.SaveChangesAsync(ct);

    }

}

