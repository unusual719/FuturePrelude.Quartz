<template>
  <div class="login-card">
    <!-- Logo 区域 -->
    <div class="login-brand-area">
      <div class="login-brand-icon">
        <i class="fas fa-tasks"></i>
      </div>
      <div class="login-brand-text">任务调度平台</div>
      <div class="login-brand-sub"></div>
    </div>

    <!-- 欢迎语 -->
    <div class="login-welcome-text">
      <h2>欢迎回来</h2>
      <p>请输入您的访问令牌登录系统</p>
    </div>

    <!-- 错误提示 -->
    <div class="login-error-message" v-if="errorMessage">
      <i class="fas fa-exclamation-circle"></i>
      <span>{{ errorMessage }}</span>
    </div>

    <!-- Token 输入区域 -->
    <div class="token-input-group">
      <div class="login-input-label">
        <i class="fas fa-key"></i>
        <span>访问令牌</span>
      </div>
      <div class="token-input-wrapper">
        <input
          type="password"
          class="token-input"
          v-model="token"
          @keyup.enter="handleLogin"
          placeholder="请输入访问令牌"
          autocomplete="off"
        />
      </div>
    </div>

    <!-- 登录按钮 -->
    <button class="login-btn" @click="handleLogin" :disabled="loading">
      <span v-if="!loading"> 登录系统 <i class="fas fa-arrow-right"></i> </span>
      <span v-else> <i class="login-spinner"></i> 验证中... </span>
    </button>
  </div>
</template>

<script setup>
import { ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import { getPostLoginRedirectPath } from "../utils/authSession.js";

const router = useRouter();
const route = useRoute();

const token = ref("");
const loading = ref(false);
const errorMessage = ref("");

// API 地址
const API_URL = "/api/auth";

/**
 * 调用登录接口
 */
const handleLogin = async () => {
  // 清除旧错误
  errorMessage.value = "";

  // 验证 Token 是否为空
  if (!token.value || token.value.trim() === "") {
    errorMessage.value = "请输入访问令牌";
    return;
  }

  const tokenValue = token.value.trim();
  loading.value = true;

  try {
    const response = await fetch(API_URL, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ token: tokenValue }),
    });

    const result = await response.json();

    // 检查是否有 AccessToken 来判断成功（兼容 PascalCase 和 camelCase）
    if (result.Code == 500) {
      errorMessage.value =
        result.Message || result.message || `请求失败 (${response.status})`;
      loading.value = false;
      return;
    }

    if (result.Data.AccessToken || result.Data.accessToken) {
      const accessToken = result.Data.AccessToken || result.Data.accessToken;
      const refreshToken = result.Data.RefreshToken || result.Data.refreshToken;

      // 登录成功，保存 Token
      localStorage.setItem("scheduler_token", accessToken);
      localStorage.setItem("scheduler_refresh_token", refreshToken);

      // 存储用户信息（从 JWT 解码或后端返回）
      const userInfo = parseToken(accessToken);
      localStorage.setItem(
        "scheduler_user",
        JSON.stringify({
          ...userInfo,
          loginTime: new Date().toISOString(),
        }),
      );

      // 登录成功后优先回到认证失效前的页面
      router.replace(getPostLoginRedirectPath(route.query?.redirect));
    } else {
      // 登录失败，显示错误信息
      errorMessage.value =
        result.Message || result.message || `请求失败 (${response.status})`;
      loading.value = false;
    }
  } catch (error) {
    console.error("登录请求失败:", error);
    errorMessage.value = "网络错误，请稍后重试";
    loading.value = false;
  }
};

/**
 * 简单解析 JWT Token 获取用户信息
 * 注意：这是前端解析，实际生产环境应该由后端返回完整的用户信息
 */
const parseToken = (accessToken) => {
  try {
    // JWT 格式: header.payload.signature
    const payload = accessToken.split(".")[1];
    // Base64 解码（需要处理 URL 安全的字符）
    const decoded = JSON.parse(
      atob(payload.replace(/-/g, "+").replace(/_/g, "/")),
    );
    return {
      name: decoded.name || decoded.sub || "用户",
      role: decoded.role || "user",
      avatar: (decoded.name || decoded.sub || "U")
        .substring(0, 2)
        .toUpperCase(),
    };
  } catch {
    return {
      name: "用户",
      role: "user",
      avatar: "US",
    };
  }
};
</script>
