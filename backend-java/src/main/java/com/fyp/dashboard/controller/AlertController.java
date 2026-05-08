package com.fyp.dashboard.controller;

import java.util.List;

import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import com.fyp.dashboard.model.AlertsResponse;
import com.fyp.dashboard.model.SecurityEvent;
import com.fyp.dashboard.service.EventQueryService;
import com.fyp.dashboard.service.EventStoreService;

@RestController
@RequestMapping("/api/alerts")
public class AlertController {
    private final EventStoreService eventStoreService;
    private final EventQueryService eventQueryService;

    public AlertController(EventStoreService eventStoreService, EventQueryService eventQueryService) {
        this.eventStoreService = eventStoreService;
        this.eventQueryService = eventQueryService;
    }

    @GetMapping
    public AlertsResponse getAlerts() {
        List<SecurityEvent> alerts = eventQueryService.alertEvents(
                eventStoreService.getLiveEventsLast24Hours()
        );

        return new AlertsResponse(alerts);
    }
}