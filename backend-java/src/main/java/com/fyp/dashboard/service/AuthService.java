package com.fyp.dashboard.service;

import com.fyp.dashboard.dto.AuthRequest;
import com.fyp.dashboard.dto.AuthResponse;
import com.fyp.dashboard.model.User;
import com.fyp.dashboard.repository.UserRepository;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;

import java.util.Optional;

@Service
public class AuthService {
    private final UserRepository userRepository;
    private final PasswordEncoder passwordEncoder;

    public AuthService(UserRepository userRepository, PasswordEncoder passwordEncoder) {
        this.userRepository = userRepository;
        this.passwordEncoder = passwordEncoder;
    }

    public AuthResponse signup(AuthRequest request) {
        String username = request.getUsername();
        String password = request.getPassword();

        if (username == null || username.isBlank()) {
            return new AuthResponse(false, "Username is required", null, null);
        }

        if (password == null || password.isBlank()) {
            return new AuthResponse(false, "Password is required", null, null);
        }

        if (userRepository.existsByUsername(username)) {
            return new AuthResponse(false, "Username already exists", null, null);
        }

        String hashedPassword = passwordEncoder.encode(password);

        User user = new User(username, hashedPassword);
        User savedUser = userRepository.save(user);

        return new AuthResponse(
                true,
                "Signup successful",
                savedUser.getUsername(),
                savedUser.getRole()
        );
    }

    public AuthResponse login(AuthRequest request) {
        Optional<User> userOptional = userRepository.findByUsername(request.getUsername());

        if (userOptional.isEmpty()) {
            return new AuthResponse(false, "Invalid username or password", null, null);
        }

        User user = userOptional.get();

        boolean passwordMatches = passwordEncoder.matches(
                request.getPassword(),
                user.getPassword()
        );

        if (!passwordMatches) {
            return new AuthResponse(false, "Invalid username or password", null, null);
        }

        return new AuthResponse(
                true,
                "Login successful",
                user.getUsername(),
                user.getRole()
        );
    }
}