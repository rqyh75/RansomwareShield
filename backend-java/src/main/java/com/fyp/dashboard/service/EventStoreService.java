package com.fyp.dashboard.service;

import java.time.Instant;
import java.util.ArrayList;
import java.util.Collections;
import java.util.LinkedHashMap;
import java.util.LinkedList;
import java.util.List;
import java.util.Map;

import org.springframework.stereotype.Service;

import com.fyp.dashboard.model.SecurityEvent;
import com.fyp.dashboard.repository.SecurityEventRepository;

@Service
public class EventStoreService {
    private final List<SecurityEvent> liveEvents = Collections.synchronizedList(new LinkedList<>());
    private final SecurityEventRepository securityEventRepository;

    public EventStoreService(SecurityEventRepository securityEventRepository) {
        this.securityEventRepository = securityEventRepository;
    }

    public SecurityEvent addEvent(SecurityEvent event) {
        SecurityEvent normalized = normalize(event);

        liveEvents.add(0, normalized);

        SecurityEvent saved = securityEventRepository.save(normalized);

        return saved;
    }

    public SecurityEvent addFromMap(Map<String, Object> payload) {
        return addEvent(fromMap(payload));
    }

    @SuppressWarnings("unchecked")
    public List<SecurityEvent> addFromPayload(Object payload) {
        List<SecurityEvent> saved = new ArrayList<>();

        if (payload instanceof List<?> list) {
            for (Object item : list) {
                if (item instanceof Map<?, ?> map) {
                    saved.add(addFromMap((Map<String, Object>) map));
                }
            }
            return saved;
        }

        if (payload instanceof Map<?, ?> map) {
            Map<String, Object> typedMap = (Map<String, Object>) map;
            Object items = firstValue(typedMap, "items", "events", "alerts", "data");

            if (items instanceof List<?> itemList) {
                for (Object item : itemList) {
                    if (item instanceof Map<?, ?> itemMap) {
                        saved.add(addFromMap((Map<String, Object>) itemMap));
                    }
                }

                if (!saved.isEmpty()) {
                    return saved;
                }
            }

            saved.add(addFromMap(typedMap));
        }

        return saved;
    }

    public List<SecurityEvent> getLiveEvents() {
        synchronized (liveEvents) {
            return new ArrayList<>(liveEvents);
        }
    }

    public List<SecurityEvent> getLiveEventsLast24Hours() {
        Instant cutoff = Instant.now().minusSeconds(24 * 60 * 60);

        synchronized (liveEvents) {
            return liveEvents.stream()
                    .filter(event -> parseInstant(event.getTimestamp())
                            .map(time -> !time.isBefore(cutoff))
                            .orElse(false))
                    .toList();
        }
    }

    public List<SecurityEvent> getArchivedEvents() {
        return securityEventRepository.findAll()
                .stream()
                .sorted((a, b) -> safe(b.getTimestamp()).compareTo(safe(a.getTimestamp())))
                .toList();
    }

    public List<SecurityEvent> getRecentLiveEvents(int limit) {
        return getLiveEvents()
                .stream()
                .limit(limit)
                .toList();
    }

    public void clearLiveEvents() {
        liveEvents.clear();
    }

    public void clearArchivedEvents() {
        securityEventRepository.deleteAll();
    }

    private SecurityEvent normalize(SecurityEvent event) {
        if (event.getTimestamp() == null || event.getTimestamp().isBlank()) {
            event.setTimestamp(Instant.now().toString());
        }

        if (event.getSeverity() == null || event.getSeverity().isBlank()) {
            event.setSeverity("low");
        }

        if (event.getSource() == null || event.getSource().isBlank()) {
            event.setSource("response-agent");
        }

        if (event.getRule_name() == null || event.getRule_name().isBlank()) {
            event.setRule_name(firstNonBlank(
                    asString(valueFrom(event, "ruleName", "rule", "title", "name", "message")),
                    "Security Event"
            ));
        }

        if (event.getResponse_taken() == null || event.getResponse_taken().isBlank()) {
            event.setResponse_taken(firstNonBlank(
                    asString(valueFrom(event, "responseTaken", "response", "action_taken", "action")),
                    "logged_only"
            ));
        }

        if (event.getEvent_type() == null || event.getEvent_type().isBlank()) {
            event.setEvent_type(firstNonBlank(
                    asString(valueFrom(event, "eventType", "type", "operation")),
                    event.getSource()
            ));
        }

        return event;
    }

