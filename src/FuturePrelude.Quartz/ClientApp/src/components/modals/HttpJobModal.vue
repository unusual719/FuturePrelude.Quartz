<template>
  <div class="modal-overlay" :class="{ open: visible }">
    <div class="modal large" :class="{ 'modal-with-bearer': isBearerAuth }">
      <div class="modal-header">
        <h3><i class="fas fa-globe"></i> {{ title }}</h3>
        <button class="icon-btn" @click="handleClose">
          <i class="fas fa-times"></i>
        </button>
      </div>

      <div class="modal-body">
        <div
          class="modal-content-grid"
          :class="{ 'with-side-panel': isBearerAuth }"
        >
          <section class="modal-main-column">
            <div class="form-group">
              <label>任务名称 <span class="required">*</span></label>
              <input v-model="localJob.jobName" placeholder="输入任务名称" />
            </div>

            <div class="form-row">
              <div class="form-group">
                <label>请求方式</label>
                <select v-model="localJob.httpConfig.requestMethod">
                  <option value="GET">GET</option>
                  <option value="POST">POST</option>
                  <option value="PUT">PUT</option>
                  <option value="DELETE">DELETE</option>
                  <option value="PATCH">PATCH</option>
                </select>
              </div>
              <div class="form-group" style="flex: 2">
                <label>请求地址 <span class="required">*</span></label>
                <input
                  v-model="localJob.httpConfig.requestUrl"
                  placeholder="https://api.example.com/endpoint"
                />
              </div>
            </div>

            <div class="form-group">
              <label
                >Content-Type<span class="required"
                  >（暂仅支持：application/json）</span
                ></label
              >
              <select
                v-model="localJob.httpConfig.contentType"
                :disabled="true"
              >
                <option value="application/json">application/json</option>
                <option value="application/x-www-form-urlencoded">
                  application/x-www-form-urlencoded
                </option>
                <option value="text/plain">text/plain</option>
                <option value="text/xml">text/xml</option>
              </select>
            </div>

            <div class="form-group">
              <label>请求头 (JSON)</label>
              <textarea
                v-model="localJob.httpConfig.requestHeaders"
                rows="2"
                placeholder='{"Content-Type": "application/json"}'
              ></textarea>
            </div>

            <div class="form-group">
              <label>请求参数 (JSON)</label>
              <textarea
                v-model="localJob.httpConfig.requestBody"
                rows="3"
                placeholder='{"key": "value"}'
              ></textarea>
            </div>

            <div class="form-row">
              <div class="form-group">
                <label>超时时间 (秒)</label>
                <input
                  type="number"
                  v-model.number="localJob.httpConfig.timeoutSeconds"
                  min="1"
                  max="86400"
                />
              </div>
              <div class="form-group">
                <label>认证方式</label>
                <select
                  v-model="localJob.httpConfig.authType"
                  @change="handleAuthTypeChange"
                >
                  <option
                    v-for="opt in authTypeOptions"
                    :key="opt.value"
                    :value="opt.value"
                  >
                    {{ opt.label }}
                  </option>
                </select>
              </div>
            </div>

            <div class="form-group">
              <label>任务描述</label>
              <textarea
                v-model="localJob.description"
                rows="2"
                placeholder="可选的任务描述"
              ></textarea>
            </div>

            <div class="advanced-settings">
              <div
                class="advanced-header"
                @click="showAdvanced = !showAdvanced"
              >
                <span><i class="fas fa-cog"></i> 高级设置</span>
                <i
                  :class="
                    showAdvanced ? 'fas fa-chevron-up' : 'fas fa-chevron-down'
                  "
                ></i>
              </div>
              <div class="advanced-body" v-show="showAdvanced">
                <div class="form-group checkbox-group">
                  <label
                    class="checkbox-label"
                    style="display: flex; justify-content: left"
                  >
                    <input
                      type="checkbox"
                      v-model="localJob.disallowConcurrent"
                    />
                    <span>禁止并发执行</span>
                  </label>
                  <p class="form-hint">启用后，同一任务不会同时执行</p>
                </div>

                <div class="form-group checkbox-group">
                  <label
                    class="checkbox-label"
                    style="display: flex; justify-content: left"
                  >
                    <input type="checkbox" v-model="localJob.retryOnFailure" />
                    <span>失败自动重试</span>
                  </label>
                  <p class="form-hint">任务执行失败后自动重试</p>
                </div>

                <div class="form-row" v-if="localJob.retryOnFailure">
                  <div class="form-group">
                    <label>最大重试次数</label>
                    <input
                      type="number"
                      v-model.number="localJob.maxRetry"
                      min="1"
                      max="10"
                    />
                  </div>
                  <div class="form-group">
                    <label>重试退避 (秒)</label>
                    <input
                      type="number"
                      v-model.number="localJob.retryBackoffSeconds"
                      min="1"
                      max="3600"
                    />
                  </div>
                </div>
              </div>
            </div>
          </section>

          <aside v-if="isBearerAuth" class="modal-side-column">
            <div class="side-panel">
              <div class="panel-title">
                Bearer 认证配置（<code>AuthCredentials</code>）
              </div>
              <p class="panel-subtitle"></p>

              <div class="form-group checkbox-group auth-mode-group">
                <label
                  class="checkbox-label"
                  style="display: flex; justify-content: left"
                >
                  <input
                    type="checkbox"
                    v-model="bearerNeedRequestToken"
                    @change="handleNeedRequestTokenChange"
                  />
                  <span style="padding-left: 2px"
                    >是否通过认证接口获取 Token</span
                  >
                </label>
                <p class="form-hint">
                  关闭：使用固定 Token 直接请求；开启：先调用认证接口获取
                  Token，再发起请求。
                </p>
              </div>

              <div class="json-toolbar">
                <button
                  type="button"
                  class="btn-outline-xs"
                  @click="formatBearerAuthCredentials"
                >
                  <i class="fas fa-code"></i> 格式化 JSON
                </button>
                <button
                  type="button"
                  class="btn-outline-xs"
                  @click="resetBearerAuthCredentialsTemplate"
                >
                  <i class="fas fa-undo-alt"></i> 重置模板
                </button>
              </div>

              <div class="form-group bearer-json-group">
                <label>Bearer 认证 JSON</label>
                <textarea
                  v-model="bearerAuthCredentialsText"
                  rows="18"
                  placeholder="请输入 Bearer 认证配置 JSON"
                ></textarea>
                <p class="form-hint">
                  你可以直接手填完整 JSON；保存前会校验格式并自动格式化。
                </p>
              </div>

              <div class="template-preview-card">
                <div class="template-preview-title">当前模板预览</div>
                <pre>{{ currentBearerTemplatePreview }}</pre>
              </div>
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
import { computed, ref, watch } from "vue";
import { showConfirm } from "../../utils/confirm.js";
import {
  createBearerAuthCredentialsTemplate,
  detectNeedRequestToken,
  formatJsonText,
} from "../../utils/httpJobBearerAuth.js";
import { showToast } from "../../utils/toast.js";

