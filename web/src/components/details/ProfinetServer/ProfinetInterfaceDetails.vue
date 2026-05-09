<template>
  <div class="details-content">
    <table class="properties-table">
      <tbody>
        <tr>
          <td class="prop-label">Активация</td>
          <td class="prop-value">
            <input type="checkbox" v-model="node.active" />
          </td>
        </tr>
        <tr>
          <td class="prop-label">Имя</td>
          <td class="prop-value">{{ node.name }}</td>
        </tr>
        <tr>
          <td class="prop-label">Описание</td>
          <td class="prop-value">
            <input type="text" v-model="node.description" class="input-field" />
          </td>
        </tr>

        <tr v-if="node.deviceAddress !== undefined">
          <td class="prop-label">Device Address</td>
          <td class="prop-value">
            <input type="text" v-model="node.deviceAddress" class="input-field" />
          </td>
        </tr>
        <tr v-if="node.port !== undefined">
          <td class="prop-label">Port</td>
          <td class="prop-value">
            <input type="text" v-model="node.port" class="input-field" />
          </td>
        </tr>
        <tr v-if="node.speed !== undefined">
          <td class="prop-label">Speed</td>
          <td class="prop-value">
            <select v-model="node.speed" class="select-field">
              <option :value="9600">9600</option>
              <option :value="19200">19200</option>
              <option :value="38400">38400</option>
              <option :value="57600">57600</option>
              <option :value="115200">115200</option>
            </select>
          </td>
        </tr>
        <tr v-if="node.busLevel !== undefined">
          <td class="prop-label">Bus Level</td>
          <td class="prop-value">
            <select v-model="node.busLevel" class="select-field">
              <option :value="0">0</option>
              <option :value="1">1</option>
              <option :value="2">2</option>
            </select>
          </td>
        </tr>
        <tr v-if="node.deviceType !== undefined">
          <td class="prop-label">Device Type</td>
          <td class="prop-value">
            <select v-model="node.deviceType" class="select-field">
              <option value="serial">serial</option>
              <option value="dummyslave">dummyslave</option>
            </select>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
  
  <div class="details-actions">
    <button 
      v-if="!isIec" 
      class="action-btn" 
      @click="emitAddStation"
    >
      + Add Station
    </button>

    <button 
      v-if="isIec" 
      class="action-btn" 
      @click="emitAddChannel"
    >
      + Add Channel (Канал)
    </button>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { useDeviceStore } from '../../../stores/deviceStore'

const props = defineProps({ node: Object })
const store = useDeviceStore()

const isIec = computed(() => {
  if (props.node.type === 'interface_iec') return true;
  if (props.node.type === 'interface_profinet') return false;
  
  const parentServer = store.nodes?.find(n => 
    n.interfaces?.some(i => i.id === props.node.id) || 
    n.channels?.some(c => c.id === props.node.id)
  );

  return parentServer?.type === 'server_iec104' || parentServer?.type === 'iec104';
})

const emitAddStation = async () => {
  const name = prompt('Enter Station Name:', 'Station')
  if (name) {
    try {
      await store.addStation(props.node.id, name)
    } catch(e) {
      alert('Ошибка добавления станции: ' + e)
    }
  }
}

const emitAddChannel = async () => {
  const name = prompt('Введите имя канала:', 'Канал')
  if (name) {
    try {
      await store.addIecChannel(props.node.id, name)
    } catch(e) {
      alert('Ошибка добавления канала: ' + e)
    }
  }
}
</script>

<style scoped>
.select-field {
  width: 100%;
  background: white;
  border: 1px solid #ccc;
  color: #333;
  padding: 4px 6px;
  border-radius: 2px;
  font-size: 12px;
  cursor: pointer;
}
</style>