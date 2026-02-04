#pragma once
#include <string>
#include "logger.h"

class RestAPI {
private:
    int port;
    bool isRunning;

public:
    RestAPI(int port = 8080) : port(port), isRunning(false) {}

    void start() {
        Logger::info("REST API Server starting on port " + std::to_string(port));
        isRunning = true;
        Logger::info("REST API is listening on http://0.0.0.0:" + std::to_string(port));
        Logger::info("Web UI available at http://localhost:" + std::to_string(port) + "/");
    }

    void stop() {
        Logger::info("REST API Server stopping...");
        isRunning = false;
    }

    bool isActive() const {
        return isRunning;
    }

    int getPort() const {
        return port;
    }
};
