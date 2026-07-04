using Microsoft.AspNetCore.Mvc;
using PatientMonitor.Api.Dtos;
using PatientMonitor.Api.Services;

namespace PatientMonitor.Api.Controllers;

[ApiController]
[Route("api/patients")]
public class PatientController : ControllerBase
{
    private readonly IPatientService _patientService;

    public PatientController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    /// <summary>获取所有患者</summary>
    [HttpGet]
    public ActionResult<List<object>> GetAllPatients()
    {
        return Ok(_patientService.GetAllPatients());
    }

    /// <summary>获取单个患者及其行为日志</summary>
    [HttpGet("{id}")]
    public ActionResult<object> GetPatient(long id)
    {
        return Ok(_patientService.GetPatientWithLogsById(id));
    }

    /// <summary>获取异常患者及其日志</summary>
    [HttpGet("abnormal")]
    public ActionResult<List<object>> GetAbnormalPatients()
    {
        return Ok(_patientService.GetAbnormalPatientsWithLogs());
    }

    /// <summary>更新患者状态</summary>
    [HttpPut("{id}/status")]
    public ActionResult<string> UpdatePatientStatus(long id, [FromBody] StatusUpdateRequest request)
    {
        _patientService.UpdatePatientStatus(id, request.Status);
        return Ok("状态更新成功");
    }

    /// <summary>更新患者异常等级</summary>
    [HttpPut("{id}/severity")]
    public ActionResult<string> UpdatePatientSeverity(long id, [FromBody] SeverityUpdateRequest request)
    {
        _patientService.UpdatePatientSeverity(id, request.Severity);
        return Ok("异常等级更新成功");
    }
}
