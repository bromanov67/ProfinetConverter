<template>
  <div class="details-panel">
    <template v-if="store.selectedNode">
      <div class="details-header">
        <h2>{{ store.selectedNode.name }}</h2>
        <span class="details-type">{{ store.selectedNode.type }}</span>
      </div>

      <!-- PROFINET SERVER -->
      <div v-if="store.selectedNode.type === 'server'" class="details-content">
        <table class="properties-table">
          <tbody>
            <tr>
              <td class="prop-label">Активация</td>
              <td class="prop-value">
                <input type="checkbox" v-model="store.selectedNode.active" />
              </td>
            </tr>
            <tr>
              <td class="prop-label">Имя</td>
              <td class="prop-value">
                <input
                  type="text"
                  v-model="store.selectedNode.name"
                  class="input-field"
                />
              </td>
            </tr>
            <tr>
              <td class="prop-label">Описание</td>
              <td class="prop-value">
                <input
                  type="text"
                  v-model="store.selectedNode.description"
                  class="input-field"
                />
              </td>
            </tr>
            <tr>
              <td class="prop-label">Версия протокола</td>
              <td class="prop-value">{{ store.selectedNode.protocolVersion }}</td>
            </tr>
            <tr>
              <td class="prop-label">Класс мастера</td>
              <td class="prop-value">{{ store.selectedNode.masterClass }}</td>
            </tr>
          </tbody>
        </table>
      </div>

        <!-- ETHERNET INTERFACE -->
    <div v-else-if="store.selectedNode.type === 'interface'" class="details-content">
        <table class="properties-table">
        <tbody>
        <tr>
            <td class="prop-label">Активация</td>
            <td class="prop-value">
            <input type="checkbox" v-model="store.selectedNode.active" />
            </td>
        </tr>
        <tr>
            <td class="prop-label">Имя</td>
            <td class="prop-value">{{ store.selectedNode.name }}</td>
        </tr>
        <tr>
            <td class="prop-label">Описание</td>
            <td class="prop-value">
            <input
                type="text"
                v-model="store.selectedNode.description"
                class="input-field"
            />
            </td>
        </tr>
        <tr>
            <td class="prop-label">Адрес ведущего устройства</td>
            <td class="prop-value">
            <input
                type="text"
                v-model="store.selectedNode.deviceAddress"
                class="input-field"
            />
            </td>
        </tr>
        <tr>
            <td class="prop-label">Порт</td>
            <td class="prop-value">
            <input
                type="text"
                v-model="store.selectedNode.port"
                class="input-field"
            />
            </td>
        </tr>
        <tr>
            <td class="prop-label">Скорость</td>
            <td class="prop-value">
            <select v-model="store.selectedNode.speed" class="select-field">
                <option value="9600">9600</option>
                <option value="19200">19200</option>
                <option value="38400">38400</option>
                <option value="57600">57600</option>
                <option value="115200">115200</option>
            </select>
            </td>
        </tr>
        <tr>
            <td class="prop-label">Уровень отладки</td>
            <td class="prop-value">
            <select v-model="store.selectedNode.busLevel" class="select-field">
                <option value="0">0</option>
                <option value="1">1</option>
                <option value="2">2</option>
            </select>
            </td>
        </tr>
        <tr>
            <td class="prop-label">Тип устройства</td>
            <td class="prop-value">
            <select v-model="store.selectedNode.deviceType" class="select-field">
                <option value="serial">serial</option>
                <option value="dummy_slave">dummy_slave</option>
            </select>
            </td>
        </tr>
        </tbody>
        </table>
    </div>


      <!-- STATION -->
        <div v-else-if="store.selectedNode.type === 'station'" class="details-content">
        <table class="properties-table">
            <tbody>
            <tr>
                <td class="prop-label">Активация</td>
                <td class="prop-value">
                <input type="checkbox" v-model="store.selectedNode.active" />
                </td>
            </tr>
            <tr>
                <td class="prop-label">Имя</td>
                <td class="prop-value">{{ store.selectedNode.name }}</td>
            </tr>
            <tr>
                <td class="prop-label">Описание</td>
                <td class="prop-value">
                <input
                    type="text"
                    v-model="store.selectedNode.description"
                    class="input-field"
                />
                </td>
            </tr>
            <tr class="section-header" @click="toggleConfigSection">
                <td colspan="2" style="cursor: pointer;">
                <span class="expand-icon">{{ configExpanded ? '▼' : '▶' }}</span>
                Конфигурация
                </td>
            </tr>
            <template v-if="configExpanded">
                <tr>
                <td class="prop-label">Адрес станции</td>
                <td class="prop-value">{{ store.selectedNode.address }}</td>
                </tr>
                <tr class="subsection" @click="toggleGsdmlSection">
                <td colspan="2" style="cursor: pointer;">
                    <span class="expand-icon">{{ gsdmlExpanded ? '▼' : '▶' }}</span>
                    Общее описание станции (GSDML)
                </td>
                </tr>
                <template v-if="gsdmlExpanded">
                <tr>
                    <td class="prop-label">Идентификатор</td>
                    <td class="prop-value">
                    <input
                        type="text"
                        v-model="store.selectedNode.configuration.identifier"
                        class="input-field"
                        disabled
                    />
                    </td>
                </tr>
                <tr>
                    <td class="prop-label">Производитель</td>
                    <td class="prop-value">
                    <input
                        type="text"
                        v-model="store.selectedNode.configuration.manufacturer"
                        class="input-field"
                        disabled
                    />
                    </td>
                </tr>
                <tr>
                    <td class="prop-label">Модель</td>
                    <td class="prop-value">
                    <input
                        type="text"
                        v-model="store.selectedNode.configuration.model"
                        class="input-field"
                        disabled
                    />
                    </td>
                </tr>
                <tr>
                    <td class="prop-label">Версия</td>
                    <td class="prop-value">
                    <input
                        type="text"
                        v-model="store.selectedNode.configuration.version"
                        class="input-field"
                        disabled
                    />
                    </td>
                </tr>
                <tr>
                    <td class="prop-label">Последовательность</td>
                    <td class="prop-value">
                    <input
                        type="text"
                        v-model="store.selectedNode.configuration.consistency"
                        class="input-field"
                    />
                    </td>
                </tr>
                </template>
            </template>
            </tbody>
        </table>
        </div>


      <!-- BUTTONS -->
      <div v-if="store.selectedNode.type === 'project'" class="details-actions">
        <button class="action-btn" @click="addServer">Add Server</button>
      </div>

      <div v-if="store.selectedNode.type === 'server'" class="details-actions">
        <button class="action-btn" @click="addInterface">Add Interface</button>
      </div>

      <div v-else-if="store.selectedNode.type === 'interface'" class="details-actions">
        <button class="action-btn" @click="addStation">Add Station</button>
      </div>
    </template>

    <div v-else class="details-empty">
      <p>Select an item to view details</p>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useDeviceStore } from '../stores/deviceStore'