const BEARER_AUTH_TYPE = 2;

const props = defineProps({
  visible: Boolean,
  job: Object,
  groupId: [String, Number],
});

const emit = defineEmits(["close", "save"]);

const getFieldValue = (source, ...keys) => {
  for (const key of keys) {
    const value = source?.[key];
    if (value !== undefined && value !== null) {
      return value;
    }
  }

  return undefined;
};

const title = computed(() =>
  getFieldValue(props.job, "id", "Id")
    ? "编辑 HTTP API 任务"
    : "新建 HTTP API 任务",
);

const normalizeBooleanValue = (value, fallback = false) => {
  if (value === undefined || value === null) {
    return fallback;
  }

  if (typeof value === "boolean") {
    return value;
  }

  if (typeof value === "number") {
    return value === 1;
  }

  if (typeof value === "string") {
    return value === "1" || value.toLowerCase() === "true";
  }

  return Boolean(value);
};

const authTypeOptions = [
  { value: 0, label: "无认证" },
  // { value: 1, label: "Basic" },
  { value: 2, label: "Bearer" },
  // { value: 3, label: "APIKey" },
];

const getEmptyJob = () => ({
  jobName: "",
  description: "",
  disallowConcurrent: true,
  retryOnFailure: false,
  maxRetry: 3,
  retryBackoffSeconds: 10,
  httpConfig: {
    requestMethod: "GET",
    contentType: "application/json",
    requestUrl: "",
    requestHeaders: "{}",
    requestBody: "{}",
    timeoutSeconds: 3600,
    authType: 0,
    authCredentials: null,
  },
});

const localJob = ref(getEmptyJob());
const showAdvanced = ref(true);
const bearerNeedRequestToken = ref(false);
const bearerAuthCredentialsText = ref("");
const lastGeneratedBearerAuthCredentials = ref("");

const isBearerAuth = computed(
  () => Number(localJob.value.httpConfig.authType) === BEARER_AUTH_TYPE,
);

