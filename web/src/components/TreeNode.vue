<template>
  <div class="tree-node">
    <div
      class="node-header"
      :class="{ active: isSelected, 'slot-highlight': isSlotHighlighted }"
      @click="$emit('select', node)"
      @contextmenu.prevent="showContextMenu"
    >
      <button v-if="childNodes.length" class="expand-btn" @click.stop="isExpanded = !isExpanded">
        {{ isExpanded ? '▼' : '▶' }}
      </button>
      <span v-else class="expand-placeholder"></span>
      <span class="node-icon">{{ getIcon(node.type) }}</span>
      <span class="node-name">{{ node.name }}</span>
    </div>

    <!-- Context Menu -->
    <div v-if="showMenu" class="context-menu" :style="menuPosition">
      
      <!-- Уровень Проекта -->
      <button v-if="node.type === 'project'" class="context-menu-item" @click="emitAdd('server_profinet')">
        + PROFINET Сервер
      </button>
      <button v-if="node.type === 'project'" class="context-menu-item" @click="emitAdd('server_iec104')">
        + МЭК 104 Сервер
      </button>
      
      <!-- Уровень Сервера -->
      <button v-if="node.type === 'server' || node.type === 'server_profinet' || node.type === 'server_iec104'" class="context-menu-item" @click="emitAdd('interface')">
        + Add Interface
      </button>
      
            <!-- Уровень Интерфейса (PROFINET) -->
      <button 
        v-if="(node.type === 'interface_profinet' || node.type === 'interface') && !isIecNode" 
        class="context-menu-item" 
        @click="emitAdd('station')"
      >
        + Add Station
      </button>
      
      <!-- Уровень Интерфейса (МЭК 104) -->
      <button 
        v-if="node.type === 'interface_iec' || (node.type === 'interface' && isIecNode)" 
        class="context-menu-item" 
        @click="emitAdd('channel_iec')"
      >
        + Add Channel (Канал)
      </button>

      <!-- Разделитель (не показываем для слотов и папок сигналов) -->
      <div v-if="['slot', 'signals_folder', 'commands_folder', 'iec_signals_folder', 'iec_commands_folder'].includes(node.type) === false" class="divider"></div>
      
      <!-- Специфичные действия -->
      <button v-if="node.type === 'station'" class="context-menu-item" @click="openFileImport">
        📁 Import GSDML
      </button>
      
      <!-- Действия для слотов -->
      <button v-if="node.type === 'slot' && node.module" class="context-menu-item delete-item" @click="handleClearSlot">
        🗑️ Очистить модуль
      </button>
      
      <!-- Удаление (для всех кроме слотов) -->
      <button v-if="node.type !== 'slot'" class="context-menu-item delete-item" @click="handleDelete">
        🗑️ Delete
      </button>
    </div>

    <!-- Скрытый инпут для импорта GSDML -->
    <input ref="fileInput" type="file" accept=".gsdml,.xml" style="display:none" @change="handleFileImport" />

    <!-- Рекурсивный рендер дочерних элементов -->
    <div v-if="isExpanded && childNodes.length" class="node-children">
      <TreeNode
        v-for="child in childNodes"
        :key="child.id"
        :node="child"
        :selected-id="selectedId"
        @select="$emit('select', $event)"
      />
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useDeviceStore } from '../stores/deviceStore'

const props = defineProps({ node: Object, selectedId: String })
const emit = defineEmits(['select'])

const store = useDeviceStore()
const isExpanded = ref(true)
const showMenu = ref(false)
const menuPosition = ref({ top: '0px', left: '0px' })
const fileInput = ref(null)

// Computed свойства состояния
const isSelected = computed(() => props.selectedId === props.node.id)

const isSlotHighlighted = computed(() =>
  props.node.type === 'slot' &&
  store.hoveredModule !== null &&
  store.hoveredModule?.allowedInSlots?.includes(props.node.slotNumber)
)

// ИСПРАВЛЕНИЕ 2: Ищем в store.projects вместо store.nodes
const isIecNode = computed(() => {
  if (props.node.type === 'interface_iec') return true;
  if (props.node.type === 'interface_profinet') return false;
  
  let parentServer = null;
  // Проходим по дереву проектов, чтобы найти сервер, которому принадлежит этот интерфейс
  for (const proj of store.projects || []) {
    for (const srv of proj.servers || []) {
      if (srv.interfaces && srv.interfaces.some(i => i.id === props.node.id)) {
        parentServer = srv;
        break;
      }
    }
    if (parentServer) break;
  }
  
  return parentServer?.type === 'server_iec104';
});

