#include <iostream>
#include "logger.h"
#include "config.h"
#include "data_pool.h"
#include "profinet_controller.h"
#include "iec104_server.h"
#include "rest_api.h"

int main() {
    // Инициализировать логирование
    Logger::init("profinet_converter.log");
    Logger::info("=== PROFINET Converter v1.0 Starting ===");

    // Получить синглтоны
    Config* config = Config::getInstance();
    DataPool* pool = DataPool::getInstance();

    // Загрузить конфигурацию
    config->loadFromJson("../external/config/config.json");

    Logger::info("Configuration loaded");
    Logger::info("PROFINET Port: " + config->get("profinet_port"));
    Logger::info("IEC 104 Port: " + config->get("iec104_port"));
    Logger::info("REST API Port: " + config->get("rest_api_port"));

    // Инициализировать PROFINET Controller
    ProfinetController profinet;
    profinet.init();

    // Добавить тестовые устройства
    profinet.addDevice("192.168.1.10", 1, "Device_1");
    profinet.addDevice("192.168.1.11", 1, "Device_2");
    profinet.addDevice("192.168.1.12", 1, "Device_3");

    Logger::info("PROFINET Controller has " + std::to_string(profinet.getDeviceCount()) + " devices");

    // Подключиться к устройствам
    profinet.connect();

    // Инициализировать IEC 104 Server
    IEC104Server iec104(config->getInt("iec104_port"));
    iec104.start();

    // Имитировать клиентов
    iec104.onClientConnected();
    iec104.onClientConnected();

    // Инициализировать REST API
    RestAPI restApi(config->getInt("rest_api_port"));
    restApi.start();

    // Сохранить данные в Pool
    pool->setValue("profinet_devices", std::to_string(profinet.getDeviceCount()));
    pool->setValue("iec104_clients", std::to_string(iec104.getClientCount()));
    pool->setValue("rest_api_port", config->get("rest_api_port"));
    pool->setValue("server_status", "running");

    Logger::debug("Data pool size: " + std::to_string(pool->size()));

    // Вывести статус
    Logger::info("=== Server Status ===");
    Logger::info("PROFINET: " + std::string(profinet.isConnected() ? "Connected" : "Disconnected"));
    Logger::info("IEC 104: " + std::string(iec104.isActive() ? "Active" : "Inactive"));
    Logger::info("REST API: " + std::string(restApi.isActive() ? "Active" : "Inactive"));
    Logger::info("Connected devices: " + pool->getValue("profinet_devices"));
    Logger::info("Connected clients: " + pool->getValue("iec104_clients"));
    Logger::info("=== Server Ready ===");

    std::cout << "\nPress Enter to exit..." << std::endl;
    std::cin.get();

    // Отключиться
    profinet.disconnect();
    iec104.stop();
    restApi.stop();

    Logger::info("=== Server Stopped ===");
    return 0;
}
