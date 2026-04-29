<template>
  <section class="dashboard-panel">
    <header class="dashboard-panel-header">
      <div>
        <p class="dashboard-panel-eyebrow">Latest Runs</p>
        <h3><i class="fas fa-history"></i> 最近执行记录</h3>
      </div>
      <button type="button" class="dashboard-link-btn" @click="$emit('view-all')">
        查看全部 <i class="fas fa-arrow-right"></i>
      </button>
    </header>

    <div v-if="rows.length === 0" class="dashboard-empty-state">
      暂无执行记录
    </div>

    <div v-else class="dashboard-table-shell">
      <table class="dashboard-table">
        <thead>
          <tr>
            <th>任务名称</th>
            <th>执行时间</th>
            <th>状态</th>
            <th>耗时</th>
            <th>结果摘要</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="row in rows" :key="row.id">
            <td>
              <strong>{{ row.taskName }}</strong>
              <div class="dashboard-table-sub">{{ row.taskGroup }}</div>
            </td>
            <td>{{ row.executedAt }}</td>
            <td>
              <span class="dashboard-status-pill" :class="row.statusClass">
                <i :class="row.icon"></i>
                {{ row.statusText }}
              </span>
            </td>
            <td>{{ row.durationText }}</td>
            <td class="dashboard-message-cell">{{ row.message }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>

<script setup>
import { computed } from "vue";
import { formatDateTimeToSeconds } from "../../utils/dateTime.js";

const props = defineProps({
  items: {
    type: Array,
    required: true,
  },
});

defineEmits(["view-all"]);

const formatDuration = (value) => {
  if (value === null || value === undefined || value === "") {
    return "-";
  }

  const numberValue = Number(value);
  if (!Number.isFinite(numberValue)) {
    return "-";
  }

  if (numberValue < 1000) {
    return `${Math.round(numberValue)} ms`;
  }

  return `${(numberValue / 1000).toFixed(2)} s`;
};

const resolveStatusVisual = (status) => {
  if (status === "Success") {
    return {
      statusClass: "success",
      icon: "fas fa-check-circle",
    };
  }

  if (status === "Failed" || status === "Timeout") {
    return {
      statusClass: "failed",
      icon: "fas fa-circle-xmark",
    };
  }

  if (status === "Cancelled") {
    return {
      statusClass: "neutral",
      icon: "fas fa-ban",
    };
  }

  return {
    statusClass: "running",
    icon: "fas fa-spinner dashboard-icon-spinning",
  };
};

const rows = computed(() =>
  props.items.map((item) => {
    const statusVisual = resolveStatusVisual(item.status);
    return {
      id: item.id,
      taskName: item.taskName,
      taskGroup: item.taskGroup,
      executedAt: formatDateTimeToSeconds(item.executedAt),
      statusText: item.statusText,
      durationText: formatDuration(item.durationMs),
      message: item.message || "-",
      ...statusVisual,
    };
  }),
);
</script>
