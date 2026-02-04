#pragma once
#include <string>
#include <map>
#include <mutex>

class DataPool {
private:
    static DataPool* instance;
    std::map<std::string, std::string> data;
    std::mutex dataMutex;

    DataPool() {}

public:
    static DataPool* getInstance() {
        if (!instance) {
            instance = new DataPool();
        }
        return instance;
    }

    void setValue(const std::string& key, const std::string& value) {
        std::lock_guard<std::mutex> lock(dataMutex);
        data[key] = value;
    }

    std::string getValue(const std::string& key) {
        std::lock_guard<std::mutex> lock(dataMutex);
        if (data.find(key) != data.end()) {
            return data[key];
        }
        return "";
    }

    int size() {
        std::lock_guard<std::mutex> lock(dataMutex);
        return data.size();
    }
};

