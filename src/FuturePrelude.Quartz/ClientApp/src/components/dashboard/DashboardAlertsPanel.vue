<template>
  <section class="dashboard-panel dashboard-alerts-panel" v-if="false">
    <header class="dashboard-panel-header dashboard-alerts-header">
      <div>
        <p class="dashboard-panel-eyebrow">Alerts</p>
        <h3><i class="fas fa-bell"></i> 实时告警</h3>
      </div>
      <span class="dashboard-panel-tag">{{ alerts.length }} 条</span>
    </header>

    <div
      v-if="alerts.length === 0"
      class="dashboard-empty-state dashboard-alerts-empty"
    >
      <i class="fas fa-shield-heart"></i>
      <div>
        <strong>暂无告警规则命中</strong>
        <p>当前版本已预留告警卡位，后续可接入真实告警规则与处理动作。</p>
      </div>
    </div>

    <div v-else class="dashboard-alert-list">
      <article
        v-for="(alert, index) in alerts"
        :key="`${alert.title}-${index}`"
        class="dashboard-alert-item"
      >
        <div class="dashboard-alert-icon">
          <i class="fas fa-triangle-exclamation"></i>
        </div>
        <div class="dashboard-alert-copy">
          <strong>{{ alert.title }}</strong>
          <p>{{ alert.description }}</p>
        </div>
        <span class="dashboard-alert-time">{{ alert.createdAt }}</span>
      </article>
    </div>
  </section>
</template>

<script setup>
defineProps({
  alerts: {
    type: Array,
    required: true,
  },
});
</script>
