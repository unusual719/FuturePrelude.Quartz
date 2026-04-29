/**
 * 调度器 API
 * Quartz Scheduler 全局运维接口
 */
import { post } from "./request.js";

const API_BASE = "/api/scheduler";

/**
 * 暂停全部任务调度
 * @returns {Promise<boolean>}
 */
export async function pauseAllSchedules() {
  return await post(`${API_BASE}/pause-all`);
}

/**
 * 恢复全部任务调度
 * @returns {Promise<boolean>}
 */
export async function resumeAllSchedules() {
  return await post(`${API_BASE}/resume-all`);
}
