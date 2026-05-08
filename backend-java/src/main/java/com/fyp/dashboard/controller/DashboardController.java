package com.fyp.dashboard.controller;

import org.springframework.web.bind.annotation.CrossOrigin;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import com.fyp.dashboard.model.DashboardResponse;
import com.fyp.dashboard.service.DashboardService;

@RestController
@RequestMapping("/api")
@CrossOrigin
public class DashboardController {
    private final DashboardService dashboardService;

    public DashboardController(DashboardService dashboardService) {
        this.dashboardService = dashboardService;
    }

    @GetMapping("/dashboard")
    public DashboardResponse getDashboard() {
        return dashboardService.buildDashboardResponse();
    }

    @GetMapping("/reports")
public DashboardResponse getReports(
        @RequestParam(required = false) String from,
        @RequestParam(required = false) String to,
        @RequestParam(required = false) String severity,
        @RequestParam(required = false) String hostname,
        @RequestParam(required = false) String source
) {
    return dashboardService.buildReportResponse(from, to, severity, hostname, source);
}
}
