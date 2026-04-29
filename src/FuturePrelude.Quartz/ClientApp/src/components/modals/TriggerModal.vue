<template>
  <div class="modal-overlay" :class="{ open: visible }">
    <div class="modal">
      <div class="modal-header">
        <div>
          <h3><i class="fas fa-clock"></i> {{ title }}</h3>
          <p class="modal-subtitle">当前版本仅支持 CRON 触发器</p>
        </div>
        <button class="icon-btn" @click="handleClose">
          <i class="fas fa-times"></i>
        </button>
      </div>

      <div class="modal-body">
        <div class="modal-content-grid">
          <section class="modal-main-column">
            <div class="panel">
              <div class="panel-title">基础配置</div>

              <div class="form-group">
                <label>触发器名称</label>
                <input
                  v-model="form.TriggerName"
                  placeholder="输入触发器名称"
                />
                <div v-if="errors.TriggerName" class="form-error">
                  {{ errors.TriggerName }}
                </div>
              </div>

              <div class="form-row">
                <div class="form-group">
                  <label>开始时间</label>
                  <input v-model="form.startTimeLocal" type="datetime-local" />
                  <div class="form-hint">留空表示立即生效</div>
                </div>

                <div class="form-group">
                  <label>结束时间</label>
                  <input v-model="form.endTimeLocal" type="datetime-local" />
                  <div v-if="errors.endTimeLocal" class="form-error">
                    {{ errors.endTimeLocal }}
                  </div>
                  <div v-else class="form-hint">留空表示永不结束</div>
                </div>
              </div>

              <div class="form-group">
                <label>触发器类型</label>
                <select v-model="form.TriggerType">
                  <option value="CRON">CRON</option>
                </select>
              </div>
            </div>

            <div class="panel">
              <div class="panel-title">调度规则</div>

              <div class="form-group">
                <label>Cron 表达式</label>
                <input
                  v-model="form.CronExpression"
                  @input="handleCronExpressionInput"
                  placeholder="0 */10 * * * ?"
                />
                <div v-if="errors.CronExpression" class="form-error">
                  {{ errors.CronExpression }}
                </div>
                <div v-else class="form-hint">
                  例如: <code>0 0 12 * * ?</code> 表示每天 12 点执行
                </div>
              </div>

              <div class="form-group">
                <label>Cron 描述</label>
                <input
                  v-model="form.CronDescription"
                  placeholder="例如: 每10分钟执行一次"
                />
              </div>

              <div class="form-group">
                <label>触发器描述</label>
                <textarea
                  v-model="form.Description"
                  rows="4"
                  placeholder="输入触发器用途或备注"
                />
              </div>
            </div>
          </section>

          <aside class="modal-side-column">
            <div class="panel">
              <div class="panel-title">策略配置</div>

              <div class="form-group">
                <label>Misfire 策略</label>
                <select v-model.number="form.MisfireStrategy">
                  <option :value="0">0 - 不处理</option>
                  <option :value="1">1 - DoNothing</option>
                  <option :value="2">2 - FireOnceNow</option>
                </select>
              </div>

              <div class="form-group">
                <label>Priority 优先级</label>
                <input v-model.number="form.Priority" type="number" step="1" />
              </div>
            </div>

            <div class="panel">
              <div class="panel-title">执行预览</div>

              <div class="preview-card">
                <div class="preview-label">首个执行时间</div>
                <div class="preview-value">{{ previewSummaryText }}</div>
                <div class="preview-meta">时区: {{ form.TimeZoneId }}</div>
              </div>

              <div class="preview-section">
                <div class="preview-section-title">未来 5 次执行</div>
                <div
                  v-if="previewState.executions.length === 0"
                  class="preview-empty-state"
                >
                  {{ previewState.message }}
                </div>
                <ol v-else class="preview-run-list">
                  <li
                    v-for="(item, index) in previewState.executions"
                    :key="item.isoUtc"
                    class="preview-run-item"
                  >
                    <span class="preview-run-index">{{ index + 1 }}</span>
                    <span class="preview-run-time">{{ item.display }}</span>
                  </li>
                </ol>
              </div>

              <div class="summary-list">
                <div class="summary-item">
                  <span>生效窗口</span>
                  <strong>{{ effectiveWindowText }}</strong>
                </div>
                <div class="summary-item">
                  <span>当前策略</span>
                  <strong>{{ misfireLabel }}</strong>
                </div>
                <div class="summary-item">
                  <span>优先级</span>
                  <strong>{{ form.Priority || 5 }}</strong>
                </div>
              </div>
            </div>

            <div class="panel panel-soft">
              <div class="panel-title">填写提示</div>
              <ul class="tip-list">
                <li><code>0 */10 * * * ?</code> 每 10 分钟执行一次</li>
                <li><code>0 0 9 * * ?</code> 每天 9 点执行一次</li>
                <li>客户端只校验必填和时间范围，Cron 语法最终由后端校验</li>
              </ul>
            </div>
          </aside>
        </div>
      </div>

      <div class="modal-footer">
        <button class="btn-outline-sm" @click="handleClose">取消</button>
        <button class="btn-primary-sm" @click="handleSave">保存</button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, onBeforeUnmount, ref, watch } from "vue";
