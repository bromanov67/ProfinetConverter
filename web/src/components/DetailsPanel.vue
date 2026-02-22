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
        </tbody>
        </table>
    </div>


      <!-- STATION -->
        <div v-else-if="store.selectedNode.type === 'station'" class="details-content">
        <table class="properties-table">
          <tbody>
            <tr><td class="prop-label">Активация</td>
              <td class="prop-value"><input type="checkbox" v-model="store.selectedNode.active" /></td>
            </tr>
            <tr><td class="prop-label">Имя</td>
              <td class="prop-value">{{ store.selectedNode.name }}</td>
            </tr>
            <tr><td class="prop-label">Описание</td>
              <td class="prop-value">
                <input type="text" v-model="store.selectedNode.description" class="input-field" />
              </td>
            </tr>

            <!-- ▶ Конфигурация -->
            <tr class="section-header" @click="toggleSection('config')">
              <td colspan="2"><span class="expand-icon">{{ expanded.config ? '▼' : '▶' }}</span>Конфигурация</td>
            </tr>
            <template v-if="expanded.config">
              <tr><td class="prop-label">Адрес станции</td>
                <td class="prop-value">{{ store.selectedNode.address }}</td>
              </tr>

              <!-- ▶▶ Описание устройства GSDML -->
              <tr class="subsection-header" @click="toggleSection('gsdml')">
                <td colspan="2"><span class="expand-icon">{{ expanded.gsdml ? '▼' : '▶' }}</span>Общее описание устройства (GSDML)</td>
              </tr>
              <template v-if="expanded.gsdml">
                <tr><td class="prop-label">Краткое обозначение</td>
                  <td class="prop-value"><input class="input-field" disabled :value="store.selectedNode.configuration?.shortDesignation" /></td>
                </tr>
                <tr><td class="prop-label">Описание</td>
                  <td class="prop-value"><input class="input-field" disabled :value="store.selectedNode.configuration?.deviceDescription" /></td>
                </tr>
                <tr><td class="prop-label">Производитель</td>
                  <td class="prop-value"><input class="input-field" disabled :value="store.selectedNode.configuration?.manufacturer" /></td>
                </tr>
                <tr><td class="prop-label">Идентификатор устройства</td>
                  <td class="prop-value"><input class="input-field" disabled :value="store.selectedNode.configuration?.identifier" /></td>
                </tr>
                <tr><td class="prop-label">Артикул</td>
                  <td class="prop-value"><input class="input-field" disabled :value="store.selectedNode.configuration?.articleNo" /></td>
                </tr>
                <tr><td class="prop-label">Версия прошивки</td>
                  <td class="prop-value"><input class="input-field" disabled :value="store.selectedNode.configuration?.firmwareVersion" /></td>
                </tr>
                <tr><td class="prop-label">Версия аппаратного обеспечения</td>
                  <td class="prop-value"><input class="input-field" disabled :value="store.selectedNode.configuration?.hardwareVersion" /></td>
                </tr>
                <tr><td class="prop-label">GSD файл</td>
                  <td class="prop-value"><input class="input-field" disabled :value="store.selectedNode.configuration?.gsdFile" /></td>
                </tr>
                <tr><td class="prop-label">Последовательность</td>
                  <td class="prop-value">
                    <input class="input-field" v-model="store.selectedNode.configuration.consistency" />
                  </td>
                </tr>
              </template>

              <!-- ▶▶ IP-протокол -->
              <tr class="subsection-header" @click="toggleSection('ip')">
                <td colspan="2"><span class="expand-icon">{{ expanded.ip ? '▼' : '▶' }}</span>IP-протокол</td>
              </tr>
              <template v-if="expanded.ip">
                <tr><td class="prop-label">IP-адрес</td>
                  <td class="prop-value">
                    <input class="input-field" v-model="store.selectedNode.configuration.ipAddress" placeholder="192.168.0.1" />
                  </td>
                </tr>
                <tr><td class="prop-label">Маска подсети</td>
                  <td class="prop-value">
                    <input class="input-field" v-model="store.selectedNode.configuration.subnetMask" placeholder="255.255.255.0" />
                  </td>
                </tr>
              </template>

              <!-- ▶▶ PROFINET -->
              <tr class="subsection-header" @click="toggleSection('profinet')">
                <td colspan="2"><span class="expand-icon">{{ expanded.profinet ? '▼' : '▶' }}</span>PROFINET</td>
              </tr>
              <template v-if="expanded.profinet">
                <tr><td class="prop-label">Имя устройства PROFINET</td>
                  <td class="prop-value">
                    <input class="input-field" v-model="store.selectedNode.configuration.profinetDeviceName" />
                  </td>
                </tr>
                <tr><td class="prop-label">Номер устройства</td>
                  <td class="prop-value">
                    <input type="number" class="input-field" v-model.number="store.selectedNode.configuration.deviceNumber" min="1" />
                  </td>
                </tr>
              </template>
            </template>
          </tbody>
        </table>
      </div>

      <!-- ═══ SLOT ═══ -->
      <div v-else-if="store.selectedNode.type === 'slot'" class="details-content">
        <table class="properties-table">
          <tbody>
            <tr><td class="prop-label">Активация</td>
              <td class="prop-value"><input type="checkbox" v-model="store.selectedNode.active" /></td>
            </tr>
            <tr><td class="prop-label">Имя</td>
              <td class="prop-value">{{ store.selectedNode.name }}</td>
            </tr>
            <tr><td class="prop-label">Описание</td>
              <td class="prop-value">
                <input type="text" class="input-field" v-model="store.selectedNode.description" />
              </td>
            </tr>
            <tr><td class="prop-label">Тип данных [in]</td>
              <td class="prop-value">{{ slotDetails.dataTypeIn }}</td>
            </tr>
            <tr><td class="prop-label">Количество данных [in]</td>
              <td class="prop-value">{{ slotDetails.dataCountIn }}</td>
            </tr>
            <tr><td class="prop-label">Тип данных [out]</td>
              <td class="prop-value">{{ slotDetails.dataTypeOut }}</td>
            </tr>
            <tr><td class="prop-label">Количество данных [out]</td>
              <td class="prop-value">{{ slotDetails.dataCountOut }}</td>
            </tr>
            <tr><td class="prop-label">Модель</td>
              <td class="prop-value">{{ store.selectedNode.module?.name || '' }}</td>
            </tr>
            <tr><td class="prop-label">Предустановленный</td>
              <td class="prop-value"><input type="checkbox" disabled :checked="false" /></td>
            </tr>
            <tr><td class="prop-label">Слот</td>
              <td class="prop-value">{{ store.selectedNode.slotNumber }}</td>
            </tr>
            <tr><td class="prop-label">Последовательность</td>
              <td class="prop-value">{{ slotDetails.consistency }}</td>
            </tr>
          </tbody>
        </table>
        <!-- ═══ КАТАЛОГ ДЛЯ ЭТОГО СЛОТА ═══ -->
        <div v-if="slotCatalogModules.length" class="catalog-section">
          <div class="catalog-header" @click="toggleSection('catalog')">
            <span class="expand-icon">{{ expanded.catalog ? '▼' : '▶' }}</span>
            Доступные модули для этого слота ({{ slotCatalogModules.length }})
          </div>
          <template v-if="expanded.catalog">
            <input type="text" v-model="moduleSearch" placeholder="Поиск..." class="catalog-search" />
            <div class="catalog-list">
              <div
                v-for="mod in filteredSlotModules"
                :key="mod.id"
                class="catalog-item"
                :class="{ 'catalog-item-active': store.selectedNode.module?.id === mod.id }"
                @mouseenter="store.setHoveredModule(mod)"
                @mouseleave="store.setHoveredModule(null)"
                @click="assignModuleToThisSlot(mod)"
              >
                <span class="catalog-item-name">{{ mod.name }}</span>
                <span class="catalog-item-check" v-if="store.selectedNode.module?.id === mod.id">✓</span>
              </div>
            </div>
          </template>
        </div>
        <div v-else-if="!slotCatalogModules.length && parentStation" class="catalog-empty">
          <p>Нет доступных модулей для этого слота</p>
        </div>
        <div v-if="store.selectedNode.module" class="details-actions">
          <button class="action-btn danger-btn" @click="clearCurrentSlot">🗑️ Очистить модуль</button>
        </div>
      </div>

      <!-- ═══ КНОПКИ ═══ -->
      <div v-if="store.selectedNode.type === 'project'" class="details-actions">
        <button class="action-btn" @click="addServer">+ Add Server</button>
      </div>
      <div v-if="store.selectedNode.type === 'server'" class="details-actions">
        <button class="action-btn" @click="addInterface">+ Add Interface</button>
      </div>
      <div v-else-if="store.selectedNode.type === 'interface'" class="details-actions">
        <button class="action-btn" @click="addStation">+ Add Station</button>
      </div>
    </template>

    <div v-else class="details-empty"><p>Выберите элемент для просмотра</p></div>
  </div>
