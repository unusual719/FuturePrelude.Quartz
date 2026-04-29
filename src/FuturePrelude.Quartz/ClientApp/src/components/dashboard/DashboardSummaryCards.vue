<template>
  <section class="dashboard-stats-grid">
    <article
      v-for="card in cards"
      :key="card.key"
      :data-card-key="card.key"
      class="dashboard-stat-card"
      :class="`dashboard-stat-${card.tone}`"
    >
      <div class="dashboard-stat-header">
        <div class="dashboard-stat-copy">
          <span class="dashboard-stat-title">{{ card.title }}</span>
          <span class="dashboard-stat-badge" :class="`dashboard-stat-badge-${card.tone}`">
            {{ card.badge }}
          </span>
        </div>
        <div class="dashboard-stat-icon" :class="card.tone">
          <i :class="card.icon"></i>
        </div>
      </div>
      <div class="dashboard-stat-value">{{ card.value }}</div>
      <div class="dashboard-stat-meta">{{ card.meta }}</div>
    </article>
  </section>
</template>

<script setup>
import { computed } from "vue";

const props = defineProps({
  summary: {
    type: Object,
    required: true,
  },
});

const cards = computed(() => [
  {
    key: "total",
    title: "总任务数",
    value: props.summary.totalTasks ?? 0,
    meta: "当前所有有效任务",
    icon: "fas fa-tasks",
    tone: "blue",
    badge: "全量",
  },
  {
    key: "running",
    title: "运行中",
    value: props.summary.runningTasks ?? 0,
    meta: "当前正在运行或调度中",
    icon: "fas fa-play-circle",
    tone: "green",
    badge: "活跃",
  },
  {
    key: "idle",
    title: "空闲中",
    value: props.summary.idleTaskCount ?? 0,
    meta: "等待触发或处于调度空闲",
    icon: "fas fa-pause-circle",
    tone: "cyan",
    badge: "待命",
  },
  {
    key: "latency",
    title: "平均响应",
    value: `${props.summary.averageResponseMs ?? 0} ms`,
    meta: "今日已完成执行平均耗时",
    icon: "fas fa-stopwatch",
    tone: "amber",
    badge: "耗时",
  },
  {
    key: "success-rate",
    title: "成功率",
    value: `${props.summary.successRate ?? 0}%`,
    meta: "今日终态执行成功占比",
    icon: "fas fa-percent",
    tone: "indigo",
    badge: "质量",
  },
]);
</script>

<style scoped>
.dashboard-stat-card {
  position: relative;
  overflow: hidden;
  min-height: 184px;
  padding: 22px 20px 20px;
  border-radius: 26px;
  border: 1px solid rgba(220, 230, 241, 0.96);
  box-shadow: 0 18px 34px rgba(15, 23, 42, 0.06);
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.98) 0%, rgba(245, 249, 255, 0.96) 100%);
}

.dashboard-stat-card::before {
  content: "";
  position: absolute;
  inset: 0 0 auto;
  height: 5px;
  background: linear-gradient(90deg, #3b82f6, #93c5fd);
}

.dashboard-stat-card.dashboard-stat-blue {
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.98) 0%, rgba(239, 246, 255, 0.9) 100%);
}

.dashboard-stat-card.dashboard-stat-green {
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.98) 0%, rgba(236, 253, 245, 0.92) 100%);
}

.dashboard-stat-card.dashboard-stat-cyan {
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.98) 0%, rgba(236, 254, 255, 0.92) 100%);
}

.dashboard-stat-card.dashboard-stat-amber {
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.98) 0%, rgba(255, 251, 235, 0.92) 100%);
}

.dashboard-stat-card.dashboard-stat-indigo {
  background: linear-gradient(180deg, rgba(255, 255, 255, 0.98) 0%, rgba(238, 242, 255, 0.92) 100%);
}

.dashboard-stat-card.dashboard-stat-green::before {
  background: linear-gradient(90deg, #10b981, #6ee7b7);
}

.dashboard-stat-card.dashboard-stat-cyan::before {
  background: linear-gradient(90deg, #06b6d4, #67e8f9);
}

.dashboard-stat-card.dashboard-stat-amber::before {
  background: linear-gradient(90deg, #f59e0b, #fcd34d);
}

.dashboard-stat-card.dashboard-stat-indigo::before {
  background: linear-gradient(90deg, #4f46e5, #818cf8);
}

.dashboard-stat-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 24px;
}

.dashboard-stat-copy {
  display: grid;
  gap: 10px;
}

.dashboard-stat-title {
  font-size: 14px;
  font-weight: 700;
  color: #475a77;
}

.dashboard-stat-badge {
  width: fit-content;
  display: inline-flex;
  align-items: center;
  padding: 6px 10px;
  border-radius: 999px;
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.04em;
}

.dashboard-stat-badge-blue {
  background: rgba(37, 99, 235, 0.12);
  color: #2563eb;
}

.dashboard-stat-badge-green {
  background: rgba(16, 185, 129, 0.12);
  color: #0f9f6e;
}

.dashboard-stat-badge-cyan {
  background: rgba(6, 182, 212, 0.12);
  color: #0891b2;
}

.dashboard-stat-badge-amber {
  background: rgba(245, 158, 11, 0.14);
  color: #d97706;
}

.dashboard-stat-badge-indigo {
  background: rgba(79, 70, 229, 0.12);
  color: #4f46e5;
}

.dashboard-stat-icon {
  width: 50px;
  height: 50px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 18px;
  font-size: 20px;
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.55);
}

.dashboard-stat-icon.blue {
  background: #dbeafe;
  color: #2563eb;
}

.dashboard-stat-icon.green {
  background: #dcfce7;
  color: #10b981;
}

.dashboard-stat-icon.cyan {
  background: #cffafe;
  color: #0891b2;
}

.dashboard-stat-icon.amber {
  background: #fef3c7;
  color: #d97706;
}

.dashboard-stat-icon.indigo {
  background: #e0e7ff;
  color: #4f46e5;
}

.dashboard-stat-value {
  font-size: 38px;
  line-height: 1;
  font-weight: 800;
  letter-spacing: -0.04em;
  color: #13233d;
}

.dashboard-stat-meta {
  margin-top: 12px;
  max-width: 18ch;
  font-size: 12px;
  line-height: 1.6;
  color: #70839d;
}
</style>
