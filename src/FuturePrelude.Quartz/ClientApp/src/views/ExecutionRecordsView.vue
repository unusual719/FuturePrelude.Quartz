<template>
  <div class="app-wrapper">
    <PlatformTopNav />

    <main class="records-page">
      <section class="records-header">
        <div>
          <h1><i class="fas fa-history"></i> 执行记录</h1>
          <p>
            查看所有任务的历史执行详情，支持按任务名称、执行状态、时间范围筛选。
          </p>
        </div>
      </section>

      <section class="filter-bar">
        <div class="filter-group">
          <label>任务名称</label>
          <input
            v-model="filters.taskName"
            type="text"
            placeholder="输入任务名称"
            @keyup.enter="searchRecords"
          />
        </div>
        <div class="filter-group">
          <label>执行状态</label>
          <select v-model="filters.result">
            <option value="">全部</option>
            <option
              v-for="option in statusOptions"
              :key="option.value"
              :value="option.value"
            >
              {{ option.label }}
            </option>
          </select>
        </div>
        <div class="filter-group filter-group-wide">
          <label>时间范围</label>
          <div class="date-range">
            <input v-model="filters.startDate" type="date" />
            <span>至</span>
            <input v-model="filters.endDate" type="date" />
          </div>
        </div>
        <div class="filter-actions">
          <button class="btn-primary-sm" @click="searchRecords">
            <i class="fas fa-search"></i> 查询
          </button>
          <button class="btn-outline-sm" @click="resetFilters">
            <i class="fas fa-undo-alt"></i> 重置
          </button>
        </div>
      </section>

      <section class="records-panel">
        <div v-if="loading" class="loading-state">
          <i class="fas fa-spinner fa-spin"></i> 执行记录加载中...
        </div>
        <template v-else>
          <div class="records-table-shell">
            <table class="records-table">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>任务信息</th>
                  <th>执行来源</th>
                  <th>执行时间</th>
                  <th>状态</th>
                  <th>耗时</th>
                  <th>执行摘要</th>
                  <th>结果码</th>
                  <th>结果说明</th>
                  <th>返回值</th>
                  <th>执行上下文</th>
                  <th>异常类型</th>
                  <th>异常消息</th>
                  <th>异常堆栈</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="record in records" :key="record.id">
                  <td class="id-cell">#{{ record.id }}</td>
                  <td class="task-info-cell">
                    <strong>{{ record.taskName }}</strong>
                    <span>{{ record.triggerName }}</span>
                  </td>
                  <td class="source-cell">
                    <strong>{{ record.executionSource }}</strong>
                    <span v-if="record.attemptLabel !== '-'">
                      {{ record.attemptLabel }}
                    </span>
                  </td>
                  <td class="time-cell">{{ record.execTime }}</td>
                  <td style="width: 120px">
                    <span
                      class="record-status-badge"
                      :class="'status-' + record.status"
                    >
                      <i :class="getStatusIconClass(record.status)"></i>
                      {{ record.statusText }}
                    </span>
                  </td>
                  <td>{{ record.duration }}</td>
                  <td class="detail-cell detail-wide">
                    <button
                      v-if="isViewable(record.summaryRaw)"
                      type="button"
                      class="detail-link"
                      @click="
                        openContentViewer(
                          `执行摘要 - ${record.execTime}`,
                          record.summaryRaw,
                        )
                      "
                    >
                      {{ record.summary }}
                    </button>
                    <span v-else>{{ record.summary }}</span>
                  </td>
                  <td class="detail-cell">
                    <span>
                      {{
                        record.status === "success"
                          ? "SUCCESS"
                          : record.reasonCode
                      }}
                    </span>
                  </td>
                  <td class="detail-cell detail-medium">
                    <button
                      v-if="isViewable(record.reasonMessageRaw)"
                      type="button"
                      class="detail-link"
                      @click="
                        openContentViewer(
                          `结果说明 - ${record.execTime}`,
                          record.reasonMessageRaw,
                        )
                      "
                    >
                      {{ record.reasonMessage }}
                    </button>
                    <span v-else>{{ record.reasonMessage }}</span>
                  </td>
                  <td class="detail-cell detail-wide">
                    <button
                      v-if="isViewable(record.returnValueRaw)"
                      type="button"
                      class="detail-link"
                      @click="
                        openContentViewer(
                          `返回值 - ${record.execTime}`,
                          record.returnValueRaw,
                        )
                      "
                    >
                      {{ record.returnValue }}
                    </button>
                    <span v-else>{{ record.returnValue }}</span>
                  </td>
                  <td class="detail-cell detail-wide">
                    <button
                      v-if="isViewable(record.executionContextJsonRaw)"
                      type="button"
                      class="detail-link"
                      @click="
                        openContentViewer(
                          `执行上下文 - ${record.execTime}`,
                          record.executionContextJsonRaw,
                        )
                      "
                    >
                      {{ record.executionContextJson }}
                    </button>
                    <span v-else>{{ record.executionContextJson }}</span>
                  </td>
                  <td class="detail-cell">
                    <button
                      v-if="isViewable(record.exceptionTypeRaw)"
                      type="button"
                      class="detail-link"
                      @click="
                        openContentViewer(
                          `异常类型 - ${record.execTime}`,
                          record.exceptionTypeRaw,
                        )
                      "
                    >
                      {{ record.exceptionType }}
                    </button>
                    <span v-else>{{ record.exceptionType }}</span>
                  </td>
                  <td class="detail-cell detail-wide">
                    <button
                      v-if="isViewable(record.exceptionRaw)"
                      type="button"
                      class="detail-link"
                      @click="
                        openContentViewer(
                          `异常消息 - ${record.execTime}`,
                          record.exceptionRaw,
                        )
                      "
                    >
                      {{ record.exception }}
                    </button>
                    <span v-else>{{ record.exception }}</span>
                  </td>
                  <td class="detail-cell detail-stack">
                    <button
                      v-if="isViewable(record.stackTraceRaw)"
                      type="button"
                      class="detail-link"
                      @click="
                        openContentViewer(
                          `异常堆栈 - ${record.execTime}`,
                          record.stackTraceRaw,
                        )
                      "
                    >
                      {{ record.stackTrace }}
                    </button>
                    <span v-else>{{ record.stackTrace }}</span>
                  </td>
                </tr>
                <tr v-if="records.length === 0">
                  <td colspan="14" class="empty-cell">暂无执行记录</td>
                </tr>
              </tbody>
            </table>
          </div>

          <div class="pagination" v-if="pageState.totalCount > 0">
            <span class="pagination-info">
              共 {{ pageState.totalCount }} 条，第 {{ pageState.pageIndex }}/{{
                pageState.totalPages || 1
              }}
              页
            </span>
            <div class="pagination-controls">
              <button
                class="pagination-btn"
                :disabled="pageState.pageIndex <= 1"
                @click="changePage(pageState.pageIndex - 1)"
              >
                <i class="fas fa-chevron-left"></i> 上一页
              </button>
              <button
                class="pagination-btn"
                :disabled="pageState.pageIndex >= pageState.totalPages"
                @click="changePage(pageState.pageIndex + 1)"
              >
                下一页 <i class="fas fa-chevron-right"></i>
              </button>
            </div>
          </div>
        </template>
      </section>
    </main>

    <ContentViewerModal
      :visible="contentViewer.visible"
      :title="contentViewer.title"
      :content="contentViewer.content"
      @close="closeContentViewer"
    />
  </div>
