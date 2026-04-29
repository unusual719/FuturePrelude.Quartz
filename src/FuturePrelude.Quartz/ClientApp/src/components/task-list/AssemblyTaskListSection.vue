<template>
  <div class="task-table-wrapper">
    <table class="task-table">
      <thead>
        <tr>
          <th style="width: 32px"></th>
          <th>任务名称</th>
          <th>程序集路径</th>
          <th>类型全名</th>
          <th>方法名</th>
          <th>禁止并发执行</th>
          <th>失败是否重试</th>
          <th>最大重试次数</th>
          <th>重试退避（秒）</th>
          <th>状态</th>
          <th>上次运行时间</th>
          <th>上次运行耗时（毫秒）</th>
          <th>创建时间</th>
          <th style="width: 140px">操作</th>
        </tr>
      </thead>
      <tbody>
        <template v-for="job in currentAssemblyJobs" :key="job.id">
          <tr class="task-row">
            <td>
              <i
                class="fas fa-chevron-right expand-icon"
                :class="{ 'rotate-icon': isRowExpanded(getJobId(job)) }"
                @click.stop="toggleExpand(getJobId(job))"
              ></i>
            </td>
            <td>
              <strong>{{ job.name }}</strong>
              <div class="job-remark">{{ job.remark || "" }}</div>
            </td>
            <td class="url-cell">{{ job.assemblyPath || "-" }}</td>
            <td class="url-cell">{{ job.typeFullName || "-" }}</td>
            <td>{{ job.methodName || "-" }}</td>
            <td>
              {{
                formatBooleanText(
                  getFieldValue(job, "DisallowConcurrent", "disallowConcurrent"),
                )
              }}
            </td>
            <td>
              {{
                formatBooleanText(
                  getFieldValue(job, "RetryOnFailure", "retryOnFailure"),
                )
              }}
            </td>
            <td>
              {{
                formatRetryDependentText(
                  getFieldValue(job, "RetryOnFailure", "retryOnFailure"),
                  getFieldValue(job, "MaxRetry", "maxRetry"),
                )
              }}
            </td>
            <td>
              {{
                formatRetryDependentText(
                  getFieldValue(job, "RetryOnFailure", "retryOnFailure"),
                  getFieldValue(
                    job,
                    "RetryBackoffSeconds",
                    "retryBackoffSeconds",
                  ),
                )
              }}
            </td>
            <td>
              <span
                class="status-badge"
                :class="
                  job.status === 'running' ? 'status-running' : 'status-stopped'
                "
              >
                <i
                  :class="
                    job.status === 'running' ? 'fas fa-play' : 'fas fa-stop'
                  "
                ></i>
                {{ job.status === "running" ? "运行中" : "已停止" }}
              </span>
            </td>
            <td class="time-cell">
              {{ formatDateTime(getFieldValue(job, "LastRunTime", "lastRunTime")) }}
            </td>
            <td>
              {{
                formatNullableText(
                  getFieldValue(job, "LastRunDurationMs", "lastRunDurationMs"),
                )
              }}
            </td>
            <td class="time-cell">
              {{ formatDateTime(getFieldValue(job, "CreateTime", "createTime")) }}
            </td>
            <td>
              <button
                class="icon-btn"
                @click.stop="toggleJobStatus(job)"
                :title="job.status === 'running' ? '暂停' : '启动'"
              >
                <i
                  :class="
                    job.status === 'running' ? 'fas fa-pause' : 'fas fa-play'
                  "
                ></i>
              </button>
              <button
                class="icon-btn"
                @click.stop="openEditJobModal(job)"
                title="编辑"
              >
                <i class="fas fa-edit"></i>
              </button>
              <button
                class="icon-btn"
                @click.stop="viewJobRecords(job)"
                title="执行记录"
              >
                <i class="fas fa-history"></i>
              </button>
              <button
                class="icon-btn"
                @click.stop="openCreateTriggerModal(getJobId(job))"
                title="添加触发器"
              >
                <i class="fas fa-plus-circle"></i>
              </button>
            </td>
          </tr>
          <tr v-if="isRowExpanded(getJobId(job))" class="task-detail-row">
            <td colspan="14" class="task-detail-cell">
              <div class="detail-container">
                <div class="trigger-header">
                  <span>
                    <i class="fas fa-bolt"></i> 触发器列表 ({{
                      getTriggerList(job).length
                    }})
                  </span>
                </div>
                <div class="trigger-cards">
                  <div
                    v-for="trigger in getTriggerList(job)"
                    :key="getTriggerId(trigger)"
                    class="trigger-item"
                  >
                    <div class="trigger-info">
                      <div class="trigger-name">
                        <strong>{{ getTriggerName(trigger) }}</strong>
                        <span class="trigger-rule">
                          {{
                            trigger.CronExpression ||
                            trigger.rule ||
                            (trigger.IntervalSeconds
                              ? trigger.IntervalSeconds + "秒"
                              : "-")
                          }}
                        </span>
                      </div>
                      <div class="trigger-meta-line">
                        <span class="trigger-meta-segment">
                          <em>下次运行</em>
                          <strong>{{
                            formatTriggerTime(
                              trigger.NextFireTimeUtc ??
                                trigger.NextFireTime ??
                                trigger.nextTime,
                            )
                          }}</strong>
                        </span>
                        <span class="trigger-meta-segment">
                          <em>时间窗口</em>
                          <strong>{{
                            formatTriggerWindow(
                              trigger.StartTimeUtc ?? trigger.startTime,
                              trigger.EndTimeUtc ?? trigger.endTime,
                            )
                          }}</strong>
                        </span>
                        <span class="trigger-meta-segment">
                          <em>类型</em>
                          <strong>{{
                            formatTriggerType(
                              trigger.TriggerType ?? trigger.triggerType,
                            )
                          }}</strong>
                        </span>
                        <span class="trigger-meta-segment">
                          <em>Misfire</em>
                          <strong>{{
                            formatMisfireStrategy(
                              trigger.MisfireStrategy ??
                                trigger.misfireStrategy,
                            )
                          }}</strong>
                        </span>
                        <span class="trigger-meta-segment">
                          <em>P</em>
                          <strong>{{
                            trigger.Priority ?? trigger.priority ?? "-"
                          }}</strong>
                        </span>
                      </div>
                    </div>
                    <div class="trigger-actions">
                      <button
                        class="icon-btn"
                        @click.stop="runingTrigger(trigger)"
                        title="立即执行"
                      >
                        <i class="fas fa-play"></i>
                      </button>
                      <button
                        class="icon-btn"
                        @click.stop="
                          openEditTriggerModal(getJobId(job), trigger)
                        "
                        title="编辑"
                      >
                        <i class="fas fa-edit"></i>
                      </button>
                      <button
                        class="icon-btn"
                        @click.stop="viewTriggerRecords(getJobId(job), trigger)"
                        title="执行记录"
                      >
                        <i class="fas fa-history"></i>
                      </button>
                      <button
                        class="icon-btn"
                        @click.stop="deleteTriggerItem(getJobId(job), trigger)"
                        title="删除"
                      >
                        <i class="fas fa-trash"></i>
                      </button>
                      <div
                        class="toggle-switch"
                        :class="{
                          active:
                            trigger.EnableStatus === 1 || trigger.enabled,
                        }"
                        @click.stop="
                          toggleTriggerStatus(
                            getJobId(job),
                            getTriggerId(trigger),
                          )
                        "
                      ></div>
                    </div>
                  </div>
                  <div
                    v-if="getTriggerList(job).length === 0"
                    class="trigger-empty"
                  >
                    暂无触发器，点击上方 + 添加
                  </div>
                </div>
              </div>
            </td>
          </tr>
        </template>
        <tr v-if="currentAssemblyJobs.length === 0">
          <td colspan="14" class="empty-cell">暂无插件任务</td>
        </tr>
      </tbody>
    </table>

    <AssemblyJobModal
      :visible="jobModalVisible"
      :job="editingJob"
      @close="closeJobModal"
      @save="handleJobSave"
    />

    <TriggerModal
      :visible="triggerModalVisible"
      :trigger="editingTrigger"
      :job-id="editingTriggerJobId"
      @close="closeTriggerModal"
      @save="handleTriggerSave"
    />

    <RecordModal
      :visible="recordModalVisible"
      :records="currentRecords"
      :title="recordTitle"
      :loading="false"
      :page-index="1"
      :total-pages="1"
      :total-count="currentRecords.length"
      :has-prev-page="false"
      :has-next-page="false"
      @close="closeRecordModal"
    />
  </div>
