using PatientMonitor.Api.Models;

namespace PatientMonitor.Api.Services;

public interface IBehaviorLogService
{
    void SaveLog(BehaviorLog behaviorLog);
}
