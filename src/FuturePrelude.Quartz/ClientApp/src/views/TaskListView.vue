<template>
  <div class="app-wrapper">
    <PlatformTopNav />

    <div class="platform-container">
      <aside class="group-sidebar">
        <div class="sidebar-header">
          <h3><i class="fas fa-folder-tree"></i> 任务分组</h3>
          <button
            class="group-config-btn"
            @click="openCreateGroupModal"
            title="新建分组"
          >
            <i class="fas fa-plus"></i>
          </button>
        </div>
        <ul class="group-tree">
          <li
            v-for="group in groups"
            :key="group.Id"
            class="group-item"
            :class="{ active: isSameEntityId(currentGroup, group.Id) }"
            @click="selectGroup(group.Id)"
          >
            <div class="group-name">
              <i :class="group.Icon"></i>
              {{ group.Name }}
            </div>
            <div class="group-actions">
              <button
                v-if="group.Name !== 'Default'"
                class="group-action-btn"
                @click.stop="openEditGroupModal(group)"
                title="编辑"
              >
                <i class="fas fa-edit"></i>
              </button>
              <button
                v-if="group.Name !== 'Default'"
                class="group-action-btn"
                @click.stop="deleteGroup(group)"
                title="删除"
              >
                <i class="fas fa-trash"></i>
              </button>
            </div>
          </li>
        </ul>
      </aside>

      <main class="main-content">
        <div class="content-header">
          <div class="group-info">
            <h2><i :class="currentGroupIcon"></i> {{ currentGroupName }}</h2>
            <p>HTTP API 和 AssemblyPlugin 任务分开管理，支持执行记录查看</p>
          </div>
          <div class="global-actions">
            <button
              class="btn-outline-sm"
              @click="resumeAllJobs"
              :disabled="schedulerActionLoading"
            >
              <i class="fas fa-play"></i> 恢复全部任务
            </button>
            <button
              class="btn-danger-sm"
              @click="pauseAllJobs"
              :disabled="schedulerActionLoading"
            >
              <i class="fas fa-pause"></i> 暂停全部任务
            </button>
            <button
              class="btn-primary-sm"
              @click="openCreateJobModal"
              :disabled="schedulerActionLoading"
            >
              <i class="fas fa-plus"></i> 新建任务
            </button>
          </div>
        </div>

        <div class="job-tabs">
          <div
            class="job-tab"
            :class="{ active: currentTab === 'http' }"
            @click="currentTab = 'http'"
          >
            <i class="fas fa-globe"></i> HTTP API 任务
            <span class="tab-count">({{ httpJobCount }})</span>
          </div>
          <div
            class="job-tab"
            style="pointer-events: none; opacity: 0.5; cursor: not-allowed"
            :class="{ active: currentTab === 'assembly' }"
            @click="currentTab = 'assembly'"
          >
            <i class="fas fa-puzzle-piece"></i> 插件任务-待开发
            <span class="tab-count">({{ assemblyJobCount }})</span>
          </div>
        </div>

        <HttpTaskListSection
          ref="httpTaskSectionRef"
          v-show="currentTab === 'http'"
          :group-id="currentGroup"
          :active="currentTab === 'http'"
          @count-change="handleHttpJobCountChange"
        />

        <AssemblyTaskListSection
          ref="assemblyTaskSectionRef"
          v-show="currentTab === 'assembly'"
          :group-id="currentGroup"
          :active="currentTab === 'assembly'"
          @count-change="handleAssemblyJobCountChange"
        />
      </main>
    </div>

    <GroupModal
      :visible="groupModalVisible"
      :group="editingGroup"
      @close="closeGroupModal"
      @save="handleGroupSave"
    />
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from "vue";
import { showToast } from "../utils/toast.js";
import { showConfirm } from "../utils/confirm.js";
import {
  getJobGroupList,
  createJobGroup,
  updateJobGroup,
  deleteJobGroup,
} from "../utils/jobGroupApi.js";
import {
  pauseAllSchedules,
  resumeAllSchedules,
} from "../utils/schedulerApi.js";
import GroupModal from "../components/modals/GroupModal.vue";
import PlatformTopNav from "../components/PlatformTopNav.vue";
import HttpTaskListSection from "../components/task-list/HttpTaskListSection.vue";
import AssemblyTaskListSection from "../components/task-list/AssemblyTaskListSection.vue";
import "../styles/tasklist.css";

const groupsData = ref([]);
const currentGroup = ref(null);
const currentTab = ref("http");
const groupModalVisible = ref(false);
const editingGroup = ref(null);
const schedulerActionLoading = ref(false);
const httpJobCount = ref(0);
const assemblyJobCount = ref(0);
const httpTaskSectionRef = ref(null);
const assemblyTaskSectionRef = ref(null);

