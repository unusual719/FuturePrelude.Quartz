/**
 * 任务分组 API
 */
import { get, post, put, del } from "./request.js";

const API_BASE = "/api/sys-job-group";

/**
 * 获取任务分组列表
 * @returns {Promise<Array>}
 */
export async function getJobGroupList() {
  return await get(`${API_BASE}/list`);
}

/**
 * 创建任务分组
 * @param {Object} data - { Name, Icon, Status, Description }
 * @returns {Promise<Object>}
 */
export async function createJobGroup(data) {
  return await post(API_BASE, data);
}

/**
 * 更新任务分组
 * @param {Object} data - { Id, Name, Icon, Status, Description }
 * @returns {Promise<Object>}
 */
export async function updateJobGroup(data) {
  return await put(API_BASE, data);
}

/**
 * 删除任务分组
 * @param {number} id - 分组ID
 * @returns {Promise<boolean>}
 */
export async function deleteJobGroup(id) {
  return await del(`${API_BASE}/${id}`);
}