const configExpanded = ref(true)
const gsdmlExpanded = ref(true)

const toggleConfigSection = () => {
  configExpanded.value = !configExpanded.value
}

const toggleGsdmlSection = () => {
  gsdmlExpanded.value = !gsdmlExpanded.value
}

const store = useDeviceStore()

const addServer = () => {
  store.addServer(store.selectedNode.id)
}

const addInterface = () => {
  store.addInterface(store.selectedNode.id)
}

const addStation = () => {
  store.addStation(store.selectedNode.id)
}
</script>

<style scoped>
.details-panel {
  flex: 1;
  background: white;
  display: flex;
  flex-direction: column;
  border-left: 1px solid #e0e8f0;
}

.details-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 20px;
  border-bottom: 1px solid #e0e8f0;
  background: #f5f5f5;
}

.details-header h2 {
  margin: 0;
  color: #333;
  font-size: 16px;
}

.details-type {
  background: #e8f0f8;
  color: #2c5aa0;
  padding: 4px 10px;
  border-radius: 3px;
  font-size: 11px;
  font-weight: 600;
  text-transform: uppercase;
}

.details-content {
  flex: 1;
  overflow-y: auto;
  padding: 0;
}

.expand-icon {
  display: inline-block;
  width: 4px;
  margin-right: 8px;
  font-weight: 600;
  color: #333;
}

.properties-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 13px;
  background: #f0f0f0;
  color: #333;
}

.properties-table tbody tr {
  border-bottom: 1px solid #ddd;
}

.properties-table td {
  padding: 8px 12px;
  vertical-align: middle;
}

.prop-label {
  width: 50%;
  background: #e8e8e8;
  font-weight: 500;
  color: #444;
}

.prop-value {
  width: 50%;
  background: #f5f5f5;
  color: #333;
}

.input-field {
  width: 100%;
  background: white;
  border: 1px solid #ccc;
  color: #333;
  padding: 4px 6px;
  border-radius: 2px;
  font-size: 12px;
}

.input-field:disabled {
  background: #e8e8e8;
  color: #666;
  cursor: not-allowed;
  border-color: #bbb;
}

.input-field:focus {
  outline: none;
  border-color: #2c5aa0;
  background: #fafafa;
}

.select-field {
  width: 100%;
  background: white;
  border: 1px solid #ccc;
  color: #333;
  padding: 4px 6px;
  border-radius: 2px;
  font-size: 12px;
  font-family: inherit;
  cursor: pointer;
}

.select-field:focus {
  outline: none;
  border-color: #2c5aa0;
  background: #fafafa;
}

.section-header {
  background: #d0d0d0 !important;
  font-weight: 600;
  color: #333 !important;
}

.section-header td {
  padding: 10px 12px !important;
}

.subsection {
  background: #e0e0e0 !important;
  color: #666 !important;
  font-style: italic;
}

.details-actions {
  display: flex;
  gap: 10px;
  padding: 16px 20px;
  border-top: 1px solid #e0e8f0;
  background: #f5f5f5;
}

.action-btn {
  flex: 1;
  padding: 10px 16px;
  background: #2c5aa0;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-weight: 500;
  font-size: 13px;
}

.action-btn:hover {
  background: #1e3f6b;
}

.details-empty {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  color: #999;
  font-style: italic;
}
</style>
