#pragma once
#include <string>
#include <map>

class Config {
private:
    static Config* instance;
    std::map<std::string, std::string> settings;

    Config() {}

public:
    static Config* getInstance() {
        if (!instance) {
            instance = new Config();
        }
        return instance;
    }

    bool loadFromJson(const std::string& filename) {
        // Для минимальной версии просто устанавливаем значения
        settings["profinet_port"] = "34964";
        settings["iec104_port"] = "2404";
        settings["rest_api_port"] = "8080";
        settings["profinet_timeout"] = "5000";
        settings["max_devices"] = "10";
        return true;
    }

    void set(const std::string& key, const std::string& value) {
        settings[key] = value;
    }

    std::string get(const std::string& key) {
        if (settings.find(key) != settings.end()) {
            return settings[key];
        }
        return "";
    }

    int getInt(const std::string& key) {
        std::string value = get(key);
        if (value.empty()) return 0;
        return std::stoi(value);
    }
};