</template>


<script setup>
import { ref, computed, reactive } from 'vue'
import { useDeviceStore } from '../stores/deviceStore'

const store = useDeviceStore()

// Состояние раскрытых секций
const expanded = reactive({ config: true, gsdml: true, ip: true, profinet: true, catalog: true })
const toggleSection = (key) => { expanded[key] = !expanded[key] }

// ─── КАТАЛОГ ───

const parentStation = computed(() => {
  if (store.selectedNode?.type !== 'slot') return null
  const stationId = store.selectedNode.parentStationId
  for (const proj of store.projects) {
    for (const srv of proj.servers ?? []) {
      for (const iface of srv.interfaces ?? []) {
        const st = (iface.stations ?? []).find(s => s.id === stationId)
        if (st) return st
      }
    }
  }
  return null
})

// Модули, доступные для текущего слота (фильтр по allowedInSlots)
const slotCatalogModules = computed(() => {
  if (!parentStation.value) return []
  const slotNum = store.selectedNode?.slotNumber
  return (parentStation.value.configuration?.modules ?? [])
    .filter(m => m.allowedInSlots?.includes(slotNum))
})

// С учётом поиска
const filteredSlotModules = computed(() => {
  if (!moduleSearch.value) return slotCatalogModules.value
  const q = moduleSearch.value.toLowerCase()
  return slotCatalogModules.value.filter(m =>
    m.name?.toLowerCase().includes(q)
  )
})

