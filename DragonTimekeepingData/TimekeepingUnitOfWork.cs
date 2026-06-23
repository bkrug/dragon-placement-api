using CommonDataLayer.Repositories;
using Microsoft.EntityFrameworkCore;
using DragonTimekeepingApplication;
using EfModels = DragonTimekeepingData.Models;
using DomainModels = DragonTimekeepingDomain.Models;

namespace DragonTimekeepingData;

public class TimekeepingUnitOfWork(TimekeepingContext context) : IDisposable, ITimekeepingUnitOfWork
{
    private readonly TimekeepingContext _context = context;

    public async Task SaveAsync()
    {
        await _context.SaveChangesAsync();
    }

    private bool disposed = false;

    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
                _context.Dispose();
            }
        }
        this.disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public IEnumerable<DomainModels.PayPeriod> GetPayPeriodsByAssignment(int assignmentId)
    {
        return _context.PayPeriods
            .Where(pp => pp.AssignmentId == assignmentId)
            .OrderByDescending(pp => pp.StartDateUnix)
            .AsEnumerable()
            .Select(MapPayPeriod);
    }

    public async Task<DomainModels.PayPeriod?> GetPayPeriodWithHoursWorkedAsync(int payPeriodId)
    {
        var ef = await _context.PayPeriods
            .Include(p => p.HoursWorked.OrderBy(hw => hw.StartDateTimeUnix))
            .FirstOrDefaultAsync(p => p.PayPeriodId == payPeriodId);
        return ef == null ? null : MapPayPeriod(ef);
    }

    public void InsertPayPeriod(DomainModels.PayPeriod payPeriod)
    {
        var ef = MapToEf(payPeriod);
        _context.PayPeriods.Add(ef);
        payPeriod.PayPeriodId = ef.PayPeriodId;
    }

    public DeleteResult DeletePayPeriod(int payPeriodId)
    {
        var entity = _context.PayPeriods.Find(payPeriodId);
        if (entity == null)
            return DeleteResult.NotFound;
        _context.PayPeriods.Remove(entity);
        return DeleteResult.Deleted;
    }

    private static DomainModels.PayPeriod MapPayPeriod(EfModels.PayPeriod ef)
    {
        return new DomainModels.PayPeriod
        {
            PayPeriodId = ef.PayPeriodId,
            AssignmentId = ef.AssignmentId,
            StartDate = DateTimeOffset.FromUnixTimeSeconds(ef.StartDateUnix).UtcDateTime,
            EndDate = DateTimeOffset.FromUnixTimeSeconds(ef.EndDateUnix).UtcDateTime,
            SubmissionStatus = ef.SubmissionStatus,
            HoursWorked = ef.HoursWorked.Select(hw => new DomainModels.HoursWorked
            {
                HoursWorkedId = hw.HoursWorkedId,
                StartDateTime = DateTimeOffset.FromUnixTimeSeconds(hw.StartDateTimeUnix).UtcDateTime,
                EndDateTime = DateTimeOffset.FromUnixTimeSeconds(hw.EndDateTimeUnix).UtcDateTime,
                PayPeriodId = hw.PayPeriodId
            }).ToList()
        };
    }

    private static EfModels.PayPeriod MapToEf(DomainModels.PayPeriod domain)
    {
        return new EfModels.PayPeriod
        {
            PayPeriodId = domain.PayPeriodId,
            AssignmentId = domain.AssignmentId,
            StartDateUnix = new DateTimeOffset(domain.StartDate, TimeSpan.Zero).ToUnixTimeSeconds(),
            EndDateUnix = new DateTimeOffset(domain.EndDate, TimeSpan.Zero).ToUnixTimeSeconds(),
            SubmissionStatus = domain.SubmissionStatus,
            HoursWorked = domain.HoursWorked.Select(hw => new EfModels.HoursWorked
            {
                HoursWorkedId = hw.HoursWorkedId,
                StartDateTimeUnix = new DateTimeOffset(hw.StartDateTime, TimeSpan.Zero).ToUnixTimeSeconds(),
                EndDateTimeUnix = new DateTimeOffset(hw.EndDateTime, TimeSpan.Zero).ToUnixTimeSeconds(),
                PayPeriodId = hw.PayPeriodId
            }).ToList()
        };
    }
}
