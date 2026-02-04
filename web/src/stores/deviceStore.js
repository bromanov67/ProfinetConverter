import { defineStore } from 'pinia'
import { ref, watch } from 'vue'

const API_URL = 'http://localhost:5000/api'
const STORAGE_KEY = 'profinet_projects'

export const useDeviceStore = defineStore('device', () => {
  const loadProjects = () => {
    const stored = localStorage.getItem(STORAGE_KEY)
    if (stored) {
      try {
        return JSON.parse(stored)
      } catch (e) {
        console.error('Ошибка при загрузке данных:', e)
        return getDefaultProjects()
      }
    }
    return getDefaultProjects()
  }

  const getDefaultProjects = () => [
    {
      id: 'proj-1',
      name: 'PROFINET Converter',
      type: 'project',
      children: [
        {
          id: 'server-1',
          name: 'PROFINET Server',
          type: 'server',
          active: true,
          address: 1,
          description: '',
          protocolVersion: 'V0',
          masterClass: 'Class 1',
          children: [
            {
              id: 'bus-1',
              name: 'Ethernet Interface',
              type: 'interface',
              active: true,
              deviceAddress: 1,
              port: 'COM1',
              speed: 9600,
              busLevel: 0,
              deviceType: 'serial',
              children: [
                {
                  id: 'station-1',
                  name: 'Station',
                  type: 'station',
                  active: true,
                  address: 1,
                  description: 'Станция 1',
                  configuration: {
                    identifier: '',
                    manufacturer: '',
                    model: '',
                    version: '',
                    consistency: ''
                  }
                }
              ]
            }
          ]
        }
      ]
    }
  ]

  const projects = ref(loadProjects())
  const selectedNode = ref(null)

  watch(
    () => projects.value,
    (newProjects) => {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(newProjects))
    },
    { deep: true }
  )

  const findNodeById = (nodeId, nodes = projects.value) => {
    for (let node of nodes) {
      if (node.id === nodeId) return node
      if (node.children) {
        const found = findNodeById(nodeId, node.children)
        if (found) return found
      }
    }
    return null
  }

  const selectNode = (node) => {
    selectedNode.value = node
  }

  const addServer = (name) => {
    const newServer = {
      id: `server-${Date.now()}`,
      name: 'PROFINET Server',
      type: 'server',
      active: true,
      address: 1,
      description: '',
      protocolVersion: 'V0',
      masterClass: 'Class 1'
     ,
      children: [
        {
          id: `bus-${Date.now()}`,
          name: 'Ethernet Interface',
          type: 'interface',
          active: true,
          deviceAddress: 1,
          port: 'COM1',
          speed: 9600,
          busLevel: 0,
          deviceType: 'serial',
          children: [
            {
              id: `station-${Date.now()}`,
              name: 'Station',
              type: 'station',
              active: true,
              address: 1,
              description: 'New Station',
              configuration: {
                identifier: 'Station1',
                manufacturer: '',
                model: '',
                version: '1.0.0',
                consistency: ''
              }
            }
          ]
        }
      ]
    }

    projects.value[0].children.push(newServer)
  }

  const addInterface = (parentId) => {
    const parent = findNodeById(parentId)
    if (parent && parent.type === 'server') {
      parent.children = parent.children || []
      parent.children.push({
        id: `bus-${Date.now()}`,
        name: 'Ethernet Interface',
        type: 'interface',
        active: true,
        deviceAddress: 1,
        port: 'COM1',
        speed: 9600,
        busLevel: 0,
        deviceType: 'serial',
        children: []
      })
    }
  }

  const addStation = (parentId) => {
    const parent = findNodeById(parentId)
    if (parent && parent.type === 'interface') {
      parent.children = parent.children || []
      parent.children.push({
        id: `station-${Date.now()}`,
        name: 'Station',
        type: 'station',
        active: true,
        address: parent.children.length + 1,
        description: `Station ${parent.children.length + 1}`,
        configuration: {
          identifier: `Station${parent.children.length + 1}`,
          manufacturer: '',
          model: '',
          version: '1.0.0',
          consistency: ''
        }
      })
    }
  }

  const deleteNode = (nodeId) => {
    const deleteRecursive = (nodes) => {
      for (let i = 0; i < nodes.length; i++) {
        if (nodes[i].id === nodeId) {
          nodes.splice(i, 1)
          return true
        }
        if (nodes[i].children && deleteRecursive(nodes[i].children)) {
          return true
        }
      }
      return false
    }
    deleteRecursive(projects.value)
    if (selectedNode.value?.id === nodeId) {
      selectedNode.value = null
    }
  }

  const clearAllData = () => {
    projects.value = getDefaultProjects()
    selectedNode.value = null
    localStorage.removeItem(STORAGE_KEY)
  }

  const importGSDML = (nodeId, gsdmlData) => {
    const station = findNodeById(nodeId)
    if (station && station.type === 'station') {
      station.configuration.identifier = gsdmlData.identifier || station.configuration.identifier
      station.configuration.manufacturer = gsdmlData.manufacturer || station.configuration.manufacturer
      station.configuration.model = gsdmlData.model || station.configuration.model
      station.configuration.version = gsdmlData.version || station.configuration.version
      station.description = gsdmlData.description || station.description
    }
  }

  return {
    projects,
    selectedNode,
    selectNode,
    findNodeById,
    addServer,
    addInterface,
    addStation,
    deleteNode,
    clearAllData,
    importGSDML
  }
})
