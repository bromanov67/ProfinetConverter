#pragma once
#include <string>
#include <vector>
#include "logger.h"

class ProfinetDevice {
public:
    std::string ip;
    int slot;
    std::string name;
    std::string status;

    ProfinetDevice(const std::string& ip, int slot, const std::string& name)
        : ip(ip), slot(slot), name(name), status("disconnected") {
    }
};

class ProfinetController {
private:
    std::vector<ProfinetDevice> devices;
    bool isRunning;

public:
    ProfinetController() : isRunning(false) {}

    void init() {
        Logger::info("PROFINET Controller initialized");
        isRunning = true;
    }

    void addDevice(const std::string& ip, int slot, const std::string& name) {
        devices.emplace_back(ip, slot, name);
        Logger::debug("Device added: " + name + " (" + ip + ")");
    }

    void connect() {
        if (!isRunning) {
            Logger::error("Controller not initialized");
            return;
        }

        Logger::info("Connecting to PROFINET devices...");
        for (auto& device : devices) {
            device.status = "connected";
            Logger::info("Device connected: " + device.name);
        }
    }

    void disconnect() {
        Logger::info("Disconnecting from PROFINET devices...");
        for (auto& device : devices) {
            device.status = "disconnected";
        }
    }

    int getDeviceCount() const {
        return devices.size();
    }

    bool isConnected() const {
        return isRunning;
    }
};
