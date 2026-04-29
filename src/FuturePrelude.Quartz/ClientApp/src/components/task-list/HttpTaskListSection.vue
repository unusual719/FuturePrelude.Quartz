<template>
  <div class="task-table-wrapper">
    <div v-if="httpJobsLoading" class="loading-state">
      <i class="fas fa-spinner fa-spin"></i> 加载中...
    </div>
    <table class="task-table" v-else>
      <thead>
        <tr>
          <th style="width: 32px"></th>
          <th>任务名称</th>
          <th>请求方式</th>
          <th>请求地址</th>
          <th>超时（s）</th>
          <th>认证</th>
          <th>是否并发执行</th>
          <th>失败是否重试</th>
          <th>最大重试次数</th>
          <th>重试退避（s）</th>
          <th>启用/禁用</th>
          <th>状态</th>
          <th>上次运行时间</th>
          <th>上次运行耗时(ms)</th>
          <th>创建时间</th>
          <th>备注</th>
          <th style="width: 140px">操作</th>
        </tr>
      </thead>
      <tbody>
        <template v-for="job in currentHttpJobs" :key="job.Id">
          <tr class="task-row" @click.stop="toggleExpand(getJobId(job))">
            <td>
              <i
                class="fas fa-chevron-right expand-icon"
                :class="{ 'rotate-icon': isRowExpanded(getJobId(job)) }"
                @click.stop="toggleExpand(getJobId(job))"
              ></i>
            </td>
            <td>
              <strong>{{ job.JobName }}</strong>
            </td>
            <td>
              <span
                class="method-tag"
                :class="'method-' + (job.HttpConfig?.RequestMethod || 'GET')"
              >
                {{ job.HttpConfig?.RequestMethod || "GET" }}
              </span>
            </td>
            <td class="url-cell" :title="job.HttpConfig?.RequestUrl">
              {{ job.HttpConfig?.RequestUrl || "-" }}
            </td>
            <td>{{ job.HttpConfig?.TimeoutSeconds || 0 }}</td>
            <td>{{ formatAuthType(job.HttpConfig?.AuthType) }}</td>
            <td>
              {{
                formatBooleanText(
                  getFieldValue(
                    job,
                    "DisallowConcurrent",
                    "disallowConcurrent",
                  ),
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
              <div
                class="toggle-switch"
                :class="{ active: job.EnableStatus === 1 }"
                @click.stop="toggleJobEnableStatus(job)"
                :title="job.EnableStatus === 1 ? '点击禁用' : '点击启用'"
              ></div>
            </td>
            <td>
              <span
                class="job-runtime-badge"
                :class="getHttpJobStatusClass(job.Id)"
              >
                <i :class="getHttpJobStatusIconClass(job.Id)"></i>
                {{ getHttpJobStatusText(job.Id) }}
              </span>
            </td>
            <td class="time-cell">
              {{
                formatDateTime(getFieldValue(job, "LastRunTime", "lastRunTime"))
              }}
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
              {{ job.Description || "-" }}
            </td>
            <td>
              <button
                class="icon-btn"
                v-if="job.JobStatus === 2"
                @click.stop="resumeJob(job)"
                title="恢复任务"
              >
                <i class="fas fa-play"></i>
              </button>
              <button
                class="icon-btn"
                v-if="job.JobStatus === 0"
                @click.stop="pauseJob(job)"
                title="暂停任务"
              >
                <i class="fas fa-pause"></i>
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
                @click.stop="deleteJob(job)"
                title="删除"
              >
                <i class="fas fa-trash"></i>
              </button>
            </td>
          </tr>
          <tr v-if="isRowExpanded(getJobId(job))" class="task-detail-row">
            <td colspan="17" class="task-detail-cell">
              <div class="detail-container">
                <div class="trigger-header">
                  <span>
                    <i class="fas fa-bolt"></i> 触发器列表 ({{
                      getHttpTriggerTotalCount(job.Id)
                    }})
                  </span>
                  <button
                    class="icon-btn"
                    @click.stop="openCreateTriggerModal(job.Id)"
                    title="添加触发器"
                  >
                    <i class="fas fa-plus-circle"></i>
                  </button>
                </div>
                <div class="trigger-cards">
                  <div
                    v-if="getHttpTriggerPageState(job.Id).loading"
                    class="loading-state"
                  >
                    <i class="fas fa-spinner fa-spin"></i> 加载触发器中...
                  </div>
                  <div
                    v-else-if="getHttpTriggerPageState(job.Id).error"
                    class="trigger-empty"
                  >
                    <span>{{ getHttpTriggerPageState(job.Id).error }}</span>
                    <button
                      class="pagination-btn"
                      @click.stop="retryHttpTriggerPage(job.Id)"
                    >
                      重试
                    </button>
                  </div>
                  <template v-else>
                    <div
                      v-for="trigger in getHttpTriggerItems(job.Id)"
                      :key="trigger.Id"
                      class="trigger-item"
                    >
                      <div class="trigger-info">
                        <div class="trigger-name">
                          <strong>{{
                            trigger.TriggerName || trigger.Name || trigger.name
                          }}</strong>
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
                            <em>上一次运行时间</em>
                            <strong>{{
                              formatTriggerTime(trigger.PrevFireTimeUtc)
                            }}</strong>
                          </span>
                          <span class="trigger-meta-segment">
                            <em>下次运行时间</em>
                            <strong>{{
                              trigger.TriggerState == 0
                                ? formatTriggerTime(
                                    trigger.NextFireTimeUtc ??
                                      trigger.NextFireTime ??
                                      trigger.nextTime,
                                  )
                                : "-"
                            }}</strong>
                          </span>
                          <span class="trigger-meta-segment">
                            <em>时间窗口</em>
                            <strong>{{
                              formatTriggerWindow(
                                trigger.StartTimeUtc,
                                trigger.EndTimeUtc,
                              )
                            }}</strong>
                          </span>
                          <span class="trigger-meta-segment">
                            <em>触发器类型</em>
                            <strong>{{
                              formatTriggerType(
                                trigger.TriggerType ?? trigger.triggerType,
                              )
                            }}</strong>
                          </span>
                          <span class="trigger-meta-segment">
                            <em>Misfire 策略</em>
                            <strong>{{
                              formatMisfireStrategy(
                                trigger.MisfireStrategy ??
                                  trigger.misfireStrategy,
                              )
                            }}</strong>
                          </span>
                          <span class="trigger-meta-segment">
                            <em>优先级</em>
                            <strong>{{
                              trigger.Priority ?? trigger.priority ?? "-"
                            }}</strong>
                          </span>
                          <span class="trigger-meta-segment">
                            <em>状态</em>
                            <strong>
                              <span
                                class="trigger-state-badge"
                                :class="getHttpTriggerStatusClass(trigger)"
                                :title="
                                  getHttpTriggerStatusDescription(trigger)
                                "
                              >
                                <i
                                  :class="
                                    getHttpTriggerStatusIconClass(trigger)
                                  "
                                ></i>
                                {{ getHttpTriggerStatusText(trigger) }}
                              </span>
                            </strong>
                          </span>
                        </div>
                      </div>
                      <div class="trigger-actions">
                        <button
                          class="icon-btn"
                          @click.stop="runingTrigger(trigger)"
                          title="立即运行"
                        >
                          <i class="fas fa-play"></i>
                        </button>
                        <button
                          class="icon-btn"
                          @click.stop="openEditTriggerModal(job.Id, trigger)"
                          title="编辑"
                        >
                          <i class="fas fa-edit"></i>
                        </button>
                        <button
                          class="icon-btn"
                          @click.stop="viewTriggerRecords(job.Id, trigger)"
                          title="执行记录"
                        >
                          <i class="fas fa-history"></i>
                        </button>
                        <button
                          class="icon-btn"
                          @click.stop="deleteTriggerItem(job.Id, trigger)"
                          title="删除"
                        >
                          <i class="fas fa-trash"></i>
                        </button>
                        <div
                          class="toggle-switch"
                          :title="
                            trigger.EnableStatus === 1 ? '点击禁用' : '点击启用'
                          "
                          :data-testid="`http-trigger-toggle-${trigger.Id}`"
                          :class="{ active: trigger.EnableStatus === 1 }"
                          @click.stop="toggleTriggerStatus(job.Id, trigger)"
                        ></div>
                      </div>
                    </div>
                    <div
                      v-if="getHttpTriggerItems(job.Id).length === 0"
                      class="trigger-empty"
                    >
                      暂无触发器
                    </div>
                    <div
                      v-if="shouldShowHttpTriggerPagination(job.Id)"
                      class="trigger-pagination"
                    >
                      <span class="pagination-info">
                        共
                        {{ getHttpTriggerPageState(job.Id).totalCount }} 条，第
                        {{ getHttpTriggerPageState(job.Id).pageIndex }}/{{
                          getHttpTriggerPageState(job.Id).totalPages
                        }}
                        页
                      </span>
                      <div class="pagination-controls">
                        <button
                          class="pagination-btn"
                          :disabled="
                            getHttpTriggerPageState(job.Id).pageIndex <= 1
                          "
                          @click.stop="prevHttpTriggerPage(job.Id)"
                        >
                          <i class="fas fa-chevron-left"></i> 上一页
                        </button>
                        <button
                          class="pagination-btn"
                          :disabled="
                            getHttpTriggerPageState(job.Id).pageIndex >=
                            getHttpTriggerPageState(job.Id).totalPages
                          "
                          @click.stop="nextHttpTriggerPage(job.Id)"
                        >
                          下一页 <i class="fas fa-chevron-right"></i>
                        </button>
                      </div>
                    </div>
                  </template>
                </div>
              </div>
            </td>
          </tr>
        </template>
        <tr v-if="currentHttpJobs.length === 0">
          <td colspan="17" class="empty-cell">暂无 HTTP API 任务</td>
        </tr>
      </tbody>
    </table>

    <div class="pagination" v-if="httpJobsData.totalCount > 0">
      <span class="pagination-info">
        共 {{ httpJobsData.totalCount }} 条，第 {{ httpJobsData.pageIndex }}/{{
          totalPages
        }}
        页
      </span>
      <div class="pagination-controls">
        <button
          class="pagination-btn"
          :disabled="!hasPrevPage"
          @click="prevPage"
        >
          <i class="fas fa-chevron-left"></i> 上一页
        </button>
        <button
          v-for="page in visiblePages"
          :key="page"
          class="pagination-btn"
          :class="{ active: page === httpJobsData.pageIndex }"
          @click="goToPage(page)"
        >
          {{ page }}
        </button>
        <button
          class="pagination-btn"
          :disabled="!hasNextPage"
          @click="nextPage"
        >
          下一页 <i class="fas fa-chevron-right"></i>
        </button>
      </div>
    </div>

    <HttpJobModal
      :visible="jobModalVisible"
      :job="editingJob"
      :group-id="props.groupId"
      @close="closeJobModal"
      @save="handleHttpJobSave"
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
      :loading="recordLoading"
      :page-index="recordPage.pageIndex"
      :total-pages="recordPage.totalPages || 1"
      :total-count="recordPage.totalCount"
      :has-prev-page="recordPage.pageIndex > 1"
      :has-next-page="recordPage.pageIndex < recordPage.totalPages"
      @prev-page="prevRecordPage"
      @next-page="nextRecordPage"
      @close="closeRecordModal"
    />
  </div>
</template>

<script setup>
import { computed, onBeforeUnmount, ref, watch } from "vue";
import { formatDateTimeToSeconds } from "../../utils/dateTime.js";
import { showToast } from "../../utils/toast.js";
import { showConfirm } from "../../utils/confirm.js";
import {
  getJobDetailPageList,
  getJobDetailById,
  getJobDetailStatuses,
  createJobDetail,
  updateJobDetail,
  deleteJobDetail,
  enableJobDetail,
  disableJobDetail,
  pauseJobDetail,
  resumeJobDetail,
} from "../../utils/jobDetailApi.js";
import { getJobExecutionLogPageList } from "../../utils/jobExecutionLogApi.js";
import {
  createTrigger,
  deleteTrigger,
  disableTrigger,
  enableTrigger,
  getTriggerPageList,
  getTriggerStatusList,
  triggerOnce,
  updateTrigger,
} from "../../utils/triggerApi.js";
import {
  applyTriggerStatuses,
  getTriggerStatusClass,
  getTriggerStatusIconClass,
  resolveTriggerStatusInfo,
} from "../../utils/triggerStatus.js";
import { useHttpTriggerPages } from "../../composables/useHttpTriggerPages.js";
import HttpJobModal from "../modals/HttpJobModal.vue";
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

const HTTP_JOB_STATUS_POLL_INTERVAL_MS = 10000;
const HTTP_TRIGGER_STATUS_POLL_INTERVAL_MS = 10000;
const RECORD_PAGE_SIZE = 10;

const expandedRows = ref(new Set());
const httpJobsData = ref({
  items: [],
  totalCount: 0,
  pageIndex: 1,
  pageSize: 20,
  totalPages: 0,
});
const httpJobsLoading = ref(false);
const jobStatusMap = ref({});
let httpJobStatusPollTimer = null;
let httpJobStatusRequestVersion = 0;
let httpTriggerStatusPollTimer = null;
let httpTriggerStatusRequestVersion = 0;

const currentHttpJobs = computed(() => httpJobsData.value.items);
const totalPages = computed(() => httpJobsData.value.totalPages);
const hasNextPage = computed(
  () => httpJobsData.value.pageIndex < totalPages.value,
);
const hasPrevPage = computed(() => httpJobsData.value.pageIndex > 1);

const jobModalVisible = ref(false);
const triggerModalVisible = ref(false);
const recordModalVisible = ref(false);
const editingJob = ref(null);
const editingTrigger = ref(null);
const editingTriggerJobId = ref(null);
const recordTitle = ref("");
const recordLoading = ref(false);
const currentRecords = ref([]);
const recordPage = ref({
  totalCount: 0,
  pageIndex: 1,
  pageSize: RECORD_PAGE_SIZE,
  totalPages: 0,
});
const recordQuery = ref({
  jobId: null,
  triggerId: null,
  pageIndex: 1,
  pageSize: RECORD_PAGE_SIZE,
});
let recordRequestVersion = 0;

const {
  getTriggerPage: getHttpTriggerPageState,
  loadTriggerPage: loadHttpTriggerPage,
  refreshTriggerPage: refreshHttpTriggerPage,
} = useHttpTriggerPages(getTriggerPageList);

const toEntityIdKey = (value) => {
  if (value === null || value === undefined || value === "") {
    return "";
  }

  return String(value);
};

const getJobId = (job) => job?.Id ?? job?.id;
const getJobName = (job) => job?.JobName ?? job?.name ?? "未命名任务";
const getTriggerId = (trigger) => trigger?.Id ?? trigger?.id;
const getTriggerName = (trigger) =>
  trigger?.TriggerName ?? trigger?.Name ?? trigger?.name ?? "未命名触发器";

const getFieldValue = (source, ...keys) => {
  if (!source) return undefined;

  for (const key of keys) {
    if (Object.prototype.hasOwnProperty.call(source, key)) {
      return source[key];
    }
  }

  return undefined;
};

const clearHttpJobStatusMap = () => {
  Object.keys(jobStatusMap.value).forEach((key) => {
    delete jobStatusMap.value[key];
  });
};

const clearHttpJobStatusPollTimer = () => {
  if (httpJobStatusPollTimer) {
    clearTimeout(httpJobStatusPollTimer);
    httpJobStatusPollTimer = null;
  }
};

const clearHttpTriggerStatusPollTimer = () => {
  if (httpTriggerStatusPollTimer) {
    clearTimeout(httpTriggerStatusPollTimer);
    httpTriggerStatusPollTimer = null;
  }
};

const invalidateHttpJobStatusRequests = () => {
  httpJobStatusRequestVersion += 1;
};

const invalidateHttpTriggerStatusRequests = () => {
  httpTriggerStatusRequestVersion += 1;
};

const invalidateRecordRequests = () => {
  recordRequestVersion += 1;
};

const isRowExpanded = (id) => expandedRows.value.has(toEntityIdKey(id));
const toJobStatusKey = (id) => toEntityIdKey(id);

const applyHttpJobStatuses = (ids, items) => {
  if (ids.length === 0) {
    clearHttpJobStatusMap();
    return;
  }

  const expectedKeys = new Set(ids.map((id) => toJobStatusKey(id)));
  Object.keys(jobStatusMap.value).forEach((key) => {
    if (!expectedKeys.has(key)) {
      delete jobStatusMap.value[key];
    }
  });

  ids.forEach((id) => {
    jobStatusMap.value[toJobStatusKey(id)] = "-";
  });

  for (const item of items) {
    const itemId = item?.id ?? item?.Id;
    const key = toJobStatusKey(itemId);
    if (!expectedKeys.has(key)) {
      continue;
    }

    jobStatusMap.value[key] = item?.status ?? item?.Status ?? "-";
  }
};

const resolveHttpTriggerStatusInfo = (source) =>
  resolveTriggerStatusInfo(source);

const collectActiveHttpTriggerItems = () => {
  const items = [];
  const seenTriggerIds = new Set();

  currentHttpJobs.value.forEach((job) => {
    const jobId = getJobId(job);
    if (!isRowExpanded(jobId)) {
      return;
    }

    const page = getHttpTriggerPageState(jobId);
    if (!page.loaded || !Array.isArray(page.items) || page.items.length === 0) {
      return;
    }

    page.items.forEach((trigger) => {
      const triggerIdKey = toEntityIdKey(getTriggerId(trigger));
      if (!triggerIdKey || seenTriggerIds.has(triggerIdKey)) {
        return;
      }

      seenTriggerIds.add(triggerIdKey);
      items.push(trigger);
    });
  });

  return items;
};

const applyHttpTriggerStatuses = (triggerItems, items) => {
  applyTriggerStatuses(triggerItems, items);
};

const scheduleHttpJobStatusPolling = () => {
  clearHttpJobStatusPollTimer();

  if (!props.active) {
    return;
  }

  httpJobStatusPollTimer = setTimeout(async () => {
    httpJobStatusPollTimer = null;
    await refreshHttpJobStatuses();
    scheduleHttpJobStatusPolling();
  }, HTTP_JOB_STATUS_POLL_INTERVAL_MS);
};

const scheduleHttpTriggerStatusPolling = () => {
  clearHttpTriggerStatusPollTimer();

  if (!props.active || collectActiveHttpTriggerItems().length === 0) {
    return;
  }

  httpTriggerStatusPollTimer = setTimeout(async () => {
    httpTriggerStatusPollTimer = null;
    await refreshHttpTriggerStatuses();
    scheduleHttpTriggerStatusPolling();
  }, HTTP_TRIGGER_STATUS_POLL_INTERVAL_MS);
};

const toggleExpand = async (id) => {
  const rowKey = toEntityIdKey(id);
  if (!rowKey) {
    return;
  }

  const next = new Set(expandedRows.value);
  const isOpening = !next.has(rowKey);

  if (isOpening) {
    next.add(rowKey);
  } else {
    next.delete(rowKey);
  }

  expandedRows.value = next;

  if (isOpening) {
    try {
      await loadHttpTriggerPage(id);
      if (props.active) {
        invalidateHttpTriggerStatusRequests();
        await refreshHttpTriggerStatuses();
        scheduleHttpTriggerStatusPolling();
      }
    } catch (error) {
      showToast("$加载触发器失败", "error");
    }
  } else if (props.active) {
    invalidateHttpTriggerStatusRequests();
    await refreshHttpTriggerStatuses();
    scheduleHttpTriggerStatusPolling();
  }
};

const loadHttpJobs = async () => {
  if (!toEntityIdKey(props.groupId)) {
    httpJobsData.value = {
      items: [],
      totalCount: 0,
      pageIndex: 1,
      pageSize: 20,
      totalPages: 0,
    };
    clearHttpJobStatusMap();
    return;
  }

  try {
    invalidateHttpJobStatusRequests();
    invalidateHttpTriggerStatusRequests();
    clearHttpJobStatusPollTimer();
    clearHttpTriggerStatusPollTimer();
    httpJobsLoading.value = true;

    const result = await getJobDetailPageList({
      groupId: props.groupId,
      jobType: 1,
      pageIndex: httpJobsData.value.pageIndex,
      pageSize: httpJobsData.value.pageSize,
    });

    httpJobsData.value = {
      items: result.Items || [],
      totalCount: result.TotalCount || 0,
      pageIndex: result.PageIndex || 1,
      pageSize: result.PageSize || 20,
      totalPages: result.TotalPages || 0,
    };

    if (props.active) {
      await refreshHttpJobStatuses();
      await refreshHttpTriggerStatuses();
      scheduleHttpJobStatusPolling();
      scheduleHttpTriggerStatusPolling();
    } else {
      clearHttpJobStatusMap();
    }
  } catch (error) {
    showToast("加载任务列表失败", "error");
    httpJobsData.value.items = [];
    clearHttpJobStatusMap();
    clearHttpJobStatusPollTimer();
    clearHttpTriggerStatusPollTimer();
  } finally {
    httpJobsLoading.value = false;
  }
};

const goToPage = (page) => {
  if (page < 1 || page > totalPages.value) {
    return;
  }

  httpJobsData.value.pageIndex = page;
  loadHttpJobs();
};

const nextPage = () => goToPage(httpJobsData.value.pageIndex + 1);
const prevPage = () => goToPage(httpJobsData.value.pageIndex - 1);

const visiblePages = computed(() => {
  const total = totalPages.value;
  const current = httpJobsData.value.pageIndex;

  if (total <= 5) {
    return Array.from({ length: total }, (_, i) => i + 1);
  }

  let start = Math.max(1, current - 2);
  let end = Math.min(total, start + 4);
  if (end - start < 4) {
    start = Math.max(1, end - 4);
  }

  return Array.from({ length: end - start + 1 }, (_, i) => start + i);
});

const getHttpTriggerItems = (jobId) => getHttpTriggerPageState(jobId).items;
const getHttpTriggerTotalCount = (jobId) =>
  getHttpTriggerPageState(jobId).totalCount;

const shouldShowHttpTriggerPagination = (jobId) => {
  const page = getHttpTriggerPageState(jobId);
  return page.totalCount > page.pageSize;
};

const retryHttpTriggerPage = async (jobId) => {
  try {
    await refreshHttpTriggerPage(jobId);
    if (props.active) {
      invalidateHttpTriggerStatusRequests();
      await refreshHttpTriggerStatuses();
      scheduleHttpTriggerStatusPolling();
    }
  } catch (error) {
    showToast(error.message || "加载触发器失败", "error");
  }
};

const goToHttpTriggerPage = async (jobId, pageIndex) => {
  const page = getHttpTriggerPageState(jobId);

  if (
    page.loading ||
    pageIndex < 1 ||
    (page.totalPages > 0 && pageIndex > page.totalPages)
  ) {
    return;
  }

  try {
    await loadHttpTriggerPage(jobId, { PageIndex: pageIndex }, { force: true });
    if (props.active) {
      invalidateHttpTriggerStatusRequests();
      await refreshHttpTriggerStatuses();
      scheduleHttpTriggerStatusPolling();
    }
  } catch (error) {
    showToast(error.message || "加载触发器失败", "error");
  }
};

const nextHttpTriggerPage = async (jobId) => {
  const page = getHttpTriggerPageState(jobId);
  await goToHttpTriggerPage(jobId, page.pageIndex + 1);
};

const prevHttpTriggerPage = async (jobId) => {
  const page = getHttpTriggerPageState(jobId);
  await goToHttpTriggerPage(jobId, page.pageIndex - 1);
};

const formatDateTime = (value) => formatDateTimeToSeconds(value);

const tryParseDate = (value) => {
  if (!value) return null;

  if (value instanceof Date) {
    return Number.isNaN(value.getTime()) ? null : value;
  }

  const text = String(value).trim();
  if (!text) return null;

  const candidates = [text, text.replace(" ", "T"), text.replace(/-/g, "/")];
  for (const candidate of candidates) {
    const date = new Date(candidate);
    if (!Number.isNaN(date.getTime())) {
      return date;
    }
  }

  return null;
};

const formatBooleanText = (value) => {
  if (value === null || value === undefined || value === "") return "-";
  return value ? "是" : "否";
};

const formatNullableText = (value) => {
  if (value === null || value === undefined || value === "") return "-";
  return String(value);
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

const formatDateTimeWithSeconds = (value) => {
  const date = tryParseDate(value);
  if (!date) return formatNullableText(value);

  return date.toLocaleString("zh-CN", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    hour12: false,
  });
};

const formatDurationText = (durationMs) => {
  if (durationMs === null || durationMs === undefined || durationMs === "") {
    return "-";
  }

  const value = Number(durationMs);
  if (!Number.isFinite(value)) {
    return "-";
  }

  if (value < 1000) {
    return `${Math.round(value)} ms`;
  }

  return `${(value / 1000).toFixed(2)} s`;
};

const normalizeExecutionRecordStatus = (resultText, isRunning) => {
  const text = formatNullableText(resultText);

  if (
    isRunning ||
    text.includes("执行中") ||
    text.includes("等待") ||
    text.includes("重试")
  ) {
    return { key: "running", text: isRunning ? "执行中" : text };
  }

  if (text.includes("取消") || text.toLowerCase().includes("cancel")) {
    return { key: "cancelled", text };
  }

  if (text.includes("成功")) {
    return { key: "success", text };
  }

  if (text.includes("失败") || text.includes("超时")) {
    return { key: "failed", text };
  }

  return { key: "running", text };
};

const formatRetryDependentText = (retryOnFailure, value) => {
  if (retryOnFailure === false) return "-";
  return formatNullableText(value);
};

const formatAuthType = (authType) => {
  if (!authType && authType !== 0) return "无认证";
  const authMap = { 0: "无认证", 1: "Basic", 2: "Bearer", 3: "APIKey" };
  return authMap[authType] || "无认证";
};

const getHttpJobStatusText = (jobId) =>
  jobStatusMap.value[toJobStatusKey(jobId)] || "-";

const getHttpJobStatusClass = (jobId) => {
  const status = getHttpJobStatusText(jobId);

  if (status.includes("运行")) return "job-runtime-running";
  if (status.includes("暂停")) return "job-runtime-paused";
  if (status.includes("空闲")) return "job-runtime-idle";

  return "job-runtime-unknown";
};

const getHttpJobStatusIconClass = (jobId) => {
  const status = getHttpJobStatusText(jobId);

  if (status.includes("运行")) {
    return "fas fa-spinner job-runtime-icon job-runtime-icon-spinning";
  }

  if (status.includes("暂停")) {
    return "fas fa-stop job-runtime-icon";
  }

  if (status.includes("空闲")) {
    return "fas fa-circle job-runtime-icon";
  }

  return "fas fa-circle job-runtime-icon";
};

const getHttpTriggerStatusInfo = (trigger) =>
  resolveHttpTriggerStatusInfo(trigger);
const getHttpTriggerStatusText = (trigger) =>
  getHttpTriggerStatusInfo(trigger).status;
const getHttpTriggerStatusDescription = (trigger) =>
  getHttpTriggerStatusInfo(trigger).description;
const getHttpTriggerStatusClass = (trigger) => getTriggerStatusClass(trigger);
const getHttpTriggerStatusIconClass = (trigger) =>
  getTriggerStatusIconClass(trigger);

const resetRecordPage = () => {
  currentRecords.value = [];
  recordPage.value = {
    totalCount: 0,
    pageIndex: 1,
    pageSize: RECORD_PAGE_SIZE,
    totalPages: 0,
  };
};

const toRecordNumericId = (value) => {
  if (value === null || value === undefined || value === "") {
    return null;
  }

  const parsed = Number(value);
  if (!Number.isFinite(parsed) || parsed <= 0) {
    return null;
  }

  return parsed;
};

const mapExecutionRecordItem = (item, index) => {
  const isRunning = Boolean(item?.IsRunning ?? item?.isRunning);
  const resultText =
    item?.ResultText ?? item?.resultText ?? item?.Result ?? item?.result ?? "-";
  const status = normalizeExecutionRecordStatus(resultText, isRunning);
  const displayMessageRaw =
    item?.DisplayMessage ??
    item?.displayMessage ??
    item?.ReturnValue ??
    item?.returnValue ??
    item?.Exception ??
    item?.exception ??
    item?.ReasonMessage ??
    item?.reasonMessage;
  const reasonCodeRaw = item?.ReasonCode ?? item?.reasonCode;
  const reasonMessageRaw = item?.ReasonMessage ?? item?.reasonMessage;
  const returnValueRaw = item?.ReturnValue ?? item?.returnValue;
  const executionContextJsonRaw =
    item?.ExecutionContextJson ?? item?.executionContextJson;
  const exceptionTypeRaw = item?.ExceptionType ?? item?.exceptionType;
  const exceptionRaw = item?.Exception ?? item?.exception;
  const stackTraceRaw = item?.StackTrace ?? item?.stackTrace;

  return {
    id: item?.Id ?? item?.id ?? `${Date.now()}-${index}`,
    time: formatDateTimeWithSeconds(
      item?.DisplayTimeUtc ??
        item?.displayTimeUtc ??
        item?.EndTimeUtc ??
        item?.endTimeUtc ??
        item?.StartTimeUtc ??
        item?.startTimeUtc ??
        item?.CreateTime ??
        item?.createTime,
    ),
    status: status.key,
    statusText: status.text,
    duration: formatDurationText(item?.DurationMs ?? item?.durationMs),
    result: formatNullableText(displayMessageRaw),
    resultRaw: normalizeViewerContent(displayMessageRaw),
    reasonCode: formatNullableText(reasonCodeRaw),
    reasonCodeRaw: normalizeViewerContent(reasonCodeRaw),
    reasonMessage: formatNullableText(reasonMessageRaw),
    reasonMessageRaw: normalizeViewerContent(reasonMessageRaw),
    returnValue: formatNullableText(returnValueRaw),
    returnValueRaw: normalizeViewerContent(returnValueRaw),
    executionContextJson: formatNullableText(executionContextJsonRaw),
    executionContextJsonRaw: normalizeViewerContent(executionContextJsonRaw),
    exceptionType: formatNullableText(exceptionTypeRaw),
    exceptionTypeRaw: normalizeViewerContent(exceptionTypeRaw),
    exception: formatNullableText(exceptionRaw),
    exceptionRaw: normalizeViewerContent(exceptionRaw),
    stackTrace: formatNullableText(stackTraceRaw),
    stackTraceRaw: normalizeViewerContent(stackTraceRaw),
  };
};

const applyRecordPageResult = (result) => {
  const items = Array.isArray(result?.Items)
    ? result.Items
    : Array.isArray(result?.items)
      ? result.items
      : [];
  const pageIndex = Number(result?.PageIndex ?? result?.pageIndex ?? 1) || 1;
  const pageSize =
    Number(result?.PageSize ?? result?.pageSize ?? RECORD_PAGE_SIZE) ||
    RECORD_PAGE_SIZE;
  const totalCount =
    Number(result?.TotalCount ?? result?.totalCount ?? items.length) || 0;
  const totalPagesValue =
    Number(result?.TotalPages ?? result?.totalPages ?? 0) ||
    (pageSize > 0 ? Math.ceil(totalCount / pageSize) : 0);

  currentRecords.value = items.map(mapExecutionRecordItem);
  recordPage.value = {
    totalCount,
    pageIndex,
    pageSize,
    totalPages: totalPagesValue,
  };
  recordQuery.value.pageIndex = pageIndex;
  recordQuery.value.pageSize = pageSize;
};

const loadRecordPage = async (pageIndex = 1) => {
  const jobId = recordQuery.value.jobId;
  const triggerId = recordQuery.value.triggerId;

  if (!jobId) {
    resetRecordPage();
    return;
  }

  const requestVersion = ++recordRequestVersion;

  try {
    recordLoading.value = true;
    const result = await getJobExecutionLogPageList({
      jobId,
      triggerId,
      pageIndex,
      pageSize: recordQuery.value.pageSize,
    });

    if (requestVersion !== recordRequestVersion || !recordModalVisible.value) {
      return;
    }

    applyRecordPageResult(result);
  } catch (error) {
    if (requestVersion !== recordRequestVersion || !recordModalVisible.value) {
      return;
    }

    resetRecordPage();
    showToast(error.message || "加载执行记录失败", "error");
  } finally {
    if (requestVersion === recordRequestVersion) {
      recordLoading.value = false;
    }
  }
};

const openRecordModal = async ({ title, jobId, triggerId = null }) => {
  recordTitle.value = title;
  recordModalVisible.value = true;
  recordLoading.value = false;
  invalidateRecordRequests();
  resetRecordPage();

  recordQuery.value = {
    jobId: toRecordNumericId(jobId),
    triggerId: toRecordNumericId(triggerId),
    pageIndex: 1,
    pageSize: RECORD_PAGE_SIZE,
  };

  if (!recordQuery.value.jobId) {
    return;
  }

  await loadRecordPage(1);
};

const closeRecordModal = () => {
  invalidateRecordRequests();
  recordModalVisible.value = false;
  recordTitle.value = "";
  recordLoading.value = false;
  resetRecordPage();
  recordQuery.value = {
    jobId: null,
    triggerId: null,
    pageIndex: 1,
    pageSize: RECORD_PAGE_SIZE,
  };
};

const prevRecordPage = async () => {
  if (recordLoading.value || recordPage.value.pageIndex <= 1) {
    return;
  }

  await loadRecordPage(recordPage.value.pageIndex - 1);
};

const nextRecordPage = async () => {
  if (
    recordLoading.value ||
    recordPage.value.pageIndex >= recordPage.value.totalPages
  ) {
    return;
  }

  await loadRecordPage(recordPage.value.pageIndex + 1);
};

const refreshHttpJobStatuses = async () => {
  if (!props.active) {
    return;
  }

  const ids = currentHttpJobs.value
    .map((job) => getJobId(job))
    .filter((id) => toEntityIdKey(id));

  if (ids.length === 0) {
    clearHttpJobStatusMap();
    return;
  }

  const requestVersion = ++httpJobStatusRequestVersion;

  try {
    const result = await getJobDetailStatuses(ids);

    if (requestVersion !== httpJobStatusRequestVersion || !props.active) {
      return;
    }

    applyHttpJobStatuses(ids, Array.isArray(result) ? result : []);
  } catch {}
};

const refreshHttpTriggerStatuses = async () => {
  if (!props.active) {
    return;
  }

  const triggerItems = collectActiveHttpTriggerItems();
  const triggerIds = triggerItems
    .map((trigger) => getTriggerId(trigger))
    .filter((id) => toEntityIdKey(id));

  if (triggerIds.length === 0) {
    return;
  }

  const requestVersion = ++httpTriggerStatusRequestVersion;

  try {
    const result = await getTriggerStatusList(triggerIds);

    if (requestVersion !== httpTriggerStatusRequestVersion || !props.active) {
      return;
    }

    applyHttpTriggerStatuses(triggerItems, Array.isArray(result) ? result : []);
  } catch {}
};

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

const toggleJobEnableStatus = async (job) => {
  const confirmed = await showConfirm(
    `确定要${job.EnableStatus === 1 ? "禁用" : "启用"}任务「${job.JobName}」吗？`,
    "提示",
  );
  if (!confirmed) return;

  try {
    if (job.EnableStatus === 1) {
      await disableJobDetail(job.Id);
      showToast(`任务「${job.JobName}」已禁用`);
    } else {
      await enableJobDetail(job.Id);
      showToast(`任务「${job.JobName}」已启用`);
    }

    await loadHttpJobs();
  } catch (error) {
    showToast(error.message || "操作失败", "error");
  }
};

const pauseJob = async (job) => {
  const confirmed = await showConfirm(
    `确定要暂停任务「${job.JobName}」吗？`,
    "提示",
  );
  if (!confirmed) return;

  try {
    await pauseJobDetail(job.Id);
    showToast(`任务「${job.JobName}」已暂停`);
    await loadHttpJobs();
  } catch (error) {
    showToast(error.message || "暂停任务失败", "error");
  }
};

const resumeJob = async (job) => {
  const confirmed = await showConfirm(
    `确定要恢复任务「${job.JobName}」吗？`,
    "提示",
  );
  if (!confirmed) return;

  try {
    await resumeJobDetail(job.Id);
    showToast(`任务「${job.JobName}」已恢复`);
    await loadHttpJobs();
  } catch (error) {
    showToast(error.message || "恢复任务失败", "error");
  }
};

const deleteJob = async (job) => {
  const confirmed = await showConfirm(
    `确定要删除任务「${job.JobName}」吗？`,
    "警告",
    "danger",
  );
  if (!confirmed) return;

  try {
    await deleteJobDetail(job.Id);
    showToast(`任务「${job.JobName}」已删除`);
    await loadHttpJobs();
  } catch (error) {
    showToast(error.message || "删除失败", "error");
  }
};

const openCreateJobModal = () => {
  editingJob.value = null;
  jobModalVisible.value = true;
};

const openEditJobModal = async (job) => {
  editingJob.value = { ...job };
  jobModalVisible.value = true;

  const jobId = getJobId(job);
  if (!jobId) {
    return;
  }

  try {
    const detail = await getJobDetailById(jobId);
    const currentEditingJobId = getJobId(editingJob.value);

    if (
      !jobModalVisible.value ||
      toEntityIdKey(currentEditingJobId) !== toEntityIdKey(jobId) ||
      !detail
    ) {
      return;
    }

    editingJob.value = detail;
  } catch (error) {
    showToast(error.message || "加载任务详情失败", "error");
  }
};

const closeJobModal = () => {
  jobModalVisible.value = false;
  editingJob.value = null;
};

const handleHttpJobSave = async (data) => {
  try {
    if (data.id) {
      await updateJobDetail(data);
      showToast(`任务「${data.jobName}」更新成功`);
    } else {
      await createJobDetail(data);
      showToast(`任务「${data.jobName}」创建成功`);
    }

    closeJobModal();
    await loadHttpJobs();
  } catch (error) {
    showToast(error.message || "保存任务失败", "error");
  }
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

const handleTriggerSave = async (data) => {
  try {
    if (data.Id) {
      await updateTrigger(data);
      showToast(`触发器 ${data.TriggerName} 更新成功`);
      closeTriggerModal();
      await refreshHttpTriggerPage(data.JobId);
    } else {
      await createTrigger(data);
      showToast(`触发器 ${data.TriggerName} 创建成功`);
      closeTriggerModal();
      await loadHttpTriggerPage(data.JobId, { PageIndex: 1 }, { force: true });
    }

    if (props.active) {
      invalidateHttpTriggerStatusRequests();
      await refreshHttpTriggerStatuses();
      scheduleHttpTriggerStatusPolling();
    }
  } catch (error) {
    showToast(error.message || "保存触发器失败", "error");
  }
};

const toggleTriggerStatus = async (jobId, trigger) => {
  const isEnabled = trigger.EnableStatus === 1;
  const confirmed = await showConfirm(
    `确定要${isEnabled ? "禁用" : "启用"}触发器「${getTriggerName(trigger)}」吗？`,
    "提示",
  );
  if (!confirmed) return;

  try {
    if (isEnabled) {
      await disableTrigger(trigger.Id);
      showToast(`触发器 ${getTriggerName(trigger)} 已禁用`);
    } else {
      await enableTrigger(trigger.Id);
      showToast(`触发器 ${getTriggerName(trigger)} 已启用`);
    }

    await refreshHttpTriggerPage(jobId);
    if (props.active) {
      invalidateHttpTriggerStatusRequests();
      await refreshHttpTriggerStatuses();
      scheduleHttpTriggerStatusPolling();
    }
  } catch (error) {
    showToast(error.message || "切换触发器状态失败", "error");
  }
};

const deleteHttpTrigger = async (jobId, trigger) => {
  const confirmed = await showConfirm(
    `确定要删除触发器「${getTriggerName(trigger)}」吗？`,
    "警告",
    "danger",
  );
  if (!confirmed) return;

  try {
    const pageState = getHttpTriggerPageState(jobId);
    await deleteTrigger(trigger.Id);
    showToast(`触发器 ${getTriggerName(trigger)} 已删除`);

    const isLastItemOnPage = pageState.items.length <= 1;
    const targetPageIndex =
      isLastItemOnPage && pageState.pageIndex > 1
        ? pageState.pageIndex - 1
        : pageState.pageIndex;

    await loadHttpTriggerPage(
      jobId,
      { PageIndex: targetPageIndex },
      { force: true },
    );

    if (props.active) {
      invalidateHttpTriggerStatusRequests();
      await refreshHttpTriggerStatuses();
      scheduleHttpTriggerStatusPolling();
    }
  } catch (error) {
    showToast(error.message || "删除触发器失败", "error");
  }
};

const deleteTriggerItem = async (jobId, trigger) => {
  await deleteHttpTrigger(jobId, trigger);
};

const runingTrigger = async (trigger) => {
  const triggerId = getTriggerId(trigger);
  const triggerName = getTriggerName(trigger);

  const confirmed = await showConfirm(
    `确定要立即执行触发器「${triggerName}」吗？`,
    "提示",
    "warning",
  );
  if (!confirmed) return;

  if (!triggerId) {
    showToast("触发器ID无效，无法立即执行", "error");
    return;
  }

  try {
    await triggerOnce(triggerId);
    showToast(`触发器 ${triggerName} 已提交立即执行`);
  } catch (error) {
    showToast(error.message || "立即执行触发器失败", "error");
  }
};

const viewJobRecords = async (job) => {
  await openRecordModal({
    title: `任务「${getJobName(job)}」`,
    jobId: getJobId(job),
  });
};

const viewTriggerRecords = async (jobId, trigger) => {
  await openRecordModal({
    title: `触发器「${getTriggerName(trigger)}」`,
    jobId,
    triggerId: getTriggerId(trigger),
  });
};

const refreshAfterSchedulerAction = async () => {
  if (!props.active) {
    return;
  }

  await refreshHttpJobStatuses();
  scheduleHttpJobStatusPolling();
};

watch(
  currentHttpJobs,
  (jobs) => {
    emit("count-change", Array.isArray(jobs) ? jobs.length : 0);
  },
  { immediate: true },
);

watch(
  () => props.groupId,
  async () => {
    expandedRows.value = new Set();
    httpJobsData.value.pageIndex = 1;
    invalidateHttpJobStatusRequests();
    invalidateHttpTriggerStatusRequests();
    clearHttpJobStatusPollTimer();
    clearHttpTriggerStatusPollTimer();
    clearHttpJobStatusMap();
    closeJobModal();
    closeTriggerModal();
    closeRecordModal();

    if (!toEntityIdKey(props.groupId)) {
      httpJobsData.value.items = [];
      return;
    }

    await loadHttpJobs();
  },
  { immediate: true },
);

watch(
  () => props.active,
  async (active) => {
    invalidateHttpJobStatusRequests();
    invalidateHttpTriggerStatusRequests();
    clearHttpJobStatusPollTimer();
    clearHttpTriggerStatusPollTimer();

    if (!active) {
      return;
    }

    await refreshHttpJobStatuses();
    await refreshHttpTriggerStatuses();
    scheduleHttpJobStatusPolling();
    scheduleHttpTriggerStatusPolling();
  },
  { immediate: true },
);

onBeforeUnmount(() => {
  invalidateHttpJobStatusRequests();
  invalidateHttpTriggerStatusRequests();
  clearHttpJobStatusPollTimer();
  clearHttpTriggerStatusPollTimer();
  invalidateRecordRequests();
});

defineExpose({
  openCreateJobModal,
  refreshAfterSchedulerAction,
  reload: loadHttpJobs,
});
</script>