// Применить модуль к текущему слоту
const assignModuleToThisSlot = (mod) => {
  const n = store.selectedNode
  if (n?.type === 'slot') {
    store.assignModule(n.parentStationId, n.slotNumber, mod)
  }
}

const moduleSearch = ref('')

const filteredModules = computed(() => {
  const all = store.selectedNode?.configuration?.modules || []
  if (!moduleSearch.value) return all
  const q = moduleSearch.value.toLowerCase()
  return all.filter(m => m.name?.toLowerCase().includes(q) || m.info?.toLowerCase().includes(q))
})

// Допустимые слоты по ID модуля (по GSDML FANUC: INPUT→1, OUTPUT→2, SAFETY→3)
const getAllowedSlots = (mod) => {
  const id = (mod.id || '').toUpperCase()
  if (id.startsWith('INPUT'))  return [1]
  if (id.startsWith('OUTPUT')) return [2]
  if (id.includes('SAFETY'))   return [3]
  return [1]
}

const isSlotTaken = (slotNum) => {
  const slots = store.selectedNode?.configuration?.slots || []
  return !!slots.find(s => s.number === slotNum)?.module
}

const assignModule = (mod, slotNum) => {
  store.assignModule(store.selectedNode.id, slotNum, mod)
}

// ─── ДЕТАЛИ СЛОТА ───
const slotDetails = computed(() => {
  const mod = store.selectedNode?.module
  if (!mod) return { dataTypeIn: 'BYTE', dataCountIn: 0, dataTypeOut: 'BYTE', dataCountOut: 0, consistency: '' }
  const id = (mod.id || '').toUpperCase()
  const numMatch = mod.name?.match(/(\d+)/)
  const bytes = numMatch ? parseInt(numMatch[1]) : 0
  if (id.startsWith('INPUT'))
    return { dataTypeIn: 'BYTE', dataCountIn: bytes, dataTypeOut: 'BYTE', dataCountOut: 0, consistency: 'All items consistency' }
  if (id.startsWith('OUTPUT'))
    return { dataTypeIn: 'BYTE', dataCountIn: 0, dataTypeOut: 'BYTE', dataCountOut: bytes, consistency: 'All items consistency' }
  if (id.includes('SAFETY')) {
    const m = id.match(/SAFETYINPUTOUTPUT([0-9A-F]{4})([0-9A-F]{4})/i)
    return {
      dataTypeIn: 'BYTE', dataCountIn: m ? parseInt(m[1], 16) : 0,
      dataTypeOut: 'BYTE', dataCountOut: m ? parseInt(m[2], 16) : 0,
      consistency: 'All items consistency'
    }
  }
  return { dataTypeIn: 'BYTE', dataCountIn: bytes, dataTypeOut: 'BYTE', dataCountOut: 0, consistency: '' }
})

const clearCurrentSlot = () => {
  const n = store.selectedNode
  if (n?.type === 'slot') store.clearSlotModule(n.parentStationId, n.slotNumber)
}

// ─── ДОБАВЛЕНИЕ УЗЛОВ ───
const addServer = async () => { const n = prompt('Server Name:'); if (n) await store.addServer(store.selectedNode.id, n) }
const addInterface = async () => { const n = prompt('Interface Name:'); if (n) await store.addInterface(store.selectedNode.id, n) }
const addStation = async () => { const n = prompt('Station Name:'); if (n) await store.addStation(store.selectedNode.id, n) }
</script>

