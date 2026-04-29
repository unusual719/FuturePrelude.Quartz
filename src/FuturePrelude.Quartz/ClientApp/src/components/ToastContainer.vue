<template>
  <Teleport to="body">
    <Transition name="toast">
      <div v-if="toastState.visible.value" class="toast-container">
        <div class="toast-icon" :class="toastState.type.value">
          <i :class="iconClass"></i>
        </div>
        <span class="toast-message">{{ toastState.message.value }}</span>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup>
import { computed } from "vue";
import { toastState } from "../utils/toast.js";

const iconClass = computed(() => {
  switch (toastState.type.value) {
    case "success":
      return "fas fa-check-circle";
    case "warning":
      return "fas fa-exclamation-circle";
    case "error":
      return "fas fa-times-circle";
    default:
      return "fas fa-info-circle";
  }
});
</script>

<style scoped>
.toast-container {
  position: fixed;
  top: 25px;
  left: 50%;
  transform: translateX(-50%);
  background: #1e293b;
  color: white;
  padding: 12px 20px;
  border-radius: 40px;
  font-size: 14px;
  z-index: 3000;
  box-shadow: 0 10px 25px rgba(0, 0, 0, 0.2);
  font-family: "Inter", sans-serif;
  display: flex;
  align-items: center;
  gap: 10px;
}

.toast-icon {
  font-size: 16px;
}

.toast-icon.success {
  color: #10b981;
}

.toast-icon.warning {
  color: #f59e0b;
}

.toast-icon.error {
  color: #ef4444;
}

.toast-icon.info {
  color: #3b82f6;
}

.toast-enter-active,
.toast-leave-active {
  transition: all 0.2s ease;
}

.toast-enter-from,
.toast-leave-to {
  opacity: 0;
  transform: translateX(-50%) translateY(10px);
}
</style>
