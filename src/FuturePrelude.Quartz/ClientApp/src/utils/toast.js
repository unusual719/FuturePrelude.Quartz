import { ref } from 'vue'

// 全局 toast 状态
const visible = ref(false)
const message = ref('')
const type = ref('info')
let timer = null

// 全局 loading 状态
const loadingVisible = ref(false)
const loadingCount = ref(0)

/**
 * 显示 toast 提示
 * @param {string} msg - 提示消息
 * @param {string} toastType - 类型：info | success | warning | error
 * @param {number} duration - 显示时长(ms)
 */
export function showToast(msg, toastType = 'info', duration = 2000) {
    // 如果有正在倒计时的 timer，先清除
    if (timer) {
        clearTimeout(timer)
        timer = null
    }

    message.value = msg
    type.value = toastType
    visible.value = true

    timer = setTimeout(() => {
        visible.value = false
        timer = null
    }, duration)
}

/**
 * 显示 loading
 */
export function showLoading() {
    loadingCount.value++
    loadingVisible.value = true
}

/**
 * 隐藏 loading（引用计数减少）
 */
export function hideLoading() {
    loadingCount.value--
    if (loadingCount.value <= 0) {
        loadingCount.value = 0
        loadingVisible.value = false
    }
}

// 导出状态供组件绑定
export const toastState = {
    visible,
    message,
    type
}

export const loadingState = {
    visible: loadingVisible,
    count: loadingCount
}