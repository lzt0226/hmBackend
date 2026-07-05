package com.hospital.monitor.controller;

import com.hospital.monitor.dto.SeverityUpdateRequest;
import com.hospital.monitor.dto.StatusUpdateRequest;
import com.hospital.monitor.service.PatientService;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.springframework.web.bind.annotation.*;

@RestController
@RequestMapping("/api/patients")
@RequiredArgsConstructor
@Tag(name = "患者管理", description = "患者相关接口")
public class PatientController {

    private final PatientService patientService;

    @GetMapping
    @Operation(summary = "获取所有患者")
    public Object getAllPatients() {
        return patientService.getAllPatients();
    }

    @GetMapping("/{id}")
    @Operation(summary = "获取单个患者")
    public Object getPatient(@PathVariable Long id) {
        return patientService.getPatientWithLogsById(id);
    }

    @GetMapping("/abnormal")
    @Operation(summary = "获取异常患者及其日志")
    public Object getAbnormalPatients() {
        return patientService.getAbnormalPatientsWithLogs();
    }

    @PutMapping("/{id}/status")
    @Operation(summary = "更新患者状态")
    public Object updatePatientStatus(@PathVariable Long id,
                                       @Valid @RequestBody StatusUpdateRequest request) {
        patientService.updatePatientStatus(id, request.getStatus());
        return "状态更新成功";
    }

    @PutMapping("/{id}/severity")
    @Operation(summary = "更新患者异常等级")
    public Object updatePatientSeverity(@PathVariable Long id,
                                         @Valid @RequestBody SeverityUpdateRequest request) {
        patientService.updatePatientSeverity(id, request.getSeverity());
        return "异常等级更新成功";
    }

    @GetMapping("/{id}/behavior-logs")
    @Operation(summary = "获取患者所有行为记录（用于图表统计）")
    public Object getPatientAllBehaviorLogs(@PathVariable Long id) {
        return patientService.getAllBehaviorLogsByPatientId(id);
    }
}
