<template>
  <div class="app-wrapper">
    <PlatformTopNav />

    <main class="dashboard-page">
      <section class="dashboard-header-card">
        <div class="dashboard-header-copy">
          <p class="dashboard-eyebrow">Global Overview</p>
          <h1>监控大盘</h1>
          <p class="dashboard-subtitle">
            全局掌握所有分组的调度状态、执行趋势与最近执行结果。
          </p>
        </div>
        <div class="dashboard-header-meta">
          <div class="dashboard-meta-row">
            <div class="dashboard-meta-item">
              <span>最后更新</span>
              <strong>{{ lastUpdatedLabel }}</strong>
            </div>
            <div class="dashboard-meta-item">
              <span>实时状态</span>
              <strong
                class="dashboard-connection-status"
                :class="connectionStatusClass"
              >
                <i :class="connectionStatusIconClass" aria-hidden="true"></i>
                <span>{{ connectionText }}</span>
              </strong>
            </div>
          </div>
          <div class="dashboard-header-actions">
            <button
              type="button"
              class="dashboard-refresh-btn"
              :disabled="refreshing"
              @click="handleManualRefresh"
            >
              <i class="fas fa-rotate-right"></i>
              {{ refreshing ? "刷新中..." : "手动刷新" }}
            </button>
          </div>
        </div>
      </section>

      <section v-if="loading" class="dashboard-state-card">
        <i class="fas fa-spinner fa-spin"></i>
        监控概览加载中...
      </section>

      <section v-else-if="errorMessage" class="dashboard-state-card dashboard-state-error">
        <i class="fas fa-circle-exclamation"></i>
        <div>
          <strong>加载失败</strong>
          <p>{{ errorMessage }}</p>
        </div>
        <button type="button" class="dashboard-refresh-btn" @click="handleManualRefresh">
          重新加载
        </button>
      </section>

      <template v-else>
        <DashboardSummaryCards :summary="overview.summary" />

        <section class="dashboard-charts-grid">
          <DashboardTrendChart :points="overview.trend" />
          <DashboardHealthChart :items="overview.healthDistribution" />
        </section>

        <DashboardAlertsPanel :alerts="overview.alerts" />

        <DashboardRecentExecutions
          :items="overview.recentExecutions"
          @view-all="openExecutionRecords"
        />
      </template>
    </main>
  </div>
</template>

