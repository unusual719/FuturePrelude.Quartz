import { ref } from 'vue'

// 全局 toast 状态
const visible = ref(false)
const message = ref('')
let timer = null

export function useToast() {
  const show = (msg, duration = 2000) => {
    // 如果有正在倒计时的 timer，先清除
    if (timer) {
      clearTimeout(timer)
      timer = null
    }

    message.value = msg
    visible.value = true

    timer = setTimeout(() => {
      visible.value = false
      timer = null
    }, duration)
  }

  return {
    visible,
    message,
    show
  }
}