const currentBearerTemplatePreview = computed(() =>
  createBearerAuthCredentialsTemplate(bearerNeedRequestToken.value),
);

const convertToForm = (job) => {
  if (!job) {
    return getEmptyJob();
  }

  const httpConfig = getFieldValue(job, "httpConfig", "HttpConfig") || {};

  return {
    id: getFieldValue(job, "id", "Id"),
    groupId: getFieldValue(job, "groupId", "GroupId"),
    jobName: getFieldValue(job, "jobName", "JobName") || "",
    description: getFieldValue(job, "description", "Description") || "",
    disallowConcurrent:
      getFieldValue(job, "disallowConcurrent", "DisallowConcurrent") ?? true,
    retryOnFailure: normalizeBooleanValue(
      getFieldValue(job, "retryOnFailure", "RetryOnFailure"),
    ),
    maxRetry: getFieldValue(job, "maxRetry", "MaxRetry") ?? 3,
    retryBackoffSeconds:
      getFieldValue(job, "retryBackoffSeconds", "RetryBackoffSeconds") ?? 10,
    httpConfig: {
      requestMethod:
        getFieldValue(httpConfig, "requestMethod", "RequestMethod") || "GET",
      contentType:
        getFieldValue(httpConfig, "contentType", "ContentType") ||
        "application/json",
      requestUrl: getFieldValue(httpConfig, "requestUrl", "RequestUrl") || "",
      requestHeaders:
        getFieldValue(httpConfig, "requestHeaders", "RequestHeaders") || "{}",
      requestBody:
        getFieldValue(httpConfig, "requestBody", "RequestBody") || "{}",
      timeoutSeconds:
        getFieldValue(httpConfig, "timeoutSeconds", "TimeoutSeconds") || 3600,
      authType: getFieldValue(httpConfig, "authType", "AuthType") ?? 0,
      authCredentials:
        getFieldValue(httpConfig, "authCredentials", "AuthCredentials") || null,
    },
  };
};

const convertToApi = (form) => {
  const retryOnFailure = Boolean(form.retryOnFailure);
  const maxRetry = retryOnFailure ? Number(form.maxRetry || 3) : null;
  const retryBackoffSeconds = retryOnFailure
    ? Number(form.retryBackoffSeconds || 10)
    : null;
  const groupId = Number(props.groupId ?? form.groupId ?? 0) || 0;

  const base = {
    groupId,
    jobName: form.jobName,
    description: form.description,
    jobType: 1,
    disallowConcurrent: form.disallowConcurrent,
    retryOnFailure,
    maxRetry,
    retryBackoffSeconds,
    httpConfig: {
      requestMethod: form.httpConfig.requestMethod,
      contentType: form.httpConfig.contentType,
      requestUrl: form.httpConfig.requestUrl,
      requestHeaders: form.httpConfig.requestHeaders,
      requestBody: form.httpConfig.requestBody,
      timeoutSeconds: form.httpConfig.timeoutSeconds,
      authType: form.httpConfig.authType,
      authCredentials:
        Number(form.httpConfig.authType) === BEARER_AUTH_TYPE
          ? form.httpConfig.authCredentials
          : null,
    },
  };

  if (form.id) {
    return { ...base, id: form.id };
  }

  return base;
};

const normalizeAuthCredentialsText = (value) => String(value ?? "").trim();

const isEmptyBearerAuthCredentials = (value) => {
  const normalized = normalizeAuthCredentialsText(value);
  return normalized === "" || normalized === "{}" || normalized === "null";
};

const setBearerAuthCredentialsText = (value, isGenerated = false) => {
  bearerAuthCredentialsText.value = value;
  localJob.value.httpConfig.authCredentials = value;
  lastGeneratedBearerAuthCredentials.value = isGenerated
    ? normalizeAuthCredentialsText(value)
    : "";
};

const applyBearerTemplate = (needRequestToken) => {
  const template = createBearerAuthCredentialsTemplate(needRequestToken);
  setBearerAuthCredentialsText(template, true);
  bearerNeedRequestToken.value = Boolean(needRequestToken);
};

const isUsingGeneratedBearerTemplate = () =>
  normalizeAuthCredentialsText(bearerAuthCredentialsText.value) !== "" &&
  normalizeAuthCredentialsText(bearerAuthCredentialsText.value) ===
    lastGeneratedBearerAuthCredentials.value;

