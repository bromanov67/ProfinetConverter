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
        <tr>
          <td class="prop-label">IP адрес</td>
          <td class="prop-value">
            <!-- Используем node.configuration.ipAddress, если конфигурация существует, иначе можно обращаться напрямую -->
            <input type="text" v-model="iecConfig.ipAddress" class="input-field" placeholder="0.0.0.0" />
          </td>
        </tr>
        <tr>
          <td class="prop-label">Порт</td>
          <td class="prop-value">
            <input type="number" v-model.number="iecConfig.port" class="input-field" />
          </td>
        </tr>
        <tr>
          <td class="prop-label">Часовой пояс</td>
          <td class="prop-value">
             <select v-model="iecConfig.timezone" class="input-field">
                <option value="UTC">UTC</option>
                <option value="Local">Local</option>
             </select>
          </td>
        </tr>
        <tr>
          <td class="prop-label">k</td>
          <td class="prop-value">
            <input type="number" v-model.number="iecConfig.k" class="input-field" />
          </td>
        </tr>
        <tr>
          <td class="prop-label">w</td>
          <td class="prop-value">
            <input type="number" v-model.number="iecConfig.w" class="input-field" />
          </td>
        </tr>
        <tr>
          <td class="prop-label">t0</td>
          <td class="prop-value">
            <input type="number" v-model.number="iecConfig.t0" class="input-field" />
          </td>
        </tr>
        <tr>
          <td class="prop-label">t1</td>
          <td class="prop-value">
            <input type="number" v-model.number="iecConfig.t1" class="input-field" />
          </td>
        </tr>
        <tr>
          <td class="prop-label">t2</td>
          <td class="prop-value">
            <input type="number" v-model.number="iecConfig.t2" class="input-field" />
          </td>
        </tr>
        <tr>
          <td class="prop-label">t3</td>
          <td class="prop-value">
            <input type="number" v-model.number="iecConfig.t3" class="input-field" />
          </td>
        </tr>
        <tr>
          <td class="prop-label">Буферизация</td>
          <td class="prop-value">
            <input type="checkbox" v-model="iecConfig.buffering" />
          </td>
        </tr>
        <tr v-if="iecConfig.buffering">
          <td class="prop-label">Размер буфера ТИ</td>
          <td class="prop-value">
            <input type="number" v-model.number="iecConfig.bufferTi" class="input-field" />
          </td>
        </tr>
        <tr v-if="iecConfig.buffering">
          <td class="prop-label">Размер буфера ТС</td>
          <td class="prop-value">
            <input type="number" v-model.number="iecConfig.bufferTs" class="input-field" />
          </td>
        </tr>
      </tbody>
    </table>
  </div>
  
  <!-- Если для Канала МЭК нужны какие-то дочерние сущности, кнопки можно добавить сюда -->
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({ node: Object })

// Создаем fallback объект, чтобы не было ошибок, если configuration не инициализировано
const iecConfig = computed(() => {
  if (!props.node.configuration) {
    // В идеале это должно создаваться на бэкенде при добавлении Канала
    props.node.configuration = {
      ipAddress: '0.0.0.0',
      port: 2404,
      timezone: 'UTC',
      k: 12,
      w: 8,
      t0: 30,
      t1: 15,
      t2: 10,
      t3: 20,
      buffering: true,
      bufferTi: 25,
      bufferTs: 1000
    }
  }
  return props.node.configuration
})
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
</style>
