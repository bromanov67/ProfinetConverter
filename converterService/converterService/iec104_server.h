#pragma once
#include <string>
#include "logger.h"

class IEC104Server {
private:
    int port;
    bool isRunning;
    int clientCount;

public:
    IEC104Server(int port = 2404)
        : port(port), isRunning(false), clientCount(0) {
    }

    void start() {
        Logger::info("IEC 104 Server starting on port " + std::to_string(port));
        isRunning = true;
        Logger::info("IEC 104 Server is listening...");
    }

    void stop() {
        Logger::info("IEC 104 Server stopping...");
        isRunning = false;
    }

    void onClientConnected() {
        clientCount++;
        Logger::debug("Client connected. Total clients: " + std::to_string(clientCount));
    }

    void onClientDisconnected() {
        if (clientCount > 0) clientCount--;
        Logger::debug("Client disconnected. Total clients: " + std::to_string(clientCount));
    }

    void sendData(const std::string& data) {
        if (isRunning) {
            Logger::debug("IEC 104 sending: " + data);
        }
    }

    int getClientCount() const {
        return clientCount;
    }

    bool isActive() const {
        return isRunning;
    }
};
