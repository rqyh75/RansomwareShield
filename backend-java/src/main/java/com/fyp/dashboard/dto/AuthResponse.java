package com.fyp.dashboard.dto;

public class AuthResponse {
    private boolean success;
    private String message;
    private String username;
    private String role;

    public AuthResponse(boolean success, String message, String username, String role) {
        this.success = success;
        this.message = message;
        this.username = username;
        this.role = role;
    }

    public boolean isSuccess() {
        return success;
    }

    public String getMessage() {
        return message;
    }

    public String getUsername() {
        return username;
    }

    public String getRole() {
        return role;
    }
}