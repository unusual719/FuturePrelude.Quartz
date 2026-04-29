import { ref } from 'vue'

// 全局确认弹框状态
const visible = ref(false)
const title = ref('确认操作')
const message = ref('')
const type = ref('warning') // warning | danger | info
let resolveCallback = null

/**
 * 显示确认弹框
 * @param {string} msg - 确认消息
 * @param {string} confirmTitle - 标题
 * @param {string} confirmType - 类型：warning | danger | info
 * @returns {Promise<boolean>} 用户点击确定返回 true，取消返回 false
 */
export function showConfirm(msg, confirmTitle = '确认操作', confirmType = 'warning') {
    return new Promise((resolve) => {
        message.value = msg
        title.value = confirmTitle
        type.value = confirmType
        visible.value = true
        resolveCallback = resolve
    })
}

// 导出状态供组件绑定
export const confirmState = {
    visible,
    title,
    message,
    type,
    resolveCallback
}

/**
 * 确认操作
 */
export function handleConfirm() {
    if (resolveCallback) {
        resolveCallback(true)
    }
    visible.value = false
    resolveCallback = null
}

/**
 * 取消操作
 */
export function handleCancel() {
    if (resolveCallback) {
        resolveCallback(false)
    }
    visible.value = false
    resolveCallback = null
}