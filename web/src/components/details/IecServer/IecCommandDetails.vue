<template>
  <div class="details-content">
    
    <!-- ВЕРХНЯЯ ЧАСТЬ: Команды IEC (Зона для Drop) -->
    <div 
      class="top-pane" 
      @dragover.prevent="onDragOver" 
      @dragenter.prevent="isDraggingOver = true"
      @dragleave.prevent="isDraggingOver = false"
      @drop="onDrop"
      :class="{ 'drag-over-active': isDraggingOver }"
    >
      <div class="toolbar">
        <button class="tool-btn" @click="addCommand" title="Добавить команду">+</button>
        <button class="tool-btn" @click="addCommandGroup" title="Добавить группу команд">++</button>
        <span v-if="isDraggingOver" class="drag-hint">Отпустите, чтобы добавить команду...</span>
      </div>

      <div class="table-container">
        <table class="data-table">
          <thead>
            <tr>
              <th style="width: 30px;">...</th>
              <th>Узел</th>
              <th>Приёмник</th>
              <th>Адрес станции (ASDU)</th>
              <th>Адрес команды (IOA)</th>
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
            <tr v-for="item in activeIecCommands" :key="item.id">
              <td class="checkbox-cell"><input type="checkbox" v-model="item.checked" /></td>
              <td><input type="text" v-model="item.node" class="editable-input" /></td>
              <td><input type="text" v-model="item.receiverName" class="editable-input" /></td>
              <td><input type="number" v-model="item.asdu" class="editable-input" /></td>
              <td><input type="number" v-model="item.ioa" class="editable-input" /></td>
              <td><input type="text" v-model="item.frameType" class="editable-input" /></td>
              <td><input type="text" v-model="item.csDataType" class="editable-input" /></td>
            </tr>
            <tr v-if="activeIecCommands.length === 0 && !isDraggingOver">
               <td colspan="7" style="text-align: center; padding: 15px; color: #aaa;">
                 Перетащите команды снизу или нажмите "+"
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

    <!-- НИЖНЯЯ ЧАСТЬ: Доступные команды из Profinet (Draggable) -->
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
            <!-- Добавлены атрибуты для Drag & Drop -->
            <tr 
              v-for="pn in availableProfinetCommands" 
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
            <tr v-if="availableProfinetCommands.length === 0">
              <td colspan="5" style="text-align: center; color: #aaa; padding: 10px;">
                Нет доступных команд в PROFINET станциях.
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <div class="bottom-footer">Приёмник Свойства объекта</div>
    </div>

  </div>
</template>

<script setup>
import { ref, computed } from 'vue';
import { useDeviceStore } from '../../../stores/deviceStore';

const props = defineProps({ node: Object });
const store = useDeviceStore();

const isDraggingOver = ref(false);

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

const activeIecCommands = computed(() => {
  const channel = getRealChannel();
  if (channel) {
    if (!channel.commands) channel.commands = [];
    return channel.commands;
  }
  return [];
});

const availableProfinetCommands = computed(() => {
  const allCommands = [];
  
  // Собираем ID уже использованных команд (предполагаем, что мы сохраняем оригинальный id в поле sourceId)
  const usedSourceIds = activeIecCommands.value.map(cmd => cmd.sourceId).filter(Boolean);

  for (const proj of store.projects) {
    for (const srv of proj.servers || []) {
      if (srv.type !== 'server_profinet') continue;
      
      for (const iface of srv.interfaces || []) {
        for (const station of iface.stations || []) {
          const slots = station.configuration?.slots || [];
          for (const slot of slots) {
            if (slot.commands && Array.isArray(slot.commands)) {
              for (const cmd of slot.commands) {
                // Если команда уже перенесена наверх, пропускаем её
                if (usedSourceIds.includes(cmd.id)) continue;

                const sourcePath = `${proj.name}/${srv.name}/${iface.name}/${station.name}/${slot.number.toString().padStart(2, '0')} : ${slot.label || 'Module'} /Команды/...`;
                allCommands.push({
                  id: cmd.id,
                  sourcePath: sourcePath,
                  name: cmd.name,
                  dataType: cmd.csDataType || cmd.dataType || 'BOOLEAN',
                  node: cmd.node || 'Root'
                });
              }
            }
          }
        }
      }
    }
  }
  return allCommands;
});

const addCommand = (overrideData = {}) => {
  const channel = getRealChannel();
  if (!channel) return alert("Канал МЭК не найден.");
  if (!channel.commands) channel.commands = [];

  const nextIndex = channel.commands.length + 1;
  channel.commands.push({
    id: Date.now().toString() + Math.random().toString().slice(2, 6),
    sourceId: overrideData.id || null, // <--- СОХРАНЯЕМ ID ИСТОЧНИКА
    checked: true,
    node: overrideData.node || 'Root',
    receiverName: overrideData.name || `Command_${nextIndex.toString().padStart(2, '0')}`,
    asdu: 1,
    ioa: 200 + nextIndex,
    frameType: 'C_SC_NA_1',
    csDataType: overrideData.dataType || 'BOOLEAN'
  });
};

const addCommandGroup = () => {
  const countStr = prompt('Введите количество команд для создания:', '5');
  if (!countStr) return;
  const count = parseInt(countStr, 10);
  if (!isNaN(count) && count > 0) {
    for (let i = 0; i < count; i++) addCommand();
  }
};

// --- DRAG AND DROP ЛОГИКА ---

// 1. При начале перетаскивания сохраняем объект команды в JSON
const onDragStart = (event, command) => {
  event.dataTransfer.dropEffect = 'copy';
  event.dataTransfer.effectAllowed = 'copy';
  // Сериализуем данные команды, чтобы передать их
  event.dataTransfer.setData('application/json', JSON.stringify(command));
};

// 2. Для того чтобы drop сработал, dragover должен вызывать event.preventDefault() 
// (это мы делаем прямо в template через @dragover.prevent)
const onDragOver = (event) => {
  isDraggingOver.value = true;
};

// 3. Обработка броска (Drop)
const onDrop = (event) => {
  isDraggingOver.value = false;
  try {
    const rawData = event.dataTransfer.getData('application/json');
    if (!rawData) return;
    const droppedCommand = JSON.parse(rawData);
    
    addCommand({
      id: droppedCommand.id, // <--- Передаем ID
      name: droppedCommand.name,
      node: droppedCommand.node,
      dataType: droppedCommand.dataType
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
  background: #556b2f !important; /* Легкий зеленый оттенок */
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
