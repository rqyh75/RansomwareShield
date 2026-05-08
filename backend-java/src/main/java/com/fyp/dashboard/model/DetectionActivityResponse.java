package com.fyp.dashboard.model;

import java.util.List;
import java.util.Map;

public class DetectionActivityResponse {
    private Map<String, Integer> summary;
    private List<DetectionActivityItem> items;

    public DetectionActivityResponse() {}

    public DetectionActivityResponse(Map<String, Integer> summary, List<DetectionActivityItem> items) {
        this.summary = summary;
        this.items = items;
    }

    public Map<String, Integer> getSummary() { return summary; }
    public void setSummary(Map<String, Integer> summary) { this.summary = summary; }
    public List<DetectionActivityItem> getItems() { return items; }
    public void setItems(List<DetectionActivityItem> items) { this.items = items; }
}
