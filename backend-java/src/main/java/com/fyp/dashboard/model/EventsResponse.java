package com.fyp.dashboard.model;

import java.util.List;

public class EventsResponse {
    private List<SecurityEvent> items;
    private int count;

    public EventsResponse() {}

    public EventsResponse(List<SecurityEvent> items) {
        this.items = items;
        this.count = items == null ? 0 : items.size();
    }

    public List<SecurityEvent> getItems() { return items; }
    public void setItems(List<SecurityEvent> items) {
        this.items = items;
        this.count = items == null ? 0 : items.size();
    }

    public int getCount() { return count; }
    public void setCount(int count) { this.count = count; }
}
