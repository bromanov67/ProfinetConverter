<template>
  <div class="details-content">
    <div class="toolbar">
      <button class="tool-btn" @click="addSignal" title="Добавить сигнал">+</button>
      <button class="tool-btn" @click="addSignalGroup" title="Добавить группу сигналов">++</button>
    </div>

    <div class="table-container">
      <table class="data-table">
        <thead>
          <tr>
            <th style="width: 30px;">...</th>
            <th>Имя</th>
            <th>Узел</th>
            <th>Адрес регистра</th>
            <th>Тип данных</th>
            <th>Тип данных КС</th>
            <th>Byte Offset</th>
            <th>Bit Offset</th>
            <th>Ретрансляция (в МЭК 104)</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in activeSignals" :key="item.id">
            <td class="checkbox-cell"><input type="checkbox" v-model="item.checked" /></td>
            <td><input type="text" v-model="item.name" class="editable-input" /></td>
            <td><input type="text" v-model="item.node" class="editable-input" /></td>
            <td><input type="number" v-model="item.regAddress" class="editable-input" /></td>
            <td>
              <select v-model="item.dataType" @change="updateCsType(item)" class="select-field">
                <option v-for="type in dataTypes" :key="type" :value="type">{{ type }}</option>
              </select>
            </td>
            <td>{{ item.csDataType }}</td>

            <!-- Поле: Byte Offset -->
            <td>
              <input 
                type="number" 
                v-model="item.byteOffset" 
                class="editable-input" 
                min="0" 
                max="63"
              />
            </td>
            
            <!-- Поле: Bit Offset (активно только для Bool) -->
            <td>
              <input 
                type="number" 
                v-model="item.bitOffset" 
                class="editable-input" 
                min="0" 
                max="7"
                :disabled="item.dataType !== 'Bool'"
                :style="{ opacity: item.dataType === 'Bool' ? '1' : '0.4' }"
              />
            </td>

            <td>
              <!-- Выпадающий список всех доступных МЭК сигналов -->
              <select v-model="item.archive" class="select-field">
                <option value="-">Не выбрано</option>
                <option v-for="iec in availableIecSignals" :key="iec.id" :value="iec.id">
                  {{ iec.label }}
                </option>
              </select>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue';
import { useDeviceStore } from '../../../stores/deviceStore';

const props = defineProps({ node: Object });
const store = useDeviceStore();

const dataTypeMapping = {
  'Bool': 'BOOLEAN', 'Float32': 'FLOAT', 'Int16': 'INT', 'Int16U': 'UINT',
  'Int32': 'DINT', 'Int32U': 'UDINT', 'Int8': 'SINT', 'Int8U': 'USINT'
};
const dataTypes = Object.keys(dataTypeMapping);

// Собираем все сигналы МЭК 104 из проекта для выпадающего списка
const availableIecSignals = computed(() => {
  const iecList = [];
  for (const proj of store.projects) {
    for (const srv of proj.servers || []) {
      if (srv.type !== 'server_iec104') continue;
      for (const iface of srv.interfaces || []) {
        for (const channel of iface.channels || []) {
          if (channel.signals) {
            channel.signals.forEach(sig => {
              iecList.push({
                id: sig.id,
                label: `[МЭК: ${channel.name}] ${sig.name} (IOA: ${sig.regAddress})`
              });
            });
          }
        }
      }
    }
  }
  return iecList;
});

// Функция поиска реального слота в конфигурации
const getRealSlot = () => {
  if (!props.node || !props.node.stationId || props.node.slotNumber === undefined) return null;
  
  let targetStation = null;
  for (const proj of store.projects) {
    for (const srv of proj.servers || []) {
      for (const iface of srv.interfaces || []) {
        const found = (iface.stations || []).find(s => s.id === props.node.stationId);
        if (found) { targetStation = found; break; }
      }
      if (targetStation) break;
    }
    if (targetStation) break;
  }

  if (!targetStation || !targetStation.configuration || !targetStation.configuration.slots) return null;
  return targetStation.configuration.slots.find(s => s.number === props.node.slotNumber);
};

// Реактивно вычисляем массив сигналов из реального слота
const activeSignals = computed(() => {
  const slot = getRealSlot();
  if (slot) {
    if (!slot.signals) slot.signals = [];
    return slot.signals;
  }
  return [];
});

const updateCsType = (item) => {
  item.csDataType = dataTypeMapping[item.dataType];
  // Сбрасываем bitOffset при смене типа с Bool на другой
  if (item.dataType !== 'Bool') {
    item.bitOffset = 0;
  }
};

const addSignal = () => {
  const slot = getRealSlot();
  if (!slot) {
    alert("Слот не найден. Невозможно добавить сигнал.");
    return;
  }
  if (!slot.signals) slot.signals = [];
  
  const nextIndex = slot.signals.length + 1;
  slot.signals.push({
    id: Date.now().toString() + Math.random().toString().slice(2, 6),
    checked: true,
    name: `Signal_${nextIndex.toString().padStart(2, '0')}`,
    node: 'Root',
    regAddress: 0,
    dataType: 'Bool',
    csDataType: 'BOOLEAN',
    byteOffset: 0, // Инициализация Byte Offset
    bitOffset: 0,  // Инициализация Bit Offset
    archive: '-' 
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
</script>

<style scoped>
.details-content { flex: 1; overflow: hidden; display: flex; flex-direction: column; background: #555; padding: 2px;}
.toolbar { display: flex; gap: 5px; padding: 4px; background: #666; border: 1px solid #444; border-bottom: none; }
.tool-btn { background: #555; color: #eee; border: 1px solid #777; border-radius: 3px; padding: 4px 15px; cursor: pointer; font-weight: bold; font-size: 14px; }
.tool-btn:hover { background: #777; }
.table-container { flex: 1; overflow: auto; background: #666; border: 1px solid #444;}
.data-table { width: 100%; border-collapse: collapse; font-size: 13px; color: #eee; text-align: left; }
.data-table th { background: #555; padding: 6px 8px; border: 1px solid #777; font-weight: normal; }
.data-table td { background: #5a5a5a; padding: 4px 8px; border: 1px solid #777; }
.checkbox-cell { text-align: center; background: #eee !important; }
.editable-input { width: 100%; background: transparent; color: #eee; border: 1px solid transparent; font-size: 13px; outline: none; padding: 2px;}
.editable-input:focus { border-bottom: 1px solid #999; }
.editable-input:disabled { cursor: not-allowed; }
.select-field { width: 100%; background: #5a5a5a; color: #eee; border: 1px solid #777; padding: 2px 4px; outline: none; }
.select-field:focus { border-color: #999; }
</style>ы