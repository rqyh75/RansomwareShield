package com.fyp.dashboard.model;

import java.util.Map;

public class AlertItem extends SecurityEvent {
    public AlertItem() {}

    public AlertItem(String id, String timestamp, String hostname, String source,
                     String severity, String rule_name, String response_taken,
                     Map<String, Object> data) {
        setId(id);
        setTimestamp(timestamp);
        setHostname(hostname);
        setSource(source);
        setSeverity(severity);
        setRule_name(rule_name);
        setResponse_taken(response_taken);
        setData(data);
    }
}
