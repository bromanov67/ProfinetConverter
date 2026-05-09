<template>
  <div class="details-content">
    <table class="properties-table">
      <tbody>
        <tr>
          <td class="prop-label">Активация</td>
          <td class="prop-value"><input type="checkbox" v-model="node.active" /></td>
        </tr>
        <tr>
          <td class="prop-label">Имя</td>
          <td class="prop-value">{{ node.name }}</td>
        </tr>
        <tr>
          <td class="prop-label">Описание</td>
          <td class="prop-value"><input type="text" class="input-field" v-model="node.description" /></td>
        </tr>
        <tr>
          <td class="prop-label">Тип данных [in]</td>
          <td class="prop-value">{{ slotDetails.dataTypeIn }}</td>
        </tr>
        <tr>
          <td class="prop-label">Количество данных [in]</td>
          <td class="prop-value">{{ slotDetails.dataCountIn }}</td>
        </tr>
        <tr>
          <td class="prop-label">Тип данных [out]</td>
          <td class="prop-value">{{ slotDetails.dataTypeOut }}</td>
        </tr>
        <tr>
          <td class="prop-label">Количество данных [out]</td>
          <td class="prop-value">{{ slotDetails.dataCountOut }}</td>
        </tr>
        <tr>
          <td class="prop-label">Модель</td>
          <td class="prop-value">{{ node.module?.name || '' }}</td>
        </tr>
        <tr>
          <td class="prop-label">Слот</td>
          <td class="prop-value">{{ node.slotNumber }}</td>
        </tr>
        <tr>
          <td class="prop-label">Последовательность</td>
          <td class="prop-value">{{ slotDetails.consistency }}</td>
        </tr>
      </tbody>
    </table>

    <div v-if="node.module?.submodules && node.module.submodules.length > 0" class="catalog-section">
      <div class="catalog-header" @click="expandedSubmodules = !expandedSubmodules">
        <span class="expand-icon">{{ expandedSubmodules ? '▼' : '▶' }}</span>
        Встроенные каналы ({{ node.module.submodules.length }})
      </div>
      
      <template v-if="expandedSubmodules">
        <div class="catalog-list">
          <div v-for="sm in node.module.submodules" :key="sm.id" class="catalog-item">
            <div style="display:flex; flex-direction:column; gap:4px;">
              <span class="catalog-item-name">{{ sm.name }}</span>
              <span style="font-size:11px; color:#666;">
                Subslot: {{ sm.fixedInSubslots?.[0] }} | In: {{ sm.inputLength }} B | Out: {{ sm.outputLength }} B
              </span>
            </div>
          </div>
        </div>
      </template>
    </div>

    <div v-if="slotCatalogModules.length > 0" class="catalog-section">
      <div class="catalog-header" @click="expandedCatalog = !expandedCatalog">
        <span class="expand-icon">{{ expandedCatalog ? '▼' : '▶' }}</span>
        Доступные модули ({{ slotCatalogModules.length }})
      </div>
      
      <template v-if="expandedCatalog">
        <input type="text" v-model="moduleSearch" placeholder="Поиск по модулям..." class="catalog-search" />
        <div class="catalog-list">
          <div
            v-for="mod in filteredSlotModules"
            :key="mod.id"
            class="catalog-item"
            :class="{ 'catalog-item-active': node.module?.id === mod.id }"
            @mouseenter="store.setHoveredModule(mod)"
            @mouseleave="store.setHoveredModule(null)"
            @click="assignModule(mod)"
          >
            <span class="catalog-item-name">{{ mod.name }}</span>
            <span class="catalog-item-check" v-if="node.module?.id === mod.id">✓</span>
          </div>
        </div>
      </template>
    </div>
    
    <div v-else-if="parentStation && !node.module?.isBuiltIn" class="catalog-empty">
      <p>Нет доступных модулей для этого слота</p>
    </div>
  </div>

  <div v-if="node.module && !node.module.isBuiltIn" class="details-actions">
    <button class="action-btn danger-btn" @click="clearCurrentSlot">🗑️ Очистить модуль</button>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useDeviceStore } from '../../../stores/deviceStore'

const props = defineProps({ node: Object })
const store = useDeviceStore()

const expandedCatalog = ref(true)
const expandedSubmodules = ref(true)
const moduleSearch = ref('')

const parentStation = computed(() => {
  if (props.node.type !== 'slot') return null
  const stationId = props.node.parentStationId
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

const slotCatalogModules = computed(() => {
  if (!parentStation.value) return []
  const slotNum = props.node.slotNumber
  return (parentStation.value.configuration?.modules ?? [])
    .filter(m => m.allowedInSlots?.includes(slotNum))
})

const filteredSlotModules = computed(() => {
  if (!moduleSearch.value) return slotCatalogModules.value
  const q = moduleSearch.value.toLowerCase()
  return slotCatalogModules.value.filter(m => m.name?.toLowerCase().includes(q))
})

const slotDetails = computed(() => {
  const mod = props.node.module
  if (!mod) return { dataTypeIn: 'BYTE', dataCountIn: 0, dataTypeOut: 'BYTE', dataCountOut: 0, consistency: '' }
  
  let dataIn = mod.inputLength || 0;
  let dataOut = mod.outputLength || 0;

  if (mod.submodules && mod.submodules.length > 0) {
    dataIn = mod.submodules.reduce((sum, sm) => sum + (sm.inputLength || 0), 0);
    dataOut = mod.submodules.reduce((sum, sm) => sum + (sm.outputLength || 0), 0);
  } 
  else if (dataIn === 0 && dataOut === 0) {
    const id = (mod.id || '').toUpperCase()
    const numMatch = mod.name?.match(/(\d+)/)
    const bytes = numMatch ? parseInt(numMatch[1]) : 0

    if (id.includes('IN')) dataIn = bytes
    else if (id.includes('OUT')) dataOut = bytes
    else dataIn = bytes
  }

  return { dataTypeIn: 'BYTE', dataCountIn: dataIn, dataTypeOut: 'BYTE', dataCountOut: dataOut, consistency: '' }
})

const assignModule = (mod) => {
  store.assignModule(props.node.parentStationId, props.node.slotNumber, mod)
}

const clearCurrentSlot = () => {
  store.clearSlotModule(props.node.parentStationId, props.node.slotNumber)
}
</script>

<style scoped>
.catalog-section { border-top: 2px solid #2c5aa0; margin-top: 10px; }
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
  padding: 8px 12px; border-bottom: 1px solid #f0f0f0; cursor: pointer;
}
.catalog-item:hover { background: #f5f8ff; }
.catalog-item-name { font-size: 12px; font-weight: 500; color: #24292f; }
.catalog-item-active { background: #e8f5e9; border-left: 3px solid #2e7d32; }
.catalog-item-active .catalog-item-name { color: #2e7d32; font-weight: 600; }
.catalog-item-check { color: #2e7d32; font-weight: bold; }
.catalog-empty { padding: 24px 20px; text-align: center; color: #888; border-top: 1px solid #e0e0e0; font-size: 13px; }
.danger-btn { background: #cf222e !important; }
.danger-btn:hover { background: #a81c26 !important; }
</style>