/**
 * 统一 API 请求工具
 * 处理认证头、响应格式、错误信息
 */

import {
    handleUnauthorizedSessionExpiry,
    refreshAuthTokens,
} from "./authSession.js";
import { showLoading, hideLoading } from "./toast.js";

/**
 * 获取认证头
 */
function getAuthHeaders() {
    const token = localStorage.getItem("scheduler_token");
    let auth = "";
    if (token) {
        auth = token.startsWith("Bearer ") ? token : `Bearer ${token}`;
    }
    return {
        "Content-Type": "application/json",
        Authorization: auth,
    };
}

async function readResponsePayload(response) {
    try {
        return await response.json();
    } catch {
        return {};
    }
}

function buildFinalOptions(restOptions = {}) {
    const defaultOptions = {
        headers: getAuthHeaders(),
    };

    return {
        ...defaultOptions,
        ...restOptions,
        headers: {
            ...defaultOptions.headers,
            ...restOptions.headers,
        },
    };
}

async function executeRequest(url, restOptions, hasRetried = false) {
    const response = await fetch(url, buildFinalOptions(restOptions));
    const result = await readResponsePayload(response);

    if (!response.ok) {
        if (response.status === 401 && !hasRetried) {
            await refreshAuthTokens();
            return executeRequest(url, restOptions, true);
        }

        if (response.status === 401) {
            handleUnauthorizedSessionExpiry();
        }

        const statusMessages = {
            401: "未授权，请重新登录",
            404: "请求的资源不存在",
        };
        const msg =
            statusMessages[response.status] ||
            `请求失败 (${response.status})，${result.Message || result.message || "未知错误"}`;
        throw new Error(msg);
    }

    if (result.Code === 200 || result.code === 200) {
        return result.Data ?? result.data;
    }

    throw new Error(result.Message || result.message || "请求失败");
}

/**
 * 发送 API 请求
 * @param {string} url - 请求地址
 * @param {Object} options - fetch 选项 (method, body, etc.)
 * @param {boolean} options.skipLoading - 跳过 loading 显示
 * @returns {Promise<any>} - 返回 data 部分
 */
export async function request(url, options = {}) {
    const { skipLoading = false, ...restOptions } = options;

    if (!skipLoading) {
        showLoading();
    }

    try {
        return await executeRequest(url, restOptions);
    } catch (error) {
        throw error;
    } finally {
        if (!skipLoading) {
            hideLoading();
        }
    }
}

/**
 * GET 请求
 */
export function get(url, options = {}) {
    return request(url, { method: "GET", ...options });
}

/**
 * POST 请求
 */
export function post(url, data, options = {}) {
    return request(url, {
        method: "POST",
        body: JSON.stringify(data),
        ...options,
    });
}

/**
 * PUT 请求
 */
export function put(url, data, options = {}) {
    return request(url, {
        method: "PUT",
        body: JSON.stringify(data),
        ...options,
    });
}

/**
 * DELETE 请求
 */
export function del(url, options = {}) {
    return request(url, { method: "DELETE", ...options });
}