using System.ComponentModel.DataAnnotations;

namespace PatientMonitor.Api.Dtos;

public class StatusUpdateRequest
{
    [Required(ErrorMessage = "状态不能为空")]
    public string Status { get; set; } = string.Empty;
}
