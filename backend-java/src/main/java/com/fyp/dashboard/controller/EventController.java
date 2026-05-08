package com.fyp.dashboard.controller;

import java.util.List;

import org.springframework.web.bind.annotation.CrossOrigin;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import com.fyp.dashboard.model.EventsResponse;
import com.fyp.dashboard.model.SecurityEvent;
import com.fyp.dashboard.service.EventStoreService;

@RestController
@RequestMapping("/api/events")
@CrossOrigin
public class EventController {
    private final EventStoreService eventStoreService;

    public EventController(EventStoreService eventStoreService) {
        this.eventStoreService = eventStoreService;
    }

    @GetMapping
    public EventsResponse getEvents() {
        return new EventsResponse(eventStoreService.getLiveEventsLast24Hours());
    }

    @PostMapping
    public EventsResponse receiveEvents(@RequestBody Object payload) {
        List<SecurityEvent> savedEvents = eventStoreService.addFromPayload(payload);
        return new EventsResponse(savedEvents);
    }

    @DeleteMapping
    public EventsResponse clearEvents() {
        eventStoreService.clearLiveEvents();
        return new EventsResponse(List.of());
    }
}
