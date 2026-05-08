package com.fyp.dashboard.service;

import java.time.Instant;
import java.time.ZoneOffset;
import java.time.format.DateTimeFormatter;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

import org.springframework.stereotype.Service;

import com.fyp.dashboard.model.DashboardResponse;
import com.fyp.dashboard.model.RecentAlert;
import com.fyp.dashboard.model.SecurityEvent;

@Service
public class DashboardService {
    private final EventStoreService eventStoreService;
    private final EventQueryService eventQueryService;

    public DashboardService(EventStoreService eventStoreService, EventQueryService eventQueryService) {
        this.eventStoreService = eventStoreService;
        this.eventQueryService = eventQueryService;
    }

    public DashboardResponse buildDashboardResponse() {
        List<SecurityEvent> liveEvents = eventStoreService.getLiveEventsLast24Hours();

    return buildResponseFromEvents(liveEvents, true);
    }

private DashboardResponse buildResponseFromEvents(List<SecurityEvent> inputEvents, boolean hourlyTimeline) {
    List<SecurityEvent> alerts = eventQueryService.alertEvents(inputEvents);

    int totalAlerts = alerts.size();
    int criticalAlerts = countBySeverity(alerts, "critical");
    int highRiskAlerts = (int) alerts.stream()
            .filter(event -> {
                String severity = eventQueryService.safeLower(event.getSeverity());
                return severity.equals("critical") || severity.equals("high");
            })
            .count();

    int affectedHosts = (int) alerts.stream()
            .map(SecurityEvent::getHostname)
            .filter(value -> value != null && !value.isBlank())
            .distinct()
            .count();

    String topRiskHost = getTopValue(alerts.stream()
            .filter(event -> event.getHostname() != null && !event.getHostname().isBlank())
            .collect(Collectors.groupingBy(
                    SecurityEvent::getHostname,
                    Collectors.summingInt(event -> severityWeight(event.getSeverity()))
            )));

    String topRule = getTopValue(alerts.stream()
            .filter(event -> event.getRule_name() != null && !event.getRule_name().isBlank())
            .collect(Collectors.groupingBy(
                    SecurityEvent::getRule_name,
                    Collectors.summingInt(event -> severityWeight(event.getSeverity()))
            )));

    String topSource = getTopValue(alerts.stream()
            .filter(event -> event.getSource() != null && !event.getSource().isBlank())
            .collect(Collectors.groupingBy(
                    event -> eventQueryService.safeLower(event.getSource()),
                    Collectors.summingInt(event -> 1)
            )));

    Map<String, Integer> responseSummary = new LinkedHashMap<>();
    responseSummary.put("terminate_process", countByResponse(alerts, "terminate_process"));
    responseSummary.put("alert_only", countByResponse(alerts, "alert_only"));
    responseSummary.put("logged_only", countByResponse(alerts, "logged_only"));
    responseSummary.put("blocked", countByResponse(alerts, "blocked"));

    Map<String, Integer> sourceBreakdown = new LinkedHashMap<>();
    sourceBreakdown.put("canary", countBySource(alerts, "canary"));
    sourceBreakdown.put("etw", countBySource(alerts, "etw"));
    sourceBreakdown.put("minifilter", countBySource(alerts, "minifilter"));
    sourceBreakdown.put("response-agent", countBySource(alerts, "response-agent"));

    Map<String, Integer> severityBreakdown = new LinkedHashMap<>();
    severityBreakdown.put("critical", countBySeverity(alerts, "critical"));
    severityBreakdown.put("high", countBySeverity(alerts, "high"));
    severityBreakdown.put("medium", countBySeverity(alerts, "medium"));
    severityBreakdown.put("low", countBySeverity(alerts, "low"));

    List<String> timelineLabels = new ArrayList<>();
    List<Integer> timelineCounts = new ArrayList<>();

    if (hourlyTimeline) {
        buildLast24HourTimeline(alerts, timelineLabels, timelineCounts);
    } else {
        buildDailyTimeline(alerts, timelineLabels, timelineCounts);
    }

    List<RecentAlert> recentAlerts = alerts.stream()
            .sorted(compareByTimestampDescending())
            .limit(20)
            .map(this::toRecentAlert)
            .toList();

    return new DashboardResponse(
            totalAlerts,
            criticalAlerts,
            highRiskAlerts,
            affectedHosts,
            topRiskHost,
            topRule,
            topSource,
            responseSummary,
            sourceBreakdown,
            severityBreakdown,
            timelineLabels,
            timelineCounts,
            recentAlerts
    );
}

private boolean matchesText(String actual, String filter) {
    if (filter == null || filter.isBlank() || filter.equalsIgnoreCase("all")) {
        return true;
    }

    if (actual == null) {
        return false;
    }

    return actual.equalsIgnoreCase(filter);
}

private boolean matchesDateRange(SecurityEvent event, String from, String to) {
    Instant eventTime = eventQueryService.parseInstant(event.getTimestamp()).orElse(null);

    if (eventTime == null) {
        return true;
    }

    try {
        if (from != null && !from.isBlank()) {
            Instant fromTime = Instant.parse(from + "T00:00:00Z");
            if (eventTime.isBefore(fromTime)) {
                return false;
            }
        }

        if (to != null && !to.isBlank()) {
            Instant toTime = Instant.parse(to + "T23:59:59Z");
            if (eventTime.isAfter(toTime)) {
                return false;
            }
        }
    } catch (Exception e) {
        return true;
    }

    return true;
}

private void buildDailyTimeline(List<SecurityEvent> events, List<String> labels, List<Integer> counts) {
    Instant now = Instant.now();

    DateTimeFormatter formatter = DateTimeFormatter
            .ofPattern("MMM dd")
            .withZone(ZoneOffset.UTC);

    for (int i = 6; i >= 0; i--) {
        Instant dayStart = now.minusSeconds(i * 86400L)
                .atZone(ZoneOffset.UTC)
                .withHour(0)
                .withMinute(0)
                .withSecond(0)
                .withNano(0)
                .toInstant();

        Instant dayEnd = dayStart.plusSeconds(86400L);

        labels.add(formatter.format(dayStart));

        int count = (int) events.stream()
                .filter(event -> eventQueryService.parseInstant(event.getTimestamp())
                        .map(time -> !time.isBefore(dayStart) && time.isBefore(dayEnd))
                        .orElse(false))
                .count();

        counts.add(count);
    }
}