import { describeCronExpression } from "../../utils/triggerApi";
import {
  buildTriggerPayload,
  computeUpcomingExecutions,
  createTriggerForm,
  normalizeTriggerForForm,
  PREVIEW_DEBOUNCE_MS,
  validateTriggerForm,
} from "../../utils/triggerModalForm";

const props = defineProps({
  visible: Boolean,
  trigger: Object,
  jobId: [String, Number],
});

const emit = defineEmits(["close", "save"]);

const form = ref(createTriggerForm(props.jobId ?? null));
const errors = ref({});
const previewState = ref(computeUpcomingExecutions(form.value));
let previewTimer = null;
let cronDescribeTimer = null;
let cronDescribeRequestId = 0;

const title = computed(() =>
  props.trigger?.Id || props.trigger?.id ? "编辑触发器" : "新建触发器",
);

const effectiveWindowText = computed(() => {
  const start = form.value.startTimeLocal
    ? form.value.startTimeLocal.replace("T", " ")
    : "立即生效";
  const end = form.value.endTimeLocal
    ? form.value.endTimeLocal.replace("T", " ")
    : "永不结束";

  return `${start} ~ ${end}`;
});

const misfireLabel = computed(() => {
  const optionMap = {
    0: "不处理",
    1: "DoNothing",
    2: "FireOnceNow",
  };

  return optionMap[Number(form.value.MisfireStrategy)] ?? "不处理";
});

const previewSummaryText = computed(
  () => previewState.value.executions[0]?.display ?? previewState.value.message,
);

function clearPreviewTimer() {
  if (previewTimer) {
    clearTimeout(previewTimer);
    previewTimer = null;
  }
}

function clearCronDescribeTimer() {
  if (cronDescribeTimer) {
    clearTimeout(cronDescribeTimer);
    cronDescribeTimer = null;
  }
}

function cancelCronDescribe() {
  cronDescribeRequestId += 1;
  clearCronDescribeTimer();
}

function refreshPreview() {
  clearPreviewTimer();
  previewState.value = computeUpcomingExecutions(form.value);
}

function schedulePreviewRefresh() {
  clearPreviewTimer();

  if (!props.visible) {
    return;
  }

  previewTimer = setTimeout(() => {
    previewState.value = computeUpcomingExecutions(form.value);
    previewTimer = null;
  }, PREVIEW_DEBOUNCE_MS);
}

async function applyCronDescription(cronExpression, requestId) {
  try {
    const description = ((await describeCronExpression(cronExpression)) ?? "").trim();

    if (requestId !== cronDescribeRequestId) {
      return;
    }

    if ((form.value.CronExpression ?? "").trim() !== cronExpression) {
      return;
    }

    form.value.CronDescription = description;
    form.value.Description = description;
  } catch {
    if (requestId !== cronDescribeRequestId) {
      return;
    }
  }
}

function handleCronExpressionInput() {
  cancelCronDescribe();

  if (!props.visible) {
    return;
  }

  const cronExpression = (form.value.CronExpression ?? "").trim();
  if (!cronExpression) {
    form.value.CronDescription = "";
    form.value.Description = "";
    return;
  }

  const requestId = cronDescribeRequestId;
  cronDescribeTimer = setTimeout(() => {
    cronDescribeTimer = null;
    applyCronDescription(cronExpression, requestId);
  }, PREVIEW_DEBOUNCE_MS);
}

watch(
  () => [props.trigger, props.jobId, props.visible],
  () => {
    cancelCronDescribe();
    form.value = normalizeTriggerForForm(props.trigger, props.jobId ?? null);
    errors.value = {};
    refreshPreview();
  },
  { immediate: true },
);

watch(
  () => [
    form.value.CronExpression,
    form.value.startTimeLocal,
    form.value.endTimeLocal,
    form.value.TimeZoneId,
  ],
  () => {
    schedulePreviewRefresh();
  },
);

onBeforeUnmount(() => {
  clearPreviewTimer();
  cancelCronDescribe();
});

const handleClose = () => {
  cancelCronDescribe();
  form.value = normalizeTriggerForForm(null, props.jobId ?? null);
  errors.value = {};
  refreshPreview();
  emit("close");
};

const handleSave = () => {
  errors.value = validateTriggerForm(form.value);

  if (Object.keys(errors.value).length > 0) {
    return;
  }

  emit("save", buildTriggerPayload(form.value));
};
</script>

<style scoped>
.modal-overlay {
  position: fixed;
  inset: 0;
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
  border-radius: 24px;
  width: min(980px, 94vw);
  max-height: 92vh;
  overflow: hidden;
  box-shadow: 0 24px 48px rgba(15, 23, 42, 0.18);
  transform: scale(0.95);
  transition: transform 0.2s ease;
  display: flex;
  flex-direction: column;
}

.modal-overlay.open .modal {
  transform: scale(1);
}

