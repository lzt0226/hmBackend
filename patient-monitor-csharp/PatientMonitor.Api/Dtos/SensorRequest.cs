namespace PatientMonitor.Api.Dtos;

public class SensorRequest
{
    public long PatientId { get; set; }
    public string BehaviorType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsAbnormal { get; set; }
    public int? Severity { get; set; }
}