// ГЕНЕРАЦИЯ ДОЧЕРНИХ УЗЛОВ
const childNodes = computed(() => {
  if (!props.node) return []

  // 1. Стандартные вложенные массивы данных с бекенда
  const directChildren = props.node.children 
                      || props.node.servers 
                      || props.node.interfaces 
                      || props.node.stations 
                      || props.node.channels // <--- ВАЖНО: берем каналы для интерфейса МЭК

  if (directChildren && directChildren.length > 0) {
    return directChildren
  }

  // 2. Для Станции (Profinet) генерируем Слоты
  if (props.node.type === 'station') {
    const slots = props.node.configuration?.slots
    if (slots && slots.length > 0) {
      return slots.map(slot => ({
        id: `${props.node.id}__slot__${slot.number}`,
        type: 'slot',
        name: slot.module ? slot.module.name : (slot.label || `Слот ${slot.number}`),
        slotNumber: slot.number,
        label: slot.label,
        module: slot.module ?? null,
        parentStationId: props.node.id,
        active: true
      }))
    }
  }

    // 3. Для Слота (с назначенным модулем) генерируем папки Сигналов/Команд
  if (props.node.type === 'slot' && props.node.module) {
    const mod = props.node.module;
    const children = [];
    
    // Считаем данные так же, как в SlotDetails
    let dataIn = mod.inputLength || 0;
    let dataOut = mod.outputLength || 0;

    if (mod.submodules && mod.submodules.length > 0) {
      dataIn = mod.submodules.reduce((sum, sm) => sum + (sm.inputLength || 0), 0);
      dataOut = mod.submodules.reduce((sum, sm) => sum + (sm.outputLength || 0), 0);
    } 
    // Fallback: если длины 0, но это реальный модуль, просто покажем обе папки на всякий случай
    else if (dataIn === 0 && dataOut === 0) {
      const id = (mod.id || '').toUpperCase();
      if (id.includes('IN')) dataIn = 1;
      if (id.includes('OUT')) dataOut = 1;
      if (!id.includes('IN') && !id.includes('OUT')) {
        dataIn = 1; dataOut = 1; // Показываем обе, чтобы пользователь сам решил
      }
    }

    // Добавляем папку "Сигналы", если модуль имеет входы
    if (dataIn > 0) {
      children.push({
        id: `${props.node.id}__signals`,
        type: 'signals_folder',
        name: 'Сигналы',
        stationId: props.node.parentStationId,
        slotNumber: props.node.slotNumber,
        module: mod
      });
    }

    // Добавляем папку "Команды", если модуль имеет выходы
    if (dataOut > 0) {
      children.push({
        id: `${props.node.id}__commands`,
        type: 'commands_folder',
        name: 'Команды',
        stationId: props.node.parentStationId,
        slotNumber: props.node.slotNumber,
        module: mod
      });
    }
    
    return children;
  }

  // 5. Каналы (IEC 104) генерируют свои папки сигналов/команд
  if (props.node.type === 'channel_iec') {
    return [
      { id: `${props.node.id}_cmds`, type: 'iec_commands_folder', name: 'Команды', channelId: props.node.id },
      { id: `${props.node.id}_sigs`, type: 'iec_signals_folder', name: 'Сигналы', channelId: props.node.id }
    ]
  }

  return []
})

// ИКОНКИ
const getIcon = (type) => {
  switch (type?.toLowerCase()) {
    case 'project':           return '📁'
    case 'server_profinet':   return '🖥️'
    case 'server_iec104':     return '📟'
    case 'interface':         
    case 'interface_profinet':
    case 'interface_iec':     return '🔌'
    case 'station':           return '⚙️'
    case 'channel_iec':       return '📡' 
    case 'slot':              return props.node.module ? '📦' : '⬜'
    case 'signals_folder':    
    case 'iec_signals_folder': return '📥'
    case 'commands_folder':   
    case 'iec_commands_folder':return '📤'
    default:                  return '📄'
  }
}

