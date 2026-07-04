using Microsoft.EntityFrameworkCore;
using PatientMonitor.Api.Data;
using PatientMonitor.Api.Models;

namespace PatientMonitor.Api.Services;

public class BehaviorLogService : IBehaviorLogService
{
    private readonly AppDbContext _context;

    public BehaviorLogService(AppDbContext context)
    {
        _context = context;
    }

    public void SaveLog(BehaviorLog behaviorLog)
    {
        _context.BehaviorLogs.Add(behaviorLog);
        _context.SaveChanges();
    }
}
