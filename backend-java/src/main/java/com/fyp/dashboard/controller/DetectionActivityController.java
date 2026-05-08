package com.fyp.dashboard.controller;

import com.fyp.dashboard.model.DetectionActivityResponse;
import com.fyp.dashboard.service.DetectionActivityService;
import org.springframework.web.bind.annotation.CrossOrigin;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/detection-activity")
@CrossOrigin
public class DetectionActivityController {
    private final DetectionActivityService detectionActivityService;

    public DetectionActivityController(DetectionActivityService detectionActivityService) {
        this.detectionActivityService = detectionActivityService;
    }

    @GetMapping
    public DetectionActivityResponse getDetectionActivity() {
        return detectionActivityService.buildDetectionActivity();
    }
}