</template>

<script setup>
import { onBeforeUnmount, onMounted, ref } from "vue";
import PlatformTopNav from "../components/PlatformTopNav.vue";
import ContentViewerModal from "../components/modals/ContentViewerModal.vue";
import { getJobExecutionLogPageList } from "../utils/jobExecutionLogApi.js";
import {
  buildLocalDateBoundaryUtcIso,
  createLatestRequestGuard,
} from "../utils/executionRecords.js";
import { formatDateTimeToSeconds } from "../utils/dateTime.js";
import { showToast } from "../utils/toast.js";
import "../styles/tasklist.css";

const PAGE_SIZE = 12;

const statusOptions = [
  { label: "运行中", value: "Running" },
  { label: "等待执行", value: "Pending" },
  { label: "成功", value: "Success" },
  { label: "失败", value: "Failed" },
  { label: "取消", value: "Cancelled" },
  { label: "超时", value: "Timeout" },
  { label: "重试中", value: "Retrying" },
];

const filters = ref({
  taskName: "",
  result: "",
  startDate: "",
  endDate: "",
});

const loading = ref(false);
const records = ref([]);
const pageState = ref({
  totalCount: 0,
  pageIndex: 1,
  pageSize: PAGE_SIZE,
  totalPages: 0,
});
const contentViewer = ref({
  visible: false,
  title: "",
  content: "",
});
const recordRequestGuard = createLatestRequestGuard();

const formatNullableText = (value) => {
  if (value === null || value === undefined || value === "") {
    return "-";
  }

  return String(value);
};

