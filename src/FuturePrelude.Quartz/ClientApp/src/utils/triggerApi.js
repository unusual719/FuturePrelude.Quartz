import { del, get, post, put } from "./request.js";

const API_BASE = "/api/sys-job-trigger";

export async function getTriggerPageList(query) {
  return await post(`${API_BASE}/list`, query);
}

export async function getTriggerById(id) {
  return await get(`${API_BASE}/${id}`);
}

export async function createTrigger(data) {
  return await post(API_BASE, data);
}

export async function updateTrigger(data) {
  return await put(API_BASE, data);
}

export async function deleteTrigger(id) {
  return await del(`${API_BASE}/${id}`);
}

export async function enableTrigger(id) {
  return await post(`${API_BASE}/${id}/enable`);
}

export async function disableTrigger(id) {
  return await post(`${API_BASE}/${id}/disable`);
}

export async function triggerOnce(id) {
  return await post(`${API_BASE}/${id}/trigger-once`);
}

export async function getTriggerStatusList(triggerIds) {
  return await post(
    `${API_BASE}/trigger-status-list`,
    { triggerIds },
    { skipLoading: true },
  );
}

export async function describeCronExpression(cronExpression, options = {}) {
  return await post(
    `${API_BASE}/describe-cron`,
    { cronExpression },
    { skipLoading: true, ...options },
  );
}
