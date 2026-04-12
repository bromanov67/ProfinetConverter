<template>
  <div class="details-content">
    
    <!-- ВЕРХНЯЯ ЧАСТЬ: Сигналы IEC (Зона для Drop) -->
    <div 
      class="top-pane" 
      @dragover.prevent="onDragOver" 
      @dragenter.prevent="isDraggingOver = true"
      @dragleave.prevent="isDraggingOver = false"
      @drop="onDrop"
      :class="{ 'drag-over-active': isDraggingOver }"
    >
      <div class="toolbar">
        <button class="tool-btn" @click="addSignal" title="Добавить сигнал">+</button>
        <button class="tool-btn" @click="addSignalGroup" title="Добавить группу сигналов">++</button>
        <span v-if="isDraggingOver" class="drag-hint">Отпустите, чтобы добавить сигнал...</span>
      </div>

      <div class="table-container">
        <table class="data-table">
          <thead>
            <tr>
              <th style="width: 30px;">...</th>
              <th>Узел</th>
              <th>Отправитель (Имя)</th>
              <th>Адрес станции (ASDU)</th>
              <th>Адрес сигнала (IOA)</th>
              <th>Тип кадра</th>
              <th>Тип данных КС</th>
            </tr>
            <tr class="filter-row">
              <td>...</td>
              <td><input type="text" placeholder="Фильтр" /></td>
              <td><input type="text" placeholder="Фильтр" /></td>
              <td><input type="text" placeholder="Фильтр" /></td>
              <td><input type="text" placeholder="Фильтр" /></td>
              <td><input type="text" placeholder="Фильтр" /></td>
              <td><input type="text" placeholder="Фильтр" /></td>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in activeIecSignals" :key="item.id">
              <td class="checkbox-cell"><input type="checkbox" v-model="item.checked" /></td>
              <td><input type="text" v-model="item.node" class="editable-input" /></td>
              <td><input type="text" v-model="item.senderName" class="editable-input" /></td>
              <td><input type="number" v-model="item.asdu" class="editable-input" /></td>
              <td><input type="number" v-model="item.ioa" class="editable-input" /></td>
              <td><input type="text" v-model="item.frameType" class="editable-input" /></td>
              <td><input type="text" v-model="item.csDataType" class="editable-input" /></td>
            </tr>
            <tr v-if="activeIecSignals.length === 0 && !isDraggingOver">
               <td colspan="7" style="text-align: center; padding: 15px; color: #aaa;">
                 Перетащите сигналы снизу или нажмите "+"
               </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- РАЗДЕЛИТЕЛЬ (визуальный) -->
    <div class="pane-resizer">
      <div class="resizer-line"></div>
    </div>

    <!-- НИЖНЯЯ ЧАСТЬ: Доступные сигналы из Profinet (Draggable) -->
    <div class="bottom-pane">
      <div class="table-container">
        <table class="data-table">
          <thead>
            <tr>
              <th style="width: 30px;"></th>
              <th>Источник</th>
              <th>Имя</th>
              <th>Тип данных</th>
              <th>Узел</th>
            </tr>
            <tr class="filter-row">
              <td></td>
              <td><input type="text" placeholder="Фильтр" /></td>
              <td><input type="text" placeholder="Фильтр" /></td>
              <td><input type="text" placeholder="Фильтр" /></td>
              <td><input type="text" placeholder="Фильтр" /></td>
            </tr>
          </thead>
          <tbody>
            <tr 
              v-for="pn in availableProfinetSignals" 
              :key="pn.id" 
              class="profinet-row"
              draggable="true"
              @dragstart="onDragStart($event, pn)"
            >
              <td style="cursor: grab; text-align: center; color: #999;">⋮⋮</td>
              <td :title="pn.sourcePath">{{ pn.sourcePath }}</td>
              <td>{{ pn.name }}</td>
              <td>{{ pn.dataType }}</td>
              <td>{{ pn.node }}</td>
            </tr>
            <tr v-if="availableProfinetSignals.length === 0">
              <td colspan="5" style="text-align: center; color: #aaa; padding: 10px;">
                Нет доступных сигналов в PROFINET станциях.
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <div class="bottom-footer">Отправитель Свойства объекта</div>
    </div>

  </div>
</template>

<script setup>
import { ref, computed } from 'vue';
import { useDeviceStore } from '../../../stores/deviceStore';

const props = defineProps({ node: Object });
const store = useDeviceStore();

const isDraggingOver = ref(false);

// Ищем реальный канал в дереве
const getRealChannel = () => {
  if (!props.node || !props.node.channelId) return null;
  for (const proj of store.projects) {
    for (const srv of proj.servers || []) {
      for (const iface of srv.interfaces || []) {
        const foundChannel = (iface.channels || []).find(ch => ch.id === props.node.channelId);
        if (foundChannel) return foundChannel;
      }
    }
  }
  return null;
};

// Реактивный список добавленных IEC сигналов
const activeIecSignals = computed(() => {
  const channel = getRealChannel();
  if (channel) {
    if (!channel.signals) channel.signals = [];
    return channel.signals;
  }
  return [];
});

