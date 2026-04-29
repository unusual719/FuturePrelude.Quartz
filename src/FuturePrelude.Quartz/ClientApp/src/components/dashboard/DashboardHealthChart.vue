<template>
  <section class="dashboard-chart-card">
    <header class="dashboard-panel-header">
      <div>
        <p class="dashboard-panel-eyebrow">Current Snapshot</p>
        <h3><i class="fas fa-chart-pie"></i> 任务健康分布</h3>
      </div>
      <span class="dashboard-panel-tag">{{ totalCount }} 个任务</span>
    </header>
    <div
      v-if="totalCount > 0"
      class="dashboard-health-tags"
      role="list"
      aria-label="任务健康标签"
    >
      <div
        v-for="item in normalizedItems"
        :key="item.label"
        class="dashboard-health-tag"
        :style="{
          '--dashboard-health-tag-color': item.color,
        }"
        role="listitem"
      >
        <span class="dashboard-health-tag-dot" aria-hidden="true"></span>
        <span class="dashboard-health-tag-label">{{ item.label }}</span>
        <strong class="dashboard-health-tag-count">{{ item.count }}</strong>
      </div>
    </div>
    <div v-if="totalCount === 0" class="dashboard-empty-state">
      暂无任务数据
    </div>
    <div v-else ref="chartRef" class="dashboard-chart-body"></div>
  </section>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, ref, watch } from "vue";
import { echarts } from "../../utils/dashboardEcharts.js";

const HEALTH_STYLES = {
  运行中: {
    color: "#17b26a",
    backgroundColor: "rgba(23, 178, 106, 0.12)",
    borderColor: "rgba(23, 178, 106, 0.2)",
  },
  已停止: {
    color: "#6b7a99",
    backgroundColor: "rgba(107, 122, 153, 0.12)",
    borderColor: "rgba(107, 122, 153, 0.2)",
  },
  空闲中: {
    color: "#f59e0b",
    backgroundColor: "rgba(245, 158, 11, 0.12)",
    borderColor: "rgba(245, 158, 11, 0.2)",
  },
};

const props = defineProps({
  items: {
    type: Array,
    required: true,
  },
});

const chartRef = ref(null);
let chartInstance = null;

const normalizedItems = computed(() =>
  props.items.map((item) => {
    const style = HEALTH_STYLES[item.label] ?? {
      color: item.color || "#5a6f8f",
      backgroundColor: "rgba(90, 111, 143, 0.12)",
      borderColor: "rgba(90, 111, 143, 0.2)",
    };

    return {
      label: item.label,
      count: Number(item.count) || 0,
      color: style.color,
      backgroundColor: style.backgroundColor,
      borderColor: style.borderColor,
    };
  }),
);

const totalCount = computed(() =>
  normalizedItems.value.reduce((sum, item) => sum + item.count, 0),
);

const resizeChart = () => {
  chartInstance?.resize();
};

const renderChart = () => {
  if (!chartRef.value || totalCount.value === 0) {
    return;
  }

  if (!chartInstance) {
    chartInstance = echarts.init(chartRef.value);
  }

  chartInstance.setOption({
    color: normalizedItems.value.map((item) => item.color),
    tooltip: {
      trigger: "item",
      backgroundColor: "rgba(255,255,255,0.96)",
      borderColor: "#dbe7f3",
      textStyle: { color: "#10233f" },
      formatter: ({ name, value, percent }) =>
        `${name}<br/>数量：${value}<br/>占比：${percent}%`,
    },
    legend: {
      show: false,
    },
    series: [
      {
        type: "pie",
        radius: ["48%", "70%"],
        center: ["50%", "58%"],
        label: {
          color: "#1f3554",
          fontWeight: 700,
          formatter: (params) => {
            return `${params.name}（${params.percent}%）`;
          },
        },
        labelLine: {
          length: 9,
          length2: 10,
        },
        itemStyle: {
          borderColor: "#ffffff",
          borderWidth: 3,
          borderRadius: 10,
        },
        emphasis: {
          scale: true,
          scaleSize: 4,
        },
        data: normalizedItems.value.map((item) => ({
          value: item.count,
          name: item.label,
          itemStyle: { color: item.color },
        })),
      },
    ],
  });
};

watch(
  () => props.items,
  () => {
    renderChart();
  },
  { deep: true },
);

onMounted(() => {
  renderChart();
  window.addEventListener("resize", resizeChart);
});

onBeforeUnmount(() => {
  window.removeEventListener("resize", resizeChart);
  chartInstance?.dispose();
  chartInstance = null;
});
</script>

<style scoped>
.dashboard-health-tags {
  display: flex;
  justify-content: center;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
  margin: 4px 0 16px;
}

.dashboard-health-tag {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  min-width: 112px;
  padding: 10px 14px;
  border: 1px solid var(--dashboard-health-tag-border);
  border-radius: 999px;
  background: var(--dashboard-health-tag-bg);
  color: #1d3152;
}

.dashboard-health-tag-dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background: var(--dashboard-health-tag-color);
  box-shadow: 0 0 0 4px
    color-mix(in srgb, var(--dashboard-health-tag-color) 18%, transparent);
}

.dashboard-health-tag-label {
  font-size: 13px;
  font-weight: 600;
}

.dashboard-health-tag-count {
  color: var(--dashboard-health-tag-color);
  font-size: 13px;
  font-weight: 800;
}

@media (max-width: 768px) {
  .dashboard-health-tags {
    justify-content: flex-start;
  }

  .dashboard-health-tag {
    flex: 1 1 calc(50% - 12px);
    min-width: 132px;
  }
}
</style>