    private void buildLast24HourTimeline(List<SecurityEvent> events, List<String> labels, List<Integer> counts) {
        Instant now = Instant.now();
        Instant start = now.minusSeconds(23 * 3600L).atZone(ZoneOffset.UTC)
                .withMinute(0).withSecond(0).withNano(0).toInstant();

        DateTimeFormatter formatter = DateTimeFormatter.ofPattern("HH:00").withZone(ZoneOffset.UTC);
        for (int i = 0; i < 24; i++) {
            Instant bucketStart = start.plusSeconds(i * 3600L);
            Instant bucketEnd = bucketStart.plusSeconds(3600L);
            labels.add(formatter.format(bucketStart));
            int count = (int) events.stream()
                    .filter(event -> eventQueryService.parseInstant(event.getTimestamp())
                            .map(time -> !time.isBefore(bucketStart) && time.isBefore(bucketEnd))
                            .orElse(false))
                    .count();
            counts.add(count);
        }
    }

    private RecentAlert toRecentAlert(SecurityEvent event) {
        return new RecentAlert(
                event.getId(),
                eventQueryService.capitalize(event.getSeverity()),
                event.getRule_name(),
                event.getTimestamp(),
                event.getHostname(),
                eventQueryService.safeLower(event.getSource()),
                event.getResponse_taken(),
                eventQueryService.getProcessName(event),
                eventQueryService.getParentProcessName(event)
        );
    }

    private Comparator<SecurityEvent> compareByTimestampDescending() {
        return (a, b) -> {
            Instant ia = eventQueryService.parseInstant(a.getTimestamp()).orElse(Instant.EPOCH);
            Instant ib = eventQueryService.parseInstant(b.getTimestamp()).orElse(Instant.EPOCH);
            return ib.compareTo(ia);
        };
    }

    private int countBySeverity(List<SecurityEvent> events, String severity) {
        return (int) events.stream()
                .filter(event -> eventQueryService.safeLower(event.getSeverity()).equals(severity))
                .count();
    }

    private int countBySource(List<SecurityEvent> events, String source) {
        return (int) events.stream()
                .filter(event -> eventQueryService.safeLower(event.getSource()).equals(source))
                .count();
    }

    private int countByResponse(List<SecurityEvent> events, String response) {
        return (int) events.stream()
                .filter(event -> eventQueryService.safeLower(event.getResponse_taken()).equals(response))
                .count();
    }

    private String getTopValue(Map<String, Integer> map) {
        return map.entrySet().stream()
                .max(Map.Entry.comparingByValue())
                .map(Map.Entry::getKey)
                .orElse("-");
    }

    private int severityWeight(String severity) {
        return switch (eventQueryService.safeLower(severity)) {
            case "critical" -> 4;
            case "high" -> 3;
            case "medium" -> 2;
            case "low" -> 1;
            default -> 0;
        };
    }

    public DashboardResponse buildReportResponse(
        String from,
        String to,
        String severity,
        String hostname,
        String source
) {
    List<SecurityEvent> allEvents = eventStoreService.getArchivedEvents();

    List<SecurityEvent> filteredEvents = allEvents.stream()
            .filter(event -> matchesDateRange(event, from, to))
            .filter(event -> matchesText(event.getSeverity(), severity))
            .filter(event -> matchesText(event.getHostname(), hostname))
            .filter(event -> matchesText(event.getSource(), source))
            .toList();

    return buildResponseFromEvents(filteredEvents, false);
}
}
