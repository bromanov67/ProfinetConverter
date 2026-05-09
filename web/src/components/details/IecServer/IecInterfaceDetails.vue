<template>
  <div class="details-content">
    <table class="properties-table">
      <tbody>
        <tr>
          <td class="prop-label">Активация</td>
          <td class="prop-value"><input type="checkbox" v-model="node.active" /></td>
        </tr>
        <tr>
          <td class="prop-label">Имя</td>
          <td class="prop-value">{{ node.name }}</td>
        </tr>
        <tr>
          <td class="prop-label">Описание</td>
          <td class="prop-value"><input type="text" v-model="node.description" class="input-field" /></td>
        </tr>
      </tbody>
    </table>
  </div>
  <div class="details-actions">
    <button class="action-btn" @click="emitAddChannel">+ Add Channel (Канал)</button>
  </div>
</template>

<script setup>
import { useDeviceStore } from '../../../stores/deviceStore'
const props = defineProps({ node: Object })
const store = useDeviceStore()

const emitAddChannel = async () => {
  const name = prompt('Введите имя канала:', 'Канал')
  if (name) await store.addIecChannel(props.node.id, name)
}
</script>