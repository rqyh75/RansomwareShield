package com.fyp.dashboard.controller;

import java.time.Instant;
import java.util.Comparator;
import java.util.List;

import org.springframework.web.bind.annotation.CrossOrigin;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import com.fyp.dashboard.model.SecurityEvent;
import com.fyp.dashboard.model.StatusResponse;
import com.fyp.dashboard.service.EventQueryService;
import com.fyp.dashboard.service.EventStoreService;

@RestController
@RequestMapping("/api/status")
@CrossOrigin
public class StatusController {
    private final EventStoreService eventStoreService;
    private final EventQueryService eventQueryService;

    public StatusController(EventStoreService eventStoreService, EventQueryService eventQueryService) {
        this.eventStoreService = eventStoreService;
        this.eventQueryService = eventQueryService;
    }

    @GetMapping
    public StatusResponse getStatus() {
        List<SecurityEvent> events = eventStoreService.getLiveEventsLast24Hours();
        boolean ransomwareDetected = eventQueryService.alertEvents(events).stream()
                .anyMatch(event -> {
                    String severity = eventQueryService.safeLower(event.getSeverity());
                    return severity.equals("critical") || severity.equals("high");
                });

        String lastUpdate = events.stream()
                .map(SecurityEvent::getTimestamp)
                .filter(value -> value != null && !value.isBlank())
                .max(Comparator.comparing(value -> eventQueryService.parseInstant(value).orElse(Instant.EPOCH)))
                .orElse("connected");

        String systemStatus = events.isEmpty()
                ? "Waiting for response-agent events"
                : "Receiving response-agent events";

        return new StatusResponse(true, systemStatus, ransomwareDetected, lastUpdate);
    }
}
