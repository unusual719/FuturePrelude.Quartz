/**
 * 任务详情 API
 * HTTP API 任务管理接口
 */
import { get, post, put, del } from "./request.js";

const API_BASE = "/api/sys-job-detail";

/**
 * 获取任务详情分页列表
 * @param {Object} query - { groupId, jobType, enableStatus, keyword, pageIndex, pageSize }
 * @returns {Promise<{items: Array, totalCount: number, pageIndex: number, pageSize: number, totalPages: number}>}
 */
export async function getJobDetailPageList(query) {
  return await post(`${API_BASE}/list`, query);
}

/**
 * 根据ID获取任务详情
 * @param {number} id - 任务ID
 * @returns {Promise<Object>}
 */
export async function getJobDetailById(id) {
  return await get(`${API_BASE}/${id}`);
}

/**
 * 创建任务详情
 * @param {Object} data - 任务详情创建数据
 * @returns {Promise<Object>}
 */
export async function createJobDetail(data) {
  return await post(API_BASE, data);
}

/**
 * 更新任务详情
 * @param {Object} data - 任务详情更新数据
 * @returns {Promise<Object>}
 */
export async function updateJobDetail(data) {
  return await put(API_BASE, data);
}

/**
 * 删除任务详情
 * @param {number} id - 任务ID
 * @returns {Promise<boolean>}
 */
export async function deleteJobDetail(id) {
  return await del(`${API_BASE}/${id}`);
}

/**
 * 启用任务
 * @param {number} id - 任务ID
 * @returns {Promise<boolean>}
 */
export async function enableJobDetail(id) {
  return await post(`${API_BASE}/${id}/enable`);
}

/**
 * 禁用任务
 * @param {number} id - 任务ID
 * @returns {Promise<boolean>}
 */
export async function disableJobDetail(id) {
  return await post(`${API_BASE}/${id}/disable`);
}

/**
 * 暂停任务
 * @param {number} id - 任务ID
 * @returns {Promise<boolean>}
 */
export async function pauseJobDetail(id) {
  return await post(`${API_BASE}/${id}/pause`);
}

/**
 * 恢复任务
 * @param {number} id - 任务ID
 * @returns {Promise<boolean>}
 */
export async function resumeJobDetail(id) {
  return await post(`${API_BASE}/${id}/resume`);
}

/**
 * 批量获取任务运行状态
 * @param {Array<number|string>} ids - 任务ID列表
 * @returns {Promise<Array<{Id: number, Status: string}>>}
 */
export async function getJobDetailStatuses(ids) {
  return await post(
    `${API_BASE}/job-status`,
    { ids },
    { skipLoading: true },
  );
}
