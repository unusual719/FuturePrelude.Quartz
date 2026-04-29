const TRIGGER_STATUS_META = {
  none: {
    key: "none",
    status: "未注册",
    description: "触发器未注册到调度器",
  },
  normal: {
    key: "normal",
    status: "正常",
    description: "触发器处于正常等待状态",
  },
  paused: {
    key: "paused",
    status: "暂停",
    description: "触发器暂停触发",
  },
  complete: {
    key: "complete",
    status: "已完成",
    description: "触发器已触发完毕，不再执行",
  },
  error: {
    key: "error",
    status: "错误",
    description: "触发器处于错误状态",
  },
  blocked: {
    key: "blocked",
    status: "阻塞",
    description: "触发器被阻塞，无法执行",
  },
  unknown: {
    key: "unknown",
    status: "未知",
    description: "未知状态",
  },
};

function toEntityIdKey(value) {
  if (value === null || value === undefined || value === "") {
    return "";
  }

  return String(value);
}

export function normalizeTriggerStateKey(value) {
  if (value === null || value === undefined || value === "") {
    return "";
  }

  const text = String(value).trim();
  if (!text) {
    return "";
  }

  const normalized = text.toLowerCase();
  const stateKeyMap = {
    0: "normal",
    1: "paused",
    2: "complete",
    3: "error",
    4: "blocked",
    5: "none",
    "-1": "unknown",
    none: "none",
    normal: "normal",
    paused: "paused",
    complete: "complete",
    error: "error",
    blocked: "blocked",
    未注册: "none",
    正常: "normal",
    暂停: "paused",
    已完成: "complete",
    错误: "error",
    阻塞: "blocked",
  };

  return stateKeyMap[normalized] || stateKeyMap[text] || "";
}

export function resolveTriggerStatusInfo(source) {
  const statusRaw =
    source?.TriggerStateName ??
    source?.triggerStateName ??
    source?.Status ??
    source?.status;
  const descriptionRaw =
    source?.TriggerStateDescription ??
    source?.triggerStateDescription ??
    source?.Description ??
    source?.description;
  const stateRaw = source?.TriggerState ?? source?.triggerState;
  const normalizedKey =
    normalizeTriggerStateKey(stateRaw) ||
    normalizeTriggerStateKey(statusRaw) ||
    "unknown";
  const meta = TRIGGER_STATUS_META[normalizedKey] || TRIGGER_STATUS_META.unknown;
  const statusText =
    statusRaw === null || statusRaw === undefined || statusRaw === ""
      ? meta.status
      : String(statusRaw);
  const descriptionText =
    descriptionRaw === null ||
    descriptionRaw === undefined ||
    descriptionRaw === ""
      ? meta.description
      : String(descriptionRaw);

  return {
    key: meta.key,
    state: stateRaw,
    status: statusText,
    description: descriptionText,
  };
}

export function applyTriggerStatuses(triggerItems, items) {
  if (!Array.isArray(triggerItems) || triggerItems.length === 0) {
    return;
  }

  const statusMap = new Map();
  (Array.isArray(items) ? items : []).forEach((item) => {
    const itemId = item?.id ?? item?.Id;
    const itemKey = toEntityIdKey(itemId);
    if (!itemKey) {
      return;
    }

    statusMap.set(itemKey, {
      ...resolveTriggerStatusInfo(item),
      state: item?.TriggerState ?? item?.triggerState,
    });
  });

  triggerItems.forEach((trigger) => {
    const triggerIdKey = toEntityIdKey(trigger?.Id ?? trigger?.id);
    const nextStatus = statusMap.get(triggerIdKey);
    if (!nextStatus) {
      return;
    }

    if (nextStatus.state !== null && nextStatus.state !== undefined) {
      trigger.TriggerState = nextStatus.state;
      trigger.triggerState = nextStatus.state;
    }

    trigger.TriggerStateName = nextStatus.status;
    trigger.triggerStateName = nextStatus.status;
    trigger.TriggerStateDescription = nextStatus.description;
    trigger.triggerStateDescription = nextStatus.description;
  });
}

export function getTriggerStatusClass(trigger) {
  const { key } = resolveTriggerStatusInfo(trigger);

  if (key === "normal") return "trigger-state-normal";
  if (key === "paused") return "trigger-state-paused";
  if (key === "complete") return "trigger-state-complete";
  if (key === "error") return "trigger-state-error";
  if (key === "blocked") return "trigger-state-blocked";
  if (key === "none") return "trigger-state-none";

  return "trigger-state-unknown";
}

export function getTriggerStatusIconClass(trigger) {
  const { key } = resolveTriggerStatusInfo(trigger);

  if (key === "normal") return "fas fa-circle trigger-state-icon";
  if (key === "paused") return "fas fa-stop trigger-state-icon";
  if (key === "complete") return "fas fa-check-circle trigger-state-icon";
  if (key === "error") return "fas fa-times-circle trigger-state-icon";
  if (key === "blocked") return "fas fa-ban trigger-state-icon";
  if (key === "none") return "fas fa-minus-circle trigger-state-icon";

  return "fas fa-question-circle trigger-state-icon";
}
