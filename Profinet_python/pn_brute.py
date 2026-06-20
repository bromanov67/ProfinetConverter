import sys
import time
import struct
import threading
import queue
from concurrent import futures

import grpc
import profinet_pb2
import profinet_pb2_grpc

from scapy.all import sniff

from profinet.util import ethernet_socket, get_mac
from profinet.dcp import send_discover
from profinet.rpc import RPCCon, get_station_info
try:
    from profinet.protocol import IOCRSetup
except ImportError:
    try:
        from profinet.rpc import IOCRSetup
    except ImportError:
        from profinet.cyclic import IOCRSetup

import logging
logging.basicConfig(level=logging.INFO, format="%(message)s")

CSHARP_SERVER_URL = "192.168.56.1:5002"
stop_event = threading.Event()

class DummySlot:
    def __init__(self, slot, subslot, input_len, output_len, module_id, submodule_id):
        self.slot = slot
        self.subslot = subslot
        self.input_length = input_len
        self.output_length = output_len
        self.api = 0
        self.module_ident = module_id
        self.submodule_ident = submodule_id

        if input_len == 0 and output_len == 0:
            self.properties = 0x0000
        elif input_len > 0 and output_len == 0:
            self.properties = 0x0001
        elif output_len > 0 and input_len == 0:
            self.properties = 0x0002
        else:
            self.properties = 0x0003

def pack_desc(direction, data_len, len_iocs=1, len_iops=1):
    return struct.pack(">HHBB", direction, data_len, len_iocs, len_iops)

def pack_submodule(subslot, submodule_ident, submodule_props, descs):
    return struct.pack(">HIH", subslot, submodule_ident, submodule_props) + b"".join(descs)

def pack_api_entry(api, slot, module_ident, module_props, submodules):
    return struct.pack(">IHIHH", api, slot, module_ident, module_props, len(submodules)) + b"".join(submodules)

def get_dynamic_expected_block(req):
    """Динамически формируем блок ожидаемой конфигурации на основе параметров из C#"""
    descs = []
    if req.input_length > 0:
        descs.append(pack_desc(0x0001, req.input_length, 1, 1))
    if req.output_length > 0:
        descs.append(pack_desc(0x0002, req.output_length, 1, 1))

    props = 0
    if req.input_length > 0 and req.output_length == 0: props = 0x0001
    elif req.output_length > 0 and req.input_length == 0: props = 0x0002
    elif req.input_length > 0 and req.output_length > 0: props = 0x0003

    sub_1_1 = pack_submodule(
        subslot=1,
        submodule_ident=req.submodule_ident,
        submodule_props=props,
        descs=descs
    )
    
    api_entry = pack_api_entry(
        api=0x00000000,
        slot=1,
        module_ident=req.module_ident,
        module_props=0x0000,
        submodules=[sub_1_1]
    )
    rest = struct.pack(">BBH", 1, 0, 1) + api_entry
    block_length = len(rest)
    return struct.pack(">HH", 0x0104, block_length) + rest

def discover_target(iface_name, target_name):
    print(f"[Python] Ищем ПЛК '{target_name}' на интерфейсе '{iface_name}'...")
    sock = ethernet_socket(iface_name)
    src_mac = get_mac(iface_name)
    send_discover(sock, src_mac)
    
    target_info = None
    start_time = time.time()
    while time.time() - start_time < 5.0:
        if stop_event.is_set(): break
        try:
            info = get_station_info(sock, src_mac, target_name)
            if info:
                target_info = info
                break
        except Exception:
            pass
    sock.close()

    if not target_info:
        print("[Python] ОШИБКА: ПЛК не найден.")
        return None, None
    print(f"[Python] Найдено! MAC: {target_info.mac}, IP: {target_info.ip}")
    return target_info, src_mac

def patch_rpc(rpc, setup_io, target_name, req):
    orig_build_iocr = rpc._build_iocr_block
    def patched_build_iocr(iocr_type, iocr_reference, setup):
        return orig_build_iocr(iocr_type=iocr_type, iocr_reference=iocr_reference, setup=setup_io)
    rpc._build_iocr_block = patched_build_iocr

    def patched_build_expected(setup):
        return get_dynamic_expected_block(req)
    rpc._build_expected_submodule_block = patched_build_expected

    orig_create_nrd = rpc._create_nrd
    def patched_create_nrd(payload):
        bad_name = b"\x00\x02tp"
        good_name = struct.pack(">H", len(target_name)) + target_name.encode("utf-8")
        if bad_name in payload:
            len_diff = len(target_name) - 2
            old_block_len = struct.unpack(">H", payload[2:4])[0]
            new_block_len = old_block_len + len_diff
            payload = payload[:2] + struct.pack(">H", new_block_len) + payload[4:]
            payload = payload.replace(bad_name, good_name)
        return orig_create_nrd(payload)
    rpc._create_nrd = patched_create_nrd

def safe_call(label, func, *args, **kwargs):
    try:
        res = func(*args, **kwargs)
        print(f"   {label}: {res}")
        return True, res
    except Exception as e:
        print(f"   {label} error: {e}")
        return False, None

