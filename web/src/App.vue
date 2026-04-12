<template>
  <div class="app-container">
    <Header />
    
    <!-- Состояние загрузки -->
    <div v-if="store.loading" class="loading-overlay">
      <div class="spinner"></div>
      <p>Загрузка...</p>
    </div>

    <!-- Состояние ошибки -->
    <div v-else-if="store.error" class="error-overlay">
      <p>❌ {{ store.error }}</p>
      <button @click="store.loadProjects()">Повторить</button>
    </div>

    <!-- Основной контент -->
    <div v-else class="app-content">
      <div class="tree-panel">
        <div class="tree-header">
          <h3>Projects</h3>
          <div class="header-actions">
            <!-- Кнопка создания проекта -->
            <button class="add-btn project-btn" @click="handleCreateProject">
              + Project
            </button>
            
            <!-- Кнопка сохранения проекта (вместо + Server) -->
            <button 
              class="add-btn save-btn" 
              @click="handleSaveProject" 
              :disabled="projects.length === 0"
              title="Сохранить текущие изменения (модули, настройки и т.д.)"
            >
              💾 Save
            </button>

            <button 
              @click="store.toggleRuntimeMode()" 
              :class="['action-btn', { 'runtime-active': store.isRuntimeMode }]"
              style="margin-left: 20px;"
            >
              {{ store.isRuntimeMode ? '🛑 Остановить режим' : '▶ Режим исполнения' }}
            </button>
          </div>
        </div>

        <div class="tree-container">
          <!-- Пустое состояние -->
          <div v-if="projects.length === 0" class="empty-state">
            Нет проектов. Создайте первый!
          </div>

          <!-- Дерево -->
          <TreeNode
            v-for="project in projects"
            :key="project.id"
            :node="project"
            :selected-id="selectedNode?.id"
            @select="store.selectNode"
          />
        </div>
      </div>
      
      <!-- Панель деталей -->
      <DetailsPanel />
    </div>
  </div>
</template>

<script setup>
import { onMounted } from 'vue'
import { storeToRefs } from 'pinia'
import { useDeviceStore } from './stores/deviceStore'
import Header from './components/Header.vue'
import TreeNode from './components/TreeNode.vue'
import DetailsPanel from './components/DetailsPanel.vue'

const store = useDeviceStore()

// Деструктурируем реактивные переменные
const { projects, selectedNode } = storeToRefs(store)

onMounted(() => {
  store.loadProjects()
})

// --- Логика создания проекта ---
const handleCreateProject = async () => {
  const name = prompt('Введите имя нового проекта:', 'My Profinet Project')
  
  if (name) {
    await store.addProject(name)
  }
}

// --- Логика сохранения проекта ---
const handleSaveProject = async () => {
  if (projects.value.length === 0) {
    alert('Нет проектов для сохранения!');
    return;
  }

  // Если вы хотите сохранять весь стейт или конкретный выбранный проект:
  try {
    store.loading = true; // Можно включить лоадер на время сохранения
    await store.saveAllProjects(); // Вызов метода из store (см. ниже)
    alert('Изменения успешно сохранены!');
  } catch (error) {
    alert('Ошибка при сохранении: ' + error.message);
  } finally {
    store.loading = false;
  }
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

/* --- Загрузка и Ошибки --- */
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

/* --- Боковая панель (Дерево) --- */
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
  flex-direction: column; /* Изменил на колонку для кнопок */
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

.server-btn {
  background: #fff;
  color: #555;
}
.server-btn:hover {
  background: #f5f5f5;
}
.server-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
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

/* Скроллбар */
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

.tree-container { flex: 1; overflow-y: auto; padding: 8px 0; }
.empty-state { padding: 20px; text-align: center; color: #888; font-size: 13px; }
.tree-container::-webkit-scrollbar { width: 6px; }
.tree-container::-webkit-scrollbar-thumb { background: #c0d0e0; border-radius: 3px; }

.runtime-active {
  background-color: #28a745 !important;
  box-shadow: 0 0 8px rgba(40, 167, 69, 0.6);
}
</style>