<style scoped>
.catalog-section { border-top: 2px solid #2c5aa0; }
.catalog-header {
  display: flex; align-items: center; padding: 10px 12px;
  background: #2c5aa0; color: white; font-weight: 600;
  font-size: 13px; cursor: pointer; user-select: none;
}
.catalog-header:hover { background: #245090; }
.catalog-search {
  width: 100%; box-sizing: border-box; padding: 8px 12px;
  border: none; border-bottom: 1px solid #e0e0e0;
  font-size: 13px; background: #f9f9f9;
}
.catalog-list { max-height: 340px; overflow-y: auto; background: white; }
.catalog-item {
  display: flex; align-items: center; justify-content: space-between;
  padding: 6px 12px; border-bottom: 1px solid #f0f0f0; gap: 8px;
}
.catalog-item:hover { background: #f5f8ff; }
.catalog-item-name { font-size: 12px; font-weight: 500; color: #24292f; }
.catalog-item-actions { display: flex; gap: 4px; flex-shrink: 0; }
.slot-assign-btn {
  padding: 3px 8px; font-size: 11px; font-weight: 500;
  background: #e8f0f8; color: #2c5aa0;
  border: 1px solid #b0c8e0; border-radius: 3px; cursor: pointer;
}
.slot-assign-btn:hover { background: #2c5aa0; color: white; }
.slot-assign-btn.assigned { background: #e8f5e9; color: #2e7d32; border-color: #a5d6a7; }
.slot-assign-btn.assigned:hover { background: #e53935; color: white; border-color: #e53935; }
.catalog-empty { padding: 24px 20px; text-align: center; color: #888; border-top: 1px solid #e0e0e0; }
.catalog-hint { font-size: 11px; color: #aaa; }
.danger-btn { background: #cf222e !important; }
.danger-btn:hover { background: #a81c26 !important; }
.subsection-header { background: #dcdcdc !important; cursor: pointer; }
.subsection-header td { padding: 8px 20px !important; color: #555 !important; font-style: italic; }
.subsection-header:hover td { background: #d0d0d0 !important; }

.catalog-item-active {
  background: #e8f5e9;
  border-left: 3px solid #2e7d32;
}
.catalog-item-active .catalog-item-name { color: #2e7d32; font-weight: 600; }
.catalog-item-check { margin-left: auto; color: #2e7d32; font-weight: bold; }

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

/* --- Стили для каталога слотов и модулей --- */
.slots-container {
  display: flex;
  gap: 15px;
  align-items: flex-start;
}

.slots-list {
  flex: 1;
  border: 1px solid #d1d9e0;
  border-radius: 4px;
  background: #f6f8fa;
  overflow: hidden;
  box-shadow: inset 0 1px 3px rgba(0,0,0,0.05);
}

.slot-item {
  display: flex;
  padding: 10px 12px;
  border-bottom: 1px solid #e0e8f0;
  cursor: pointer;
  background: white;
  transition: all 0.2s ease;
}

.slot-item:last-child {
  border-bottom: none;
}

.slot-item:hover {
  background: #f0f6fc;
}

.slot-item.active {
  background: #e0efff;
  border-left: 4px solid #0969da;
}

.slot-number {
  width: 65px;
  font-weight: 600;
  color: #57606a;
  font-size: 13px;
}

.slot-module {
  flex: 1;
  font-size: 13px;
}

.module-badge {
  background: #e1eeb8;
  color: #3f5910;
  padding: 2px 6px;
  border-radius: 4px;
  font-weight: 500;
  border: 1px solid #c7dd88;
}

.empty-badge {
  color: #9a9a9a;
  font-style: italic;
}

.modules-panel {
  flex: 1.2;
  border: 1px solid #d1d9e0;
  border-radius: 4px;
  background: white;
  display: flex;
  flex-direction: column;
  max-height: 350px;
  box-shadow: 0 4px 12px rgba(0,0,0,0.08);
}

.modules-panel h4 {
  margin: 0;
  padding: 10px 12px;
  background: #f6f8fa;
  border-bottom: 1px solid #d1d9e0;
  font-size: 13px;
  color: #24292f;
}

.search-input {
  margin: 8px;
  padding: 6px 10px;
  border: 1px solid #d1d9e0;
  border-radius: 4px;
  font-size: 13px;
  outline: none;
}
.search-input:focus {
  border-color: #0969da;
}

.modules-list {
  flex: 1;
  overflow-y: auto;
}

.module-item {
  padding: 8px 12px;
  border-bottom: 1px solid #f0f6fc;
  cursor: pointer;
}

.module-item:hover {
  background: #f0f6fc;
}

.clear-item {
  background: #fff8f8;
  color: #cf222e;
}
.clear-item:hover {
  background: #ffebe9;
}

.module-item strong {
  display: block;
  font-size: 13px;
  color: #0969da;
}

.module-info {
  display: block;
  font-size: 11px;
  color: #57606a;
  margin-top: 2px;
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
