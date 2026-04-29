<template>
  <div class="modal-overlay" :class="{ open: visible }">
    <div class="modal large">
      <div class="modal-header">
        <h3><i class="fas fa-puzzle-piece"></i> {{ title }}</h3>
        <button class="icon-btn" @click="handleClose">
          <i class="fas fa-times"></i>
        </button>
      </div>
      <div class="modal-body">
        <div class="form-group">
          <label>任务名称 <span class="required">*</span></label>
          <input v-model="localJob.name" placeholder="输入任务名称" />
        </div>
        <div class="form-group">
          <label>备注</label>
          <textarea
            v-model="localJob.remark"
            rows="2"
            placeholder="可选"
          ></textarea>
        </div>
        <div class="form-group">
          <label>程序集路径</label>
          <input
            v-model="localJob.assemblyPath"
            placeholder="/plugins/MyPlugin.dll"
          />
        </div>
        <div class="form-group">
          <label>类型全名</label>
          <input
            v-model="localJob.typeFullName"
            placeholder="Namespace.ClassName, AssemblyName"
          />
        </div>
        <div class="form-group">
          <label>方法名</label>
          <input v-model="localJob.methodName" placeholder="Execute" />
        </div>
        <div class="form-group">
          <label>插件参数 (JSON)</label>
          <textarea
            v-model="localJob.pluginParams"
            rows="4"
            placeholder='{"input": "value"}'
          ></textarea>
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
import { ref, computed, watch } from "vue";

const props = defineProps({
  visible: Boolean,
  job: Object,
});

const emit = defineEmits(["close", "save"]);

const title = computed(() => (props.job?.id ? "编辑插件任务" : "新建插件任务"));

const getEmptyJob = () => ({
  name: "",
  remark: "",
  assemblyPath: "",
  typeFullName: "",
  methodName: "",
  pluginParams: "{}",
  status: "stopped",
});

const localJob = ref(getEmptyJob());

watch(
  () => props.job,
  (newJob) => {
    if (newJob) {
      localJob.value = { ...newJob };
    } else {
      localJob.value = getEmptyJob();
    }
  },
  { immediate: true },
);

const handleClose = () => {
  localJob.value = getEmptyJob();
  emit("close");
};

const handleSave = () => {
  emit("save", { ...localJob.value });
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
</style>
