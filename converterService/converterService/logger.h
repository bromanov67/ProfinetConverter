#pragma once
#include <string>
#include <fstream>
#include <iostream>
#include <ctime>
#include <iomanip>
#include <sstream>

class Logger {
private:
    static std::string logFile;
    static std::ofstream logStream;

    static std::string getCurrentTime() {
        auto now = std::time(nullptr);
        auto tm = *std::localtime(&now);

        std::ostringstream oss; 
        oss << std::put_time(&tm, "%Y-%m-%d %H:%M:%S");
        return oss.str();
    }

public:
    static void init(const std::string& filename) {
        logFile = filename;
        logStream.open(logFile, std::ios::app);
        if (!logStream.is_open()) {
            std::cerr << "Failed to open log file: " << filename << std::endl;
        }
    }

    static void info(const std::string& message) {
        log("INFO", message);
    }

    static void error(const std::string& message) {
        log("ERROR", message);
    }

    static void debug(const std::string& message) {
        log("DEBUG", message);
    }

private:
    static void log(const std::string& level, const std::string& message) {
        std::string timestamp = getCurrentTime();
        std::string logMsg = "[" + timestamp + "] [" + level + "] " + message;

        // В консоль
        std::cout << logMsg << std::endl;

        // В файл
        if (logStream.is_open()) {
            logStream << logMsg << std::endl;
            logStream.flush();
        }
    }
};
