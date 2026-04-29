import { reactive } from "vue";
import { getTriggerPageList } from "../utils/triggerApi.js";

function createTriggerPageState() {
  return {
    items: [],
    totalCount: 0,
    pageIndex: 1,
    pageSize: 50,
    totalPages: 0,
    loading: false,
    loaded: false,
    error: "",
  };
}

export function useHttpTriggerPages(fetchPage = getTriggerPageList) {
  const triggerPages = reactive({});

  function ensureTriggerPage(jobId) {
    if (!triggerPages[jobId]) {
      triggerPages[jobId] = createTriggerPageState();
    }

    return triggerPages[jobId];
  }

  function getTriggerPage(jobId) {
    return ensureTriggerPage(jobId);
  }

  async function loadTriggerPage(jobId, overrides = {}, options = {}) {
    const page = ensureTriggerPage(jobId);
    const force = options.force === true;

    if (page.loaded && !force && Object.keys(overrides).length === 0) {
      return page;
    }

    page.loading = true;
    page.error = "";

    const query = {
      JobId: jobId,
      PageIndex: overrides.PageIndex ?? page.pageIndex ?? 1,
      PageSize: overrides.PageSize ?? page.pageSize ?? 50,
    };

    try {
      const result = await fetchPage(query);
      page.items = result.Items || [];
      page.totalCount = result.TotalCount || 0;
      page.pageIndex = result.PageIndex || query.PageIndex;
      page.pageSize = result.PageSize || query.PageSize;
      page.totalPages = result.TotalPages || 0;
      page.loaded = true;
      return page;
    } catch (error) {
      page.error = error.message || "加载触发器失败";
      throw error;
    } finally {
      page.loading = false;
    }
  }

  async function refreshTriggerPage(jobId) {
    const page = ensureTriggerPage(jobId);
    return await loadTriggerPage(
      jobId,
      { PageIndex: page.pageIndex },
      { force: true },
    );
  }

  return {
    triggerPages,
    ensureTriggerPage,
    getTriggerPage,
    loadTriggerPage,
    refreshTriggerPage,
  };
}
