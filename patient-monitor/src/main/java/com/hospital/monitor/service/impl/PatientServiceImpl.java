package com.hospital.monitor.service.impl;

import com.baomidou.mybatisplus.core.conditions.query.QueryWrapper;
import com.baomidou.mybatisplus.core.conditions.update.UpdateWrapper;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.SerializationFeature;
import com.fasterxml.jackson.datatype.jsr310.JavaTimeModule;
import com.hospital.monitor.entity.BehaviorLog;
import com.hospital.monitor.entity.Patient;
import com.hospital.monitor.mapper.BehaviorLogMapper;
import com.hospital.monitor.mapper.PatientMapper;
import com.hospital.monitor.service.BehaviorLogService;
import com.hospital.monitor.service.PatientService;
import com.hospital.monitor.websocket.MonitorWebSocketHandler;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

@Slf4j
@Service
public class PatientServiceImpl implements PatientService {

    private final PatientMapper patientMapper;
    private final BehaviorLogMapper behaviorLogMapper;
    private final BehaviorLogService behaviorLogService;
    private final MonitorWebSocketHandler webSocketHandler;
    private final ObjectMapper objectMapper;

    public PatientServiceImpl(PatientMapper patientMapper,
                            BehaviorLogMapper behaviorLogMapper,
                            BehaviorLogService behaviorLogService,
                            MonitorWebSocketHandler webSocketHandler) {
        this.patientMapper = patientMapper;
        this.behaviorLogMapper = behaviorLogMapper;
        this.behaviorLogService = behaviorLogService;
        this.webSocketHandler = webSocketHandler;
        this.objectMapper = new ObjectMapper();
        this.objectMapper.registerModule(new JavaTimeModule());
        this.objectMapper.disable(SerializationFeature.WRITE_DATES_AS_TIMESTAMPS);
    }

    /**
     * 广播单个病人状态变化给所有前端用户
     */
    private void broadcastPatientUpdate(Map<String, Object> patientData) {
        try {
            String message = objectMapper.writeValueAsString(patientData);
            webSocketHandler.broadcast(message);
            log.info("已广播病人状态变化: {}", patientData);
        } catch (Exception e) {
            log.error("广播病人状态变化失败", e);
        }
    }

    /**
     * 广播所有异常病人给所有前端用户
     */
    private void broadcastAbnormalPatients() {
        try {
            List<Map<String, Object>> abnormalPatients = getAbnormalPatientsWithLogs();
            String message = objectMapper.writeValueAsString(abnormalPatients);
            webSocketHandler.broadcast(message);
            log.info("已广播异常病人列表，共 {} 人", abnormalPatients.size());
        } catch (Exception e) {
            log.error("广播异常病人列表失败", e);
        }
    }

    @Override
    public List<Patient> getAllPatients() {
        return patientMapper.selectList(null);
    }

    @Override
    public Patient getPatientById(Long id) {
        return patientMapper.selectById(id);
    }

    @Override
    public Map<String, Object> getPatientWithLogsById(Long id) {
        Patient patient = patientMapper.selectById(id);
        if (patient == null) {
            throw new RuntimeException("病人不存在");
        }

        Map<String, Object> result = new HashMap<>();
        result.put("patient", patient);
        result.put("logs", behaviorLogMapper.selectTop10ByPatientId(id));
        return result;
    }

    @Override
    public void updatePatientStatus(Long id, String status) {
        Patient patient = patientMapper.selectById(id);
        if (patient == null) {
            throw new RuntimeException("病人不存在");
        }
        patient.setStatus(status);
        patientMapper.updateById(patient);

        // 状态改变后广播所有异常病人
        broadcastAbnormalPatients();
    }

    @Override
    public void updatePatientSeverity(Long id, Integer severity) {
        Patient patient = patientMapper.selectById(id);
        if (patient == null) {
            throw new RuntimeException("病人不存在");
        }
        
        // 使用 UpdateWrapper 只更新 severity 字段
        UpdateWrapper<Patient> updateWrapper = new UpdateWrapper<>();
        updateWrapper.eq("id", id).set("severity", severity);
        patientMapper.update(null, updateWrapper);

        // 等级改变后广播所有异常病人
        broadcastAbnormalPatients();
    }

    @Override
    public Patient handleSensorSignal(Long patientId, String behaviorType, String description, boolean isAbnormal, Integer severity) {
        Patient patient = patientMapper.selectById(patientId);
        if (patient == null) {
            throw new RuntimeException("病人不存在");
        }

        BehaviorLog log = new BehaviorLog();
        log.setPatientId(patientId);
        log.setBehaviorType(behaviorType);
        log.setDescription(description);
        log.setIsAbnormal(isAbnormal ? 1 : 0);
        behaviorLogService.saveLog(log);

        boolean statusChanged = false;

        if (isAbnormal) {
            patient.setStatus("abnormal");
            // 如果传入了severity则使用，否则根据behaviorType自动设定
            if (severity != null) {
                patient.setSeverity(severity);
            } else {
                patient.setSeverity(getDefaultSeverity(behaviorType));
            }
            patientMapper.updateById(patient);
            statusChanged = true;
        } else {
            if (!"abnormal".equals(patient.getStatus())) {
                patient.setStatus("normal");
                patient.setSeverity(0);
                patientMapper.updateById(patient);
                statusChanged = true;
            }
        }

        // 状态改变后广播该病人的最新状态（单条记录）
        if (statusChanged) {
            Map<String, Object> patientData = new HashMap<>();
            patientData.put("patient", patient);
            patientData.put("logs", behaviorLogMapper.selectTop10ByPatientId(patientId));
            broadcastPatientUpdate(patientData);
        }

        return patient;
    }

    private int getDefaultSeverity(String behaviorType) {
        return switch (behaviorType) {
            case "跌倒" -> 3;
            case "心率异常" -> 2;
            case "离床" -> 1;
            default -> 1;
        };
    }

    @Override
    public List<Map<String, Object>> getAbnormalPatientsWithLogs() {
        QueryWrapper<Patient> wrapper = new QueryWrapper<>();
        wrapper.ne("status", "normal").orderByDesc("severity");
        List<Patient> patients = patientMapper.selectList(wrapper);

        List<Map<String, Object>> result = new ArrayList<>();
        for (Patient patient : patients) {
            Map<String, Object> map = new HashMap<>();
            map.put("patient", patient);
            map.put("logs", behaviorLogMapper.selectTop10ByPatientId(patient.getId()));
            result.add(map);
        }
        return result;
    }

    @Override
    public List<Map<String, Object>> getAllBehaviorLogsByPatientId(Long id) {
        QueryWrapper<BehaviorLog> wrapper = new QueryWrapper<>();
        wrapper.eq("patient_id", id).orderByDesc("record_time");
        List<BehaviorLog> logs = behaviorLogMapper.selectList(wrapper);
        
        List<Map<String, Object>> result = new ArrayList<>();
        for (BehaviorLog log : logs) {
            Map<String, Object> map = new HashMap<>();
            map.put("id", log.getId());
            map.put("patientId", log.getPatientId());
            map.put("behaviorType", log.getBehaviorType());
            map.put("description", log.getDescription());
            map.put("isAbnormal", log.getIsAbnormal());
            map.put("recordTime", log.getRecordTime());
            result.add(map);
        }
        return result;
    }
}
