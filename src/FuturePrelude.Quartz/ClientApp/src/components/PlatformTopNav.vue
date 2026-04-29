<template>
  <header class="top-navbar">
    <div class="logo-area">
      <div class="logo-icon">
        <i class="fas fa-tasks"></i>
      </div>
      <span class="logo-text">任务调度平台</span>
    </div>

    <nav class="nav-menu">
      <button
        type="button"
        class="nav-item"
        :class="{ active: route.name === 'Dashboard' }"
        @click="goTo('Dashboard')"
      >
        <i class="fas fa-chart-line"></i> 监控大盘
      </button>
      <button
        type="button"
        class="nav-item"
        :class="{ active: route.name === 'TaskList' }"
        @click="goTo('TaskList')"
      >
        <i class="fas fa-list"></i> 任务列表
      </button>
      <button
        type="button"
        class="nav-item"
        :class="{ active: route.name === 'ExecutionRecords' }"
        @click="goTo('ExecutionRecords')"
      >
        <i class="fas fa-history"></i> 执行记录
      </button>
    </nav>

    <div class="user-area">
      <!-- <button class="icon-btn" title="通知">
        <i class="fas fa-bell"></i>
      </button> -->
      <div class="avatar" :title="userInfo.name">
        {{ userInfo.avatar }}
      </div>
      <button class="icon-btn" @click="handleLogout" title="退出登录">
        <i class="fas fa-sign-out-alt"></i>
      </button>
    </div>
  </header>
</template>

<script setup>
import { onMounted, ref } from "vue";
import { useRoute, useRouter } from "vue-router";

const router = useRouter();
const route = useRoute();
const userInfo = ref({
  name: "用户",
  avatar: "US",
});

const goTo = (name) => {
  if (route.name === name) {
    return;
  }

  router.push({ name });
};

const handleLogout = () => {
  localStorage.removeItem("scheduler_token");
  localStorage.removeItem("scheduler_refresh_token");
  localStorage.removeItem("scheduler_user");
  router.push({ name: "Login" });
};

onMounted(() => {
  try {
    const userStr = localStorage.getItem("scheduler_user");
    if (!userStr) {
      return;
    }

    const user = JSON.parse(userStr);
    userInfo.value = {
      name: user.name || "用户",
      avatar: user.avatar || "US",
    };
  } catch (error) {
    console.error("解析用户信息失败:", error);
  }
});
</script>

<style scoped>
.top-navbar {
  background: #ffffff;
  border-bottom: 1px solid #e9edf2;
  padding: 0 32px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  height: 64px;
  flex-shrink: 0;
}

.logo-area {
  display: flex;
  align-items: center;
  gap: 12px;
}

.logo-icon {
  background: #1e40af;
  width: 32px;
  height: 32px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #ffffff;
  font-size: 18px;
}

.logo-text {
  font-weight: 700;
  font-size: 18px;
  color: #0f172a;
}

.nav-menu {
  display: flex;
  gap: 8px;
  background: #f8fafc;
  padding: 4px;
  border-radius: 48px;
}

.nav-item {
  padding: 8px 20px;
  border-radius: 40px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  border: none;
  background: transparent;
  color: #334155;
  transition: all 0.2s ease;
}

.nav-item:hover {
  color: #1e40af;
}

.nav-item.active {
  background: #ffffff;
  color: #1e40af;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
}

.user-area {
  display: flex;
  align-items: center;
  gap: 16px;
}

.icon-btn {
  background: transparent;
  border: none;
  padding: 6px;
  border-radius: 8px;
  cursor: pointer;
  color: #5b6e8c;
  font-size: 13px;
  transition: all 0.2s ease;
}

.icon-btn:hover {
  background: #eef2ff;
  color: #2563eb;
}

.avatar {
  width: 36px;
  height: 36px;
  background: #eef2ff;
  border-radius: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 600;
  color: #1e40af;
}

@media (max-width: 1200px) {
  .top-navbar {
    padding: 0 20px;
    gap: 16px;
  }

  .logo-text {
    display: none;
  }

  .nav-item {
    padding: 8px 14px;
  }

  .user-area {
    gap: 10px;
  }
}

@media (max-width: 840px) {
  .top-navbar {
    height: auto;
    min-height: 64px;
    padding: 12px 16px;
    flex-wrap: wrap;
  }

  .nav-menu {
    order: 3;
    width: 100%;
    justify-content: center;
    flex-wrap: wrap;
    border-radius: 18px;
  }
}
</style>
