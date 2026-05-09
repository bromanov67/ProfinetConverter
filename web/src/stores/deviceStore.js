import { defineStore } from 'pinia'
import { ref } from 'vue'
import apiClient from '../services/apiClient'

export const useDeviceStore = defineStore('device', () => {
  const projects     = ref([])
  const selectedNode = ref(null)
  const hoveredModule = ref(null)
  const loading     = ref(false)
  const error       = ref(null)
  
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

  const addServer = async (projectId, name, serverType) => {
    loading.value = true;
    try {
      const url = serverType === 'server_iec104' 
        ? '/api/IecServers' 
        : '/api/ProfinetServers';

      const response = await fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ projectId, name }) 
      });

      if (!response.ok) throw new Error('Failed to create server');
      
      const { id } = await response.json();
      await loadProjects();

      const newServer = findNodeById(id);
      if (newServer) {
        selectedNode.value = newServer;
      }
    } catch (err) {
      error.value = 'Ошибка при создании сервера';
      console.error(err);
    } finally {
      loading.value = false;
    }
  }

  const addInterface = async (serverId, name) => {
    loading.value = true;
    try {
      const response = await fetch('/api/NetworkInterfaces', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ serverId, name })
      });

      if (!response.ok) throw new Error('Failed to create interface');
      
      const { id } = await response.json();
      await loadProjects();

      const newInterface = findNodeById(id);
      if (newInterface) {
        selectedNode.value = newInterface;
      }
    } catch (err) {
      error.value = 'Ошибка при добавлении интерфейса';
      console.error(err);
    } finally {
      loading.value = false;
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

  const addIecChannel = async (interfaceId, name) => {
    loading.value = true;
    try {
      const response = await fetch('/api/Channels', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ interfaceId, name })
      });

      if (!response.ok) throw new Error('Failed to create IEC channel');
      
      const { id } = await response.json();
      await loadProjects();

      const newChannel = findNodeById(id);
      if (newChannel) {
        selectedNode.value = newChannel;
      }
    } catch (err) {
      error.value = 'Ошибка при добавлении канала';
      console.error(err);
    } finally {
      loading.value = false;
    }
  }

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

  const assignModule = (stationId, slotNumber, module) => {
    for (const proj of projects.value) {
      for (const srv of proj.servers ?? []) {
        for (const iface of srv.interfaces ?? []) {
          const station = (iface.stations ?? []).find(s => s.id === stationId)
          if (!station) continue

          const slot = station.configuration?.slots?.find(s => s.number === slotNumber)
          if (slot) {
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
          }
          return 
        }
      }
    }
  }

  const updateStationConfiguration = async (stationId, rawConfiguration) => {
    if (!rawConfiguration) return;

    const mappedConfig = {
      id: rawConfiguration.id || rawConfiguration.identifier || "",
      manufacturer: rawConfiguration.manufacturer || "",
      model: rawConfiguration.model || "",
      version: rawConfiguration.version || "",
      shortDesignation: rawConfiguration.shortDesignation || "",
      deviceDescription: rawConfiguration.deviceDescription || "",
      articleNo: rawConfiguration.articleNo || "",
      firmwareVersion: rawConfiguration.firmwareVersion || "",
      hardwareVersion: rawConfiguration.hardwareVersion || "",
      gsdFile: rawConfiguration.gsdFile || "",
      profinetDeviceName: rawConfiguration.profinetDeviceName || "",
      ipAddress: rawConfiguration.ipAddress || "",
      subnetMask: rawConfiguration.subnetMask || "",
      deviceNumber: parseInt(rawConfiguration.deviceNumber) || 0,
      consistency: rawConfiguration.consistency || "",
      
      slots: (rawConfiguration.slots || []).map(slot => ({
        number: parseInt(slot.number) || 0,
        label: slot.label || "",
        module: slot.module ? {
          id: slot.module.id || "",
          name: slot.module.name || "",
          info: slot.module.info || "",
          allowedInSlots: Array.isArray(slot.module.allowedInSlots) 
                            ? slot.module.allowedInSlots.map(num => parseInt(num)) 
                            : []
        } : null,
        signals: (slot.signals || []).map(sig => ({
          id: String(sig.id),
          name: sig.name || "",
          node: sig.node || "Root",
          regAddress: parseInt(sig.regAddress) || 0,
          dataType: sig.dataType || "Bool",
          csDataType: sig.csDataType || "BOOLEAN",
          archive: sig.archive || "-",
          checked: !!sig.checked
        })),
        commands: (slot.commands || []).map(cmd => ({
          id: String(cmd.id),
          name: cmd.name || "",
          node: cmd.node || "Root",
          regAddress: parseInt(cmd.regAddress) || 0,
          dataType: cmd.dataType || "Bool",
          csDataType: cmd.csDataType || "BOOLEAN",
          retranslation: cmd.retranslation || "-",
          checked: !!cmd.checked
        }))
      })),
      
      modules: (rawConfiguration.modules || []).map(mod => ({
        id: mod.id || "",
        name: mod.name || "",
        info: mod.info || "",
        allowedInSlots: Array.isArray(mod.allowedInSlots) ? mod.allowedInSlots.map(num => parseInt(num)) : []
      }))
    };

    const response = await fetch(`/api/stations/${stationId}/configuration`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(mappedConfig)
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Failed to update configuration: ${response.status} ${errorText}`);
    }
  }

  const clearSlotModule = (stationId, slotNumber) => {
    assignModule(stationId, slotNumber, null)
    const activeSlotId = `${stationId}__slot__${slotNumber}`
    if (selectedNode.value?.id === activeSlotId) {
      selectedNode.value = null
    }
  }

  const updateChannelConfiguration = async (channelId, commands, signals) => {
    const payload = {
      commands: commands || [],
      signals: signals || []
    };

    const response = await fetch(`/api/channels/${channelId}/configuration`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    });

    if (!response.ok) {
      console.error(`Ошибка при сохранении канала ${channelId}`);
    }
  };

  const saveAllProjects = async () => {
    loading.value = true;
    try {
      for (const project of projects.value) {
        
        const stations = findAllStations(project);
        for (const station of stations) {
          if (station.configuration) {
            try {
              await updateStationConfiguration(station.id, station.configuration);
            } catch (e) {
              console.error(`Сетевая ошибка при сохранении станции ${station.name}`, e);
            }
          }
        }

        const channels = findAllChannels(project);
        for (const channel of channels) {
          try {
            await updateChannelConfiguration(channel.id, channel.commands, channel.signals);
          } catch (e) {
            console.error(`Сетевая ошибка при сохранении канала ${channel.name}`, e);
          }
        }
      }
    } finally {
      loading.value = false;
    }
  };

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

  const findAllStations = (node) => {
    let stations = [];
    if (node.type === 'station') {
      stations.push(node);
    }
    if (node.servers) {
      for (const server of node.servers) stations = stations.concat(findAllStations(server));
    }
    if (node.interfaces) {
      for (const iface of node.interfaces) stations = stations.concat(findAllStations(iface));
    }
    if (node.stations) {
      for (const station of node.stations) stations = stations.concat(findAllStations(station));
    }
    return stations;
  }

  const findAllChannels = (node) => {
    let channels = [];
    if (node.type === 'channel_iec') {
      channels.push(node);
    }
    if (node.servers) {
      for (const server of node.servers) channels = channels.concat(findAllChannels(server));
    }
    if (node.interfaces) {
      for (const iface of node.interfaces) channels = channels.concat(findAllChannels(iface));
    }
    if (node.channels) {
      for (const ch of node.channels) channels.push(ch);
    }
    return channels;
  };

  const findSourceSignal = (sourceId) => {
    if (!sourceId) return null;
    
    const searchDeep = (obj, targetId) => {
      if (!obj || typeof obj !== 'object') return null;

      if (String(obj.id) === String(targetId) && (obj.byteOffset !== undefined || obj.dataType)) {
         return obj;
      }

      for (const key of Object.keys(obj)) {
        const val = obj[key];
        if (val && typeof val === 'object') {
          const found = searchDeep(val, targetId);
          if (found) return found;
        }
      }
      return null;
    };

    return searchDeep(projects.value, sourceId);
  }

  const isRuntimeMode = ref(false)

  const toggleRuntimeMode = async () => {
    try {
      if (!isRuntimeMode.value) {
        const allChannels = [];
        for (const project of projects.value) {
          allChannels.push(...findAllChannels(project));
        }

        const signalsArray = [];

         allChannels.forEach(channel => {
          (channel.signals || []).forEach(sig => {
            const sourceSignal = findSourceSignal(sig.sourceId);
            
            console.log(`[Склейка Сигнала ${sig.ioa}] Нашли PROFINET?`, sourceSignal ? "ДА" : "НЕТ", "Байт:", sourceSignal?.byteOffset);

            signalsArray.push({
              name: sig.senderName || `Signal_${sig.ioa || 0}`,
              dataType: (sig.csDataType === 'FLOAT' || sig.dataType === 'Real') ? 'float32' : 'bool',
              byteOffset: parseInt(sourceSignal ? sourceSignal.byteOffset : 0) || 0,
              bitOffset: parseInt(sourceSignal ? sourceSignal.bitOffset : 0) || 0,
              ioa: parseInt(sig.ioa) || 0
            });
          });

          (channel.commands || []).forEach(cmd => {
            const sourceSignal = findSourceSignal(cmd.sourceId);
            
            console.log(`[Склейка Команды ${cmd.ioa}] Нашли PROFINET?`, sourceSignal ? "ДА" : "НЕТ", "Байт:", sourceSignal?.byteOffset);

            signalsArray.push({
              name: cmd.senderName || `Command_${cmd.ioa || 0}`,
              dataType: (cmd.csDataType === 'FLOAT' || cmd.dataType === 'Real') ? 'float32' : 'bool',
              byteOffset: parseInt(sourceSignal ? sourceSignal.byteOffset : 0) || 0,
              bitOffset: parseInt(sourceSignal ? sourceSignal.bitOffset : 0) || 0,
              ioa: parseInt(cmd.ioa) || 0
            });
          });
        });

        const uniqueSignals = Array.from(new Map(signalsArray.map(item => [item.ioa, item])).values());

        const runtimeRequest = {
          interfaceName: "enp0s3", 
          stationName: "PN-Device",
          moduleIdent: 4102, 
          submoduleIdent: 4102,
          inputLength: 64,
          outputLength: 64,
          iecIpAddress: "0.0.0.0",
          iecPort: 2404,
          signals: uniqueSignals
        };
        
        console.log("ОТПРАВЛЯЕМЫЙ JSON:", JSON.stringify(runtimeRequest, null, 2));

        const response = await fetch('http://localhost:5000/api/runtime/start', { 
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(runtimeRequest)
        }); 
        
        if (!response.ok) {
           const errText = await response.text();
           throw new Error(errText);
        }

        isRuntimeMode.value = true;
      } else {
        await fetch('http://localhost:5000/api/runtime/stop', { method: 'POST' });
        isRuntimeMode.value = false;
      }
    } catch (e) {
      console.error("Ошибка сети при смене режима исполнения", e);
      alert("Ошибка запуска: " + e.message);
    }
  }

  return {
    projects,
    selectedNode,
    loading,
    error,

    loadProjects,
    addProject,
    addServer,
    addInterface,
    addStation,
    addIecChannel,

    deleteNode,
    importGsdml,
    updateStationConfiguration,

    assignModule,     
    clearSlotModule,  
    saveAllProjects, 

    findNodeById,
    selectNode,
    findAllStations,

    hoveredModule,
    setHoveredModule,

    isRuntimeMode,
    toggleRuntimeMode,
  }
})