const normalizeViewerContent = (value) => {
  if (value === null || value === undefined) {
    return "";
  }

  if (typeof value === "string") {
    return value.trim() ? value : "";
  }

  if (
    typeof value === "number" ||
    typeof value === "boolean" ||
    Array.isArray(value) ||
    typeof value === "object"
  ) {
    try {
      return JSON.stringify(value);
    } catch {
      return String(value);
    }
  }

  return String(value);
};

const formatDurationText = (durationMs) => {
  if (durationMs === null || durationMs === undefined || durationMs === "") {
    return "-";
  }

  const value = Number(durationMs);
  if (!Number.isFinite(value)) {
    return "-";
  }

  if (value < 1000) {
    return `${Math.round(value)} ms`;
  }

  return `${(value / 1000).toFixed(2)} s`;
};

const normalizeExecutionRecordStatus = (resultText, isRunning) => {
  const text = formatNullableText(resultText);

  if (isRunning || text.includes("运行中") || text.includes("运行中")) {
    return { key: "running", text: "运行中" };
  }

  if (text.includes("等待") || text.includes("重试")) {
    return { key: "running", text };
  }

  if (text.includes("取消") || text.toLowerCase().includes("cancel")) {
    return { key: "cancelled", text };
  }

  if (text.includes("成功")) {
    return { key: "success", text };
  }

  if (text.includes("失败") || text.includes("超时")) {
    return { key: "failed", text };
  }

  return { key: "running", text };
};

const resolveExecutionSourceText = (value) => {
  const source = formatNullableText(value);

  const sourceMap = {
    Scheduler: "调度触发",
    Retry: "失败重试",
    Recovery: "恢复补偿",
    Manual: "手动触发",
  };

  return sourceMap[source] || source;
};

const isViewable = (value) => {
  if (value === null || value === undefined) {
    return false;
  }

  const text = String(value).trim();
  return text !== "" && text !== "-";
};

const getStatusIconClass = (status) => {
  if (status === "running") {
    return "fas fa-spinner job-runtime-icon job-runtime-icon-spinning";
  }

  if (status === "cancelled") {
    return "fas fa-ban";
  }

  if (status === "success") {
    return "fas fa-check-circle";
  }

  return "fas fa-times-circle";
};

const openContentViewer = (title, content) => {
  if (!isViewable(content)) {
    return;
  }

  contentViewer.value = {
    visible: true,
    title,
    content,
  };
};

const closeContentViewer = () => {
  contentViewer.value = {
    visible: false,
    title: "",
    content: "",
  };
};

const mapExecutionRecordItem = (item) => {
  const isRunning = Boolean(item?.IsRunning ?? item?.isRunning);
  const resultText =
    item?.ResultText ?? item?.resultText ?? item?.Result ?? item?.result ?? "-";
  const status = normalizeExecutionRecordStatus(resultText, isRunning);
  const executionSummaryRaw =
    item?.ExecutionSummary ??
    item?.executionSummary ??
    item?.DisplayMessage ??
    item?.displayMessage ??
    item?.ReturnValue ??
    item?.returnValue ??
    item?.Exception ??
    item?.exception ??
    item?.ReasonMessage ??
    item?.reasonMessage;
  const reasonCodeRaw = item?.ReasonCode ?? item?.reasonCode;
  const reasonMessageRaw = item?.ReasonMessage ?? item?.reasonMessage;
  const returnValueRaw = item?.ReturnValue ?? item?.returnValue;
  const executionContextJsonRaw =
    item?.ExecutionContextJson ?? item?.executionContextJson;
  const exceptionTypeRaw = item?.ExceptionType ?? item?.exceptionType;
  const exceptionRaw = item?.Exception ?? item?.exception;
  const stackTraceRaw = item?.StackTrace ?? item?.stackTrace;
  const attemptNo = Number(item?.AttemptNo ?? item?.attemptNo ?? 1);

  return {
    id: item?.Id ?? item?.id ?? "-",
    taskName: formatNullableText(
      item?.JobNameSnapshot ?? item?.jobNameSnapshot,
    ),
    triggerName: formatNullableText(
      item?.TriggerNameSnapshot ?? item?.triggerNameSnapshot,
    ),
    executionSource: resolveExecutionSourceText(
      item?.ExecutionSource ?? item?.executionSource,
    ),
    attemptLabel: attemptNo > 1 ? `第 ${attemptNo} 次尝试` : "-",
    execTime: formatDateTimeToSeconds(
      item?.DisplayTimeUtc ??
        item?.displayTimeUtc ??
        item?.StartTimeUtc ??
        item?.startTimeUtc ??
        item?.CreateTime ??
        item?.createTime,
    ),
    status: status.key,
    statusText: status.text,
    duration: formatDurationText(item?.DurationMs ?? item?.durationMs),
    summary: formatNullableText(executionSummaryRaw),
    summaryRaw: normalizeViewerContent(executionSummaryRaw),
    reasonCode: formatNullableText(reasonCodeRaw),
    reasonCodeRaw: normalizeViewerContent(reasonCodeRaw),
    reasonMessage: formatNullableText(reasonMessageRaw),
    reasonMessageRaw: normalizeViewerContent(reasonMessageRaw),
    returnValue: formatNullableText(returnValueRaw),
    returnValueRaw: normalizeViewerContent(returnValueRaw),
    executionContextJson: formatNullableText(executionContextJsonRaw),
    executionContextJsonRaw: normalizeViewerContent(executionContextJsonRaw),
    exceptionType: formatNullableText(exceptionTypeRaw),
    exceptionTypeRaw: normalizeViewerContent(exceptionTypeRaw),
    exception: formatNullableText(exceptionRaw),
    exceptionRaw: normalizeViewerContent(exceptionRaw),
    stackTrace: formatNullableText(stackTraceRaw),
    stackTraceRaw: normalizeViewerContent(stackTraceRaw),
  };
};

