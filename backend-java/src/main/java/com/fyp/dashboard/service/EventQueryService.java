package com.fyp.dashboard.service;

import com.fyp.dashboard.model.SecurityEvent;
import org.springframework.stereotype.Service;

import java.time.Duration;
import java.time.Instant;
import java.util.List;
import java.util.Locale;
import java.util.Map;

@Service
public class EventQueryService {
    public List<SecurityEvent> alertEvents(List<SecurityEvent> events) {
        return events.stream()
                .filter(this::isAlert)
                .toList();
    }

    public List<SecurityEvent> eventsWithinLast24Hours(List<SecurityEvent> events) {
        Instant cutoff = Instant.now().minus(Duration.ofHours(24));
        return events.stream()
                .filter(event -> parseInstant(event.getTimestamp()).map(t -> !t.isBefore(cutoff)).orElse(true))
                .toList();
    }

    public boolean isAlert(SecurityEvent event) {
        String severity = safeLower(event.getSeverity());
        String response = safeLower(event.getResponse_taken());
        return severity.equals("critical")
                || severity.equals("high")
                || severity.equals("medium")
                || response.equals("terminate_process")
                || response.equals("blocked")
                || response.equals("quarantine")
                || response.equals("alert_only");
    }

    public String getProcessName(SecurityEvent event) {
        return value(event, "process_name", "processName", "process", "image", "file_name", "fileName");
    }

    public String getParentProcessName(SecurityEvent event) {
        return value(event, "parent_process_name", "parentProcessName", "parent_process", "parentProcess", "parent_image", "parentImage");
    }

    public String getPath(SecurityEvent event) {
        return value(event, "file_path", "filePath", "path", "affected_path", "affectedPath", "target", "target_path", "targetPath");
    }

    public String value(SecurityEvent event, String... keys) {
        String fromData = valueFromMap(event.getData(), keys);
        if (!fromData.isBlank()) return fromData;
        return valueFromMap(event.getRaw(), keys);
    }

    public java.util.Optional<Instant> parseInstant(String timestamp) {
        if (timestamp == null || timestamp.isBlank()) {
            return java.util.Optional.empty();
        }
        try {
            return java.util.Optional.of(Instant.parse(timestamp));
        } catch (Exception ignored) {
            return java.util.Optional.empty();
        }
    }

    public String safeLower(String value) {
        return value == null ? "" : value.toLowerCase(Locale.ROOT);
    }

    public String capitalize(String value) {
        if (value == null || value.isBlank()) return "";
        return value.substring(0, 1).toUpperCase(Locale.ROOT) + value.substring(1).toLowerCase(Locale.ROOT);
    }

    private String valueFromMap(Map<String, Object> map, String... keys) {
        if (map == null) return "";
        for (String key : keys) {
            Object value = map.get(key);
            if (value != null && !String.valueOf(value).isBlank()) {
                return String.valueOf(value);
            }
        }
        return "";
    }
}
