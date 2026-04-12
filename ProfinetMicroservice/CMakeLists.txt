cmake_minimum_required(VERSION 3.15)
project(ProfinetMicroservice CXX C)

set(CMAKE_CXX_STANDARD 17)

# 1. Подключаем пакеты gRPC и Protobuf
find_package(gRPC CONFIG REQUIRED)
find_package(Protobuf CONFIG REQUIRED)

# 2. Настройка генерации из .proto файла
set(PROTO_DIR "${CMAKE_CURRENT_SOURCE_DIR}/Protos")
set(PROTO_FILE "${PROTO_DIR}/profinet.proto")

get_target_property(GRPC_CPP_PLUGIN_EXECUTABLE gRPC::grpc_cpp_plugin LOCATION)
get_target_property(PROTOBUF_PROTOC_EXECUTABLE protobuf::protoc LOCATION)

add_custom_command(
    OUTPUT "${CMAKE_CURRENT_BINARY_DIR}/profinet.pb.cc"
           "${CMAKE_CURRENT_BINARY_DIR}/profinet.pb.h"
           "${CMAKE_CURRENT_BINARY_DIR}/profinet.grpc.pb.cc"
           "${CMAKE_CURRENT_BINARY_DIR}/profinet.grpc.pb.h"
    COMMAND ${PROTOBUF_PROTOC_EXECUTABLE}
    ARGS --cpp_out="${CMAKE_CURRENT_BINARY_DIR}"
         --grpc_out="${CMAKE_CURRENT_BINARY_DIR}"
         --plugin=protoc-gen-grpc="${GRPC_CPP_PLUGIN_EXECUTABLE}"
         -I "${PROTO_DIR}"
         "${PROTO_FILE}"
    DEPENDS "${PROTO_FILE}"
)

# 3. Собираем исходники p-net
# ИСПРАВЛЕНО: закрывающая скобка file() стоит сразу после аргументов
file(GLOB_RECURSE PNET_SOURCES
    "profinet_lib/p-net/src/*.c"
    "profinet_lib/p-net/include/osal/src/windows/*.c" 
)

# ИСПРАВЛЕНО: list(FILTER) теперь СНАРУЖИ file(), выполняются корректно
list(FILTER PNET_SOURCES EXCLUDE REGEX ".*/lan9662/.*")
list(FILTER PNET_SOURCES EXCLUDE REGEX ".*pf_rte_.*")
list(FILTER PNET_SOURCES EXCLUDE REGEX ".*pf_sram_.*")
list(FILTER PNET_SOURCES EXCLUDE REGEX ".*pf_mera_.*")

# 4. Создаем исполняемый файл
add_executable(profinet_service
    ProfinetMicroservice.cpp
    ${PNET_SOURCES}
    "${CMAKE_CURRENT_BINARY_DIR}/profinet.pb.cc"
    "${CMAKE_CURRENT_BINARY_DIR}/profinet.grpc.pb.cc"
    "profinet_lib/p-net/src/pnet_export.h"
    "profinet_lib/p-net/src/pnet_version.h"
    "profinet_lib/p-net/src/pnal_config.h"
    "profinet_lib/p-net/src/pnal_sys.h"
 "profinet_lib/p-net/src/common/pf_snmp_stub.c")

# 5. Include пути
target_include_directories(profinet_service PRIVATE
    ${CMAKE_CURRENT_BINARY_DIR}
    "profinet_lib/p-net/src"
    "profinet_lib/p-net/src/common"
    "profinet_lib/p-net/src/device"
    "profinet_lib/p-net/src/drivers"
    "profinet_lib/p-net/include"
    "profinet_lib/p-net/include/osal"
    "profinet_lib/p-net/include/osal/include"
    "profinet_lib/p-net/include/osal/src/windows"
    "profinet_lib/p-net/include/osal/src/windows/sys"
)

# 6. Линковка
target_link_libraries(profinet_service PRIVATE
    gRPC::grpc++ protobuf::libprotobuf ws2_32 iphlpapi winmm
)