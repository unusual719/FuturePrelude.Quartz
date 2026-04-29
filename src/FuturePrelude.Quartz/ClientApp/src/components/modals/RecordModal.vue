<template>
  <div class="modal-overlay" :class="{ open: visible }">
    <div class="modal large">
      <div class="modal-header">
        <h3><i class="fas fa-history"></i> {{ title }} 执行记录</h3>
        <button class="icon-btn" @click="handleClose">
          <i class="fas fa-times"></i>
        </button>
      </div>
      <div class="modal-body">
        <div v-if="loading" class="record-loading">
          <i class="fas fa-spinner fa-spin"></i>
          <span>执行记录加载中...</span>
        </div>
        <div v-else class="record-table-wrapper">
          <table class="record-table">
            <thead>
              <tr>
                <th>执行时间</th>
                <th style="width: 90px">状态</th>
                <th style="width: 120px">耗时</th>
                <!-- <th>执行摘要</th> -->
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
              <tr v-for="rec in records" :key="rec.id">
                <td class="time-cell">{{ rec.time }}</td>
                <td :class="['status-cell', 'status-' + rec.status]">
                  <i :class="getStatusIconClass(rec.status)"></i>
                  <span>{{ rec.statusText }}</span>
                </td>
                <td>{{ rec.duration }}</td>
                <!-- <td class="detail-cell detail-wide">
                  <button
                    v-if="isViewable(rec.resultRaw)"
                    type="button"
                    class="detail-link"
                    @click="
                      openContentViewer(`执行摘要 - ${rec.time}`, rec.resultRaw)
                    "
                  >
                    {{ rec.result }}
                  </button>
                  <span v-else>{{ rec.result }}</span>
                </td> -->
                <td class="detail-cell">
                  <span>{{
                    rec.reasonCode === "-" ? "SUCCESS" : rec.reasonCode
                  }}</span>
                </td>
                <td class="detail-cell detail-medium">
                  <button
                    v-if="isViewable(rec.reasonMessageRaw)"
                    type="button"
                    class="detail-link"
                    @click="
                      openContentViewer(
                        `结果说明 - ${rec.time}`,
                        rec.reasonMessageRaw,
                      )
                    "
                  >
                    {{ rec.reasonMessage }}
                  </button>
                  <span v-else>{{ rec.reasonMessage }}</span>
                </td>
                <td class="detail-cell detail-wide">
                  <button
                    v-if="isViewable(rec.returnValueRaw)"
                    type="button"
                    class="detail-link"
                    @click="
                      openContentViewer(
                        `返回值 - ${rec.time}`,
                        rec.returnValueRaw,
                      )
                    "
                  >
                    {{ rec.returnValue }}
                  </button>
                  <span v-else>{{ rec.returnValue }}</span>
                </td>
                <td class="detail-cell detail-wide">
                  <button
                    v-if="isViewable(rec.executionContextJsonRaw)"
                    type="button"
                    class="detail-link"
                    @click="
                      openContentViewer(
                        `执行上下文 - ${rec.time}`,
                        rec.executionContextJsonRaw,
                      )
                    "
                  >
                    {{ rec.executionContextJson }}
                  </button>
                  <span v-else>{{ rec.executionContextJson }}</span>
                </td>
                <td class="detail-cell">
                  <button
                    v-if="isViewable(rec.exceptionTypeRaw)"
                    type="button"
                    class="detail-link"
                    @click="
                      openContentViewer(
                        `异常类型 - ${rec.time}`,
                        rec.exceptionTypeRaw,
                      )
                    "
                  >
                    {{ rec.exceptionType }}
                  </button>
                  <span v-else>{{ rec.exceptionType }}</span>
                </td>
                <td class="detail-cell detail-wide">
                  <button
                    v-if="isViewable(rec.exceptionRaw)"
                    type="button"
                    class="detail-link"
                    @click="
                      openContentViewer(
                        `异常消息 - ${rec.time}`,
                        rec.exceptionRaw,
                      )
                    "
                  >
                    {{ rec.exception }}
                  </button>
                  <span v-else>{{ rec.exception }}</span>
                </td>
                <td class="detail-cell detail-stack">
                  <button
                    v-if="isViewable(rec.stackTraceRaw)"
                    type="button"
                    class="detail-link"
                    @click="
                      openContentViewer(
                        `异常堆栈 - ${rec.time}`,
                        rec.stackTraceRaw,
                      )
                    "
                  >
                    {{ rec.stackTrace }}
                  </button>
                  <span v-else>{{ rec.stackTrace }}</span>
                </td>
              </tr>
              <tr v-if="records.length === 0">
                <td colspan="11" class="empty-cell">暂无执行记录</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
      <div class="modal-footer">
        <div v-if="totalCount > 0" class="record-pagination">
          <span class="pagination-info">
            共 {{ totalCount }} 条，第 {{ pageIndex }}/{{ totalPages }} 页
          </span>
          <div class="pagination-controls">
            <button
              class="pagination-btn"
              :disabled="loading || !hasPrevPage"
              @click="emit('prev-page')"
            >
              <i class="fas fa-chevron-left"></i> 上一页
            </button>
            <button
              class="pagination-btn"
              :disabled="loading || !hasNextPage"
              @click="emit('next-page')"
            >
              下一页 <i class="fas fa-chevron-right"></i>
            </button>
          </div>
        </div>
        <button class="btn-outline-sm" @click="handleClose">关闭</button>
      </div>
    </div>
  </div>
  <ContentViewerModal
    :visible="contentViewerVisible"
    :title="contentViewerTitle"
    :content="contentViewerContent"
    @close="closeContentViewer"
  />
