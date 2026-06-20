<template>
  <div class="tree-node">
    <div
      class="node-header"
      :class="{ active: isSelected, 'slot-highlight': isSlotHighlighted }"
      @click="emitSelect"
      @contextmenu.prevent="showContextMenu"
    >
      <button
        v-if="hasChildren"
        class="expand-btn"
        @click.stop="isExpanded = !isExpanded"
        :aria-label="isExpanded ? 'Collapse node' : 'Expand node'"
      >
        {{ isExpanded ? '▼' : '▶' }}
      </button>

      <span v-else class="expand-placeholder"></span>

      <span class="node-icon">{{ icon }}</span>
      <span class="node-name">{{ node.name }}</span>
    </div>

    <div v-if="showMenu" class="context-menu" :style="menuPosition">
      <button
        v-if="normalizedType === 'project'"
        class="context-menu-item"
        @click="emitAdd('server_profinet')"
      >
        + PROFINET
      </button>

      <button
        v-if="normalizedType === 'project'"
        class="context-menu-item"
        @click="emitAdd('server_iec104')"
      >
        + МЭК 104 Сервер
      </button>

      <button
        v-if="['server', 'server_profinet', 'server_iec104'].includes(normalizedType)"
        class="context-menu-item"
        @click="emitAdd('interface')"
      >
        + Add Interface
      </button>

      <button
        v-if="['interface_profinet', 'interface'].includes(normalizedType) && !isIecNode"
        class="context-menu-item"
        @click="emitAdd('station')"
      >
        + Add Device
      </button>

      <button
        v-if="normalizedType === 'interface_iec' || (normalizedType === 'interface' && isIecNode)"
        class="context-menu-item"
        @click="emitAdd('channel_iec')"
      >
        + Add Channel (Канал)
      </button>

      <div
        v-if="!['slot', 'signals_folder', 'commands_folder', 'iec_signals_folder', 'iec_commands_folder'].includes(normalizedType)"
        class="divider"
      ></div>

      <button
        v-if="normalizedType === 'station'"
        class="context-menu-item"
        @click="openFileImport"
      >
        📁 Import GSDML
      </button>

      <button
        v-if="normalizedType === 'slot' && node.module"
        class="context-menu-item delete-item"
        @click="handleClearSlot"
      >
        🗑️ Очистить модуль
      </button>

      <button
        v-if="normalizedType !== 'slot'"
        class="context-menu-item delete-item"
        @click="handleDelete"
      >
        🗑️ Delete
      </button>
    </div>

    <input
      ref="fileInput"
      type="file"
      accept=".gsdml,.xml"
      style="display: none"
      @change="handleFileImport"
    />

    <div v-if="isExpanded && hasChildren" class="node-children">
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
import { ref, computed, onBeforeUnmount } from 'vue'
import { useDeviceStore } from '../stores/deviceStore'

defineOptions({
  name: 'TreeNode'
})

const props = defineProps({
  node: {
    type: Object,
    required: true
  },
  selectedId: {
    type: String,
    default: null
  }
})

const emit = defineEmits(['select'])

const store = useDeviceStore()
const isExpanded = ref(true)
const showMenu = ref(false)
const menuPosition = ref({ top: '0px', left: '0px' })
const fileInput = ref(null)

const normalizeNodeType = (type) => {
  if (typeof type === 'string') {
    const t = type.toLowerCase()

    if (t === 'profinet') return 'server_profinet'
    if (t === 'iec104' || t === 'iec') return 'server_iec104'
    if (t === 'server_profinet') return 'server_profinet'
    if (t === 'server_iec104') return 'server_iec104'
    if (t === 'interface_profinet') return 'interface_profinet'
    if (t === 'interface_iec') return 'interface_iec'
    if (t === 'interface') return 'interface'
    if (t === 'project') return 'project'
    if (t === 'station') return 'station'
    if (t === 'channel_iec') return 'channel_iec'
    if (t === 'slot') return 'slot'
    if (t === 'signals_folder') return 'signals_folder'
    if (t === 'commands_folder') return 'commands_folder'
    if (t === 'iec_signals_folder') return 'iec_signals_folder'
    if (t === 'iec_commands_folder') return 'iec_commands_folder'

    return t
  }

  if (typeof type === 'number') {
    if (type === 0) return 'server_profinet'
    if (type === 1) return 'server_iec104'
  }

  return 'unknown'
}

const normalizedType = computed(() => normalizeNodeType(props.node?.type))

