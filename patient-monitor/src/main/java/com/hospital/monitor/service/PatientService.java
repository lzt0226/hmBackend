package com.hospital.monitor.service;

import com.hospital.monitor.entity.Patient;

import java.util.List;
import java.util.Map;

public interface PatientService {
    List<Patient> getAllPatients();
    Patient getPatientById(Long id);
    Map<String, Object> getPatientWithLogsById(Long id);
    void updatePatientStatus(Long id, String status);
    void updatePatientSeverity(Long id, Integer severity);
    Patient handleSensorSignal(Long patientId, String behaviorType, String description, boolean isAbnormal, Integer severity);
    List<Map<String, Object>> getAbnormalPatientsWithLogs();
    List<Map<String, Object>> getAllBehaviorLogsByPatientId(Long id);
}