<script setup>
import { computed, defineAsyncComponent, onBeforeUnmount, onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import DashboardAlertsPanel from "../components/dashboard/DashboardAlertsPanel.vue";
import DashboardRecentExecutions from "../components/dashboard/DashboardRecentExecutions.vue";
import DashboardSummaryCards from "../components/dashboard/DashboardSummaryCards.vue";
import PlatformTopNav from "../components/PlatformTopNav.vue";
import "../styles/dashboard.css";
import { formatDateTimeToSeconds } from "../utils/dateTime.js";
import { getDashboardOverview } from "../utils/dashboardApi.js";
import { createDashboardConnection } from "../utils/dashboardSignalR.js";
import { showToast } from "../utils/toast.js";

const DashboardTrendChart = defineAsyncComponent(() =>
  import("../components/dashboard/DashboardTrendChart.vue"),
);
const DashboardHealthChart = defineAsyncComponent(() =>
  import("../components/dashboard/DashboardHealthChart.vue"),
);

const router = useRouter();

const createEmptyOverview = () => ({
  lastUpdatedAt: "",
  summary: {
    totalTasks: 0,
    runningTasks: 0,
    idleTaskCount: 0,
    failedToday: 0,
    averageResponseMs: 0,
    successRate: 0,
  },
  trend: [],
  healthDistribution: [],
  alerts: [],
  recentExecutions: [],
});

const loading = ref(true);
const refreshing = ref(false);
const errorMessage = ref("");
const connectionText = ref("实时连接中");
const overview = ref(createEmptyOverview());

let dashboardConnection = null;

const lastUpdatedLabel = computed(() =>
  formatDateTimeToSeconds(overview.value.lastUpdatedAt),
);

const connectionStatusClass = computed(() => {
  if (connectionText.value.includes("重连")) {
    return "dashboard-connection-status-reconnecting";
  }

  if (connectionText.value.includes("连接中")) {
    return "dashboard-connection-status-live";
  }

  if (connectionText.value.includes("断开")) {
    return "dashboard-connection-status-offline";
  }

  return "dashboard-connection-status-warning";
});

const connectionStatusIconClass = computed(() => {
  if (connectionText.value.includes("重连")) {
    return "fas fa-rotate-right dashboard-icon-spinning";
  }

  if (connectionText.value.includes("连接中")) {
    return "fas fa-circle-notch dashboard-icon-spinning";
  }

  if (connectionText.value.includes("断开")) {
    return "fas fa-circle-xmark";
  }

  return "fas fa-circle-exclamation";
});

const normalizeOverview = (payload = {}) => {
  const summary = payload.Summary ?? payload.summary ?? {};
  const trend = payload.Trend ?? payload.trend ?? [];
  const healthDistribution =
    payload.HealthDistribution ?? payload.healthDistribution ?? [];
  const alerts = payload.Alerts ?? payload.alerts ?? [];
  const recentExecutions =
    payload.RecentExecutions ?? payload.recentExecutions ?? [];

  return {
    lastUpdatedAt: payload.LastUpdatedAt ?? payload.lastUpdatedAt ?? "",
    summary: {
      totalTasks: summary.TotalTasks ?? summary.totalTasks ?? 0,
      runningTasks: summary.RunningTasks ?? summary.runningTasks ?? 0,
      idleTaskCount: summary.IdleTaskCount ?? summary.idleTaskCount ?? 0,
      failedToday: summary.FailedToday ?? summary.failedToday ?? 0,
      averageResponseMs:
        summary.AverageResponseMs ?? summary.averageResponseMs ?? 0,
      successRate: summary.SuccessRate ?? summary.successRate ?? 0,
    },
    trend: trend.map((item) => ({
      bucketTime: item.BucketTime ?? item.bucketTime ?? "",
      successCount: item.SuccessCount ?? item.successCount ?? 0,
      failureCount: item.FailureCount ?? item.failureCount ?? 0,
    })),
    healthDistribution: healthDistribution.map((item) => ({
      label: item.Label ?? item.label ?? "",
      count: item.Count ?? item.count ?? 0,
      color: item.Color ?? item.color ?? "#94a3b8",
    })),
    alerts: alerts.map((item) => ({
      title: item.Title ?? item.title ?? "",
      description: item.Description ?? item.description ?? "",
      severity: item.Severity ?? item.severity ?? "info",
      createdAt: item.CreatedAt ?? item.createdAt ?? "",
    })),
    recentExecutions: recentExecutions.map((item) => ({
      id: item.Id ?? item.id ?? 0,
      taskName: item.TaskName ?? item.taskName ?? "-",
      taskGroup: item.TaskGroup ?? item.taskGroup ?? "未分组",
      executedAt: item.ExecutedAt ?? item.executedAt ?? "",
      status: item.Status ?? item.status ?? "Pending",
      statusText: item.StatusText ?? item.statusText ?? "等待执行",
      durationMs: item.DurationMs ?? item.durationMs ?? null,
      message: item.Message ?? item.message ?? "-",
    })),
  };
};

const applyOverview = (payload) => {
  overview.value = normalizeOverview(payload);
};

const loadOverview = async ({ silent = false } = {}) => {
  if (!silent) {
    loading.value = true;
  }

  try {
    const snapshot = await getDashboardOverview();
    applyOverview(snapshot);
    errorMessage.value = "";
  } catch (error) {
    errorMessage.value = error.message || "加载监控概览失败";
    showToast(errorMessage.value, "error");
  } finally {
    loading.value = false;
    refreshing.value = false;
  }
};

const handleManualRefresh = async () => {
  if (refreshing.value) {
    return;
  }

  refreshing.value = true;
  await loadOverview({ silent: true });
};

const openExecutionRecords = () => {
  router.push({ name: "ExecutionRecords" });
};

onMounted(async () => {
  await loadOverview();

  dashboardConnection = createDashboardConnection({
    onOverview: applyOverview,
    onReconnected: async () => {
      connectionText.value = "实时连接中";
      await loadOverview({ silent: true });
    },
    onReconnecting: () => {
      connectionText.value = "实时重连中";
    },
    onClosed: () => {
      connectionText.value = "实时连接已断开";
    },
  });

  try {
    await dashboardConnection.start();
  } catch (error) {
    connectionText.value = "实时连接不可用";
    showToast(error.message || "实时连接失败", "error");
  }
});

onBeforeUnmount(async () => {
  await dashboardConnection?.stop?.();
});
</script>
