import { CronExpressionParser } from "cron-parser";

const CRON_TRIGGER_TYPE = "CRON";
const DEFAULT_TIME_ZONE = "Asia/Shanghai";
const NEXT_FIRE_PLACEHOLDER = "保存后由系统计算";
const PREVIEW_BLANK = "填写 Cron 表达式后自动预览";
const PREVIEW_INVALID = "Cron 表达式无效";
const PREVIEW_EMPTY = "当前时间窗口内没有可执行时间";
const PREVIEW_LIMIT = 10;
const PREVIEW_DEBOUNCE_MS = 300;
const MIN_YEAR = 1970;
const MAX_YEAR = 2099;

const formatterCache = new Map();

export function createTriggerForm(jobId = null) {
    return {
        Id: null,
        JobId: jobId,
        TriggerName: "",
        TriggerType: CRON_TRIGGER_TYPE,
        CronExpression: "",
        CronDescription: "",
        TimeZoneId: DEFAULT_TIME_ZONE,
        MisfireStrategy: 0,
        Priority: 5,
        Description: "",
        startTimeLocal: "",
        endTimeLocal: "",
        nextFirePreview: NEXT_FIRE_PLACEHOLDER,
    };
}

export function normalizeTriggerForForm(trigger, jobId) {
    const form = createTriggerForm(jobId);
    if (!trigger) {
        return form;
    }

    const timeZoneId = resolveTimeZoneId(trigger.TimeZoneId ?? trigger.timeZoneId);

    return {
        ...form,
        Id: trigger.Id ?? trigger.id ?? null,
        JobId: trigger.JobId ?? trigger.jobId ?? jobId,
        TriggerName: trigger.TriggerName ?? trigger.Name ?? trigger.name ?? "",
        TriggerType: CRON_TRIGGER_TYPE,
        CronExpression: trigger.CronExpression ?? trigger.rule ?? "",
        CronDescription: trigger.CronDescription ?? "",
        TimeZoneId: timeZoneId,
        MisfireStrategy: trigger.MisfireStrategy ?? 0,
        Priority: trigger.Priority ?? 5,
        Description: trigger.Description ?? "",
        startTimeLocal: toLocalInputValue(trigger.StartTimeUtc, timeZoneId),
        endTimeLocal: toLocalInputValue(trigger.EndTimeUtc, timeZoneId),
        nextFirePreview: getNextFirePreview(trigger, timeZoneId),
    };
}

export function buildTriggerPayload(form) {
    const timeZoneId = resolveTimeZoneId(form?.TimeZoneId);

    return {
        Id: form.Id ?? undefined,
        JobId: form.JobId,
        TriggerName: (form.TriggerName ?? "").trim(),
        TriggerType: form.TriggerType ?? CRON_TRIGGER_TYPE,
        CronExpression: (form.CronExpression ?? "").trim(),
        CronDescription: (form.CronDescription ?? "").trim(),
        TimeZoneId: timeZoneId,
        MisfireStrategy: Number(form.MisfireStrategy ?? 0),
        Priority: Number(form.Priority ?? 5),
        Description: (form.Description ?? "").trim(),
        StartTimeUtc: toUtcValue(form.startTimeLocal, timeZoneId),
        EndTimeUtc: toUtcValue(form.endTimeLocal, timeZoneId),
    };
}

export function validateTriggerForm(form) {
    const errors = {};
    const timeZoneId = resolveTimeZoneId(form?.TimeZoneId);

    if (!form?.TriggerName?.trim()) {
        errors.TriggerName = "请输入触发器名称";
    }

    if (!form?.CronExpression?.trim()) {
        errors.CronExpression = "请输入 Cron 表达式";
    }

    if (form?.startTimeLocal && form?.endTimeLocal) {
        const start = parseLocalInputToUtcDate(form.startTimeLocal, timeZoneId);
        const end = parseLocalInputToUtcDate(form.endTimeLocal, timeZoneId);

        if (start && end && start > end) {
            errors.endTimeLocal = "结束时间不能早于开始时间";
        }
    }

    return errors;
}

