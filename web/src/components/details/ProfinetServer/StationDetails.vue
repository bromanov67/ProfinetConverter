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
          <td class="prop-value">{{ node.name }}</td>
        </tr>
        <tr>
          <td class="prop-label">Описание</td>
          <td class="prop-value">
            <input type="text" v-model="node.description" class="input-field" />
          </td>
        </tr>

        <!-- ▶ Конфигурация -->
        <tr class="section-header" @click="toggleSection('config')">
          <td colspan="2"><span class="expand-icon">{{ expanded.config ? '▼' : '▶' }}</span>Конфигурация</td>
        </tr>

        <template v-if="expanded.config">
          <tr>
            <td class="prop-label">Адрес станции</td>
            <td class="prop-value">{{ node.address }}</td>
          </tr>

          <!-- ▶▶ Описание устройства GSDML -->
          <tr class="subsection-header" @click="toggleSection('gsdml')">
            <td colspan="2"><span class="expand-icon">{{ expanded.gsdml ? '▼' : '▶' }}</span>Общее описание устройства (GSDML)</td>
          </tr>
          <template v-if="expanded.gsdml">
            <tr>
              <td class="prop-label">Краткое обозначение</td>
              <td class="prop-value"><input class="input-field" disabled :value="node.configuration?.shortDesignation" /></td>
            </tr>
            <tr>
              <td class="prop-label">Описание</td>
              <td class="prop-value"><input class="input-field" disabled :value="node.configuration?.deviceDescription" /></td>
            </tr>
            <tr>
              <td class="prop-label">Производитель</td>
              <td class="prop-value"><input class="input-field" disabled :value="node.configuration?.manufacturer" /></td>
            </tr>
            <tr>
              <td class="prop-label">Идентификатор устройства</td>
              <td class="prop-value"><input class="input-field" disabled :value="node.configuration?.identifier" /></td>
            </tr>
            <tr>
              <td class="prop-label">Артикул</td>
              <td class="prop-value"><input class="input-field" disabled :value="node.configuration?.articleNo" /></td>
            </tr>
            <tr>
              <td class="prop-label">Версия прошивки</td>
              <td class="prop-value"><input class="input-field" disabled :value="node.configuration?.firmwareVersion" /></td>
            </tr>
            <tr>
              <td class="prop-label">Версия аппаратного обеспечения</td>
              <td class="prop-value"><input class="input-field" disabled :value="node.configuration?.hardwareVersion" /></td>
            </tr>
            <tr>
              <td class="prop-label">GSD файл</td>
              <td class="prop-value"><input class="input-field" disabled :value="node.configuration?.gsdFile" /></td>
            </tr>
            <tr>
              <td class="prop-label">Последовательность</td>
              <td class="prop-value"><input class="input-field" v-model="node.configuration.consistency" /></td>
            </tr>
          </template>

          <!-- ▶▶ IP-протокол -->
          <tr class="subsection-header" @click="toggleSection('ip')">
            <td colspan="2"><span class="expand-icon">{{ expanded.ip ? '▼' : '▶' }}</span>IP-протокол</td>
          </tr>
          <template v-if="expanded.ip">
            <tr>
              <td class="prop-label">IP-адрес</td>
              <td class="prop-value"><input class="input-field" v-model="node.configuration.ipAddress" placeholder="192.168.0.1" /></td>
            </tr>
            <tr>
              <td class="prop-label">Маска подсети</td>
              <td class="prop-value"><input class="input-field" v-model="node.configuration.subnetMask" placeholder="255.255.255.0" /></td>
            </tr>
          </template>

          <!-- ▶▶ PROFINET -->
          <tr class="subsection-header" @click="toggleSection('profinet')">
            <td colspan="2"><span class="expand-icon">{{ expanded.profinet ? '▼' : '▶' }}</span>PROFINET</td>
          </tr>
          <template v-if="expanded.profinet">
            <tr>
              <td class="prop-label">Имя устройства PROFINET</td>
              <td class="prop-value"><input class="input-field" v-model="node.configuration.profinetDeviceName" /></td>
            </tr>
            <tr>
              <td class="prop-label">Номер устройства</td>
              <td class="prop-value"><input type="number" class="input-field" v-model.number="node.configuration.deviceNumber" min="1" /></td>
            </tr>
          </template>
        </template>
      </tbody>
    </table>
  </div>
</template>

<script setup>
import { reactive } from 'vue'

const props = defineProps({ node: Object })

const expanded = reactive({ 
  config: true, 
  gsdml: true, 
  ip: true, 
  profinet: true 
})

const toggleSection = (key) => { expanded[key] = !expanded[key] }
</script>
