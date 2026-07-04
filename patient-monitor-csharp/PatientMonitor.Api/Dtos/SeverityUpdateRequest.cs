using System.ComponentModel.DataAnnotations;

namespace PatientMonitor.Api.Dtos;

public class SeverityUpdateRequest
{
    [Required(ErrorMessage = "异常等级不能为空")]
    [Range(0, 5, ErrorMessage = "异常等级必须在0到5之间")]
    public int Severity { get; set; }
}
