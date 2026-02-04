<template>
  <div class="tree-node">
    <div
      class="node-header"
      :class="{ active: isSelected }"
      @click="$emit('select', node)"
      @contextmenu.prevent="showContextMenu"
    >
      <button
        v-if="node.children?.length"
        class="expand-btn"
        @click.stop="isExpanded = !isExpanded"
      >
        {{ isExpanded ? '▼' : '▶' }}
      </button>
      <span v-else class="expand-placeholder"></span>
      <span class="node-name">{{ node.name }}</span>
    </div>

    <div v-if="showMenu" class="context-menu" :style="menuPosition">
      <button 
        v-if="node.type === 'station'" 
        class="context-menu-item" 
        @click="openFileImport"
      >
        📁 Import GSDML
      </button>
      <button 
        v-if="node.type === 'station' || node.type === 'interface' || node.type === 'server'" 
        class="context-menu-item delete-item" 
        @click="deleteNode"
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

    <div v-if="isExpanded && node.children?.length" class="node-children">
      <TreeNode
        v-for="child in node.children"
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

const props = defineProps({
  node: Object,
  selectedId: String
})

defineEmits(['select'])

const store = useDeviceStore()
const isExpanded = ref(true)
const showMenu = ref(false)
const menuPosition = ref({ top: 0, left: 0 })
const fileInput = ref(null)

const isSelected = computed(() => props.selectedId === props.node.id)

const showContextMenu = (event) => {
  menuPosition.value = {
    top: event.pageY + 'px',
    left: event.pageX + 'px'
  }
  showMenu.value = true
  document.addEventListener('click', hideContextMenu)
}

const hideContextMenu = () => {
  showMenu.value = false
  document.removeEventListener('click', hideContextMenu)
}

const openFileImport = () => {
  fileInput.value?.click()
  hideContextMenu()
}

const deleteNode = () => {
  const confirmDelete = confirm(`Вы уверены, что хотите удалить "${props.node.name}"?`)
  if (confirmDelete) {
    store.deleteNode(props.node.id)
  }
  hideContextMenu()
}

const handleFileImport = async (event) => {
  const file = event.target.files?.[0]
  if (!file) return

  try {
    const text = await file.text()
    const parser = new DOMParser()
    const xmlDoc = parser.parseFromString(text, 'application/xml')

    if (xmlDoc.getElementsByTagName('parsererror').length > 0) {
      alert('Ошибка при чтении XML файла')
      return
    }

    const gsdmlData = parseGSDML(xmlDoc)
    store.importGSDML(props.node.id, gsdmlData)

    alert('GSDML файл импортирован успешно!')
  } catch (error) {
    console.error('Ошибка при импорте файла:', error)
    alert('Ошибка при импорте файла: ' + error.message)
  } finally {
    event.target.value = ''
  }
}

const parseGSDML = (xmlDoc) => {
  const gsdmlData = {
    identifier: '',
    manufacturer: '',
    model: '',
    version: '',
    description: ''
  }

  const projectInfo = xmlDoc.getElementsByTagName('ProjectInformation')[0]
  if (projectInfo) {
    const vendor = projectInfo.getElementsByTagName('Vendor')[0]
    if (vendor) gsdmlData.manufacturer = vendor.textContent || ''
  }

  const deviceIdentity = xmlDoc.getElementsByTagName('DeviceIdentity')[0]
  if (deviceIdentity) {
    const vendor = deviceIdentity.getAttribute('Vendor') || 
                   deviceIdentity.getElementsByTagName('Vendor')[0]?.textContent || ''
    const model = deviceIdentity.getAttribute('Model') || 
                  deviceIdentity.getElementsByTagName('Model')[0]?.textContent || ''
    const version = deviceIdentity.getAttribute('Version') || 
                    deviceIdentity.getElementsByTagName('Version')[0]?.textContent || ''

    if (vendor) gsdmlData.manufacturer = vendor
    if (model) gsdmlData.model = model
    if (version) gsdmlData.version = version
  }

  const name = xmlDoc.getElementsByTagName('Name')[0] ||
               xmlDoc.getElementsByTagName('DeviceName')[0]
  if (name) {
    gsdmlData.identifier = name.textContent || ''
    gsdmlData.description = name.textContent || ''
  }

  const description = xmlDoc.getElementsByTagName('Description')[0]
  if (description) {
    gsdmlData.description = description.textContent || gsdmlData.description
  }

  return gsdmlData
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
  padding: 8px 12px;
  margin: 2px 4px;
  border-radius: 4px;
  cursor: pointer;
  transition: all 0.2s;
}

.node-header:hover {
  background: #f0f6fc;
}

.node-header.active {
  background: #d8e8f5;
  font-weight: 500;
}

.expand-btn {
  background: none;
  border: none;
  padding: 0 4px;
  cursor: pointer;
  color: #2c5aa0;
  width: 24px;
  text-align: center;
  font-weight: 600;
}

.expand-placeholder {
  width: 24px;
}

.node-name {
  flex: 1;
  color: #333;
  font-size: 13px;
}

.node-children {
  margin-left: 12px;
  border-left: 1px solid #e0e8f0;
}

.context-menu {
  position: fixed;
  background: white;
  border: 1px solid #ccc;
  border-radius: 4px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
  z-index: 1000;
  min-width: 150px;
}

.context-menu-item {
  display: block;
  width: 100%;
  padding: 10px 16px;
  background: transparent;
  border: none;
  text-align: left;
  cursor: pointer;
  font-size: 13px;
  color: #333;
  transition: background 0.2s;
}

.context-menu-item:hover {
  background: #f0f6fc;
}

.context-menu-item.delete-item {
  color: #dc3545;
  border-top: 1px solid #eee;
}

.context-menu-item.delete-item:hover {
  background: #ffe0e0;
}
</style>