export function computeUpcomingExecutions(form, options = {}) {
    const cronExpression = (form?.CronExpression ?? "").trim();
    if (!cronExpression) {
        return createPreviewState("blank", PREVIEW_BLANK);
    }

    const timeZoneId = resolveTimeZoneId(form?.TimeZoneId);
    const startDate = parseLocalInputToUtcDate(form?.startTimeLocal, timeZoneId);
    const endDate = parseLocalInputToUtcDate(form?.endTimeLocal, timeZoneId);
    const now = toDate(options.currentDate) ?? new Date();

    if (startDate && endDate && startDate > endDate) {
        return createPreviewState("empty", PREVIEW_EMPTY);
    }

    const normalized = normalizeQuartzCron(cronExpression);
    if (!normalized) {
        return createPreviewState("invalid", PREVIEW_INVALID);
    }

    let yearMatcher;
    try {
        yearMatcher = createYearMatcher(normalized.yearField);
    } catch {
        return createPreviewState("invalid", PREVIEW_INVALID);
    }
    const currentDate = getInitialPreviewDate({
        now,
        startDate,
        endDate,
        timeZoneId,
        yearMatcher,
    });

    if (!currentDate) {
        return createPreviewState("empty", PREVIEW_EMPTY);
    }

    let interval;
    try {
        interval = createInterval(normalized.expression, {
            currentDate,
            startDate,
            endDate,
            timeZoneId,
        });
    } catch {
        return createPreviewState("invalid", PREVIEW_INVALID);
    }

    const executions = [];
    let cursor = currentDate;
    let guard = 0;

    const startBoundaryExecution = getStartBoundaryExecution({
        expression: normalized.expression,
        startDate,
        now,
        timeZoneId,
        yearMatcher,
    });

    if (startBoundaryExecution) {
        executions.push({
            isoUtc: startBoundaryExecution.toISOString(),
            display: formatPreviewDate(startBoundaryExecution, timeZoneId),
        });
        cursor = addUtcSecond(startBoundaryExecution);

        try {
            interval = createInterval(normalized.expression, {
                currentDate: cursor,
                startDate,
                endDate,
                timeZoneId,
            });
        } catch {
            return {
                state: "ready",
                message: executions[0].display,
                executions,
            };
        }
    }

    while (executions.length < PREVIEW_LIMIT && guard < PREVIEW_LIMIT * 20) {
        guard += 1;

        let nextDate;
        try {
            nextDate = interval.next().toDate();
        } catch {
            break;
        }

        if (!nextDate || Number.isNaN(nextDate.getTime())) {
            break;
        }

        const nextYear = getDateParts(nextDate, timeZoneId).year;
        if (!yearMatcher.matches(nextYear)) {
            const nextAllowedYear = yearMatcher.nextYearFrom(nextYear + 1);
            if (nextAllowedYear == null) {
                break;
            }

            cursor = maxDate(
                addUtcSecond(nextDate),
                parseLocalInputToUtcDate(`${nextAllowedYear}-01-01T00:00`, timeZoneId),
            );

            if (endDate && cursor > endDate) {
                break;
            }

            try {
                interval = createInterval(normalized.expression, {
                    currentDate: cursor,
                    startDate,
                    endDate,
                    timeZoneId,
                });
            } catch {
                break;
            }

            continue;
        }

        executions.push({
            isoUtc: nextDate.toISOString(),
            display: formatPreviewDate(nextDate, timeZoneId),
        });

        cursor = addUtcSecond(nextDate);

        try {
            interval = createInterval(normalized.expression, {
                currentDate: cursor,
                startDate,
                endDate,
                timeZoneId,
            });
        } catch {
            break;
        }
    }

    if (executions.length === 0) {
        return createPreviewState("empty", PREVIEW_EMPTY);
    }

    return {
        state: "ready",
        message: executions[0].display,
        executions,
    };
}

export function getNextFirePreview(trigger, timeZoneId = trigger?.TimeZoneId) {
    const value =
        trigger?.NextFireTimeUtc ??
        trigger?.NextFireTime ??
        trigger?.nextTime ??
        NEXT_FIRE_PLACEHOLDER;

    const date = toDate(value);
    if (!date) {
        return value;
    }

    return formatPreviewDate(date, resolveTimeZoneId(timeZoneId));
}

function createPreviewState(state, message) {
    return {
        state,
        message,
        executions: [],
    };
}

function createInterval(expression, options) {
    return CronExpressionParser.parse(expression, {
        currentDate: options.currentDate,
        startDate: options.startDate,
        endDate: options.endDate,
        tz: options.timeZoneId,
        strict: true,
    });
}