</template>

<script setup>
import { computed, ref, watch } from "vue";
import { formatDateTimeToSeconds } from "../../utils/dateTime.js";
import { showToast } from "../../utils/toast.js";
import AssemblyJobModal from "../modals/AssemblyJobModal.vue";
import TriggerModal from "../modals/TriggerModal.vue";
import RecordModal from "../modals/RecordModal.vue";

const props = defineProps({
  groupId: {
    type: [String, Number],
    default: null,
  },
  active: {
    type: Boolean,
    default: false,
  },
});

const emit = defineEmits(["count-change"]);

function generateRecords(count = 6) {
  const statuses = ["success", "failed", "running"];
  const statusText = { success: "成功", failed: "失败", running: "执行中" };
  const times = [
    "10:23:15",
    "09:15:22",
    "08:30:45",
    "07:00:10",
    "06:45:30",
    "05:20:00",
  ];
  const records = [];
  for (let i = 0; i < count; i++) {
    const status = statuses[Math.floor(Math.random() * 3)];
    records.push({
      id: Date.now() + i,
      time: `2026-04-24 ${times[i % times.length]}`,
      status,
      statusText: statusText[status],
      duration: `${(Math.random() * 5).toFixed(1)}s`,
      result:
        status === "success"
          ? '{"code":200, "message":"执行成功"}'
          : status === "failed"
            ? "连接超时: Connection refused"
            : "正在执行中...",
    });
  }
  return records;
}

