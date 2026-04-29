<template>
  <Teleport to="body">
    <Transition name="toast">
      <div v-if="visible" class="toast-msg">{{ message }}</div>
    </Transition>
  </Teleport>
</template>

<script setup>
import { ref } from "vue";

const visible = ref(false);
const message = ref("");
let timer = null;

// 显示 toast
const show = (msg, duration = 2000) => {
  // 如果有正在倒计时的 timer，先清除
  if (timer) {
    clearTimeout(timer);
    timer = null;
  }

  message.value = msg;
  visible.value = true;

  timer = setTimeout(() => {
    visible.value = false;
    timer = null;
  }, duration);
};

// 暴露 show 方法给外部调用
defineExpose({ show });
</script>

<style scoped>
.toast-msg {
  position: fixed;
  bottom: 24px;
  left: 50%;
  transform: translateX(-50%);
  background: #1e293b;
  color: white;
  padding: 10px 24px;
  border-radius: 40px;
  font-size: 13px;
  z-index: 3000;
  box-shadow: 0 10px 25px rgba(0, 0, 0, 0.2);
  font-family: "Inter", sans-serif;
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
