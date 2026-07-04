using Microsoft.AspNetCore.Mvc;
using PatientMonitor.Api.Data;

namespace PatientMonitor.Api.Controllers;

[ApiController]
[Route("api/stats")]
public class StatsController : ControllerBase
{
    private readonly AppDbContext _context;

    public StatsController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>获取统计数据</summary>
    [HttpGet]
    public ActionResult<object> GetStats()
    {
        var totalPatients = _context.Patients.Count();
        var abnormalPatients = _context.Patients.Count(p => p.Status != "normal");

        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var todayAbnormalRecords = _context.BehaviorLogs
            .Count(l => l.IsAbnormal == 1 && l.RecordTime >= today && l.RecordTime < tomorrow);

        return Ok(new
        {
            totalPatients,
            abnormalPatients,
            todayAbnormalRecords
        });
    }
}
