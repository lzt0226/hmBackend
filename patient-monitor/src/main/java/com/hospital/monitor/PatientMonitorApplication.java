package com.hospital.monitor;

import org.mybatis.spring.annotation.MapperScan;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.scheduling.annotation.EnableScheduling;

@SpringBootApplication
@MapperScan("com.hospital.monitor.mapper")
@EnableScheduling
public class PatientMonitorApplication {

    public static void main(String[] args) {
        SpringApplication.run(PatientMonitorApplication.class, args);
    }
}