function getInitialPreviewDate({ now, startDate, endDate, timeZoneId, yearMatcher }) {
    let candidate = maxDate(now, startDate);
    const lowerYear = getDateParts(candidate, timeZoneId).year;
    const allowedYear = yearMatcher.nextYearFrom(lowerYear);

    if (allowedYear == null) {
        return null;
    }

    if (allowedYear > lowerYear) {
        const yearStart = parseLocalInputToUtcDate(`${allowedYear}-01-01T00:00`, timeZoneId);
        candidate = maxDate(candidate, yearStart);
    }

    if (endDate && candidate > endDate) {
        return null;
    }

    const candidateYear = getDateParts(candidate, timeZoneId).year;
    if (!yearMatcher.matches(candidateYear)) {
        return null;
    }

    return candidate;
}

function getStartBoundaryExecution({ expression, startDate, now, timeZoneId, yearMatcher }) {
    if (!startDate || startDate < now) {
        return null;
    }

    const previousSecond = new Date(startDate.getTime() - 1000);
    let boundaryInterval;

    try {
        boundaryInterval = createInterval(expression, {
            currentDate: previousSecond,
            startDate: null,
            endDate: startDate,
            timeZoneId,
        });
    } catch {
        return null;
    }

    try {
        const candidate = boundaryInterval.next().toDate();
        const candidateYear = getDateParts(candidate, timeZoneId).year;

        if (candidate.getTime() !== startDate.getTime() || !yearMatcher.matches(candidateYear)) {
            return null;
        }

        return candidate;
    } catch {
        return null;
    }
}

function normalizeQuartzCron(expression) {
    const parts = expression.split(/\s+/).filter(Boolean);

    if (parts.length === 6) {
        return {
            expression: parts.join(" "),
            yearField: null,
        };
    }

    if (parts.length === 7) {
        return {
            expression: parts.slice(0, 6).join(" "),
            yearField: parts[6],
        };
    }

    return null;
}

function createYearMatcher(yearField) {
    if (!yearField || yearField === "*" || yearField === "?") {
        return {
            matches: () => true,
            nextYearFrom: (year) => Math.max(year, MIN_YEAR),
        };
    }

    const allowedYears = [];
    const predicate = createYearPredicate(yearField);
    for (let year = MIN_YEAR; year <= MAX_YEAR; year += 1) {
        if (predicate(year)) {
            allowedYears.push(year);
        }
    }

    if (allowedYears.length === 0) {
        throw new Error("Invalid year field");
    }

    return {
        matches: (year) => allowedYears.includes(year),
        nextYearFrom: (year) => allowedYears.find((allowedYear) => allowedYear >= year) ?? null,
    };
}

function createYearPredicate(yearField) {
    const segments = yearField.split(",");
    const segmentPredicates = segments.map((segment) => createYearSegmentPredicate(segment.trim()));

    return (year) => segmentPredicates.some((predicate) => predicate(year));
}

function createYearSegmentPredicate(segment) {
    if (!segment) {
        throw new Error("Invalid year field");
    }

    if (segment.includes("/")) {
        const [rangePart, stepPart] = segment.split("/");
        const step = Number(stepPart);
        if (!Number.isInteger(step) || step <= 0) {
            throw new Error("Invalid year step");
        }

        const [start, end] = parseYearRange(rangePart, false);
        return (year) => year >= start && year <= end && (year - start) % step === 0;
    }

    if (segment === "*" || segment === "?") {
        return () => true;
    }

    if (segment.includes("-")) {
        const [start, end] = parseYearRange(segment, true);
        return (year) => year >= start && year <= end;
    }

    const exactYear = parseYearValue(segment);
    return (year) => year === exactYear;
}

function parseYearRange(value, exactOnly) {
    if (value === "*" || value === "?" || value === "") {
        return [MIN_YEAR, MAX_YEAR];
    }

    if (value.includes("-")) {
        const [startPart, endPart] = value.split("-");
        const start = parseYearValue(startPart);
        const end = parseYearValue(endPart);

        if (start > end) {
            throw new Error("Invalid year range");
        }

        return [start, end];
    }

    const start = parseYearValue(value);
    return [start, exactOnly ? start : MAX_YEAR];
}

function parseYearValue(value) {
    const parsed = Number(value);
    if (!Number.isInteger(parsed) || parsed < MIN_YEAR || parsed > MAX_YEAR) {
        throw new Error("Invalid year value");
    }

    return parsed;
}

function toLocalInputValue(value, timeZoneId) {
    const date = toDate(value);
    if (!date) {
        return "";
    }

    const parts = getDateParts(date, timeZoneId);
    return `${parts.year}-${parts.month}-${parts.day}T${parts.hour}:${parts.minute}`;
}

