#include <iostream>
#include <memory>
#include <string>
#include <mutex>
#include <thread>

// Подключаем заголовки gRPC
#include <grpcpp/grpcpp.h>

// Подключаем сгенерированные файлы Protobuf
#include "profinet.grpc.pb.h"

// Подключаем библиотеку p-net (C-код)
extern "C" {
    // В будущем, когда вы начнете связывать функции p-net с этим сервисом,
    // вам нужно будет раскомментировать инклуды нужных заголовочных файлов p-net:
    // #include "pnet_api.h"
}

using grpc::Server;
using grpc::ServerBuilder;
using grpc::ServerContext;
using grpc::ServerReaderWriter;
using grpc::Status;

// Используем классы, сгенерированные из вашего profinet.proto
using profinet::ProfinetStackService;
using profinet::StartRequest;
using profinet::StartResponse;
using profinet::StopRequest;
using profinet::StopResponse;
using profinet::IoWriteRequest;
using profinet::IoUpdateResponse;

class ProfinetServiceImpl final : public ProfinetStackService::Service {
private:
    std::mutex mu_;
    ServerReaderWriter<IoUpdateResponse, IoWriteRequest>* current_stream_ = nullptr;
    bool is_running_ = false;

public:
    // Метод: Запуск PROFINET стека
    Status StartStack(ServerContext* context, const StartRequest* request, StartResponse* response) override {
        std::cout << "[C++] Request to Start PROFINET on interface: " << request->interface_name() << std::endl;

        // TODO: Здесь будет реальный вызов сишной функции p-net для запуска стека.
        // Пример: int result = pnet_init(request->interface_name().c_str());
        int result = 0; // Имитация успешного запуска

        if (result == 0) {
            is_running_ = true;
            response->set_success(true);
            return Status::OK;
        }
        else {
            response->set_success(false);
            response->set_error_message("Failed to initialize p-net stack.");
            return Status::OK;
        }
    }

    // Метод: Остановка PROFINET стека
    Status StopStack(ServerContext* context, const StopRequest* request, StopResponse* response) override {
        std::cout << "[C++] Request to Stop PROFINET." << std::endl;

        // TODO: Здесь будет реальный вызов остановки стека.
        // Пример: pnet_deinit();
        is_running_ = false;

        response->set_success(true);
        return Status::OK;
    }

    // Метод: Двунаправленный стрим для обмена входами/выходами
    Status StreamIoData(ServerContext* context, ServerReaderWriter<IoUpdateResponse, IoWriteRequest>* stream) override {
        std::cout << "[C++] C# Backend connected to IO Stream!" << std::endl;

        {
            std::lock_guard<std::mutex> lock(mu_);
            current_stream_ = stream;
        }

        IoWriteRequest request;

        // Блокирующий цикл чтения команд записи (Outputs) от C#
        while (stream->Read(&request)) {
            const std::string& raw_data = request.data();

            // TODO: Здесь вы передаете байты в сишную библиотеку p-net
            // Пример: pnet_write_outputs(request.offset(), (const uint8_t*)raw_data.data(), raw_data.size());

            std::cout << "[C++] Received Write command for offset: " << request.offset()
                << " Size: " << raw_data.size() << " bytes" << std::endl;
        }

        {
            std::lock_guard<std::mutex> lock(mu_);
            current_stream_ = nullptr;
        }

        std::cout << "[C++] C# Backend disconnected from IO Stream." << std::endl;
        return Status::OK;
    }

    // Эту функцию мы будем вызывать из сишного коллбэка p-net, когда от оборудования придут данные (Inputs)
    void OnProfinetDataReceived(int offset, const uint8_t* data, size_t length) {
        std::lock_guard<std::mutex> lock(mu_);
        if (current_stream_ != nullptr) {
            IoUpdateResponse response;
            response.set_offset(offset);
            response.set_data(data, length);
            response.set_is_good_quality(true);

            // Мгновенно отправляем данные в C# бэкенд по gRPC
            current_stream_->Write(response);
        }
    }
};

// Глобальный указатель на сервис (нужен, чтобы прокинуть вызов из C коллбэка в C++ класс)
ProfinetServiceImpl* g_service = nullptr;

// Пример коллбэка, который вы в будущем зарегистрируете в p-net
extern "C" void pnet_rx_callback(int offset, const uint8_t* data, size_t len) {
    if (g_service != nullptr) {
        g_service->OnProfinetDataReceived(offset, data, len);
    }
}

// Конфигурация и запуск gRPC сервера
void RunServer() {
    std::string server_address("0.0.0.0:5001");
    ProfinetServiceImpl service;
    g_service = &service;

    ServerBuilder builder;
    // Слушаем порт 5001 без шифрования (для микросервисного обмена на локалхосте это идеально)
    builder.AddListeningPort(server_address, grpc::InsecureServerCredentials());
    builder.RegisterService(&service);

    std::unique_ptr<Server> server(builder.BuildAndStart());
    std::cout << "[C++] PROFINET gRPC Microservice listening on " << server_address << std::endl;

    // Сервер работает бесконечно, пока мы не закроем консоль
    server->Wait();
}

int main(int argc, char** argv) {
    RunServer();
    return 0;
}