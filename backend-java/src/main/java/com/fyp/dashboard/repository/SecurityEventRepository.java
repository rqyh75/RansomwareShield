package com.fyp.dashboard.repository;

import com.fyp.dashboard.model.SecurityEvent;
import org.springframework.data.mongodb.repository.MongoRepository;

import java.util.List;

public interface SecurityEventRepository extends MongoRepository<SecurityEvent, String> {
    List<SecurityEvent> findTop50ByOrderByTimestampDesc();
}