const tasksData = ref({
  order: {
    assemblyJobs: [
      {
        id: "asm1",
        name: "订单对账插件",
        assemblyPath: "/plugins/OrderPlugin.dll",
        typeFullName: "OrderPlugin.ReconciliationTask",
        methodName: "Execute",
        pluginParams: '{"date":"2026-04-24"}',
        remark: "每日对账",
        status: "running",
        createTime: "2025-03-12",
        triggers: [
          {
            id: "tr2",
            name: "每天2点",
            rule: "0 0 2 * * ?",
            nextTime: "明天 02:00",
            enabled: true,
            records: generateRecords(5),
          },
        ],
        records: generateRecords(7),
      },
    ],
  },
  data: {
    assemblyJobs: [
      {
        id: "asm2",
        name: "数据清洗插件",
        assemblyPath: "/plugins/Cleaner.dll",
        typeFullName: "DataCleaner.ETLTask, Cleaner",
        methodName: "Run",
        pluginParams: '{"batchSize":1000}',
        remark: "ETL清洗",
        status: "stopped",
        createTime: "2025-04-05",
        triggers: [
          {
            id: "tr3",
            name: "每小时",
            rule: "0 0 * * * ?",
            nextTime: "16:00:00",
            enabled: true,
            records: generateRecords(6),
          },
        ],
        records: generateRecords(5),
      },
    ],
  },
  monitor: {
    assemblyJobs: [],
  },
});

const expandedRows = ref(new Set());
const jobModalVisible = ref(false);
const triggerModalVisible = ref(false);
const recordModalVisible = ref(false);
const editingJob = ref(null);
const editingTrigger = ref(null);
const editingTriggerJobId = ref(null);
const currentRecords = ref([]);
const recordTitle = ref("");

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

const getJobId = (job) => job?.Id ?? job?.id;
const getJobName = (job) => job?.JobName ?? job?.name ?? "未命名任务";
const getTriggerId = (trigger) => trigger?.Id ?? trigger?.id;
const getTriggerName = (trigger) =>
  trigger?.TriggerName ?? trigger?.Name ?? trigger?.name ?? "未命名触发器";

const getTriggerList = (job) => {
  if (!job) return [];
  if (Array.isArray(job.Triggers)) return job.Triggers;
  if (Array.isArray(job.triggers)) return job.triggers;

  job.Triggers = [];
  return job.Triggers;
};

const currentAssemblyJobs = computed(() => {
  return tasksData.value[props.groupId]?.assemblyJobs || [];
});

const isRowExpanded = (id) => expandedRows.value.has(toEntityIdKey(id));