// КОНТЕКСТНОЕ МЕНЮ
const showContextMenu = (event) => {
  if (props.node.type === 'slot' && !props.node.module) return;

  menuPosition.value = { top: event.pageY + 'px', left: event.pageX + 'px' }
  showMenu.value = true
  
  setTimeout(() => document.addEventListener('click', hideContextMenu), 0)
}

const hideContextMenu = () => {
  showMenu.value = false
  document.removeEventListener('click', hideContextMenu)
}

// ДЕЙСТВИЯ (ОБРАБОТЧИКИ)
const openFileImport = () => { 
  fileInput.value?.click()
  hideContextMenu() 
}

const handleDelete = async () => {
  if (confirm(`Удалить "${props.node.name}"?`)) {
    try { 
      await store.deleteNode(props.node.id, props.node.type) 
    } catch { 
      alert('Ошибка при удалении узла') 
    }
  }
  hideContextMenu()
}

const handleClearSlot = () => {
  store.clearSlotModule(props.node.parentStationId, props.node.slotNumber)
  hideContextMenu()
}

const handleFileImport = async (event) => {
  const file = event.target.files?.[0]
  if (!file) return
  
  try { 
    await store.importGsdml(props.node.id, file)
    alert('Импорт успешно завершен!') 
  } catch (error) { 
    alert('Ошибка импорта: ' + error.message) 
  } finally { 
    event.target.value = '' 
  }
}

const emitAdd = async (actionType) => {
  hideContextMenu()
  let name = ''
  
  try {
    switch (actionType) {
      case 'server_profinet':
        name = prompt('Введите имя PROFINET сервера:', 'Profinet Server')
        if (name) await store.addServer(props.node.id, name, 'server_profinet')
        break
        
      case 'server_iec104':
        name = prompt('Введите имя МЭК 104 сервера:', 'IEC 104 Server')
        if (name) await store.addServer(props.node.id, name, 'server_iec104')
        break
        
      case 'interface':
        name = prompt('Введите имя интерфейса:', 'Ethernet')
        if (name) await store.addInterface(props.node.id, name)
        break
        
      case 'station':
        name = prompt('Введите имя станции:', 'Station')
        if (name) await store.addStation(props.node.id, name)
        break
        
      case 'channel_iec':
        name = prompt('Введите имя канала:', 'Канал')
        if (name) await store.addIecChannel(props.node.id, name)
        break
    }
  } catch (e) {
    alert('Ошибка при создании: ' + e)
  }
}
</script>

<style scoped>
.tree-node {
  user-select: none;
  position: relative;
}

.node-header {
  display: flex;
  align-items: center;
  padding: 6px 8px;
  margin: 1px 0;
  border-radius: 4px;
  cursor: pointer;
  transition: background 0.1s;
}

.node-header.slot-highlight {
  background: #fff3cd;
  border-left: 3px solid #f59e0b;
  color: #92400e;
}

.node-header:hover {
  background: #f0f6fc;
}

.node-header.active {
  background: #e0efff;
  color: #005fb8;
}

.expand-btn {
  background: none;
  border: none;
  padding: 0;
  cursor: pointer;
  color: #666;
  width: 20px;
  font-size: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.expand-placeholder {
  width: 20px;
}

.node-icon {
  margin-right: 6px;
  font-size: 16px;
}

.node-name {
  flex: 1;
  font-size: 13px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.node-children {
  margin-left: 20px;
  border-left: 1px solid #eee;
}

/* Context Menu Styles */
.context-menu {
  position: fixed;
  background: white;
  border: 1px solid #d1d9e0;
  border-radius: 6px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  z-index: 9999;
  min-width: 160px;
  padding: 4px 0;
}

.context-menu-item {
  display: block;
  width: 100%;
  padding: 8px 16px;
  background: transparent;
  border: none;
  text-align: left;
  cursor: pointer;
  font-size: 13px;
  color: #24292f;
}

.context-menu-item:hover {
  background: #0969da;
  color: white;
}

.divider {
  height: 1px;
  background: #d1d9e0;
  margin: 4px 0;
}

.delete-item {
  color: #cf222e;
}

.delete-item:hover {
  background: #cf222e;
  color: white;
}
</style>
