/**
 * 任务执行记录 API
 */
import { post } from "./request.js";

const API_BASE = "/api/sys-job-execution-log";

/**
 * 获取任务执行记录分页列表
 * @param {Object} query - { jobId, triggerId, pageIndex, pageSize }
 * @param {Object} options - request options
 * @returns {Promise<{Items: Array, TotalCount: number, PageIndex: number, PageSize: number, TotalPages: number}>}
 */
export async function getJobExecutionLogPageList(query, options = {}) {
  return await post(`${API_BASE}/list`, query, {
    skipLoading: true,
    ...options,
  });
}