const syncBearerStateFromForm = () => {
  const authCredentials = localJob.value.httpConfig.authCredentials;

  if (isEmptyBearerAuthCredentials(authCredentials)) {
    bearerNeedRequestToken.value = false;
    bearerAuthCredentialsText.value = "";
    lastGeneratedBearerAuthCredentials.value = "";
    return;
  }

  try {
    const formatted = formatJsonText(authCredentials);
    bearerNeedRequestToken.value = detectNeedRequestToken(formatted);
    setBearerAuthCredentialsText(formatted, false);
  } catch {
    bearerNeedRequestToken.value = detectNeedRequestToken(authCredentials);
    setBearerAuthCredentialsText(String(authCredentials), false);
  }
};

const ensureBearerTemplateIfNeeded = () => {
  if (!isBearerAuth.value) {
    return;
  }

  if (isEmptyBearerAuthCredentials(bearerAuthCredentialsText.value)) {
    applyBearerTemplate(bearerNeedRequestToken.value);
  }
};

watch(
  () => props.visible,
  (visible) => {
    if (!visible) {
      return;
    }

    localJob.value = convertToForm(props.job);
    syncBearerStateFromForm();
    ensureBearerTemplateIfNeeded();
  },
  { immediate: true },
);

watch(
  () => props.job,
  (newJob) => {
    if (!props.visible) {
      return;
    }

    localJob.value = convertToForm(newJob);
    syncBearerStateFromForm();
    ensureBearerTemplateIfNeeded();
  },
);

const handleAuthTypeChange = () => {
  if (!isBearerAuth.value) {
    return;
  }

  syncBearerStateFromForm();
  ensureBearerTemplateIfNeeded();
};

const handleNeedRequestTokenChange = async () => {
  const nextValue = Boolean(bearerNeedRequestToken.value);

  if (
    isEmptyBearerAuthCredentials(bearerAuthCredentialsText.value) ||
    isUsingGeneratedBearerTemplate()
  ) {
    applyBearerTemplate(nextValue);
    return;
  }

  const confirmed = await showConfirm(
    "切换 Token 获取模式会覆盖当前右侧 JSON，是否继续？",
    "提示",
    "warning",
  );

  if (!confirmed) {
    bearerNeedRequestToken.value = !nextValue;
    return;
  }

  applyBearerTemplate(nextValue);
};

const formatBearerAuthCredentials = () => {
  if (isEmptyBearerAuthCredentials(bearerAuthCredentialsText.value)) {
    applyBearerTemplate(bearerNeedRequestToken.value);
    return;
  }

  try {
    const wasGenerated = isUsingGeneratedBearerTemplate();
    const formatted = formatJsonText(bearerAuthCredentialsText.value);
    bearerNeedRequestToken.value = detectNeedRequestToken(formatted);
    setBearerAuthCredentialsText(formatted, wasGenerated);
    showToast("Bearer 认证 JSON 已格式化");
  } catch {
    showToast("Bearer 认证 JSON 不是合法的 JSON", "error");
  }
};

const resetBearerAuthCredentialsTemplate = async () => {
  if (
    !isEmptyBearerAuthCredentials(bearerAuthCredentialsText.value) &&
    !isUsingGeneratedBearerTemplate()
  ) {
    const confirmed = await showConfirm(
      "重置模板会覆盖当前右侧 JSON，是否继续？",
      "提示",
      "warning",
    );

    if (!confirmed) {
      return;
    }
  }

  applyBearerTemplate(bearerNeedRequestToken.value);
};

const handleClose = () => {
  localJob.value = getEmptyJob();
  bearerNeedRequestToken.value = false;
  bearerAuthCredentialsText.value = "";
  lastGeneratedBearerAuthCredentials.value = "";
  emit("close");
};

const handleSave = () => {
  if (!localJob.value.jobName?.trim()) {
    showToast("请输入任务名称", "error");
    return;
  }

  if (!localJob.value.httpConfig?.requestUrl?.trim()) {
    showToast("请输入请求地址", "error");
    return;
  }

  if (
    localJob.value.retryOnFailure &&
    (!localJob.value.maxRetry || !localJob.value.retryBackoffSeconds)
  ) {
    showToast("请完善重试次数和重试退避时间", "error");
    return;
  }

  if (isBearerAuth.value) {
    if (isEmptyBearerAuthCredentials(bearerAuthCredentialsText.value)) {
      showToast("请填写 Bearer 认证 JSON", "error");
      return;
    }

    try {
      const formatted = formatJsonText(bearerAuthCredentialsText.value);
      bearerNeedRequestToken.value = detectNeedRequestToken(formatted);
      setBearerAuthCredentialsText(formatted, isUsingGeneratedBearerTemplate());
    } catch {
      showToast("Bearer 认证 JSON 格式不正确", "error");
      return;
    }
  }

  const apiData = convertToApi(localJob.value);
  emit("save", apiData);
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
  width: 720px;
}

