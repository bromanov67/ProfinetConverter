import { defineStore } from 'pinia'
import { ref } from 'vue'
import apiClient from '../services/apiClient'

export const useDeviceStore = defineStore('device', () => {

  // ─── STATE ───────────────────────────────────────────────────────────────
  const projects    = ref([])
  const selectedNode = ref(null)
  const hoveredModule = ref(null)
  const loading     = ref(false)
  const error       = ref(null)
  

  // ─── PROJECTS ────────────────────────────────────────────────────────────

  const loadProjects = async () => {
    loading.value = true
    error.value   = null
    try {
      const response = await apiClient.get('/projects')
      projects.value = response.data
    } catch (err) {
      error.value = 'Не удалось загрузить проекты'
      console.error(err)
    } finally {
      loading.value = false
    }
  }

  const addProject = async (name) => {
    loading.value = true
    try {
      await apiClient.post('/projects', { name })
      await loadProjects()
    } catch (err) {
      error.value = 'Ошибка при создании проекта'
      console.error(err)
    } finally {
      loading.value = false
    }
  }


  // ─── SERVERS / INTERFACES / STATIONS ─────────────────────────────────────

  const addServer = async (projectId, name) => {
    loading.value = true
    try {
      await apiClient.post('/profinetservers', { projectId, name })
      await loadProjects()
    } catch (err) {
      error.value = 'Ошибка при добавлении сервера'
      throw err
    } finally {
      loading.value = false
    }
  }

  const addInterface = async (serverId, name) => {
    loading.value = true
    try {
      await apiClient.post('/networkinterfaces', { serverId, name })
      await loadProjects()
    } catch (err) {
      error.value = 'Ошибка при добавлении интерфейса'
      throw err
    } finally {
      loading.value = false
    }
  }

  const addStation = async (interfaceId, name) => {
    loading.value = true
    try {
      await apiClient.post('/stations', { interfaceId, name })
      await loadProjects()
    } catch (err) {
      error.value = 'Ошибка при добавлении станции'
      throw err
    } finally {
      loading.value = false
    }
  }
    const setHoveredModule = (mod) => {
      hoveredModule.value = mod
    }

  // ─── DELETE ───────────────────────────────────────────────────────────────

  const deleteNode = async (nodeId) => {
    loading.value = true
    try {
      await apiClient.delete(`/nodes/${nodeId}`)
      if (selectedNode.value?.id === nodeId) {
        selectedNode.value = null
      }
      await loadProjects()
    } catch (err) {
      error.value = 'Ошибка при удалении'
      console.error(err)
    } finally {
      loading.value = false
    }
  }


  // ─── GSDML IMPORT ─────────────────────────────────────────────────────────

  const importGsdml = async (stationId, file) => {
    loading.value = true
    try {
      const formData = new FormData()
      formData.append('file', file)
      await apiClient.post(`/stations/${stationId}/import-gsdml`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      })
      await loadProjects()
    } catch (err) {
      error.value = 'Ошибка импорта GSDML'
      throw err
    } finally {
      loading.value = false
    }
  }


  // ─── SLOTS ────────────────────────────────────────────────────────────────

  /**
   * Назначает модуль в слот указанной станции.
   * module = null → очищает слот.
   */
  const assignModule = (stationId, slotNumber, module) => {
    // Ищем станцию в реактивном дереве проектов
    for (const proj of projects.value) {
      for (const srv of proj.servers ?? []) {
        for (const iface of srv.interfaces ?? []) {
          const station = (iface.stations ?? []).find(s => s.id === stationId)
          if (!station) continue

          const slot = station.configuration?.slots?.find(s => s.number === slotNumber)
          if (slot) {
            slot.module = module

            // Если этот слот сейчас открыт в DetailsPanel — обновляем и там
            const activeSlotId = `${stationId}__slot__${slotNumber}`
            if (selectedNode.value?.id === activeSlotId) {
              selectedNode.value = {
                ...selectedNode.value,
                module,
                name: module
                  ? `${String(slotNumber).padStart(2, '0')}: ${module.name}`
                  : `${String(slotNumber).padStart(2, '0')}: <пусто>`
              }
            }
          }
          return  // Станция найдена, выходим
        }
      }
    }
  }

  /** Очищает модуль из слота */
  const clearSlotModule = (stationId, slotNumber) => {
    assignModule(stationId, slotNumber, null)

    // Если открытый слот именно этот — сбрасываем selectedNode
    const activeSlotId = `${stationId}__slot__${slotNumber}`
    if (selectedNode.value?.id === activeSlotId) {
      selectedNode.value = null
    }
  }


  // ─── HELPERS ──────────────────────────────────────────────────────────────

  const findNodeById = (nodeId, nodes) => {
    const list = nodes ?? projects.value
    for (const node of list) {
      if (node.id === nodeId) return node
      const children = node.children ?? node.servers ?? node.interfaces ?? node.stations
      if (children?.length) {
        const found = findNodeById(nodeId, children)
        if (found) return found
      }
    }
    return null
  }

  const selectNode = (node) => {
    selectedNode.value = node
  }


  // ─── EXPOSE ───────────────────────────────────────────────────────────────

  return {
    // state
    projects,
    selectedNode,
    loading,
    error,

    // actions
    loadProjects,
    addProject,
    addServer,
    addInterface,
    addStation,
    deleteNode,
    importGsdml,

    assignModule,     
    clearSlotModule,  

    findNodeById,
    selectNode,

    hoveredModule,
    setHoveredModule,
  }
})