const applyPageResult = (result) => {
  const items = Array.isArray(result?.Items)
    ? result.Items
    : Array.isArray(result?.items)
      ? result.items
      : [];
  const pageIndex = Number(result?.PageIndex ?? result?.pageIndex ?? 1) || 1;
  const pageSize =
    Number(result?.PageSize ?? result?.pageSize ?? PAGE_SIZE) || PAGE_SIZE;
  const totalCount =
    Number(result?.TotalCount ?? result?.totalCount ?? items.length) || 0;
  const totalPages =
    Number(result?.TotalPages ?? result?.totalPages ?? 0) ||
    (pageSize > 0 ? Math.ceil(totalCount / pageSize) : 0);

  records.value = items.map(mapExecutionRecordItem);
  pageState.value = {
    totalCount,
    pageIndex,
    pageSize,
    totalPages,
  };
};

const loadRecords = async (pageIndex = 1) => {
  const requestVersion = recordRequestGuard.issue();

  try {
    loading.value = true;
    const result = await getJobExecutionLogPageList({
      taskName: filters.value.taskName.trim() || undefined,
      result: filters.value.result || undefined,
      startTime: buildLocalDateBoundaryUtcIso(filters.value.startDate),
      endTime: buildLocalDateBoundaryUtcIso(filters.value.endDate, true),
      pageIndex,
      pageSize: pageState.value.pageSize,
    });

    if (!recordRequestGuard.isLatest(requestVersion)) {
      return;
    }

    applyPageResult(result);
  } catch (error) {
    if (!recordRequestGuard.isLatest(requestVersion)) {
      return;
    }

    showToast(error.message || "加载执行记录失败", "error");
    records.value = [];
    pageState.value = {
      totalCount: 0,
      pageIndex: 1,
      pageSize: PAGE_SIZE,
      totalPages: 0,
    };
  } finally {
    if (recordRequestGuard.isLatest(requestVersion)) {
      loading.value = false;
    }
  }
};

const searchRecords = async () => {
  if (
    filters.value.startDate &&
    filters.value.endDate &&
    filters.value.startDate > filters.value.endDate
  ) {
    showToast("开始时间不能晚于结束时间", "error");
    return;
  }

  await loadRecords(1);
};

const resetFilters = async () => {
  filters.value = {
    taskName: "",
    result: "",
    startDate: "",
    endDate: "",
  };

  await loadRecords(1);
};

const changePage = async (pageIndex) => {
  if (
    loading.value ||
    pageIndex < 1 ||
    (pageState.value.totalPages > 0 && pageIndex > pageState.value.totalPages)
  ) {
    return;
  }

  await loadRecords(pageIndex);
};

onMounted(async () => {
  await loadRecords(1);
});

onBeforeUnmount(() => {
  recordRequestGuard.invalidate();
});
</script>

