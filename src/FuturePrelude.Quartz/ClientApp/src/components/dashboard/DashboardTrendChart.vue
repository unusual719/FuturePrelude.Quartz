<template>
  <section class="dashboard-chart-card">
    <header class="dashboard-panel-header">
      <div>
        <p class="dashboard-panel-eyebrow">24 Hours</p>
        <h3><i class="fas fa-chart-line"></i> 执行趋势</h3>
      </div>
      <span class="dashboard-panel-tag">{{ points.length }} 个时段</span>
    </header>
    <div
      v-if="points.length > 0"
      class="dashboard-trend-tags"
      role="list"
      aria-label="执行趋势标签"
    >
      <div
        v-for="item in trendTags"
        :key="item.label"
        class="dashboard-trend-tag"
        :style="{
          '--dashboard-trend-tag-color': item.color,
        }"
        role="listitem"
      >
        <span class="dashboard-trend-tag-dot" aria-hidden="true"></span>
        <span class="dashboard-trend-tag-label">{{ item.label }}</span>
        <strong class="dashboard-trend-tag-count">{{ item.count }}</strong>
      </div>
    </div>
    <div v-if="points.length === 0" class="dashboard-empty-state">
      暂无趋势数据
    </div>
    <div v-else ref="chartRef" class="dashboard-chart-body"></div>
  </section>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, ref, watch } from "vue";
import { formatDateTimeToSeconds } from "../../utils/dateTime.js";
import { echarts } from "../../utils/dashboardEcharts.js";

const TREND_STYLES = {
  成功: {
    color: "#17b26a",
    backgroundColor: "rgba(23, 178, 106, 0.12)",
    borderColor: "rgba(23, 178, 106, 0.2)",
    areaColor: "rgba(23, 178, 106, 0.16)",
  },
  失败: {
    color: "#f97316",
    backgroundColor: "rgba(249, 115, 22, 0.12)",
    borderColor: "rgba(249, 115, 22, 0.2)",
    areaColor: "rgba(249, 115, 22, 0.12)",
  },
};

const props = defineProps({
  points: {
    type: Array,
    required: true,
  },
});

const chartRef = ref(null);
let chartInstance = null;

const trendTags = computed(() => [
  {
    label: "成功",
    count: props.points.reduce(
      (sum, item) => sum + (Number(item.successCount) || 0),
      0,
    ),
    ...TREND_STYLES.成功,
  },
  {
    label: "失败",
    count: props.points.reduce(
      (sum, item) => sum + (Number(item.failureCount) || 0),
      0,
    ),
    ...TREND_STYLES.失败,
  },
]);

const resizeChart = () => {
  chartInstance?.resize();
};

const renderChart = () => {
  if (!chartRef.value || props.points.length === 0) {
    return;
  }

  if (!chartInstance) {
    chartInstance = echarts.init(chartRef.value);
  }

  chartInstance.setOption({
    color: [TREND_STYLES.成功.color, TREND_STYLES.失败.color],
    tooltip: {
      trigger: "axis",
      backgroundColor: "rgba(255,255,255,0.96)",
      borderColor: "#dbe7f3",
      textStyle: { color: "#10233f" },
      padding: [10, 12],
      axisPointer: {
        type: "line",
        lineStyle: {
          color: "rgba(110, 131, 165, 0.35)",
          width: 1,
        },
      },
    },
    grid: {
      left: 28,
      right: 20,
      top: 24,
      bottom: 24,
      outerBoundsMode: "same",
      outerBoundsContain: "axisLabel",
    },
    legend: {
      show: false,
    },
    xAxis: {
      type: "category",
      boundaryGap: false,
      axisLine: { lineStyle: { color: "#dde7f2" } },
      axisTick: { show: false },
      axisLabel: {
        color: "#6a7f99",
        formatter: (value) => value.slice(11, 16),
      },
      data: props.points.map((item) =>
        formatDateTimeToSeconds(item.bucketTime),
      ),
    },
    yAxis: {
      type: "value",
      splitLine: { lineStyle: { color: "#edf3f9", type: "dashed" } },
      axisLabel: { color: "#6a7f99" },
    },
    series: [
      {
        name: "成功",
        type: "line",
        smooth: true,
        symbol: "circle",
        symbolSize: 6,
        showSymbol: false,
        lineStyle: {
          width: 3,
          color: TREND_STYLES.成功.color,
        },
        itemStyle: {
          color: TREND_STYLES.成功.color,
        },
        areaStyle: {
          color: TREND_STYLES.成功.areaColor,
        },
        emphasis: {
          focus: "series",
        },
        data: props.points.map((item) => item.successCount),
      },
      {
        name: "失败",
        type: "line",
        smooth: true,
        symbol: "circle",
        symbolSize: 6,
        showSymbol: false,
        lineStyle: {
          width: 3,
          color: TREND_STYLES.失败.color,
        },
        itemStyle: {
          color: TREND_STYLES.失败.color,
        },
        areaStyle: {
          color: TREND_STYLES.失败.areaColor,
        },
        emphasis: {
          focus: "series",
        },
        data: props.points.map((item) => item.failureCount),
      },
    ],
  });
};

watch(
  () => props.points,
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
.dashboard-trend-tags {
  display: flex;
  justify-content: center;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
  margin: 4px 0 16px;
}

.dashboard-trend-tag {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  min-width: 112px;
  padding: 10px 14px;
  border: 1px solid var(--dashboard-trend-tag-border);
  border-radius: 999px;
  background: var(--dashboard-trend-tag-bg);
  color: #1d3152;
}

.dashboard-trend-tag-dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background: var(--dashboard-trend-tag-color);
}

.dashboard-trend-tag-label {
  font-size: 13px;
  font-weight: 600;
}

.dashboard-trend-tag-count {
  color: var(--dashboard-trend-tag-color);
  font-size: 13px;
  font-weight: 800;
}

@media (max-width: 768px) {
  .dashboard-trend-tags {
    justify-content: flex-start;
  }

  .dashboard-trend-tag {
    flex: 1 1 calc(50% - 12px);
    min-width: 132px;
  }
}
</style>
