using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PatientMonitor.Api.Data;
using PatientMonitor.Api.Models;
using PatientMonitor.Api.WebSockets;

namespace PatientMonitor.Api.Services;

public class PatientService : IPatientService
{
    private readonly AppDbContext _context;
    private readonly IBehaviorLogService _behaviorLogService;
    private readonly MonitorWebSocketHandler _webSocketHandler;

    public PatientService(AppDbContext context, IBehaviorLogService behaviorLogService, MonitorWebSocketHandler webSocketHandler)
    {
        _context = context;
        _behaviorLogService = behaviorLogService;
        _webSocketHandler = webSocketHandler;
    }

    /// <summary>
    /// 广播所有异常病人给所有前端用户
    /// </summary>
    private async void BroadcastAbnormalPatients()
    {
        try
        {
            var abnormalPatients = GetAbnormalPatientsWithLogs();
            var json = JsonSerializer.Serialize(abnormalPatients);
            await _webSocketHandler.BroadcastAsync(json);
        }
        catch (Exception ex)
        {
            // 广播失败不影响主流程，记录日志即可
            Console.Error.WriteLine($"广播异常病人列表失败: {ex.Message}");
        }
    }

    public List<Patient> GetAllPatients()
    {
        return _context.Patients.ToList();
    }

    public Patient GetPatientById(long id)
    {
        var patient = _context.Patients.Find(id);
        if (patient == null)
            throw new KeyNotFoundException("病人不存在");
        return patient;
    }

    public Dictionary<string, object> GetPatientWithLogsById(long id)
    {
        var patient = _context.Patients.Find(id);
        if (patient == null)
            throw new KeyNotFoundException("病人不存在");

        var logs = _context.BehaviorLogs
            .Where(l => l.PatientId == id)
            .OrderByDescending(l => l.RecordTime)
            .Take(10)
            .ToList();

        return new Dictionary<string, object>
        {
            ["patient"] = patient,
            ["logs"] = logs
        };
    }

    public void UpdatePatientStatus(long id, string status)
    {
        var patient = _context.Patients.Find(id);
        if (patient == null)
            throw new KeyNotFoundException("病人不存在");

        patient.Status = status;
        patient.UpdatedAt = DateTime.Now;
        _context.SaveChanges();

        // 状态改变后广播所有异常病人
        BroadcastAbnormalPatients();
    }

    public void UpdatePatientSeverity(long id, int severity)
    {
        var patient = _context.Patients.Find(id);
        if (patient == null)
            throw new KeyNotFoundException("病人不存在");

        patient.Severity = severity;
        patient.UpdatedAt = DateTime.Now;
        _context.SaveChanges();

        // 等级改变后广播所有异常病人
        BroadcastAbnormalPatients();
    }

    public Patient HandleSensorSignal(long patientId, string behaviorType, string description, bool isAbnormal, int? severity)
    {
        var patient = _context.Patients.Find(patientId);
        if (patient == null)
            throw new KeyNotFoundException("病人不存在");

        // 记录行为日志
        var log = new BehaviorLog
        {
            PatientId = patientId,
            BehaviorType = behaviorType,
            Description = description,
            IsAbnormal = isAbnormal ? 1 : 0,
            RecordTime = DateTime.Now
        };
        _behaviorLogService.SaveLog(log);

        bool statusChanged = false;

        if (isAbnormal)
        {
            patient.Status = "abnormal";
            patient.Severity = severity ?? GetDefaultSeverity(behaviorType);
            patient.UpdatedAt = DateTime.Now;
            statusChanged = true;
        }
        else
        {
            if (patient.Status != "abnormal")
            {
                patient.Status = "normal";
                patient.Severity = 0;
                patient.UpdatedAt = DateTime.Now;
                statusChanged = true;
            }
        }

        if (statusChanged)
        {
            _context.SaveChanges();
            BroadcastAbnormalPatients();
        }

        return patient;
    }

    private static int GetDefaultSeverity(string behaviorType)
    {
        return behaviorType switch
        {
            "跌倒" => 3,
            "心率异常" => 2,
            "离床" => 1,
            "呼吸异常" => 3,
            "体温异常" => 2,
            "血压异常" => 2,
            "紧急呼叫" => 3,
            "长时间静止" => 2,
            _ => 1
        };
    }

    public List<Dictionary<string, object>> GetAbnormalPatientsWithLogs()
    {
        var patients = _context.Patients
            .Where(p => p.Status != "normal")
            .OrderByDescending(p => p.Severity)
            .ToList();

        var result = new List<Dictionary<string, object>>();
        foreach (var patient in patients)
        {
            var logs = _context.BehaviorLogs
                .Where(l => l.PatientId == patient.Id)
                .OrderByDescending(l => l.RecordTime)
                .Take(10)
                .ToList();

            result.Add(new Dictionary<string, object>
            {
                ["patient"] = patient,
                ["logs"] = logs
            });
        }
        return result;
    }
}
