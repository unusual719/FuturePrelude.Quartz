<template>
  <Teleport to="body">
    <div
      v-if="confirmState.visible.value"
      class="confirm-overlay"
      @click.self="handleCancel"
    >
      <div class="confirm-modal">
        <div class="confirm-header">
          <div class="confirm-icon" :class="confirmState.type.value">
            <i :class="iconClass"></i>
          </div>
          <h3>{{ confirmState.title.value }}</h3>
        </div>
        <div class="confirm-body">
          <p>{{ confirmState.message.value }}</p>
        </div>
        <div class="confirm-footer">
          <button class="btn-outline-sm" @click="handleCancel">取消</button>
          <button
            class="btn-confirm-sm"
            :class="confirmState.type.value"
            @click="handleConfirm"
          >
            {{ confirmText }}
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup>
import { computed } from "vue";
import { confirmState, handleConfirm, handleCancel } from "../utils/confirm.js";

const confirmText = computed(() => {
  switch (confirmState.type.value) {
    case "danger":
      return "确定";
    case "info":
      return "确定";
    default:
      return "确定";
  }
});

const iconClass = computed(() => {
  switch (confirmState.type.value) {
    case "danger":
      return "fas fa-exclamation-triangle";
    case "info":
      return "fas fa-info-circle";
    default:
      return "fas fa-exclamation-circle";
  }
});
</script>

<style scoped>
.confirm-overlay {
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
  z-index: 2000;
  animation: fadeIn 0.15s ease;
}

@keyframes fadeIn {
  from {
    opacity: 0;
  }

  to {
    opacity: 1;
  }
}

.confirm-modal {
  background: white;
  border-radius: 20px;
  width: 400px;
  max-width: 90vw;
  box-shadow: 0 20px 35px rgba(0, 0, 0, 0.2);
  animation: slideUp 0.15s ease;
}

@keyframes slideUp {
  from {
    transform: scale(0.95);
    opacity: 0;
  }

  to {
    transform: scale(1);
    opacity: 1;
  }
}

.confirm-header {
  padding: 24px 24px 16px;
  display: flex;
  flex-direction: row;
  align-items: center;
  justify-content: left;
  gap: 12px;
}

.confirm-icon {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  flex-shrink: 0;
}

.confirm-icon.warning,
.confirm-icon.danger {
  background: #fef3c7;
  color: #d97706;
}

.confirm-icon.info {
  background: #dbeafe;
  color: #2563eb;
}

.confirm-header h3 {
  font-size: 18px;
  font-weight: 600;
  color: #0f172a;
  margin: 0;
}

.confirm-body {
  padding: 0 24px 20px;
  text-align: left;
}

.confirm-body p {
  font-size: 14px;
  color: #64748b;
  margin: 0;
  line-height: 1.6;
}

.confirm-footer {
  padding: 16px 24px;
  border-top: 1px solid #eef2f6;
  display: flex;
  flex-direction: row;
  justify-content: right;
  gap: 12px;
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
  border-color: #94a3b8;
}

.btn-confirm-sm {
  border: none;
  padding: 10px 24px;
  border-radius: 30px;
  font-size: 12px;
  cursor: pointer;
  font-weight: 500;
  color: white;
}

.btn-confirm-sm.warning,
.btn-confirm-sm.danger {
  background: linear-gradient(135deg, #1e3a8a, #1e40af);
}

.btn-confirm-sm.info {
  background: linear-gradient(135deg, #1e3a8a, #1e40af);
}

.btn-confirm-sm:hover {
  opacity: 0.9;
}
</style>
