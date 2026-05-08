package com.fyp.dashboard.model;

public class StatusResponse {
    private boolean monitoring;
    private String systemStatus;
    private boolean ransomwareDetected;
    private String lastUpdate;

    public StatusResponse() {}

    public StatusResponse(boolean monitoring, String systemStatus, boolean ransomwareDetected, String lastUpdate) {
        this.monitoring = monitoring;
        this.systemStatus = systemStatus;
        this.ransomwareDetected = ransomwareDetected;
        this.lastUpdate = lastUpdate;
    }

    public boolean isMonitoring() { return monitoring; }
    public void setMonitoring(boolean monitoring) { this.monitoring = monitoring; }
    public String getSystemStatus() { return systemStatus; }
    public void setSystemStatus(String systemStatus) { this.systemStatus = systemStatus; }
    public boolean isRansomwareDetected() { return ransomwareDetected; }
    public void setRansomwareDetected(boolean ransomwareDetected) { this.ransomwareDetected = ransomwareDetected; }
    public String getLastUpdate() { return lastUpdate; }
    public void setLastUpdate(String lastUpdate) { this.lastUpdate = lastUpdate; }
}
