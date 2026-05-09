<template>
  <div class="details-panel">
    <template v-if="store.selectedNode">
      <!-- Общий заголовок для любой выбранной ноды -->
      <div class="details-header">
        <h2>{{ store.selectedNode.name }}</h2>
        <span class="details-type">{{ store.selectedNode.type }}</span>
      </div>

      <!-- НОВЫЙ БЛОК: ВКЛАДКИ (Показываются только в режиме исполнения для узлов МЭК 104) -->
      <div class="tabs-header" v-if="store.isRuntimeMode && isIecNode(store.selectedNode)">
        <button 
          class="tab-btn" 
          :class="{ active: activeTab === 'config' }" 
          @click="activeTab = 'config'"
        >Конфигурация</button>
        <button 
          class="tab-btn" 
          :class="{ active: activeTab === 'runtime' }" 
          @click="activeTab = 'runtime'"
        >Данные онлайн</button>
      </div>

      <!-- ========================================================= -->
      <!-- ВКЛАДКА 1: КОНФИГУРАЦИЯ (Отображение ваших компонентов) -->
      <!-- ========================================================= -->
      <div class="tab-content" v-show="!store.isRuntimeMode || activeTab === 'config'">
        
        <!-- 1. Серверы -->
        <ServerDetails 
          v-if="['server', 'server_profinet'].includes(store.selectedNode.type)" 
          :node="store.selectedNode" 
        />
        <IecServerDetails 
          v-else-if="store.selectedNode.type === 'server_iec104'" 
          :node="store.selectedNode" 
        />

        <!-- 2. Интерфейсы -->
        <ProfinetInterfaceDetails 
          v-else-if="['interface', 'interface_profinet'].includes(store.selectedNode.type)" 
          :node="store.selectedNode" 
        />
        <IecInterfaceDetails 
          v-else-if="store.selectedNode.type === 'interface_iec'" 
          :node="store.selectedNode" 
        />

        <!-- 3. Станции и Слоты (Profinet) -->
        <StationDetails 
          v-else-if="store.selectedNode.type === 'station'" 
          :node="store.selectedNode" 
        />
        <SlotDetails 
          v-else-if="store.selectedNode.type === 'slot'" 
          :node="store.selectedNode" 
        />

        <!-- 4. Каналы (IEC 104) -->
        <IecChannelDetails 
          v-else-if="store.selectedNode.type === 'channel_iec'" 
          :node="store.selectedNode" 
        />

        <!-- 5. Таблицы Сигналов и Команд (Profinet) -->
        <ProfinetSignalDetails 
          v-else-if="store.selectedNode.type === 'signals_folder'" 
          :node="store.selectedNode" 
        />
        <ProfinetCommandDetails 
          v-else-if="store.selectedNode.type === 'commands_folder'" 
          :node="store.selectedNode" 
        />

        <!-- 6. Таблицы Сигналов и Команд (IEC 104) -->
        <IecSignalDetails 
          v-else-if="store.selectedNode.type === 'iec_signals_folder'" 
          :node="store.selectedNode" 
        />
        <IecCommandDetails 
          v-else-if="store.selectedNode.type === 'iec_commands_folder'" 
          :node="store.selectedNode" 
        />
      </div>

      <!-- ========================================================= -->
      <!-- ВКЛАДКА 2: ДАННЫЕ ОНЛАЙН (Таблица как на скриншоте OPC) -->
      <!-- ========================================================= -->
      <div class="tab-content runtime-container" v-show="store.isRuntimeMode && activeTab === 'runtime'">
        <table class="runtime-table">
          <thead>
            <tr>
              <th>Идентификатор</th>
              <th>Регион</th>
              <th>Адрес в регистре</th>
              <th>Значение</th>
              <th>Качество</th>
              <th>Время</th>
              <th>Тип в сервере</th>
              <th>Доступ</th>
              <th>Комментарий</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in runtimeData" :key="item.id">
              <td>{{ item.identifier }}</td>
              <td>{{ item.region }}</td>
              <td>{{ item.address }}</td>
              <td>
                <span :class="{'bad-value': item.quality !== 'GOOD'}">{{ item.value }}</span>
              </td>
              <td>
                <span :class="item.quality === 'GOOD' ? 'quality-good' : 'quality-bad'">{{ item.quality }}</span>
              </td>
              <td>{{ item.time }}</td>
              <td>{{ item.type }}</td>
              <td>{{ item.access }}</td>
              <td>{{ item.comment }}</td>
            </tr>
          </tbody>
        </table>
      </div>

    </template>
    
    <div v-else class="details-empty">
      <p>Выберите элемент для просмотра</p>
    </div>
  </div>
