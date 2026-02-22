<template>
  <div class="tree-node">
    <!-- ИСПРАВЛЕНО: закрыли тег node-header и вернули его содержимое -->
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
      <button v-if="node.type === 'project'" class="context-menu-item" @click="emitAdd('server')">+ Add Server</button>
      <button v-if="node.type === 'server'" class="context-menu-item" @click="emitAdd('interface')">+ Add Interface</button>
      <button v-if="node.type === 'interface'" class="context-menu-item" @click="emitAdd('station')">+ Add Station</button>
      <div v-if="node.type !== 'slot'" class="divider"></div>
      <button v-if="node.type === 'station'" class="context-menu-item" @click="openFileImport">
        📁 Import GSDML
      </button>
      <!-- Для слота — только очистить модуль -->
      <button v-if="node.type === 'slot' && node.module"
        class="context-menu-item delete-item" @click="handleClearSlot">
        🗑️ Очистить модуль
      </button>
      <!-- Delete только для не-слотов -->
      <button v-if="node.type !== 'slot'" class="context-menu-item delete-item" @click="handleDelete">
        🗑️ Delete
      </button>
    </div>

    <input ref="fileInput" type="file" accept=".gsdml,.xml" style="display:none" @change="handleFileImport" />

    <!-- Рекурсивные дочерние узлы -->
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

const isSelected = computed(() => props.selectedId === props.node.id)

const isSlotHighlighted = computed(() =>
  props.node.type === 'slot' &&
  store.hoveredModule !== null &&
  store.hoveredModule?.allowedInSlots?.includes(props.node.slotNumber)
)

// дочерние узлы ───
const childNodes = computed(() => {
  const direct = props.node.children || props.node.servers
               || props.node.interfaces || props.node.stations
  if (direct) return direct

  if (props.node.type === 'station') {
    const slots = props.node.configuration?.slots
    if (slots?.length > 0) {
      return slots.map(slot => ({
        id: `${props.node.id}__slot__${slot.number}`,
        type: 'slot',
        // ↓ Теперь показываем метку категории вместо <пусто>
        name: slot.module
          ? slot.module.name
          : (slot.label || `Слот ${slot.number}`),
        slotNumber: slot.number,
        label: slot.label,
        module: slot.module ?? null,
        parentStationId: props.node.id,
        active: true,
        description: ''
      }))
    }
  }
  return []
})


const getIcon = (type) => {
  switch (type?.toLowerCase()) {
    case 'project':   return '📁'
    case 'server':    return '🖥️'
    case 'interface': return '🔌'
    case 'station':   return '⚙️'
    case 'slot':      return props.node.module ? '📦' : '⬜'
    default:          return '📄'
  }
}

const showContextMenu = (event) => {
  // 1. БЛОКИРУЕМ открытие меню для пустых слотов
  if (props.node.type === 'slot' && !props.node.module) {
    return; // Ничего не делаем, меню не покажется
  }

  // Для остальных узлов открываем
  menuPosition.value = { 
    top: event.pageY + 'px', 
    left: event.pageX + 'px' 
  }
  showMenu.value = true
  setTimeout(() => document.addEventListener('click', hideContextMenu), 0)
}

const hideContextMenu = () => {
  showMenu.value = false
  document.removeEventListener('click', hideContextMenu)
}
const openFileImport = () => { fileInput.value?.click(); hideContextMenu() }

const handleDelete = async () => {
  if (confirm(`Delete "${props.node.name}"?`)) {
    try { await store.deleteNode(props.node.id, props.node.type) }
    catch { alert('Error deleting node') }
  }
  hideContextMenu()
}

// Очистка модуля из слота через стор
const handleClearSlot = () => {
  store.clearSlotModule(props.node.parentStationId, props.node.slotNumber)
  hideContextMenu()
}

const handleFileImport = async (event) => {
  const file = event.target.files?.[0]
  if (!file) return
  try { await store.importGsdml(props.node.id, file); alert('Import successful!') }
  catch (error) { alert('Import failed: ' + error.message) }
  finally { event.target.value = '' }
}

const emitAdd = async (type) => {
  const name = prompt(`Enter name for new ${type}:`)
  if (!name) return
  try {
    if (type === 'server')    await store.addServer(props.node.id, name)
    if (type === 'interface') await store.addInterface(props.node.id, name)
    if (type === 'station')   await store.addStation(props.node.id, name)
  } catch (e) { alert(e) }
  hideContextMenu()
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
  margin-left: 20px; /* Отступ */
  border-left: 1px solid #eee; /* Линия для красоты */
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
