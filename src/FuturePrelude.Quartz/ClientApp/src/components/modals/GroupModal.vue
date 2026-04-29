<template>
  <div class="modal-overlay" :class="{ open: visible }">
    <div class="modal">
      <div class="modal-header">
        <h3><i class="fas fa-folder-tree"></i> {{ title }}</h3>
        <button class="icon-btn" @click="handleClose">
          <i class="fas fa-times"></i>
        </button>
      </div>
      <div class="modal-body">
        <div class="form-group">
          <label>分组名称 <span class="required">*</span></label>
          <input v-model="localGroup.Name" placeholder="输入分组名称" />
        </div>
        <div class="form-group">
          <label>选择图标</label>
          <div class="icon-selector">
            <div
              v-for="icon in availableIcons"
              :key="icon"
              class="icon-option"
              :class="{ selected: localGroup.Icon === icon }"
              @click="localGroup.Icon = icon"
            >
              <i :class="icon"></i>
            </div>
          </div>
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
  group: Object,
});

const emit = defineEmits(["close", "save"]);

const availableIcons = [
  "fas fa-shopping-cart",
  "fas fa-database",
  "fas fa-chart-line",
  "fas fa-cloud",
  "fas fa-cogs",
  "fas fa-envelope",
  "fas fa-file-alt",
  "fas fa-flag",
  "fas fa-folder",
  "fas fa-gem",
  "fas fa-globe",
  "fas fa-link",
  "fas fa-lock",
  "fas fa-paper-plane",
  "fas fa-rocket",
  "fas fa-server",
  "fas fa-shield-alt",
  "fas fa-star",
  "fas fa-tag",
  "fas fa-trophy",
  "fas fa-truck",
  "fas fa-user",
];

const title = computed(() => (props.group?.Id ? "编辑分组" : "新建分组"));

const localGroup = ref({ Id: null, Name: "", Icon: "fas fa-shopping-cart" });

watch(
  () => props.group,
  (newGroup) => {
    if (newGroup) {
      localGroup.value = { ...newGroup };
    } else {
      localGroup.value = { Id: null, Name: "", Icon: "fas fa-shopping-cart" };
    }
  },
  { immediate: true },
);

const handleClose = () => {
  localGroup.value = { Id: null, Name: "", Icon: "fas fa-shopping-cart" };
  emit("close");
};

const handleSave = () => {
  emit("save", { ...localGroup.value });
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
  width: 480px;
  max-width: 90vw;
  max-height: 90vh;
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

.modal-header {
  padding: 20px 24px;
  display: flex;
  flex-direction: row;
  align-items: center;
  justify-content: space-between;
  border-bottom: 1px solid #eef2f6;
}

.modal-header h3 {
  font-size: 18px;
  font-weight: 600;
  color: #0f172a;
  margin: 0;
}

.modal-header h3 i {
  margin-right: 8px;
  color: #6366f1;
}

.modal-body {
  padding: 24px;
  overflow-y: auto;
  flex: 1;
}

.modal-footer {
  padding: 16px 24px;
  border-top: 1px solid #eef2f6;
  display: flex;
  flex-direction: row;
  justify-content: right;
  gap: 12px;
}

.form-group {
  margin-bottom: 20px;
}

.form-group:last-child {
  margin-bottom: 0;
}

.form-group label {
  display: block;
  font-size: 14px;
  font-weight: 500;
  color: #334155;
  margin-bottom: 8px;
}

.form-group .required {
  color: #ef4444;
}

.form-group input,
.form-group select,
.form-group textarea {
  width: 100%;
  padding: 12px 16px;
  border: 1px solid #e2e8f0;
  border-radius: 10px;
  font-size: 14px;
  color: #1e293b;
  background: #f8fafc;
  transition:
    border-color 0.2s ease,
    box-shadow 0.2s ease;
  box-sizing: border-box;
}

.form-group input:focus,
.form-group select:focus,
.form-group textarea:focus {
  outline: none;
  border-color: #6366f1;
  box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.1);
  background: white;
}

.form-group textarea {
  resize: vertical;
  min-height: 80px;
}

.icon-selector {
  display: grid;
  grid-template-columns: repeat(8, 1fr);
  gap: 8px;
}

.icon-option {
  width: 40px;
  height: 40px;
  border-radius: 10px;
  border: 1px solid #e2e8f0;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s ease;
  background: #f8fafc;
}

.icon-option:hover {
  border-color: #6366f1;
  background: #f0f0ff;
}

.icon-option.selected {
  border-color: #6366f1;
  background: #6366f1;
  color: white;
}

.icon-btn {
  background: none;
  border: none;
  width: 32px;
  height: 32px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  color: #64748b;
  transition: all 0.2s ease;
}

.icon-btn:hover {
  background: #f1f5f9;
  color: #334155;
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
  transition: all 0.2s ease;
}

.btn-outline-sm:hover {
  background: #f8fafc;
  border-color: #94a3b8;
}

.btn-primary-sm {
  border: none;
  /* background: linear-gradient(135deg, #1e40af, #1e3a8a); */
  background: #1e3a8a;
  padding: 10px 24px;
  border-radius: 30px;
  font-size: 14px;
  cursor: pointer;
  font-weight: 500;
  color: white;
  transition: all 0.2s ease;
}

.btn-primary-sm:hover {
  opacity: 0.9;
  box-shadow: 0 4px 12px rgba(99, 102, 241, 0.3);
  background: #1e40af;
}
</style>