def call_first_method(obj, names, *args, **kwargs):
    errors = []
    for name in names:
        fn = getattr(obj, name, None)
        if callable(fn):
            try:
                return fn(*args, **kwargs)
            except Exception as e:
                errors.append(f"{name}: {e}")
    raise AttributeError("Подходящий метод не найден. " + ", ".join(errors))

def grpc_sender_thread(grpc_queue):
    print(f"[Python-Client] Подключение к C# серверу (http://{CSHARP_SERVER_URL})...")
    try:
        channel = grpc.insecure_channel(CSHARP_SERVER_URL)
        stub = profinet_pb2_grpc.ProfinetServiceStub(channel)

        def request_generator():
            while not stop_event.is_set():
                try:
                    data = grpc_queue.get(timeout=0.5)
                    if data is None: break
                    yield profinet_pb2.PayloadRequest(
                        payload=data['payload'], cycle_counter=data['cycle'],
                        data_status=data['ds'], transfer_status=data['ts']
                    )
                except queue.Empty:
                    continue

        stub.StreamPayload(request_generator())
    except Exception as e:
        print(f"[Python-Client] Ошибка: {e}")

def run_profinet_logic(req):
    target_info, src_mac = discover_target(req.interface_name, req.target_name)
    if not target_info: return

    print("\n[Python] Готовим IOCR...")
    try:
        slot1 = DummySlot(1, 1, req.input_length, req.output_length, req.module_ident, req.submodule_ident)
        setup_io = IOCRSetup([slot1])
    except Exception as e:
        print(f"[Python] ОШИБКА IOCRSetup: {e}")
        return

    print("\n[Python] Устанавливаем RPC соединение...")
    rpc = RPCCon(info=target_info, timeout=5.0)
    patch_rpc(rpc, setup_io, req.target_name, req)

    try:
        rpc.connect(src_mac, iocr_setup=setup_io)
    except Exception as e:
        print(f"[Python] ОШИБКА подключения: {e}")
        return

    print("\n[Python] Доводим AR до Data Exchange...")
    safe_call("prm_begin()", call_first_method, rpc, ["prm_begin", "send_prm_begin", "send_prmbegin", "control_prm_begin"])
    time.sleep(0.2)
    safe_call("prm_end()", call_first_method, rpc, ["prm_end", "send_prm_end", "send_prmend", "control_prm_end"])
    time.sleep(0.5)
    safe_call("application_ready()", call_first_method, rpc, ["application_ready", "appl_ready", "app_ready", "send_application_ready"])

    print("\n[Python] Запуск циклического обмена и стриминга...")
    local_grpc_queue = queue.Queue()
    sender_thread = threading.Thread(target=grpc_sender_thread, args=(local_grpc_queue,), daemon=True)
    sender_thread.start()

    def handle(pkt):
        raw = bytes(pkt)
        marker = b"\x88\x92\xc0\x01" 
        i = raw.find(marker)
        if i == -1: return

        pkt_src_mac = ":".join(f"{b:02x}" for b in raw[6:12])
        if pkt_src_mac.lower() != target_info.mac.lower(): return

        rt_pdu = raw[i + 4:]
        if len(rt_pdu) < 4: return

        payload = rt_pdu[:-4]
        cycle_counter = int.from_bytes(rt_pdu[-4:-2], "big")
        ds = rt_pdu[-2]
        ts = rt_pdu[-1]

        print(f"[Payload] Cycle: {cycle_counter}, DS: {ds}, TS: {ts}, Data: {payload.hex()}")

        local_grpc_queue.put({'payload': payload, 'cycle': cycle_counter, 'ds': ds, 'ts': ts})

    def stop_sniff(pkt): return stop_event.is_set()

    sniff(iface=req.interface_name, prn=handle, stop_filter=stop_sniff, store=False, filter="(ether proto 0x8892)")
    
    local_grpc_queue.put(None)
    sender_thread.join()

    print("\n[Python] Отключение...")
    safe_call("disconnect()", rpc.disconnect)
    safe_call("close()", rpc.close)


class ProfinetControllerServicer(profinet_pb2_grpc.ProfinetControllerServicer):
    def StartBrute(self, request, context):
        stop_event.clear()
        print(f"\n[Python-Server] КОМАНДА СТАРТ. ПЛК: {request.target_name}, ModIdent: {request.module_ident}, SubIdent: {request.submodule_ident}")
        
        self.worker_thread = threading.Thread(target=run_profinet_logic, args=(request,))
        self.worker_thread.start()
        
        return profinet_pb2.StartResponse(success=True, message="Мастер запущен")

    def StopBrute(self, request, context):
        stop_event.set()
        return profinet_pb2.StopResponse(success=True, message="Остановка...")

def serve():
    print("[Python] Запуск gRPC сервера управления на порту 5005...")
    stop_event.set()
    server = grpc.server(futures.ThreadPoolExecutor(max_workers=10))
    profinet_pb2_grpc.add_ProfinetControllerServicer_to_server(ProfinetControllerServicer(), server)
    server.add_insecure_port('[::]:5005')
    server.start()
    try: server.wait_for_termination()
    except KeyboardInterrupt: stop_event.set(); server.stop(0)

if __name__ == "__main__":
    serve()