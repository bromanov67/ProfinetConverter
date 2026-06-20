import { defineStore } from 'pinia'
import { ref } from 'vue'
import apiClient from '../services/apiClient'

export const useDeviceStore = defineStore('device', () => {
  const projects = ref([])
  const selectedNode = ref(null)
  const hoveredModule = ref(null)
  const loading = ref(false)
  const error = ref(null)
  const isRuntimeMode = ref(false)

  const mapServerType = (type) => {
    if (typeof type === 'string') {
      const normalized = type.toLowerCase()

      if (normalized === 'server_profinet') return 'server_profinet'
      if (normalized === 'server_iec104') return 'server_iec104'
      if (normalized === 'profinet') return 'server_profinet'
      if (normalized === 'iec104') return 'server_iec104'
      if (normalized === 'iec') return 'server_iec104'
      if (normalized === 'server') return 'server'
    }

    if (typeof type === 'number') {
      if (type === 0) return 'server_profinet'
      if (type === 1) return 'server_iec104'
    }

    return 'server'
  }

  const mapInterfaceType = (serverType, iface) => {
    if (typeof iface?.type === 'string') {
      const normalized = iface.type.toLowerCase()
      if (normalized === 'interface_profinet') return 'interface_profinet'
      if (normalized === 'interface_iec') return 'interface_iec'
      if (normalized === 'interface') {
        if (serverType === 'server_profinet') return 'interface_profinet'
        if (serverType === 'server_iec104') return 'interface_iec'
        return 'interface'
      }
    }

    if (Array.isArray(iface?.stations)) return 'interface_profinet'
    if (Array.isArray(iface?.channels)) return 'interface_iec'
    if (serverType === 'server_profinet') return 'interface_profinet'
    if (serverType === 'server_iec104') return 'interface_iec'

    return 'interface'
  }

  const normalizeProjects = (rawProjects) => {
  return (rawProjects || []).map(project => ({
    ...project,
    type: 'project',
    servers: (project.servers || []).map(server => {
      const serverType = mapServerType(server.type)

      return {
        ...server,
        type: serverType,
        interfaces: (server.interfaces || []).map(iface => {
          const interfaceType = mapInterfaceType(serverType, iface)

          return {
            ...iface,
            type: interfaceType,
            stations: Array.isArray(iface.stations)
              ? iface.stations.map(station => ({
                  ...station,
                  type: 'station'
                }))
              : [],
            channels: Array.isArray(iface.channels)
              ? iface.channels.map(channel => ({
                  ...channel,
                  type: 'channel_iec'
                }))
              : []
          }
        })
      }
    })
  }))
}

  const applyProjects = (rawProjects) => {
    const currentSelectedId = selectedNode.value?.id ?? null
    projects.value = normalizeProjects(rawProjects)

    if (currentSelectedId) {
      selectedNode.value = findNodeById(currentSelectedId)
    } else {
      selectedNode.value = null
    }
  }

  const refreshProjects = async () => {
    const response = await apiClient.get('/projects')
    applyProjects(response.data)
  }

  const loadProjects = async () => {
    loading.value = true
    error.value = null

    try {
      await refreshProjects()
    } catch (err) {
      error.value = 'Не удалось загрузить проекты'
      console.error(err)
    } finally {
      loading.value = false
    }
  }

  const openConfigFile = async (file) => {
    loading.value = true
    error.value = null

    try {
      const formData = new FormData()
      formData.append('file', file)

      const response = await apiClient.post('/config/open', formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      })

      applyProjects(response.data)
      selectedNode.value = null
    } catch (err) {
      error.value = 'Ошибка при открытии файла конфигурации'
      console.error(err)
      throw err
    } finally {
      loading.value = false
    }
  }

  const addProject = async (name) => {
    loading.value = true
    error.value = null

    try {
      await apiClient.post('/projects', { name })
      await refreshProjects()
    } catch (err) {
      error.value = 'Ошибка при создании проекта'
      console.error(err)
      throw err
    } finally {
      loading.value = false
    }
  }

  const addServer = async (projectId, name, serverType) => {
    loading.value = true
    error.value = null

    try {
      const url = serverType === 'server_iec104'
        ? '/IecServers'
        : '/ProfinetServers'

      const response = await apiClient.post(url, { projectId, name })
      const { id } = response.data

      await refreshProjects()

      const newServer = findNodeById(id)
      if (newServer) {
        selectedNode.value = newServer
      }
    } catch (err) {
      error.value = 'Ошибка при создании сервера'
      console.error(err)
      throw err
    } finally {
      loading.value = false
    }
  }

  const addInterface = async (serverId, name) => {
    loading.value = true
    error.value = null

    try {
      const response = await apiClient.post('/NetworkInterfaces', { serverId, name })
      const { id } = response.data

      await refreshProjects()

      const newInterface = findNodeById(id)
      if (newInterface) {
        selectedNode.value = newInterface
      }
    } catch (err) {
      error.value = 'Ошибка при добавлении интерфейса'
      console.error(err)
      throw err
    } finally {
      loading.value = false
    }
  }

  const addStation = async (interfaceId, name) => {
    loading.value = true
    error.value = null

    try {
      await apiClient.post('/stations', { interfaceId, name })
      await refreshProjects()
    } catch (err) {
      error.value = 'Ошибка при добавлении станции'
      console.error(err)
      throw err
    } finally {
      loading.value = false
    }
  }

  const addIecChannel = async (interfaceId, name) => {
    loading.value = true
    error.value = null

    try {
      const response = await apiClient.post('/Channels', { interfaceId, name })
      const { id } = response.data

      await refreshProjects()

      const newChannel = findNodeById(id)
      if (newChannel) {
        selectedNode.value = newChannel
      }
    } catch (err) {
      error.value = 'Ошибка при добавлении канала'
      console.error(err)
      throw err
    } finally {
      loading.value = false
    }
  }

  const deleteNode = async (nodeId) => {
    loading.value = true
    error.value = null

    try {
      await apiClient.delete(`/nodes/${nodeId}`)

      if (selectedNode.value?.id === nodeId) {
        selectedNode.value = null
      }

      await refreshProjects()
    } catch (err) {
      error.value = 'Ошибка при удалении'
      console.error(err)
      throw err
    } finally {
      loading.value = false
    }
  }

  const importGsdml = async (stationId, file) => {
    loading.value = true
    error.value = null

    try {
      const formData = new FormData()
      formData.append('file', file)

      await apiClient.post(`/stations/${stationId}/import-gsdml`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      })

      await refreshProjects()
    } catch (err) {
      error.value = 'Ошибка импорта GSDML'
      console.error(err)
      throw err
    } finally {
      loading.value = false
    }
  }

  const setHoveredModule = (mod) => {
    hoveredModule.value = mod
  }

  const assignModule = (stationId, slotNumber, module) => {
    for (const proj of projects.value) {
      for (const srv of proj.servers ?? []) {
        for (const iface of srv.interfaces ?? []) {
          const station = (iface.stations ?? []).find(s => s.id === stationId)
          if (!station) continue

          const slot = station.configuration?.slots?.find(s => s.number === slotNumber)
          if (!slot) return

          slot.module = module
          if (!slot.commands) slot.commands = []
          if (!slot.signals) slot.signals = []

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

          return
        }
      }
    }
  }

  const clearSlotModule = (stationId, slotNumber) => {
    assignModule(stationId, slotNumber, null)

    const activeSlotId = `${stationId}__slot__${slotNumber}`
    if (selectedNode.value?.id === activeSlotId) {
      selectedNode.value = null
    }
  }

  const updateStationConfiguration = async (stationId, rawConfiguration) => {
    if (!rawConfiguration) return

    const mappedConfig = {
      id: rawConfiguration.id || rawConfiguration.identifier || '',
      manufacturer: rawConfiguration.manufacturer || '',
      model: rawConfiguration.model || '',
      version: rawConfiguration.version || '',
      shortDesignation: rawConfiguration.shortDesignation || '',
      deviceDescription: rawConfiguration.deviceDescription || '',
      articleNo: rawConfiguration.articleNo || '',
      firmwareVersion: rawConfiguration.firmwareVersion || '',
      hardwareVersion: rawConfiguration.hardwareVersion || '',
      gsdFile: rawConfiguration.gsdFile || '',
      profinetDeviceName: rawConfiguration.profinetDeviceName || '',
      ipAddress: rawConfiguration.ipAddress || '',
      subnetMask: rawConfiguration.subnetMask || '',
      deviceNumber: parseInt(rawConfiguration.deviceNumber) || 0,
      consistency: rawConfiguration.consistency || '',
      slots: (rawConfiguration.slots || []).map(slot => ({
        number: parseInt(slot.number) || 0,
        label: slot.label || '',
        module: slot.module ? {
          id: slot.module.id || '',
          name: slot.module.name || '',
          info: slot.module.info || '',
          allowedInSlots: Array.isArray(slot.module.allowedInSlots)
            ? slot.module.allowedInSlots.map(num => parseInt(num))
            : []
        } : null,
        signals: (slot.signals || []).map(sig => ({
          id: String(sig.id),
          name: sig.name || '',
          node: sig.node || 'Root',
          regAddress: parseInt(sig.regAddress) || 0,
          dataType: sig.dataType || 'Bool',
          csDataType: sig.csDataType || 'BOOLEAN',
          archive: sig.archive || '-',
          checked: !!sig.checked
        })),
        commands: (slot.commands || []).map(cmd => ({
          id: String(cmd.id),
          name: cmd.name || '',
          node: cmd.node || 'Root',
          regAddress: parseInt(cmd.regAddress) || 0,
          dataType: cmd.dataType || 'Bool',
          csDataType: cmd.csDataType || 'BOOLEAN',
          retranslation: cmd.retranslation || '-',
          checked: !!cmd.checked
        }))
      })),
      modules: (rawConfiguration.modules || []).map(mod => ({
        id: mod.id || '',
        name: mod.name || '',
        info: mod.info || '',
        allowedInSlots: Array.isArray(mod.allowedInSlots)
          ? mod.allowedInSlots.map(num => parseInt(num))
          : []
      }))
    }

    const response = await apiClient.put(`/stations/${stationId}/configuration`, mappedConfig)

    if (response.status < 200 || response.status >= 300) {
      throw new Error(`Failed to update configuration: ${response.status}`)
    }
  }

  const updateChannelConfiguration = async (channelId, commands, signals) => {
    const payload = {
      commands: commands || [],
      signals: signals || []
    }

    const response = await apiClient.put(`/channels/${channelId}/configuration`, payload)

    if (response.status < 200 || response.status >= 300) {
      throw new Error(`Ошибка при сохранении канала ${channelId}`)
    }
  }

  const findNodeById = (nodeId, nodes) => {
    const list = nodes ?? projects.value

    for (const node of list) {
      if (node.id === nodeId) return node

      const groups = [
        node.children,
        node.servers,
        node.interfaces,
        node.stations,
        node.channels
      ]

      for (const children of groups) {
        if (children?.length) {
          const found = findNodeById(nodeId, children)
          if (found) return found
        }
      }
    }

    return null
  }

  const selectNode = (node) => {
    selectedNode.value = node
  }

  const findAllStations = (node) => {
    let stations = []

    if (node.type === 'station') {
      stations.push(node)
    }

    if (node.servers) {
      for (const server of node.servers) {
        stations = stations.concat(findAllStations(server))
      }
    }

    if (node.interfaces) {
      for (const iface of node.interfaces) {
        stations = stations.concat(findAllStations(iface))
      }
    }

    if (node.stations) {
      for (const station of node.stations) {
        stations = stations.concat(findAllStations(station))
      }
    }

    return stations
  }

  const findAllChannels = (node) => {
    let channels = []

    if (node.type === 'channel_iec') {
      channels.push(node)
    }

    if (node.servers) {
      for (const server of node.servers) {
        channels = channels.concat(findAllChannels(server))
      }
    }

    if (node.interfaces) {
      for (const iface of node.interfaces) {
        channels = channels.concat(findAllChannels(iface))
      }
    }

    if (node.channels) {
      for (const channel of node.channels) {
        channels = channels.concat(findAllChannels(channel))
      }
    }

    return channels
  }

  const saveAllProjects = async () => {
    loading.value = true
    error.value = null

    try {
      for (const project of projects.value) {
        const stations = findAllStations(project)
        for (const station of stations) {
          if (station.configuration) {
            await updateStationConfiguration(station.id, station.configuration)
          }
        }

        const channels = findAllChannels(project)
        for (const channel of channels) {
          await updateChannelConfiguration(channel.id, channel.commands, channel.signals)
        }
      }
    } finally {
      loading.value = false
    }
  }

  const downloadProjectsAsJson = async (fileName = 'config.json') => {
    loading.value = true
    error.value = null

    try {
      await saveAllProjects()

      const response = await apiClient.get('/config/export', {
        responseType: 'blob'
      })

      const blob = new Blob([response.data], { type: 'application/json' })
      const url = window.URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = fileName
      document.body.appendChild(link)
      link.click()
      link.remove()
      window.URL.revokeObjectURL(url)

      return { mode: 'download' }
    } catch (err) {
      error.value = 'Ошибка при сохранении файла конфигурации'
      console.error(err)
      throw err
    } finally {
      loading.value = false
    }
  }

  const findSourceSignal = (sourceId) => {
    if (!sourceId) return null

    const searchDeep = (obj, targetId) => {
      if (!obj || typeof obj !== 'object') return null

      if (String(obj.id) === String(targetId) && (obj.byteOffset !== undefined || obj.dataType)) {
        return obj
      }

      for (const key of Object.keys(obj)) {
        const val = obj[key]
        if (val && typeof val === 'object') {
          const found = searchDeep(val, targetId)
          if (found) return found
        }
      }

      return null
    }

    return searchDeep(projects.value, sourceId)
  }

  const toggleRuntimeMode = async () => {
    try {
      if (!isRuntimeMode.value) {
        const allChannels = []
        for (const project of projects.value) {
          allChannels.push(...findAllChannels(project))
        }

        const signalsArray = []

        allChannels.forEach(channel => {
          ;(channel.signals || []).forEach(sig => {
            const sourceSignal = findSourceSignal(sig.sourceId)

            signalsArray.push({
              name: sig.senderName || `Signal_${sig.ioa || 0}`,
              dataType: (sig.csDataType === 'FLOAT' || sig.dataType === 'Real') ? 'float32' : 'bool',
              byteOffset: parseInt(sourceSignal ? sourceSignal.byteOffset : 0) || 0,
              bitOffset: parseInt(sourceSignal ? sourceSignal.bitOffset : 0) || 0,
              ioa: parseInt(sig.ioa) || 0
            })
          })

          ;(channel.commands || []).forEach(cmd => {
            const sourceSignal = findSourceSignal(cmd.sourceId)

            signalsArray.push({
              name: cmd.senderName || `Command_${cmd.ioa || 0}`,
              dataType: (cmd.csDataType === 'FLOAT' || cmd.dataType === 'Real') ? 'float32' : 'bool',
              byteOffset: parseInt(sourceSignal ? sourceSignal.byteOffset : 0) || 0,
              bitOffset: parseInt(sourceSignal ? sourceSignal.bitOffset : 0) || 0,
              ioa: parseInt(cmd.ioa) || 0
            })
          })
        })

        const uniqueSignals = Array.from(
          new Map(signalsArray.map(item => [item.ioa, item])).values()
        )

        const runtimeRequest = {
          interfaceName: 'enp0s3',
          stationName: 'PN-Device',
          moduleIdent: 4102,
          submoduleIdent: 4102,
          inputLength: 64,
          outputLength: 64,
          iecIpAddress: '0.0.0.0',
          iecPort: 2404,
          signals: uniqueSignals
        }

        const response = await fetch('http://localhost:5000/api/runtime/start', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(runtimeRequest)
        })

        if (!response.ok) {
          const errText = await response.text()
          throw new Error(errText)
        }

        isRuntimeMode.value = true
      } else {
        await fetch('http://localhost:5000/api/runtime/stop', { method: 'POST' })
        isRuntimeMode.value = false
      }
    } catch (e) {
      console.error('Ошибка сети при смене режима исполнения', e)
      alert('Ошибка запуска: ' + e.message)
    }
  }

  return {
    projects,
    selectedNode,
    hoveredModule,
    loading,
    error,
    isRuntimeMode,
    loadProjects,
    openConfigFile,
    addProject,
    addServer,
    addInterface,
    addStation,
    addIecChannel,
    deleteNode,
    importGsdml,
    updateStationConfiguration,
    updateChannelConfiguration,
    assignModule,
    clearSlotModule,
    saveAllProjects,
    downloadProjectsAsJson,
    findNodeById,
    selectNode,
    findAllStations,
    findAllChannels,
    setHoveredModule,
    toggleRuntimeMode
  }
})