<style scoped>
.records-page {
  flex: 1;
  overflow-y: auto;
  padding: 28px 32px;
  background:
    radial-gradient(
      circle at top left,
      rgba(59, 130, 246, 0.08),
      transparent 28%
    ),
    linear-gradient(180deg, #f8fbff 0%, #f1f5f9 100%);
}

.records-header {
  margin-bottom: 24px;
}

.records-header h1 {
  font-size: 28px;
  font-weight: 700;
  color: #0f172a;
  display: flex;
  align-items: center;
  gap: 12px;
}

.records-header h1 i {
  color: #2563eb;
}

.records-header p {
  margin-top: 8px;
  font-size: 14px;
  color: #64748b;
}

.filter-bar {
  background: rgba(255, 255, 255, 0.92);
  border: 1px solid rgba(226, 232, 240, 0.8);
  border-radius: 22px;
  padding: 18px 20px;
  display: flex;
  flex-wrap: wrap;
  gap: 16px;
  align-items: flex-end;
  margin-bottom: 22px;
  box-shadow: 0 12px 28px rgba(15, 23, 42, 0.04);
}

.filter-group {
  display: flex;
  flex-direction: column;
  gap: 6px;
  min-width: 180px;
}

.filter-group-wide {
  min-width: 360px;
}

.filter-group label {
  font-size: 12px;
  font-weight: 600;
  color: #64748b;
}

.filter-group input,
.filter-group select {
  width: 100%;
  padding: 10px 12px;
  border: 1px solid #d9e2ec;
  border-radius: 14px;
  font-size: 13px;
  background: white;
  color: #0f172a;
}

.filter-group input:focus,
.filter-group select:focus {
  outline: none;
  border-color: #3b82f6;
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}

.date-range {
  display: flex;
  align-items: center;
  gap: 10px;
}

.date-range span {
  color: #64748b;
  font-size: 13px;
}

.filter-actions {
  display: flex;
  gap: 10px;
  margin-left: auto;
}

.records-panel {
  background: rgba(255, 255, 255, 0.94);
  border-radius: 28px;
  border: 1px solid rgba(226, 232, 240, 0.8);
  padding: 18px 20px 22px;
  box-shadow: 0 18px 35px rgba(15, 23, 42, 0.05);
}

.records-table-shell {
  overflow: auto;
  border-radius: 18px;
}

.records-table {
  width: 100%;
  min-width: 1900px;
  border-collapse: collapse;
  font-size: 13px;
}

.records-table th,
.records-table td {
  padding: 14px 14px;
  text-align: left;
  border-bottom: 1px solid #edf2f7;
  vertical-align: middle;
}

.records-table th {
  background: #f8fbff;
  color: #64748b;
  font-size: 12px;
  font-weight: 600;
  white-space: nowrap;
  position: sticky;
  top: 0;
  z-index: 1;
}

.records-table tbody tr:hover {
  background: #fbfdff;
}

.id-cell {
  font-family: "Cascadia Code", "Consolas", monospace;
  color: #475569;
  white-space: nowrap;
}

.task-info-cell {
  width: 200px;
}

.task-info-cell strong,
.source-cell strong {
  display: block;
  color: #0f172a;
  font-weight: 600;
  max-width: 350px;
  min-width: 250px;
}

.task-info-cell span,
.source-cell span {
  display: block;
  margin-top: 4px;
  font-size: 12px;
  color: #94a3b8;
}

.record-status-badge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  min-width: 74px;
  justify-content: center;
  padding: 5px 12px;
  border-radius: 999px;
  font-size: 12px;
  font-weight: 600;
}

.record-status-badge.status-running {
  background: #ecfdf5;
  color: #059669;
}

.record-status-badge.status-success {
  background: #ecfdf5;
  color: #059669;
}

.record-status-badge.status-failed {
  background: #fef2f2;
  color: #dc2626;
}

.record-status-badge.status-cancelled {
  background: #f1f5f9;
  color: #64748b;
}

.detail-cell {
  max-width: 180px;
}

.detail-medium {
  max-width: 220px;
}

.detail-wide {
  max-width: 280px;
}

.detail-stack {
  max-width: 360px;
}

.detail-link,
.detail-cell span {
  display: inline-block;
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.detail-link {
  border: none;
  padding: 0;
  background: transparent;
  color: #2563eb;
  cursor: pointer;
  font: inherit;
  text-align: left;
}

.detail-link:hover {
  color: #1d4ed8;
  text-decoration: underline;
}

.empty-cell {
  text-align: center;
  padding: 70px 20px !important;
  color: #94a3b8;
}

@media (max-width: 1280px) {
  .records-page {
    padding: 20px;
  }

  .filter-actions {
    width: 100%;
    margin-left: 0;
  }
}
</style>