</template>

<script setup>
import { ref, watch, onUnmounted } from 'vue'
import { useDeviceStore } from '../stores/deviceStore'
import * as signalR from '@microsoft/signalr'

// Импорты PROFINET
import ServerDetails from './details/ProfinetServer/ServerDetails.vue'
import ProfinetInterfaceDetails from './details/ProfinetServer/ProfinetInterfaceDetails.vue'
import StationDetails from './details/ProfinetServer/StationDetails.vue'
import SlotDetails from './details/ProfinetServer/SlotDetails.vue'
import ProfinetSignalDetails from './details/ProfinetServer/ProfinetSignalDetails.vue'
import ProfinetCommandDetails from './details/ProfinetServer/ProfinetCommandDetails.vue'

// Импорты IEC
import IecInterfaceDetails from './details/IecServer/IecInterfaceDetails.vue'
import IecChannelDetails from './details/IecServer/IecChannelDetails.vue'
import IecServerDetails from './details/IecServer/IecServerDetails.vue'
import IecSignalDetails from './details/IecServer/IecSignalDetails.vue'
import IecCommandDetails from './details/IecServer/IecCommandDetails.vue'

const store = useDeviceStore()

// --- ЛОГИКА ВКЛАДОК И РЕЖИМА ИСПОЛНЕНИЯ ---
const activeTab = ref('config')

// Определяем, относится ли узел к МЭК 104 (чтобы показывать вкладки)
const isIecNode = (node) => {
  if (!node) return false;
  const t = node.type;
  return t === 'server_iec104' || 
         t === 'interface_iec' || 
         t === 'channel_iec' || 
         t === 'iec_signals_folder' || 
         t === 'iec_commands_folder';
};

// При переключении режима исполнения меняем вкладку
watch(() => store.isRuntimeMode, (newVal) => {
  if (newVal && isIecNode(store.selectedNode)) {
    activeTab.value = 'runtime';
  } else {
    activeTab.value = 'config';
  }
});

// Переключаем вкладку обратно на config, если перешли на узел, который не поддерживает онлайн данные
watch(() => store.selectedNode, (newNode) => {
  if (store.isRuntimeMode && isIecNode(newNode)) {
    activeTab.value = 'runtime';
  } else {
    activeTab.value = 'config';
  }
});

// --- ГЕНЕРАЦИЯ ДАННЫХ ДЛЯ ТАБЛИЦЫ RUNTIME ---
// --- ГЕНЕРАЦИЯ ДАННЫХ ДЛЯ ТАБЛИЦЫ RUNTIME ---
const runtimeData = ref([]);

// Создаем подключение к хабу
const connection = new signalR.HubConnectionBuilder()
  .withUrl('http://localhost:5000/runtimeHub')
  .withAutomaticReconnect()
  .build();

// Слушаем событие прихода данных от C#
connection.on('ReceiveRuntimeData', (data) => {
  // 1. ПРОВЕРЯЕМ БЛОКИРОВКУ
  if (!store.isRuntimeMode || !store.selectedNode || activeTab.value !== 'runtime') {
      return;
  }

  const now = new Date();
  const timeStr = now.getFullYear() + '-' + 
                  String(now.getMonth()+1).padStart(2,'0') + '-' + 
                  String(now.getDate()).padStart(2,'0') + ' ' + 
                  String(now.getHours()).padStart(2,'0') + ':' + 
                  String(now.getMinutes()).padStart(2,'0') + ':' + 
                  String(now.getSeconds()).padStart(2,'0');

  const baseId = `IEC104.${store.selectedNode.name}.`;

  // Преобразуем полученные JSON-данные в формат таблицы
  runtimeData.value = data.map((item) => {
    // Безопасный парсинг значения (превращаем в строку, чтобы избежать NaN)
    let displayValue = "0";
    if (item.value !== null && item.value !== undefined) {
       displayValue = String(item.value);
    }
    
    // Если это float, можно обрезать лишние нули (опционально)
    if (item.type === 'float' && !isNaN(parseFloat(displayValue))) {
        displayValue = parseFloat(displayValue).toString();
    }

    return {
      id: item.ioa,
      identifier: baseId + item.identifier,
      region: 'NONE',
      address: item.ioa, 
      value: displayValue, // <-- Теперь тут всегда корректная строка
      quality: item.quality || 'GOOD',
      time: timeStr,
      type: item.type,
      access: 'ReadOnly',
      comment: 'Real-time Profinet data'
    };
  });
});