// Собираем все сигналы из PROFINET
const availableProfinetSignals = computed(() => {
  const allSignals = [];
  
  // Получаем ID уже перенесенных сигналов
  const usedSourceIds = activeIecSignals.value.map(sig => sig.sourceId).filter(Boolean);

  for (const proj of store.projects) {
    for (const srv of proj.servers || []) {
      if (srv.type !== 'server_profinet') continue;
      
      for (const iface of srv.interfaces || []) {
        for (const station of iface.stations || []) {
          const slots = station.configuration?.slots || [];
          for (const slot of slots) {
            
            // ВАЖНО: берем slot.signals, а не commands!
            if (slot.signals && Array.isArray(slot.signals)) {
              for (const sig of slot.signals) {
                // Если сигнал уже используется — скрываем
                if (usedSourceIds.includes(sig.id)) continue;

                const sourcePath = `${proj.name}/${srv.name}/${iface.name}/${station.name}/${slot.number.toString().padStart(2, '0')} : ${slot.label || 'Module'} /Сигналы/...`;
                allSignals.push({
                  id: sig.id,
                  sourcePath: sourcePath,
                  name: sig.name,
                  dataType: sig.csDataType || sig.dataType || 'BOOLEAN',
                  node: sig.node || 'Root'
                });
              }
            }
          }
        }
      }
    }
  }
  return allSignals;
});

const addSignal = (overrideData = {}) => {
  const channel = getRealChannel();
  if (!channel) {
    alert("Канал МЭК не найден.");
    return;
  }
  if (!channel.signals) channel.signals = [];

  const nextIndex = channel.signals.length + 1;
  channel.signals.push({
    id: Date.now().toString() + Math.random().toString().slice(2, 6),
    sourceId: overrideData.id || null, // Связь с Profinet-сигналом
    checked: true,
    node: overrideData.node || 'Root',
    senderName: overrideData.name || `Signal_${nextIndex.toString().padStart(2, '0')}`,
    asdu: 1,
    ioa: 100 + nextIndex, // Для сигналов IOA обычно начинается со 101, в отличие от команд
    frameType: 'M_SP_NA_1', // Дефолтный тип кадра для сигналов
    csDataType: overrideData.dataType || 'BOOLEAN'
  });
};

const addSignalGroup = () => {
  const countStr = prompt('Введите количество сигналов для создания:', '5');
  if (!countStr) return;
  const count = parseInt(countStr, 10);
  if (!isNaN(count) && count > 0) {
    for (let i = 0; i < count; i++) addSignal();
  }
};

// --- DRAG AND DROP ---
const onDragStart = (event, signal) => {
  event.dataTransfer.dropEffect = 'copy';
  event.dataTransfer.effectAllowed = 'copy';
  event.dataTransfer.setData('application/json', JSON.stringify(signal));
};

const onDragOver = () => {
  isDraggingOver.value = true;
};

const onDrop = (event) => {
  isDraggingOver.value = false;
  try {
    const rawData = event.dataTransfer.getData('application/json');
    if (!rawData) return;
    
    const droppedSignal = JSON.parse(rawData);
    
    addSignal({
      id: droppedSignal.id,
      name: droppedSignal.name,
      node: droppedSignal.node,
      dataType: droppedSignal.dataType
    });
  } catch (e) {
    console.error("Ошибка при распаковке", e);
  }
};
</script>

<style scoped>
.details-content { flex: 1; overflow: hidden; display: flex; flex-direction: column; background: #444; }

.top-pane { flex: 6; display: flex; flex-direction: column; overflow: hidden; transition: all 0.2s ease; }
/* Подсветка верхней панели при наведении (dragover) */
.drag-over-active {
  background: #556b2f !important;
  box-shadow: inset 0 0 10px rgba(154, 205, 50, 0.5);
}

.drag-hint {
  margin-left: auto;
  color: #adff2f;
  font-size: 12px;
  font-weight: bold;
  align-self: center;
  padding-right: 10px;
  animation: pulse 1s infinite alternate;
}

@keyframes pulse {
  from { opacity: 0.6; }
  to { opacity: 1; }
}

.bottom-pane { flex: 4; display: flex; flex-direction: column; overflow: hidden; background: #555; }

.pane-resizer { height: 6px; background: #333; cursor: ns-resize; display: flex; align-items: center; justify-content: center; }
.resizer-line { width: 40px; height: 2px; background: #666; border-radius: 1px; }

.toolbar { display: flex; gap: 5px; padding: 4px 6px; background: #444; border-bottom: 1px solid #333; }
.tool-btn { background: #5a5a5a; color: #eee; border: 1px solid #666; border-radius: 3px; padding: 4px 15px; cursor: pointer; font-weight: bold; font-size: 14px; }
.tool-btn:hover { background: #777; }

.bottom-header { font-size: 12px; color: #ccc; padding: 4px 8px; background: #3a3a3a; font-weight: 600; border-bottom: 1px solid #222; }
.bottom-footer { font-size: 11px; color: #aaa; padding: 4px 8px; background: #333; border-top: 1px solid #222; }

.table-container { flex: 1; overflow: auto; background: #5a5a5a; }
.data-table { width: 100%; border-collapse: collapse; font-size: 12px; color: #eee; text-align: left; white-space: nowrap; }
.data-table th { background: #666; padding: 6px 8px; border: 1px solid #777; font-weight: normal; position: sticky; top: 0; z-index: 2; }
.data-table td { background: #555; padding: 4px 8px; border: 1px solid #666; max-width: 300px; overflow: hidden; text-overflow: ellipsis; }

.data-table tbody tr:hover td { background: #606060; cursor: pointer; }
.profinet-row { cursor: grab; }
.profinet-row:active { cursor: grabbing; }
.profinet-row:hover td { background: #707070; color: #fff; }

.filter-row td { background: #ccc; padding: 2px; border-color: #999; position: sticky; top: 27px; z-index: 2; }
.filter-row input { width: 100%; background: transparent; border: none; font-size: 11px; color: #333; outline: none; padding: 2px; }
.checkbox-cell { text-align: center; width: 30px; }

.editable-input { width: 100%; background: transparent; color: #eee; border: 1px solid transparent; font-size: 12px; outline: none; padding: 2px;}
.editable-input:focus { border-bottom: 1px solid #999; }
</style>