.modal.modal-with-bearer {
  width: 1240px;
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
  overflow-y: auto;
  flex: 1;
}

.modal-content-grid {
  display: block;
}

.modal-content-grid.with-side-panel {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 460px;
  gap: 20px;
  align-items: start;
}

.modal-main-column,
.modal-side-column {
  min-width: 0;
}

.modal-footer {
  padding: 16px 24px;
  border-top: 1px solid #eef2f6;
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}

.form-group {
  margin-bottom: 18px;
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

.form-group .required {
  color: #ef4444;
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
}

.form-group input:focus,
.form-group select:focus,
.form-group textarea:focus {
  outline: none;
  border-color: #3b82f6;
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}

.form-group textarea {
  resize: vertical;
  min-height: 80px;
}

.form-row {
  display: flex;
  gap: 12px;
}

.form-row .form-group {
  flex: 1;
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

.btn-outline-xs {
  border: 1px solid #cbd5e1;
  background: white;
  color: #334155;
  border-radius: 999px;
  padding: 8px 14px;
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

.btn-outline-xs:hover {
  background: #f8fafc;
}

.advanced-settings {
  margin-top: 8px;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  overflow: hidden;
}

.advanced-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px 16px;
  background: #f8fafc;
  cursor: pointer;
  font-size: 13px;
  font-weight: 600;
  color: #475569;
  user-select: none;
}

.advanced-header:hover {
  background: #f1f5f9;
}

.advanced-header i {
  font-size: 12px;
  color: #64748b;
}

.advanced-body {
  padding: 16px;
  background: #ffffff;
  border-top: 1px solid #e2e8f0;
}

.checkbox-group {
  margin-bottom: 16px;
}

.checkbox-group:last-child {
  margin-bottom: 0;
}

.checkbox-label {
  display: flex;
  align-items: center;
  justify-content: flex-start;
  gap: 12px;
  cursor: pointer;
  font-weight: 500;
  padding: 4px 0;
}

.checkbox-label input[type="checkbox"] {
  width: 16px;
  height: 16px;
  cursor: pointer;
  accent-color: #1e40af;
  margin: 0;
  flex-shrink: 0;
}

.checkbox-label span {
  line-height: 16px;
}

.form-hint {
  font-size: 11px;
  color: #94a3b8;
  margin-top: 4px;
  margin-bottom: 0;
}

.side-panel {
  position: sticky;
  top: 0;
  border: 1px solid #dbe7f3;
  border-radius: 18px;
  background: linear-gradient(
    180deg,
    rgba(248, 251, 255, 0.96) 0%,
    rgba(255, 255, 255, 0.98) 100%
  );
  padding: 18px 18px 16px;
  box-shadow: 0 12px 28px rgba(15, 23, 42, 0.06);
}

.panel-title {
  font-size: 15px;
  font-weight: 700;
  color: #0f172a;
}

.panel-subtitle {
  margin: 8px 0 16px;
  color: #64748b;
  font-size: 12px;
  line-height: 1.6;
}

.auth-mode-group {
  margin-bottom: 14px;
  padding: 14px;
  border-radius: 14px;
  background: #f8fbff;
  border: 1px solid #e2ebf5;
}

.json-toolbar {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
  margin-bottom: 14px;
}

.bearer-json-group textarea {
  min-height: 360px;
  resize: vertical;
  font-family: "Cascadia Code", "Consolas", monospace;
  line-height: 1.6;
}

.template-preview-card {
  margin-top: 16px;
  padding: 14px;
  border-radius: 14px;
  border: 1px solid #e2ebf5;
  background: #f8fbff;
}

.template-preview-title {
  font-size: 12px;
  font-weight: 700;
  color: #334155;
  margin-bottom: 10px;
}

.template-preview-card pre {
  margin: 0;
  font-size: 12px;
  line-height: 1.6;
  white-space: pre-wrap;
  word-break: break-word;
  color: #0f172a;
  font-family: "Cascadia Code", "Consolas", monospace;
}

@media (max-width: 1100px) {
  .modal.modal-with-bearer {
    width: 96vw;
  }

  .modal-content-grid.with-side-panel {
    grid-template-columns: 1fr;
  }

  .side-panel {
    position: static;
  }
}
</style>
