package com.fyp.dashboard.service;

import java.time.Duration;
import java.time.Instant;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.concurrent.atomic.AtomicInteger;

import org.springframework.stereotype.Service;

import com.fyp.dashboard.model.DetectionActivityItem;
import com.fyp.dashboard.model.DetectionActivityResponse;
import com.fyp.dashboard.model.SecurityEvent;

@Service
public class DetectionActivityService {
    private final EventStoreService eventStoreService;
    private final EventQueryService eventQueryService;

    public DetectionActivityService(EventStoreService eventStoreService, EventQueryService eventQueryService) {
        this.eventStoreService = eventStoreService;
        this.eventQueryService = eventQueryService;
    }

    public DetectionActivityResponse buildDetectionActivity() {
        List<SecurityEvent> events = eventStoreService.getLiveEventsLast24Hours();
        List<SecurityEvent> last24Hours = eventQueryService.eventsWithinLast24Hours(events);
        AtomicInteger rowNumber = new AtomicInteger(1);

        List<DetectionActivityItem> items = last24Hours.stream()
                .limit(50)
                .map(event -> toItem(rowNumber.getAndIncrement(), event))
                .toList();

        Map<String, Integer> summary = new LinkedHashMap<>();
        summary.put("monitoredProcesses", countWithProcess(last24Hours));
        summary.put("filesSeen", countWithPath(last24Hours));
        summary.put("threatEvents", eventQueryService.alertEvents(last24Hours).size());

        return new DetectionActivityResponse(summary, items);
    }

    private DetectionActivityItem toItem(int id, SecurityEvent event) {
        return new DetectionActivityItem(
                id,
                event.getId(),
                relativeTime(event.getTimestamp()),
                firstNonBlank(event.getEvent_type(), event.getSource(), "event"),
                statusFor(event),
                firstNonBlank(event.getRule_name(), event.getResponse_taken(), "activity recorded"),
                eventQueryService.getPath(event),
                actionFor(event),
                eventQueryService.getProcessName(event),
                eventQueryService.getParentProcessName(event)
        );
    }

    private String statusFor(SecurityEvent event) {
        String severity = eventQueryService.safeLower(event.getSeverity());
        String response = eventQueryService.safeLower(event.getResponse_taken());
        if (response.equals("blocked") || response.equals("terminate_process") || severity.equals("critical") || severity.equals("high")) {
            return "blocked";
        }
        if (severity.equals("medium")) {
            return "suspicious";
        }
        return "normal";
    }

    private String actionFor(SecurityEvent event) {
        String status = statusFor(event);
        if (status.equals("blocked")) return "review";
        if (status.equals("suspicious")) return "inspect";
        return "view";
    }

    private String relativeTime(String timestamp) {
        Instant time = eventQueryService.parseInstant(timestamp).orElse(null);
        if (time == null) return "unknown";

        long minutes = Duration.between(time, Instant.now()).toMinutes();
        if (minutes < 1) return "just now";
        if (minutes < 60) return minutes + " min ago";
        long hours = minutes / 60;
        if (hours < 24) return hours + " hour" + (hours == 1 ? "" : "s") + " ago";
        long days = hours / 24;
        return days + " day" + (days == 1 ? "" : "s") + " ago";
    }

    private int countWithProcess(List<SecurityEvent> events) {
        return (int) events.stream()
                .map(eventQueryService::getProcessName)
                .filter(value -> value != null && !value.isBlank())
                .distinct()
                .count();
    }

    private int countWithPath(List<SecurityEvent> events) {
        return (int) events.stream()
                .map(eventQueryService::getPath)
                .filter(value -> value != null && !value.isBlank())
                .distinct()
                .count();
    }

    private String firstNonBlank(String... values) {
        for (String value : values) {
            if (value != null && !value.isBlank()) return value;
        }
        return "";
    }
}
