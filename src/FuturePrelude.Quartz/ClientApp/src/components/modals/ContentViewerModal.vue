<template>
  <div class="modal-overlay" :class="{ open: visible }">
    <div class="modal viewer-modal">
      <div class="modal-header">
        <h3><i class="fas fa-file-alt"></i> {{ title || "内容查看" }}</h3>
        <button class="icon-btn" @click="handleClose">
          <i class="fas fa-times"></i>
        </button>
      </div>
      <div class="modal-body">
        <div class="viewer-toolbar">
          <span class="content-badge" :class="contentTypeClass">
            {{ contentTypeLabel }}
          </span>
          <button
            class="btn-outline-sm"
            :disabled="!formattedContent"
            @click="handleCopy"
          >
            <i class="fas fa-copy"></i> 复制内容
          </button>
        </div>
        <div class="viewer-content-shell">
          <pre class="viewer-content"><code>{{ formattedContent || "暂无内容" }}</code></pre>
        </div>
      </div>
      <div class="modal-footer">
        <button class="btn-outline-sm" @click="handleClose">关闭</button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from "vue";
import { showToast } from "../../utils/toast.js";

const props = defineProps({
  visible: Boolean,
  title: {
    type: String,
    default: "",
  },
  content: {
    type: [String, Number, Boolean, Object, Array],
    default: "",
  },
});

const emit = defineEmits(["close"]);

const normalizeContent = (value) => {
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

const tryParseJson = (value) => {
  if (!value) {
    return null;
  }

  try {
    return JSON.parse(value);
  } catch {
    return null;
  }
};

const rawContent = computed(() => normalizeContent(props.content));
const parsedJson = computed(() => tryParseJson(rawContent.value));
const isJsonContent = computed(() => parsedJson.value !== null);
const formattedContent = computed(() => {
  if (!rawContent.value) {
    return "";
  }

  if (isJsonContent.value) {
    return JSON.stringify(parsedJson.value, null, 2);
  }

  return rawContent.value;
});
const contentTypeLabel = computed(() =>
  isJsonContent.value ? "JSON（已格式化）" : "TEXT",
);
const contentTypeClass = computed(() =>
  isJsonContent.value ? "content-badge-json" : "content-badge-text",
);

const copyText = async (text) => {
  if (navigator.clipboard?.writeText) {
    await navigator.clipboard.writeText(text);
    return;
  }

  const textarea = document.createElement("textarea");
  textarea.value = text;
  textarea.setAttribute("readonly", "readonly");
  textarea.style.position = "fixed";
  textarea.style.top = "-9999px";
  document.body.appendChild(textarea);
  textarea.select();
  document.execCommand("copy");
  document.body.removeChild(textarea);
};

const handleCopy = async () => {
  if (!formattedContent.value) {
    return;
  }

  try {
    await copyText(formattedContent.value);
    showToast("内容已复制");
  } catch (error) {
    showToast(error?.message || "复制内容失败", "error");
  }
};

const handleClose = () => {
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
  z-index: 1100;
  opacity: 0;
  visibility: hidden;
  transition: opacity 0.2s ease, visibility 0.2s ease;
}

.modal-overlay.open {
  opacity: 1;
  visibility: visible;
}

.modal {
  background: white;
  border-radius: 20px;
  width: 960px;
  max-width: 92vw;
  max-height: 88vh;
  overflow: hidden;
  box-shadow: 0 20px 35px rgba(0, 0, 0, 0.2);
  transform: scale(0.95);
  transition: transform 0.2s ease;
  display: flex;
  flex-direction: column;
}

.modal-overlay.open .modal {
  transform: scale(1);
}

.viewer-modal {
  min-height: 520px;
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
  color: #2563eb;
}

.modal-body {
  padding: 20px 24px;
  overflow: hidden;
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.viewer-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.content-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border-radius: 999px;
  padding: 6px 12px;
  font-size: 12px;
  font-weight: 600;
  letter-spacing: 0.04em;
}

.content-badge-json {
  background: #dbeafe;
  color: #1d4ed8;
}

.content-badge-text {
  background: #e2e8f0;
  color: #475569;
}

.viewer-content-shell {
  flex: 1;
  overflow: auto;
  border: 1px solid #e2e8f0;
  border-radius: 16px;
  background: #f8fafc;
}

.viewer-content {
  margin: 0;
  padding: 18px 20px;
  font-size: 13px;
  line-height: 1.65;
  color: #0f172a;
  white-space: pre-wrap;
  word-break: break-word;
  font-family:
    "Cascadia Code", "JetBrains Mono", "Consolas", "SFMono-Regular",
    monospace;
}

.modal-footer {
  padding: 16px 24px;
  border-top: 1px solid #eef2f6;
  display: flex;
  justify-content: flex-end;
  gap: 12px;
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
  padding: 10px 18px;
  border-radius: 30px;
  font-size: 14px;
  cursor: pointer;
  font-weight: 500;
  color: #334155;
}

.btn-outline-sm:hover:not(:disabled) {
  background: #f8fafc;
}

.btn-outline-sm:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}
</style>