const getFieldValue = (source, ...keys) => {
  if (!source) return undefined;

  for (const key of keys) {
    if (Object.prototype.hasOwnProperty.call(source, key)) {
      return source[key];
    }
  }

  return undefined;
};

const formatBooleanText = (value) => {
  if (value === null || value === undefined || value === "") return "-";
  return value ? "是" : "否";
};

const formatNullableText = (value) => {
  if (value === null || value === undefined || value === "") return "-";
  return String(value);
};

const formatRetryDependentText = (retryOnFailure, value) => {
  if (retryOnFailure === false) return "-";
  return formatNullableText(value);
};

const formatDateTime = (value) => formatDateTimeToSeconds(value);

const formatTriggerTime = (value) => formatDateTimeToSeconds(value);

const formatTriggerWindow = (startValue, endValue) => {
  if (!startValue && !endValue) return "不过期（长期有效）";

  const start = formatTriggerTime(startValue);
  const end = formatTriggerTime(endValue);
  return `开始 ${start} / 结束 ${end}`;
};

const formatTriggerType = (value) => {
  if (value === null || value === undefined || value === "") return "-";

  const triggerTypeMap = {
    0: "CRON",
    1: "SIMPLE",
    2: "CALENDAR",
    3: "DAILY_TIME",
    CRON: "CRON",
    SIMPLE: "SIMPLE",
    CALENDAR: "CALENDAR",
    DAILY_TIME: "DAILY_TIME",
  };

  return triggerTypeMap[value] ?? String(value);
};

const formatMisfireStrategy = (value) => {
  if (value === null || value === undefined || value === "") return "-";

  const misfireMap = {
    0: "不处理",
    1: "DoNothing",
    2: "FireOnceNow",
  };

  return misfireMap[value] ?? String(value);
};

const normalizeViewerContent = (value) => {
  if (value === null || value === undefined) {
    return "";
  }

  if (typeof value === "string") {
    return value.trim() ? value : "";
  }

  if (
    typeof value === "number" ||
    typeof value === "boolean" ||
    Array.isArray(value) ||
    typeof value === "object"
  ) {
    try {
      return JSON.stringify(value);
    } catch {
      return String(value);
    }
  }

  return String(value);
};

const mapLocalRecordItem = (record, index) => {
  const status = record?.status || "success";
  const message = formatNullableText(record?.result);
  const isFailed = status === "failed";

  return {
    id: record?.id ?? `${Date.now()}-${index}`,
    time: formatNullableText(record?.time),
    status,
    statusText:
      record?.statusText ??
      (status === "running" ? "执行中" : status === "failed" ? "失败" : "成功"),
    duration: formatNullableText(record?.duration),
    reasonCode: "-",
    reasonCodeRaw: "",
    reasonMessage: "-",
    reasonMessageRaw: "",
    returnValue: isFailed ? "-" : message,
    returnValueRaw: isFailed ? "" : normalizeViewerContent(record?.result),
    executionContextJson: "-",
    executionContextJsonRaw: "",
    exceptionType: "-",
    exceptionTypeRaw: "",
    exception: isFailed ? message : "-",
    exceptionRaw: isFailed ? normalizeViewerContent(record?.result) : "",
    stackTrace: "-",
    stackTraceRaw: "",
  };
};

const toggleExpand = (id) => {
  const rowKey = toEntityIdKey(id);
  if (!rowKey) {
    return;
  }

  const next = new Set(expandedRows.value);
  if (next.has(rowKey)) {
    next.delete(rowKey);
  } else {
    next.add(rowKey);
  }
  expandedRows.value = next;
};

const toggleJobStatus = (job) => {
  job.status = job.status === "running" ? "stopped" : "running";
  showToast(`任务 ${job.name} 已${job.status === "running" ? "启动" : "暂停"}`);
};

const openCreateJobModal = () => {
  editingJob.value = null;
  jobModalVisible.value = true;
};

const openEditJobModal = (job) => {
  editingJob.value = { ...job };
  jobModalVisible.value = true;
};

const closeJobModal = () => {
  jobModalVisible.value = false;
  editingJob.value = null;
};

