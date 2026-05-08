package com.fyp.dashboard.model;

import java.util.LinkedHashMap;
import java.util.Map;

import org.springframework.data.annotation.Id;
import org.springframework.data.mongodb.core.mapping.Document;

@Document(collection = "security_events")
public class SecurityEvent {
    @Id
    private String id;
    private String timestamp;
    private String hostname;
    private String source;
    private String severity;
    private String rule_name;
    private String response_taken;
    private String event_type;
    private Map<String, Object> data = new LinkedHashMap<>();
    private Map<String, Object> raw = new LinkedHashMap<>();

    public SecurityEvent() {}

    public String getId() { return id; }
    public void setId(String id) { this.id = id; }

    public String getTimestamp() { return timestamp; }
    public void setTimestamp(String timestamp) { this.timestamp = timestamp; }

    public String getHostname() { return hostname; }
    public void setHostname(String hostname) { this.hostname = hostname; }

    public String getSource() { return source; }
    public void setSource(String source) { this.source = source; }

    public String getSeverity() { return severity; }
    public void setSeverity(String severity) { this.severity = severity; }

    public String getRule_name() { return rule_name; }
    public void setRule_name(String rule_name) { this.rule_name = rule_name; }

    public String getResponse_taken() { return response_taken; }
    public void setResponse_taken(String response_taken) { this.response_taken = response_taken; }

    public String getEvent_type() { return event_type; }
    public void setEvent_type(String event_type) { this.event_type = event_type; }

    public Map<String, Object> getData() { return data; }
    public void setData(Map<String, Object> data) {
        this.data = data == null ? new LinkedHashMap<>() : data;
    }

    public Map<String, Object> getRaw() { return raw; }
    public void setRaw(Map<String, Object> raw) {
        this.raw = raw == null ? new LinkedHashMap<>() : raw;
    }
}