const icon = computed(() => {
  switch (normalizedType.value) {
    case 'project':
      return '📁'
    case 'server_profinet':
      return '🖥️'
    case 'server_iec104':
      return '📟'
    case 'interface':
    case 'interface_profinet':
    case 'interface_iec':
      return '🔌'
    case 'station':
      return '⚙️'
    case 'channel_iec':
      return '📡'
    case 'slot':
      return props.node.module ? '📦' : '⬜'
    case 'signals_folder':
    case 'iec_signals_folder':
      return '📥'
    case 'commands_folder':
    case 'iec_commands_folder':
      return '📤'
    default:
      return '📄'
  }
})

const isSelected = computed(() => props.selectedId === props.node.id)

const isSlotHighlighted = computed(() =>
  normalizedType.value === 'slot' &&
  store.hoveredModule !== null &&
  store.hoveredModule?.allowedInSlots?.includes(props.node.slotNumber)
)

const childNodes = computed(() => {
  if (!props.node) return []

  const sources = [
    props.node.children,
    props.node.servers,
    props.node.interfaces,
    props.node.stations,
    props.node.channels
  ]

  for (const source of sources) {
    if (Array.isArray(source) && source.length > 0) {
      return source
    }
  }

  if (normalizedType.value === 'station') {
    const slots = props.node.configuration?.slots
    if (Array.isArray(slots) && slots.length > 0) {
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

  if (normalizedType.value === 'slot' && props.node.module) {
    const mod = props.node.module
    const children = []

    let dataIn = mod.inputLength || 0
    let dataOut = mod.outputLength || 0

    if (Array.isArray(mod.submodules) && mod.submodules.length > 0) {
      dataIn = mod.submodules.reduce((sum, sm) => sum + (sm.inputLength || 0), 0)
      dataOut = mod.submodules.reduce((sum, sm) => sum + (sm.outputLength || 0), 0)
    } else if (dataIn === 0 && dataOut === 0) {
      const id = String(mod.id || '').toUpperCase()
      if (id.includes('IN')) dataIn = 1
      if (id.includes('OUT')) dataOut = 1
      if (!id.includes('IN') && !id.includes('OUT')) {
        dataIn = 1
        dataOut = 1
      }
    }

    if (dataIn > 0) {
      children.push({
        id: `${props.node.id}__signals`,
        type: 'signals_folder',
        name: 'Сигналы',
        stationId: props.node.parentStationId,
        slotNumber: props.node.slotNumber,
        module: mod
      })
    }

    if (dataOut > 0) {
      children.push({
        id: `${props.node.id}__commands`,
        type: 'commands_folder',
        name: 'Команды',
        stationId: props.node.parentStationId,
        slotNumber: props.node.slotNumber,
        module: mod
      })
    }

    return children
  }

  if (normalizedType.value === 'channel_iec') {
    return [
      {
        id: `${props.node.id}_cmds`,
        type: 'iec_commands_folder',
        name: 'Команды',
        channelId: props.node.id
      },
      {
        id: `${props.node.id}_sigs`,
        type: 'iec_signals_folder',
        name: 'Сигналы',
        channelId: props.node.id
      }
    ]
  }

  return []
})

const hasChildren = computed(() => childNodes.value.length > 0)

const isIecNode = computed(() => {
  if (normalizedType.value === 'interface_iec') return true
  if (normalizedType.value === 'interface_profinet') return false

  let parentServer = null

  for (const proj of store.projects || []) {
    for (const srv of proj.servers || []) {
      if (Array.isArray(srv.interfaces) && srv.interfaces.some(i => i.id === props.node.id)) {
        parentServer = srv
        break
      }
    }
    if (parentServer) break
  }

  return normalizeNodeType(parentServer?.type) === 'server_iec104'
})

const emitSelect = () => {
  emit('select', props.node)
}

const hideContextMenu = () => {
  showMenu.value = false
  document.removeEventListener('click', hideContextMenu)
}

const showContextMenu = (event) => {
  if (normalizedType.value === 'slot' && !props.node.module) return

  document.dispatchEvent(new Event('click'))

  setTimeout(() => {
    menuPosition.value = {
      top: `${event.pageY}px`,
      left: `${event.pageX}px`
    }
    showMenu.value = true
    document.addEventListener('click', hideContextMenu)
  }, 0)
}

const openFileImport = () => {
  fileInput.value?.click()
  hideContextMenu()
}

const handleDelete = async () => {
  if (confirm(`Удалить "${props.node.name}"?`)) {
    try {
      await store.deleteNode(props.node.id)
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

  try {
    let name = ''

    switch (actionType) {
      case 'server_profinet':
        name = prompt('Введите имя PROFINET:', 'Profinet')
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
        name = prompt('Введите имя устройства:', 'Device')
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

onBeforeUnmount(() => {
  document.removeEventListener('click', hideContextMenu)
})
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