const handleJobSave = (data) => {
  const editingJobId = getJobId(editingJob.value);

  if (toEntityIdKey(editingJobId)) {
    const idx = currentAssemblyJobs.value.findIndex((job) =>
      isSameEntityId(getJobId(job), editingJobId),
    );

    if (idx !== -1) {
      currentAssemblyJobs.value[idx] = { ...data, id: editingJobId };
      showToast(`任务 ${data.name} 更新成功`);
    }
  } else {
    currentAssemblyJobs.value.push({
      ...data,
      id: Date.now().toString(),
      triggers: [],
      records: [],
    });
    showToast(`任务 ${data.name} 创建成功`);
  }

  closeJobModal();
};

const openCreateTriggerModal = (jobId) => {
  editingTriggerJobId.value = jobId;
  editingTrigger.value = null;
  triggerModalVisible.value = true;
};

const openEditTriggerModal = (jobId, trigger) => {
  editingTriggerJobId.value = jobId;
  editingTrigger.value = { ...trigger };
  triggerModalVisible.value = true;
};

const closeTriggerModal = () => {
  triggerModalVisible.value = false;
  editingTrigger.value = null;
  editingTriggerJobId.value = null;
};

const handleTriggerSave = (data) => {
  const job = currentAssemblyJobs.value.find((item) =>
    isSameEntityId(getJobId(item), data.JobId),
  );
  const triggerList = getTriggerList(job);
  const triggerId = data.Id ?? data.id;

  if (job) {
    if (triggerId) {
      const idx = triggerList.findIndex((item) =>
        isSameEntityId(getTriggerId(item), triggerId),
      );
      if (idx !== -1) {
        triggerList[idx] = { ...triggerList[idx], ...data };
        showToast(`触发器 ${data.TriggerName} 更新成功`);
      }
    } else {
      triggerList.push({
        ...data,
        Id: Date.now(),
        id: Date.now().toString(),
        EnableStatus: 1,
        enabled: true,
        records: [],
      });
      showToast(`触发器 ${data.TriggerName} 创建成功`);
    }
  }

  closeTriggerModal();
};

const toggleTriggerStatus = async (jobId, triggerId) => {
  const job = currentAssemblyJobs.value.find((item) =>
    isSameEntityId(getJobId(item), jobId),
  );
  const trigger = getTriggerList(job).find((item) =>
    isSameEntityId(getTriggerId(item), triggerId),
  );
  if (!trigger) return;

  const nextEnabled = !(trigger.EnableStatus === 1 || trigger.enabled);
  trigger.enabled = nextEnabled;
  trigger.EnableStatus = nextEnabled ? 1 : 0;
  showToast(
    `触发器 ${getTriggerName(trigger)} 已${nextEnabled ? "启用" : "禁用"}`,
  );
};

const deleteTriggerItem = async (jobId, trigger) => {
  const job = currentAssemblyJobs.value.find((item) =>
    isSameEntityId(getJobId(item), jobId),
  );
  const triggerList = getTriggerList(job);
  const index = triggerList.findIndex((item) =>
    isSameEntityId(getTriggerId(item), getTriggerId(trigger)),
  );

  if (index === -1) {
    return;
  }

  triggerList.splice(index, 1);
  showToast(`触发器 ${getTriggerName(trigger)} 已删除`);
};

const runingTrigger = (trigger) => {
  showToast(`插件任务触发器「${getTriggerName(trigger)}」暂未接入立即执行`, "error");
};

const openLocalRecordModal = (title, records) => {
  recordTitle.value = title;
  currentRecords.value = Array.isArray(records)
    ? records.map(mapLocalRecordItem)
    : [];
  recordModalVisible.value = true;
};

const viewJobRecords = (job) => {
  openLocalRecordModal(`任务「${getJobName(job)}」`, job?.records ?? []);
};

const viewTriggerRecords = (jobId, trigger) => {
  openLocalRecordModal(
    `触发器「${getTriggerName(trigger)}」`,
    trigger?.records ?? [],
  );
};

const closeRecordModal = () => {
  recordModalVisible.value = false;
  recordTitle.value = "";
  currentRecords.value = [];
};

watch(
  currentAssemblyJobs,
  (jobs) => {
    emit("count-change", Array.isArray(jobs) ? jobs.length : 0);
  },
  { immediate: true },
);

watch(
  () => props.groupId,
  () => {
    expandedRows.value = new Set();
    closeJobModal();
    closeTriggerModal();
    closeRecordModal();
  },
);

defineExpose({
  openCreateJobModal,
});
</script>
