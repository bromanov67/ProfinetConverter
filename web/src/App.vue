<template>
  <div class="app-container">
    <Header />

    <div v-if="loading" class="loading-overlay">
      <div class="spinner"></div>
      <p>Загрузка...</p>
    </div>

    <div v-else-if="error" class="error-overlay">
      <p>❌ {{ error }}</p>
      <button @click="handleReload">Повторить</button>
    </div>

    <div v-else class="app-content">
      <div class="tree-panel">
        <div class="tree-header">
          <h3>Projects</h3>
          <div class="header-actions">
            <button class="add-btn open-btn" @click="triggerOpenDialog">
              📂 Open
            </button>

            <button class="add-btn project-btn" @click="handleCreateProject">
              + Project
            </button>

            <button
              class="add-btn save-btn"
              @click="handleSaveProject"
              :disabled="projects.length === 0"
              title="Сохранить в конфигурационный файл"
            >
              💾 Save
            </button>

            <button
              @click="store.toggleRuntimeMode()"
              :class="['action-btn', { 'runtime-active': isRuntimeMode }]"
              style="margin-left: 20px;"
            >
              {{ isRuntimeMode ? '🛑 Остановить режим' : '▶ Режим исполнения' }}
            </button>
          </div>
        </div>

        <div class="tree-container">
          <div v-if="projects.length === 0" class="empty-state">
            Нет проектов. Создайте или откройте существующий!
          </div>

          <TreeNode
            v-for="project in projects"
            :key="project.id"
            :node="project"
            :selected-id="selectedNode?.id"
            @select="store.selectNode"
          />
        </div>
      </div>

      <DetailsPanel />
    </div>

    <input
      ref="openFileInput"
      type="file"
      accept=".json,application/json"
      style="display: none"
      @change="handleOpenProject"
    />
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { storeToRefs } from 'pinia'
import { useDeviceStore } from './stores/deviceStore'
import Header from './components/Header.vue'
import TreeNode from './components/TreeNode.vue'
import DetailsPanel from './components/DetailsPanel.vue'

const store = useDeviceStore()
const { projects, selectedNode, loading, error, isRuntimeMode } = storeToRefs(store)

const openFileInput = ref(null)

onMounted(async () => {
  await store.loadProjects()
})

const triggerOpenDialog = () => {
  openFileInput.value?.click()
}

const handleOpenProject = async (event) => {
  const file = event.target.files?.[0]
  if (!file) return

  try {
    await store.openConfigFile(file)
  } catch (e) {
    alert('Ошибка при открытии файла: ' + e.message)
  } finally {
    event.target.value = ''
  }
}

const handleCreateProject = async () => {
  const name = prompt('Введите имя нового проекта:', 'My Profinet Project')
  if (!name) return

  try {
    await store.addProject(name)
  } catch (e) {
    alert('Ошибка при создании проекта: ' + e.message)
  }
}

const handleSaveProject = async () => {
  try {
    const result = await store.downloadProjectsAsJson('config.json')

    if (result?.mode === 'download') {
      alert('Файл конфигурации отправлен на скачивание.')
    }
  } catch (e) {
    alert('Ошибка при сохранении: ' + e.message)
  }
}

const handleReload = async () => {
  await store.loadProjects()
}
</script>

<style scoped>
.app-container {
  display: flex;
  flex-direction: column;
  height: 100vh;
  background: #f8f9fa;
}

.app-content {
  display: flex;
  flex: 1;
  overflow: hidden;
}

.loading-overlay,
.error-overlay {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100vh;
  gap: 16px;
  background: rgba(255, 255, 255, 0.9);
  position: absolute;
  width: 100%;
  z-index: 100;
}

.spinner {
  border: 4px solid #f3f3f3;
  border-top: 4px solid #2c5aa0;
  border-radius: 50%;
  width: 40px;
  height: 40px;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.error-overlay p {
  color: #dc3545;
  font-size: 16px;
  font-weight: bold;
}

.error-overlay button {
  padding: 8px 16px;
  background: #2c5aa0;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}

.tree-panel {
  flex: 0 0 320px;
  background: white;
  border-right: 1px solid #e0e8f0;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.tree-header {
  display: flex;
  flex-direction: column;
  gap: 10px;
  padding: 16px 20px;
  border-bottom: 1px solid #e0e8f0;
  background: #fcfcfc;
}

.tree-header h3 {
  margin: 0;
  color: #2c5aa0;
  font-size: 14px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.header-actions {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
}

.add-btn {
  padding: 6px 12px;
  border: 1px solid #d0e0f0;
  border-radius: 4px;
  cursor: pointer;
  font-weight: 500;
  font-size: 12px;
  flex: 1;
  transition: all 0.2s;
}

.project-btn {
  background: #e8f0f8;
  color: #2c5aa0;
}

.project-btn:hover {
  background: #d8e8f5;
}

.tree-container {
  flex: 1;
  overflow-y: auto;
  padding: 8px 0;
}

.empty-state {
  padding: 20px;
  text-align: center;
  color: #888;
  font-size: 13px;
}

.tree-container::-webkit-scrollbar {
  width: 6px;
}

.tree-container::-webkit-scrollbar-thumb {
  background: #c0d0e0;
  border-radius: 3px;
}

.save-btn {
  background: #e6f4ea;
  color: #1e7e34;
  border-color: #c3e6cb;
}

.save-btn:hover {
  background: #d4edda;
}

.save-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
  background: #f8f9fa;
  color: #6c757d;
  border-color: #dee2e6;
}

.open-btn {
  background: #fff3cd;
  color: #856404;
  border-color: #ffeeba;
}

.open-btn:hover {
  background: #ffe8a1;
}

.runtime-active {
  background-color: #28a745 !important;
  box-shadow: 0 0 8px rgba(40, 167, 69, 0.6);
  color: white;
  border: none;
}
</style>