// Запускаем/останавливаем WebSocket соединение в зависимости от режима исполнения
watch(() => store.isRuntimeMode, async (isRuntime) => {
  try {
    if (isRuntime && isIecNode(store.selectedNode)) {
      activeTab.value = 'runtime';
      if (connection.state === signalR.HubConnectionState.Disconnected) {
        await connection.start();
        console.log("WebSocket подключен!");
      }
    } else {
      activeTab.value = 'config';
      if (connection.state === signalR.HubConnectionState.Connected) {
        await connection.stop();
        console.log("WebSocket отключен!");
      }
    }
  } catch (e) {
    console.error("Ошибка SignalR: ", e);
  }
});

// --- СТАРЫЕ ФУНКЦИИ ---
const addServer = async () => {
  const name = prompt('Enter Server Name:', 'PLC Server')
  if (name) {
    await store.addServer(store.selectedNode.id, name)
  }
}
</script>

<style>
/* --- ОБЩИЕ СТИЛИ --- */
.details-panel {
  flex: 1;
  background: white;
  display: flex;
  flex-direction: column;
  border-left: 1px solid #e0e8f0;
  overflow: hidden;
}

.details-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 20px;
  border-bottom: 1px solid #e0e8f0;
  background: #f5f5f5;
  flex-shrink: 0;
}

.details-header h2 { margin: 0; color: #333; font-size: 16px; }
.details-type {
  background: #e8f0f8; color: #2c5aa0;
  padding: 4px 10px; border-radius: 3px;
  font-size: 11px; font-weight: 600; text-transform: uppercase;
}

.details-empty {
  display: flex; align-items: center; justify-content: center;
  height: 100%; color: #999; font-style: italic;
}

.details-actions {
  display: flex; gap: 10px;
  padding: 12px 16px;
  border-top: 1px solid #e0e8f0;
  background: #f5f5f5;
  flex-shrink: 0;
}

.action-btn {
  flex: 1; padding: 9px 16px;
  background: #2c5aa0; color: white;
  border: none; border-radius: 4px;
  cursor: pointer; font-weight: 500; font-size: 13px;
}
.action-btn:hover { background: #1e3f6b; }

.details-content { flex: 1; overflow-y: auto; }
.properties-table { width: 100%; border-collapse: collapse; font-size: 13px; background: #f0f0f0; }
.properties-table tbody tr { border-bottom: 1px solid #ddd; }
.properties-table td { padding: 8px 12px; vertical-align: middle; }
.prop-label { width: 50%; background: #e8e8e8; font-weight: 500; color: #444; }
.prop-value { width: 50%; background: #f5f5f5; color: #333; }
.input-field {
  width: 100%; background: white; border: 1px solid #ccc; color: #333;
  padding: 4px 6px; border-radius: 2px; font-size: 12px;
}
.input-field:disabled { background: #e8e8e8; color: #666; border-color: #bbb; }
.input-field:focus { outline: none; border-color: #2c5aa0; }
.expand-icon { display: inline-block; width: 14px; margin-right: 6px; font-weight: 600; }
.section-header td { background: #d0d0d0 !important; font-weight: 600; cursor: pointer; color: #333 !important; }
.subsection-header td { background: #dcdcdc !important; font-style: italic; cursor: pointer; color: #555 !important; }

/* --- НОВЫЕ СТИЛИ ДЛЯ ВКЛАДОК И RUNTIME --- */
.tabs-header {
  display: flex;
  background: #f5f5f5;
  border-bottom: 1px solid #e0e8f0;
  padding: 0 20px;
}

.tab-btn {
  padding: 10px 16px;
  background: none;
  border: none;
  border-bottom: 3px solid transparent;
  cursor: pointer;
  font-size: 13px;
  color: #666;
  outline: none;
  font-weight: 500;
}

.tab-btn:hover { background: #e8e8e8; }
.tab-btn.active {
  border-bottom: 3px solid #2c5aa0;
  color: #2c5aa0;
  font-weight: 600;
}

.tab-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden; /* Важно, чтобы компоненты внутри скроллились сами */
}

/* Стили таблицы Runtime */
.runtime-container {
  background: #fff;
  overflow: auto;
}

.runtime-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 12px;
  color: #333;
  white-space: nowrap;
}

.runtime-table th {
  background: #f0f0f0;
  border: 1px solid #ddd;
  padding: 6px 8px;
  font-weight: 600;
  text-align: left;
  position: sticky;
  top: 0;
  z-index: 1;
}

.runtime-table td {
  border: 1px solid #eee;
  padding: 6px 8px;
}

.runtime-table tbody tr:hover { background: #f0f6fc; cursor: default; }

.quality-good { color: #107c10; font-weight: bold; }
.quality-bad { color: #d13438; font-weight: bold; }
.bad-value { color: #aaa; font-style: italic; }
</style>