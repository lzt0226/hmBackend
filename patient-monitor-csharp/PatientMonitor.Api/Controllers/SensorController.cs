using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PatientMonitor.Api.Dtos;
using PatientMonitor.Api.Services;
using PatientMonitor.Api.WebSockets;

namespace PatientMonitor.Api.Controllers;

[ApiController]
[Route("api/sensor")]
public class SensorController : ControllerBase
{
    private readonly IPatientService _patientService;
    private readonly MonitorWebSocketHandler _webSocketHandler;

    public SensorController(IPatientService patientService, MonitorWebSocketHandler webSocketHandler)
    {
        _patientService = patientService;
        _webSocketHandler = webSocketHandler;
    }

    /// <summary>处理传感器信号</summary>
    [HttpPost("signal")]
    public async Task<ActionResult<string>> HandleSensorSignal([FromBody] SensorRequest request)
    {
        var patient = _patientService.HandleSensorSignal(
            request.PatientId,
            request.BehaviorType,
            request.Description,
            request.IsAbnormal,
            request.Severity);

        if (request.IsAbnormal)
        {
            try
            {
                var json = JsonSerializer.Serialize(patient);
                await _webSocketHandler.BroadcastAsync(json);
            }
            catch (Exception ex)
            {
                return Ok($"处理成功，但WebSocket推送失败: {ex.Message}");
            }
        }

        return Ok("处理成功");
    }
}
