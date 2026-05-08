package com.fyp.dashboard.model;

public class RecentAlert {
    private String id;
    private String severity;
    private String title;
    private String time;
    private String hostname;
    private String source;
    private String responseTaken;
    private String processName;
    private String parentProcessName;

    public RecentAlert() {}

    public RecentAlert(String id, String severity, String title, String time,
                       String hostname, String source, String responseTaken,
                       String processName, String parentProcessName) {
        this.id = id;
        this.severity = severity;
        this.title = title;
        this.time = time;
        this.hostname = hostname;
        this.source = source;
        this.responseTaken = responseTaken;
        this.processName = processName;
        this.parentProcessName = parentProcessName;
    }

    public String getId() { return id; }
    public void setId(String id) { this.id = id; }
    public String getSeverity() { return severity; }
    public void setSeverity(String severity) { this.severity = severity; }
    public String getTitle() { return title; }
    public void setTitle(String title) { this.title = title; }
    public String getTime() { return time; }
    public void setTime(String time) { this.time = time; }
    public String getHostname() { return hostname; }
    public void setHostname(String hostname) { this.hostname = hostname; }
    public String getSource() { return source; }
    public void setSource(String source) { this.source = source; }
    public String getResponseTaken() { return responseTaken; }
    public void setResponseTaken(String responseTaken) { this.responseTaken = responseTaken; }
    public String getProcessName() { return processName; }
    public void setProcessName(String processName) { this.processName = processName; }
    public String getParentProcessName() { return parentProcessName; }
    public void setParentProcessName(String parentProcessName) { this.parentProcessName = parentProcessName; }
}