</template>

<script setup>
import { ref, watch } from "vue";
import ContentViewerModal from "./ContentViewerModal.vue";

const props = defineProps({
  visible: Boolean,
  records: {
    type: Array,
    default: () => [],
  },
  title: {
    type: String,
    default: "",
  },
  loading: {
    type: Boolean,
    default: false,
  },
  pageIndex: {
    type: Number,
    default: 1,
  },
  totalPages: {
    type: Number,
    default: 1,
  },
  totalCount: {
    type: Number,
    default: 0,
  },
  hasPrevPage: {
    type: Boolean,
    default: false,
  },
  hasNextPage: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits(["close", "prev-page", "next-page"]);
const contentViewerVisible = ref(false);
const contentViewerTitle = ref("");
const contentViewerContent = ref("");

const getStatusIconClass = (status) => {
  if (status === "running") {
    return "fas fa-spinner fa-spin status-icon status-icon-running";
  }

  if (status === "cancelled") {
    return "fas fa-ban status-icon status-icon-cancelled";
  }

  if (status === "success") {
    return "fas fa-check-circle status-icon status-icon-success";
  }

  return "fas fa-times-circle status-icon status-icon-failed";
};

const isViewable = (value) => {
  if (value === null || value === undefined) {
    return false;
  }

  const text = String(value).trim();
  return text !== "" && text !== "-";
};

const openContentViewer = (title, content) => {
  if (!isViewable(content)) {
    return;
  }

  contentViewerTitle.value = title;
  contentViewerContent.value = content;
  contentViewerVisible.value = true;
};

const closeContentViewer = () => {
  contentViewerVisible.value = false;
  contentViewerTitle.value = "";
  contentViewerContent.value = "";
};

watch(
  () => props.visible,
  (visible) => {
    if (!visible) {
      closeContentViewer();
    }
  },
);

const handleClose = () => {
  closeContentViewer();
  emit("close");
};
</script>

<style scoped>
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  backdrop-filter: blur(2px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  opacity: 0;
  visibility: hidden;
  transition:
    opacity 0.2s ease,
    visibility 0.2s ease;
}

.modal-overlay.open {
  opacity: 1;
  visibility: visible;
}

.modal {
  background: white;
  border-radius: 20px;
  width: 600px;
  max-width: 90vw;
  max-height: 90vh;
  overflow: hidden;
  box-shadow: 0 20px 35px rgba(0, 0, 0, 0.2);
  transform: scale(0.95);
  transition: transform 0.2s ease;
  display: flex;
  flex-direction: column;
}

.modal.large {
  width: 1480px;
  max-width: 96vw;
}

.modal-overlay.open .modal {
  transform: scale(1);
}

.modal-header {
  padding: 18px 24px;
  border-bottom: 1px solid #eef2f6;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.modal-header h3 {
  font-size: 16px;
  font-weight: 600;
  color: #0f172a;
  margin: 0;
}

.modal-header h3 i {
  margin-right: 8px;
  color: #6366f1;
}

.modal-body {
  padding: 20px 24px;
  overflow: auto;
  flex: 1;
}

.modal-footer {
  padding: 16px 24px;
  border-top: 1px solid #eef2f6;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.record-loading {
  min-height: 220px;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 12px;
  color: #5b6e8c;
  font-size: 14px;
}

.record-loading i {
  color: #2563eb;
}

.record-table {
  width: 100%;
  min-width: 1780px;
  font-size: 13px;
  border-collapse: collapse;
}

.record-table th,
.record-table td {
  padding: 12px 10px;
  text-align: left;
  border-bottom: 1px solid #f0f2f5;
  vertical-align: middle;
}

.record-table th {
  background: #f8fafc;
  font-weight: 600;
  white-space: nowrap;
  position: sticky;
  top: 0;
  z-index: 1;
}

.record-table-wrapper {
  overflow-x: auto;
}

.time-cell {
  white-space: nowrap;
}

.status-cell {
  white-space: nowrap;
}

.status-icon {
  width: 16px;
  margin-right: 5px;
  text-align: center;
}

.status-icon-running {
  color: #d97706;
  display: inline-block;
  animation: status-spin 1s linear infinite;
}

.status-icon-cancelled {
  color: #94a3b8;
}

.status-icon-success {
  color: #059669;
}

.status-icon-failed {
  color: #dc2626;
}

.detail-cell {
  max-width: 180px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.detail-link {
  width: 100%;
  border: none;
  padding: 0;
  background: transparent;
  color: #2563eb;
  cursor: pointer;
  font: inherit;
  text-align: left;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.detail-link:hover {
  color: #1d4ed8;
  text-decoration: underline;
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

.empty-cell {
  text-align: center;
  padding: 40px !important;
  color: #94a3b8;
}

.status-success {
  color: #059669;
}
.status-failed {
  color: #dc2626;
}
.status-running {
  color: #059669;
}

.status-cancelled {
  color: #64748b;
}

.record-pagination {
  display: flex;
  align-items: center;
  gap: 16px;
  color: #5b6e8c;
  font-size: 13px;
}

.pagination-controls {
  display: flex;
  gap: 8px;
}

.pagination-btn {
  border: 1px solid #cbd5e1;
  background: white;
  color: #334155;
  border-radius: 999px;
  padding: 8px 14px;
  font-size: 13px;
  cursor: pointer;
}

.pagination-btn:hover:not(:disabled) {
  background: #f8fafc;
}

.pagination-btn:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}

.icon-btn {
  background: transparent;
  border: none;
  padding: 6px;
  border-radius: 8px;
  cursor: pointer;
  color: #5b6e8c;
  font-size: 13px;
}

.icon-btn:hover {
  background: #eef2ff;
  color: #2563eb;
}

.btn-outline-sm {
  border: 1px solid #cbd5e1;
  background: white;
  padding: 10px 24px;
  border-radius: 30px;
  font-size: 14px;
  cursor: pointer;
  font-weight: 500;
  color: #334155;
}

.btn-outline-sm:hover {
  background: #f8fafc;
}

@keyframes status-spin {
  from {
    transform: rotate(0deg);
  }
  to {
    transform: rotate(360deg);
  }
}
</style>