.modal-header {
  padding: 20px 24px;
  border-bottom: 1px solid #eef2f6;
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 16px;
}

.modal-header h3 {
  font-size: 18px;
  font-weight: 600;
  color: #0f172a;
  margin: 0;
}

.modal-header h3 i {
  margin-right: 8px;
  color: #2563eb;
}

.modal-subtitle {
  margin: 6px 0 0;
  font-size: 12px;
  color: #64748b;
}

.modal-body {
  padding: 24px;
  overflow-y: auto;
  flex: 1;
  background:
    radial-gradient(
      circle at top right,
      rgba(59, 130, 246, 0.06),
      transparent 24%
    ),
    #f8fbff;
}

.modal-content-grid {
  display: grid;
  grid-template-columns: minmax(0, 1.3fr) minmax(280px, 0.9fr);
  gap: 20px;
}

.modal-main-column,
.modal-side-column {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.panel {
  background: #ffffff;
  border: 1px solid #e2ebf5;
  border-radius: 18px;
  padding: 18px;
  box-shadow: 0 8px 20px rgba(15, 23, 42, 0.04);
}

.panel-soft {
  background: linear-gradient(180deg, #f8fbff 0%, #f1f6fd 100%);
}

.panel-title {
  font-size: 13px;
  font-weight: 700;
  color: #355070;
  margin-bottom: 16px;
  letter-spacing: 0.02em;
}

.form-row {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 14px;
}

.form-group {
  margin-bottom: 16px;
}

.form-group:last-child {
  margin-bottom: 0;
}

.form-group label {
  font-size: 13px;
  font-weight: 600;
  display: block;
  margin-bottom: 6px;
  color: #334155;
}

.form-group input,
.form-group select,
.form-group textarea {
  width: 100%;
  padding: 10px 12px;
  border: 1px solid #cfdfed;
  border-radius: 12px;
  font-size: 13px;
  font-family: inherit;
  transition: border 0.2s;
  box-sizing: border-box;
  background: #fff;
}

.form-group textarea {
  resize: vertical;
  min-height: 96px;
}

.form-group input:focus,
.form-group select:focus,
.form-group textarea:focus {
  outline: none;
  border-color: #3b82f6;
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}

.form-hint,
.form-error {
  font-size: 11px;
  margin-top: 6px;
}

.form-hint {
  color: #64748b;
}

.form-hint code {
  background: #eff6ff;
  color: #1d4ed8;
  border-radius: 8px;
  padding: 1px 6px;
}

.form-error {
  color: #dc2626;
}

.preview-card {
  border-radius: 16px;
  padding: 14px;
  background: linear-gradient(135deg, #eef6ff 0%, #f8fbff 100%);
  border: 1px solid #dbe7f3;
}

.preview-label {
  font-size: 11px;
  color: #64748b;
  margin-bottom: 8px;
}

.preview-value {
  font-size: 14px;
  font-weight: 600;
  color: #0f172a;
  word-break: break-word;
}

.preview-meta {
  margin-top: 8px;
  font-size: 11px;
  color: #64748b;
}

.preview-section {
  margin-top: 14px;
}

.preview-section-title {
  font-size: 12px;
  font-weight: 600;
  color: #355070;
  margin-bottom: 10px;
}

.preview-empty-state {
  border: 1px dashed #cbd8e6;
  border-radius: 14px;
  padding: 12px 14px;
  font-size: 12px;
  color: #64748b;
  background: #f8fbff;
}

.preview-run-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.preview-run-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 12px;
  border: 1px solid #e2ebf5;
  border-radius: 12px;
  background: #fbfdff;
}

.preview-run-index {
  width: 24px;
  height: 24px;
  border-radius: 999px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-size: 11px;
  font-weight: 700;
  color: #1d4ed8;
  background: #eaf2ff;
  flex-shrink: 0;
}

.preview-run-time {
  font-size: 12px;
  color: #0f172a;
  font-weight: 500;
}

.summary-list {
  margin-top: 14px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.summary-item {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  font-size: 12px;
  color: #475569;
}

.summary-item strong {
  color: #0f172a;
  text-align: right;
}

.tip-list {
  margin: 0;
  padding-left: 18px;
  color: #475569;
  font-size: 12px;
  line-height: 1.7;
}

.tip-list code {
  background: #eaf2ff;
  color: #1d4ed8;
  border-radius: 8px;
  padding: 1px 6px;
}

.modal-footer {
  padding: 16px 24px;
  border-top: 1px solid #eef2f6;
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  background: #fff;
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

.btn-primary-sm {
  background: #1e40af;
  color: white;
  border: none;
  padding: 10px 24px;
  border-radius: 30px;
  font-size: 14px;
  cursor: pointer;
}

.btn-primary-sm:hover {
  background: #1e3a8a;
}

@media (max-width: 860px) {
  .modal {
    width: min(720px, 94vw);
  }

  .modal-body {
    padding: 18px;
  }

  .modal-content-grid,
  .form-row {
    grid-template-columns: 1fr;
  }
}
</style>