    @SuppressWarnings("unchecked")
    private SecurityEvent fromMap(Map<String, Object> payload) {
        SecurityEvent event = new SecurityEvent();

        Map<String, Object> data = asMap(firstValue(payload, "data", "details", "metadata"));
        Map<String, Object> raw = new LinkedHashMap<>(payload);

        event.setId(asString(firstValue(payload, "id", "event_id", "eventId", "alert_id", "alertId")));
        event.setTimestamp(asString(firstValue(payload, "timestamp", "time", "event_time", "eventTime", "created_at", "createdAt")));
        event.setHostname(asString(firstValue(payload, "hostname", "host", "computer", "computer_name", "computerName", "machine", "device")));
        event.setSource(asString(firstValue(payload, "source", "module", "sensor")));
        event.setSeverity(asString(firstValue(payload, "severity", "level", "risk", "priority")));
        event.setRule_name(asString(firstValue(payload, "rule_name", "ruleName", "rule", "title", "name", "message")));
        event.setResponse_taken(asString(firstValue(payload, "response_taken", "responseTaken", "response", "action_taken", "actionTaken", "action")));
        event.setEvent_type(asString(firstValue(payload, "event_type", "eventType", "type", "operation", "activity_type", "activityType")));

        if (data.isEmpty()) {
            data = new LinkedHashMap<>();

            for (Map.Entry<String, Object> entry : payload.entrySet()) {
                String key = entry.getKey();

                if (!isTopLevelField(key)) {
                    data.put(key, entry.getValue());
                }
            }
        }

        event.setData(data);
        event.setRaw(raw);

        return normalize(event);
    }

    private Object valueFrom(SecurityEvent event, String... keys) {
        Object fromData = firstValue(event.getData(), keys);

        if (fromData != null) {
            return fromData;
        }

        return firstValue(event.getRaw(), keys);
    }

    private Object firstValue(Map<String, Object> map, String... keys) {
        if (map == null) return null;

        for (String key : keys) {
            if (map.containsKey(key) && map.get(key) != null) {
                return map.get(key);
            }
        }

        return null;
    }

    private String firstNonBlank(String value, String fallback) {
        return value == null || value.isBlank() ? fallback : value;
    }

    private String asString(Object value) {
        return value == null ? null : String.valueOf(value);
    }

    @SuppressWarnings("unchecked")
    private Map<String, Object> asMap(Object value) {
        if (value instanceof Map<?, ?> map) {
            return new LinkedHashMap<>((Map<String, Object>) map);
        }

        return new LinkedHashMap<>();
    }

    private boolean isTopLevelField(String key) {
        return switch (key) {
            case "id", "event_id", "eventId", "alert_id", "alertId",
                    "timestamp", "time", "event_time", "eventTime", "created_at", "createdAt",
                    "hostname", "host", "computer", "computer_name", "computerName", "machine", "device",
                    "source", "module", "sensor", "severity", "level", "risk", "priority",
                    "rule_name", "ruleName", "rule", "title", "name", "message",
                    "response_taken", "responseTaken", "response", "action_taken", "actionTaken", "action",
                    "event_type", "eventType", "type", "operation", "activity_type", "activityType",
                    "data", "details", "metadata" -> true;
            default -> false;
        };
    }

    private java.util.Optional<Instant> parseInstant(String timestamp) {
        if (timestamp == null || timestamp.isBlank()) {
            return java.util.Optional.empty();
        }

        try {
            return java.util.Optional.of(Instant.parse(timestamp));
        } catch (Exception e) {
            return java.util.Optional.empty();
        }
    }

    private String safe(String value) {
        return value == null ? "" : value;
    }
}