function toUtcValue(value, timeZoneId) {
    const date = parseLocalInputToUtcDate(value, timeZoneId);
    return date ? date.toISOString() : null;
}

function parseLocalInputToUtcDate(value, timeZoneId) {
    const parts = parseLocalInput(value);
    if (!parts) {
        return null;
    }

    const utcMillis = resolveUtcMillis(parts, timeZoneId);
    const date = new Date(utcMillis);

    return Number.isNaN(date.getTime()) ? null : date;
}

function parseLocalInput(value) {
    if (!value) {
        return null;
    }

    const match = String(value)
        .trim()
        .match(
            /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})(?::(\d{2}))?$/,
        );

    if (!match) {
        return null;
    }

    return {
        year: Number(match[1]),
        month: Number(match[2]),
        day: Number(match[3]),
        hour: Number(match[4]),
        minute: Number(match[5]),
        second: Number(match[6] ?? 0),
    };
}

function resolveUtcMillis(parts, timeZoneId) {
    const naiveUtcMillis = Date.UTC(
        parts.year,
        parts.month - 1,
        parts.day,
        parts.hour,
        parts.minute,
        parts.second,
    );

    let utcMillis = naiveUtcMillis;
    for (let index = 0; index < 3; index += 1) {
        const offsetMillis = getTimeZoneOffsetMillis(new Date(utcMillis), timeZoneId);
        const adjustedMillis = naiveUtcMillis - offsetMillis;

        if (adjustedMillis === utcMillis) {
            break;
        }

        utcMillis = adjustedMillis;
    }

    return utcMillis;
}

function getTimeZoneOffsetMillis(date, timeZoneId) {
    const parts = getDateParts(date, timeZoneId);
    const zonedUtcMillis = Date.UTC(
        Number(parts.year),
        Number(parts.month) - 1,
        Number(parts.day),
        Number(parts.hour),
        Number(parts.minute),
        Number(parts.second),
    );

    return zonedUtcMillis - date.getTime();
}

function formatPreviewDate(date, timeZoneId) {
    const parts = getDateParts(date, timeZoneId);
    return [
        `${parts.year}-${parts.month}-${parts.day}`,
        `${parts.hour}:${parts.minute}:${parts.second}`,
    ].join(" ");
}

function getDateParts(date, timeZoneId) {
    const formatter = getFormatter(timeZoneId);
    const partMap = {};

    for (const part of formatter.formatToParts(date)) {
        if (part.type !== "literal") {
            partMap[part.type] = part.value;
        }
    }

    return {
        year: Number(partMap.year),
        month: partMap.month,
        day: partMap.day,
        hour: partMap.hour,
        minute: partMap.minute,
        second: partMap.second,
    };
}

function getFormatter(timeZoneId) {
    const normalized = resolveTimeZoneId(timeZoneId);
    if (!formatterCache.has(normalized)) {
        formatterCache.set(
            normalized,
            new Intl.DateTimeFormat("en-CA", {
                timeZone: normalized,
                calendar: "iso8601",
                numberingSystem: "latn",
                hourCycle: "h23",
                year: "numeric",
                month: "2-digit",
                day: "2-digit",
                hour: "2-digit",
                minute: "2-digit",
                second: "2-digit",
            }),
        );
    }

    return formatterCache.get(normalized);
}

function resolveTimeZoneId(value) {
    const candidate = (value ?? "").trim() || DEFAULT_TIME_ZONE;

    try {
        getFormatterUnsafe(candidate);
        return candidate;
    } catch {
        return DEFAULT_TIME_ZONE;
    }
}

function getFormatterUnsafe(timeZoneId) {
    return new Intl.DateTimeFormat("en-CA", {
        timeZone: timeZoneId,
    });
}

function maxDate(left, right) {
    if (!left) {
        return right ?? null;
    }

    if (!right) {
        return left;
    }

    return left > right ? left : right;
}

function addUtcSecond(date) {
    return new Date(date.getTime() + 1000);
}

function toDate(value) {
    if (!value) {
        return null;
    }

    if (value instanceof Date) {
        return Number.isNaN(value.getTime()) ? null : value;
    }

    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? null : date;
}

export {
    CRON_TRIGGER_TYPE,
    DEFAULT_TIME_ZONE,
    NEXT_FIRE_PLACEHOLDER,
    PREVIEW_BLANK,
    PREVIEW_INVALID,
    PREVIEW_EMPTY,
    PREVIEW_LIMIT,
    PREVIEW_DEBOUNCE_MS,
};