const toEntityIdKey = (value) => {
  if (value === null || value === undefined || value === "") {
    return "";
  }

  return String(value);
};

const isSameEntityId = (left, right) => {
  const leftKey = toEntityIdKey(left);
  const rightKey = toEntityIdKey(right);

  return leftKey !== "" && leftKey === rightKey;
};

const groups = computed(() => groupsData.value);

const currentGroupName = computed(() => {
  return (
    groupsData.value.find((group) => isSameEntityId(group.Id, currentGroup.value))
      ?.Name || ""
  );
});

const currentGroupIcon = computed(() => {
  return (
    groupsData.value.find((group) => isSameEntityId(group.Id, currentGroup.value))
      ?.Icon || "fas fa-folder"
  );
});

const openCreateGroupModal = () => {
  editingGroup.value = null;
  groupModalVisible.value = true;
};

const openEditGroupModal = (group) => {
  currentGroup.value = group.Id;
  editingGroup.value = { ...group };
  groupModalVisible.value = true;
};

const closeGroupModal = () => {
  groupModalVisible.value = false;
  editingGroup.value = null;
};

const refreshGroups = async () => {
  try {
    const list = await getJobGroupList();
    groupsData.value = list;

    if (
      currentGroup.value &&
      !list.find((group) => isSameEntityId(group.Id, currentGroup.value))
    ) {
      currentGroup.value = list[0]?.Id || null;
    }
  } catch (error) {
    console.error("刷新分组列表失败:", error);
  }
};

const handleGroupSave = async (data) => {
  try {
    if (data.Id) {
      await updateJobGroup({
        id: data.Id,
        name: data.Name,
        icon: data.Icon,
        status: data.Status || 1,
        description: data.Description || "",
      });
      showToast(`分组 ${data.Name} 更新成功`);
    } else {
      await createJobGroup({
        name: data.Name,
        icon: data.Icon,
        status: 1,
        description: data.Description || "",
      });
      showToast(`分组 ${data.Name} 创建成功`);
    }

    closeGroupModal();
    await refreshGroups();
  } catch (error) {
    showToast(error.message || "保存分组失败");
  }
};

const deleteGroup = async (group) => {
  currentGroup.value = group.Id;
  const confirmed = await showConfirm(
    `确定要删除分组「${group.Name}」吗？`,
    "警告",
    "danger",
  );
  if (!confirmed) return;

  try {
    await deleteJobGroup(group.Id);
    showToast(`分组「${group.Name}」已删除`);
    await refreshGroups();
  } catch (error) {
    showToast(error.message || "删除分组失败");
  }
};

const selectGroup = (id) => {
  currentGroup.value = id;
};

const handleHttpJobCountChange = (count) => {
  httpJobCount.value = count;
};

const handleAssemblyJobCountChange = (count) => {
  assemblyJobCount.value = count;
};

const openCreateJobModal = () => {
  const targetRef =
    currentTab.value === "http"
      ? httpTaskSectionRef.value
      : assemblyTaskSectionRef.value;

  targetRef?.openCreateJobModal?.();
};

const pauseAllJobs = async () => {
  const confirmed = await showConfirm(
    "确定要暂停全部任务吗？该操作会暂停调度器中的所有任务。",
    "提示",
    "warning",
  );
  if (!confirmed || schedulerActionLoading.value) return;

  try {
    schedulerActionLoading.value = true;
    await pauseAllSchedules();
    await httpTaskSectionRef.value?.refreshAfterSchedulerAction?.();
    showToast("全部任务已暂停");
  } catch (error) {
    showToast(error.message || "暂停全部任务失败", "error");
  } finally {
    schedulerActionLoading.value = false;
  }
};

const resumeAllJobs = async () => {
  const confirmed = await showConfirm(
    "确定要恢复全部任务吗？该操作会恢复调度器中的所有任务。",
    "提示",
    "warning",
  );
  if (!confirmed || schedulerActionLoading.value) return;

  try {
    schedulerActionLoading.value = true;
    await resumeAllSchedules();
    await httpTaskSectionRef.value?.refreshAfterSchedulerAction?.();
    showToast("全部任务已恢复");
  } catch (error) {
    showToast(error.message || "恢复全部任务失败", "error");
  } finally {
    schedulerActionLoading.value = false;
  }
};

onMounted(async () => {
  try {
    const list = await getJobGroupList();
    groupsData.value = list;

    if (list.length > 0) {
      currentGroup.value = list[0].Id;
    }
  } catch (error) {
    console.error("加载分组列表失败:", error);
    showToast("加载分组列表失败", "error");
  }
});
</script>
