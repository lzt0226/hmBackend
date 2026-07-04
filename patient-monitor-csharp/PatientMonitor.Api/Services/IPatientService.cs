using PatientMonitor.Api.Models;

namespace PatientMonitor.Api.Services;

public interface IPatientService
{
    List<Patient> GetAllPatients();
    Patient GetPatientById(long id);
    Dictionary<string, object> GetPatientWithLogsById(long id);
    void UpdatePatientStatus(long id, string status);
    void UpdatePatientSeverity(long id, int severity);
    Patient HandleSensorSignal(long patientId, string behaviorType, string description, bool isAbnormal, int? severity);
    List<Dictionary<string, object>> GetAbnormalPatientsWithLogs();
}
