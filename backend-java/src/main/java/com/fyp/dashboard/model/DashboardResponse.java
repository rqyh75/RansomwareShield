package com.fyp.dashboard.model;

import java.util.List;
import java.util.Map;

public class DashboardResponse {
    private int totalAlerts;
    private int criticalAlerts;
    private int highRiskAlerts;
    private int affectedHosts;

    private String topRiskHost;
    private String topRule;
    private String topSource;

    private Map<String, Integer> responseSummary;
    private Map<String, Integer> sourceBreakdown;
    private Map<String, Integer> severityBreakdown;

    private List<String> timelineLabels;
    private List<Integer> timelineCounts;

    private List<RecentAlert> recentAlerts;

    public DashboardResponse() {}

    public DashboardResponse(int totalAlerts, int criticalAlerts, int highRiskAlerts, int affectedHosts,
                             String topRiskHost, String topRule, String topSource,
                             Map<String, Integer> responseSummary,
                             Map<String, Integer> sourceBreakdown,
                             Map<String, Integer> severityBreakdown,
                             List<String> timelineLabels,
                             List<Integer> timelineCounts,
                             List<RecentAlert> recentAlerts) {
        this.totalAlerts = totalAlerts;
        this.criticalAlerts = criticalAlerts;
        this.highRiskAlerts = highRiskAlerts;
        this.affectedHosts = affectedHosts;
        this.topRiskHost = topRiskHost;
        this.topRule = topRule;
        this.topSource = topSource;
        this.responseSummary = responseSummary;
        this.sourceBreakdown = sourceBreakdown;
        this.severityBreakdown = severityBreakdown;
        this.timelineLabels = timelineLabels;
        this.timelineCounts = timelineCounts;
        this.recentAlerts = recentAlerts;
    }

    public int getTotalAlerts() { return totalAlerts; }
    public void setTotalAlerts(int totalAlerts) { this.totalAlerts = totalAlerts; }
    public int getCriticalAlerts() { return criticalAlerts; }
    public void setCriticalAlerts(int criticalAlerts) { this.criticalAlerts = criticalAlerts; }
    public int getHighRiskAlerts() { return highRiskAlerts; }
    public void setHighRiskAlerts(int highRiskAlerts) { this.highRiskAlerts = highRiskAlerts; }
    public int getAffectedHosts() { return affectedHosts; }
    public void setAffectedHosts(int affectedHosts) { this.affectedHosts = affectedHosts; }
    public String getTopRiskHost() { return topRiskHost; }
    public void setTopRiskHost(String topRiskHost) { this.topRiskHost = topRiskHost; }
    public String getTopRule() { return topRule; }
    public void setTopRule(String topRule) { this.topRule = topRule; }
    public String getTopSource() { return topSource; }
    public void setTopSource(String topSource) { this.topSource = topSource; }
    public Map<String, Integer> getResponseSummary() { return responseSummary; }
    public void setResponseSummary(Map<String, Integer> responseSummary) { this.responseSummary = responseSummary; }
    public Map<String, Integer> getSourceBreakdown() { return sourceBreakdown; }
    public void setSourceBreakdown(Map<String, Integer> sourceBreakdown) { this.sourceBreakdown = sourceBreakdown; }
    public Map<String, Integer> getSeverityBreakdown() { return severityBreakdown; }
    public void setSeverityBreakdown(Map<String, Integer> severityBreakdown) { this.severityBreakdown = severityBreakdown; }
    public List<String> getTimelineLabels() { return timelineLabels; }
    public void setTimelineLabels(List<String> timelineLabels) { this.timelineLabels = timelineLabels; }
    public List<Integer> getTimelineCounts() { return timelineCounts; }
    public void setTimelineCounts(List<Integer> timelineCounts) { this.timelineCounts = timelineCounts; }
    public List<RecentAlert> getRecentAlerts() { return recentAlerts; }
    public void setRecentAlerts(List<RecentAlert> recentAlerts) { this.recentAlerts = recentAlerts; }
}
