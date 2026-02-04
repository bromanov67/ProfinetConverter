<template>
  <div class="app-container">
    <Header />
    <div class="app-content">
      <div class="tree-panel">
        <div class="tree-header">
          <h3>Projects</h3>
          <button class="add-btn" @click="addNewServer">Add Server</button>
        </div>
        <div class="tree-container">
          <TreeNode
            v-for="project in store.projects"
            :key="project.id"
            :node="project"
            :selected-id="store.selectedNode?.id"
            @select="store.selectNode"
          />
        </div>
      </div>
      <DetailsPanel />
    </div>
  </div>
</template>

<script setup>
import { useDeviceStore } from './stores/deviceStore'
import Header from './components/Header.vue'
import TreeNode from './components/TreeNode.vue'
import DetailsPanel from './components/DetailsPanel.vue'

const store = useDeviceStore()

const addNewServer = () => {
  const name = prompt('Server name:', 'PROFINET Server')
  if (name) {
    store.addServer(name)
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
  align-items: center;
  justify-content: space-between;
  padding: 16px 20px;
  border-bottom: 1px solid #e0e8f0;
}

.tree-header h3 {
  margin: 0;
  color: #2c5aa0;
  font-size: 14px;
  font-weight: 600;
}

.add-btn {
  padding: 6px 12px;
  background: #e8f0f8;
  color: #2c5aa0;
  border: 1px solid #d0e0f0;
  border-radius: 4px;
  cursor: pointer;
  font-weight: 500;
  font-size: 12px;
}

.add-btn:hover {
  background: #d8e8f5;
}

.tree-container {
  flex: 1;
  overflow-y: auto;
  padding: 8px 0;
}

.tree-container::-webkit-scrollbar {
  width: 6px;
}

.tree-container::-webkit-scrollbar-thumb {
  background: #c0d0e0;
  border-radius: 3px;
}
</style>
