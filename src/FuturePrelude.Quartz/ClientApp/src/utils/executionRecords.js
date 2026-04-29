const DATE_ONLY_PATTERN = /^(\d{4})-(\d{2})-(\d{2})$/;

function parseDateOnlyParts(dateText) {
  const normalized = String(dateText ?? "").trim();
  if (!normalized) {
    return null;
  }

  const match = DATE_ONLY_PATTERN.exec(normalized);
  if (!match) {
    return null;
  }

  return {
    year: Number(match[1]),
    month: Number(match[2]),
    day: Number(match[3]),
  };
}

export function buildLocalDateBoundaryUtcIso(
  dateText,
  endOfDay = false,
  options = {},
) {
  const parts = parseDateOnlyParts(dateText);
  if (!parts) {
    return undefined;
  }

  const hour = endOfDay ? 23 : 0;
  const minute = endOfDay ? 59 : 0;
  const second = endOfDay ? 59 : 0;
  const millisecond = endOfDay ? 999 : 0;
  const { offsetMinutes } = options;

  if (Number.isFinite(offsetMinutes)) {
    const utcMillis =
      Date.UTC(
        parts.year,
        parts.month - 1,
        parts.day,
        hour,
        minute,
        second,
        millisecond,
      ) -
      Number(offsetMinutes) * 60 * 1000;

    return new Date(utcMillis).toISOString();
  }

  const localDate = new Date(
    parts.year,
    parts.month - 1,
    parts.day,
    hour,
    minute,
    second,
    millisecond,
  );

  return Number.isNaN(localDate.getTime())
    ? undefined
    : localDate.toISOString();
}

export function createLatestRequestGuard() {
  let currentVersion = 0;

  return {
    issue() {
      currentVersion += 1;
      return currentVersion;
    },
    isLatest(version) {
      return version === currentVersion;
    },
    invalidate() {
      currentVersion += 1;
      return currentVersion;
    },
  };
}
