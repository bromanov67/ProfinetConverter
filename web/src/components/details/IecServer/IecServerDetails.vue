<template>
  <div class="details-content">
    <table class="properties-table">
      <tbody>
        <tr>
          <td class="prop-label">Активация</td>
          <td class="prop-value">
            <input type="checkbox" v-model="node.active" />
          </td>
        </tr>
        <tr>
          <td class="prop-label">Имя</td>
          <td class="prop-value">
            <input type="text" v-model="node.name" class="input-field" />
          </td>
        </tr>
        <tr>
          <td class="prop-label">Описание</td>
          <td class="prop-value">
            <input type="text" v-model="node.description" class="input-field" />
          </td>
        </tr>
      </tbody>
    </table>
  </div>
  
  <div class="details-actions">
    <button class="action-btn" @click="emitAddInterface">+ Add Interface</button>
  </div>
</template>

<script setup>
import { useDeviceStore } from '../../../stores/deviceStore'

const props = defineProps({ node: Object })
const store = useDeviceStore()

const emitAddInterface = async () => {
  const name = prompt('Enter Interface Name (e.g. Ethernet):', 'Ethernet')
  if (name) {
    // Вызываем существующий метод стора, чтобы добавить интерфейс в сервер
    await store.addInterface(props.node.id, name)
  }
}
</script>

<style scoped>
.details-content {
  flex: 1;
  overflow-y: auto;
  padding: 0;
}

.properties-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 13px;
  background: #f0f0f0;
  color: #333;
}

.properties-table tbody tr {
  border-bottom: 1px solid #ddd;
}

.properties-table td {
  padding: 8px 12px;
  vertical-align: middle;
}

.prop-label {
  width: 50%;
  background: #e8e8e8;
  font-weight: 500;
  color: #444;
}

.prop-value {
  width: 50%;
  background: #f5f5f5;
  color: #333;
}

.input-field {
  width: 100%;
  background: white;
  border: 1px solid #ccc;
  color: #333;
  padding: 4px 6px;
  border-radius: 2px;
  font-size: 12px;
}

.input-field:focus {
  outline: none;
  border-color: #2c5aa0;
  background: #fafafa;
}

.details-actions {
  display: flex;
  gap: 10px;
  padding: 16px 20px;
  border-top: 1px solid #e0e8f0;
  background: #f5f5f5;
}

.action-btn {
  flex: 1;
  padding: 10px 16px;
  background: #2c5aa0;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-weight: 500;
  font-size: 13px;
}

.action-btn:hover {
  background: #1e3f6b;
}
</style>
