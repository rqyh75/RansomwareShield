package com.fyp.dashboard.model;

public class DetectionActivityItem {
    private int id;
    private String eventId;
    private String time;
    private String type;
    private String status;
    private String lastAction;
    private String path;
    private String action;
    private String processName;
    private String parentProcessName;

    public DetectionActivityItem() {}

    public DetectionActivityItem(int id, String eventId, String time, String type, String status,
                                 String lastAction, String path, String action,
                                 String processName, String parentProcessName) {
        this.id = id;
        this.eventId = eventId;
        this.time = time;
        this.type = type;
        this.status = status;
        this.lastAction = lastAction;
        this.path = path;
        this.action = action;
        this.processName = processName;
        this.parentProcessName = parentProcessName;
    }

    public int getId() { return id; }
    public void setId(int id) { this.id = id; }
    public String getEventId() { return eventId; }
    public void setEventId(String eventId) { this.eventId = eventId; }
    public String getTime() { return time; }
    public void setTime(String time) { this.time = time; }
    public String getType() { return type; }
    public void setType(String type) { this.type = type; }
    public String getStatus() { return status; }
    public void setStatus(String status) { this.status = status; }
    public String getLastAction() { return lastAction; }
    public void setLastAction(String lastAction) { this.lastAction = lastAction; }
    public String getPath() { return path; }
    public void setPath(String path) { this.path = path; }
    public String getAction() { return action; }
    public void setAction(String action) { this.action = action; }
    public String getProcessName() { return processName; }
    public void setProcessName(String processName) { this.processName = processName; }
    public String getParentProcessName() { return parentProcessName; }
    public void setParentProcessName(String parentProcessName) { this.parentProcessName = parentProcessName; }
}
