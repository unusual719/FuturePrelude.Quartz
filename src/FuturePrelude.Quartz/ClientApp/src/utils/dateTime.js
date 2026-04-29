const LOCAL_DATE_TIME_PATTERN =
  /^(\d{4})[-/](\d{1,2})[-/](\d{1,2})(?:[ T](\d{1,2})(?::(\d{1,2})(?::(\d{1,2}))?)?)?$/;

const padNumber = (value) => String(value).padStart(2, "0");

const isValidLocalDateTime = (date, year, month, day, hour, minute, second) =>
  date.getFullYear() === year &&
  date.getMonth() === month - 1 &&
  date.getDate() === day &&
  date.getHours() === hour &&
  date.getMinutes() === minute &&
  date.getSeconds() === second;

const tryParseLocalDateTime = (text) => {
  const match = LOCAL_DATE_TIME_PATTERN.exec(text);
  if (!match) {
    return null;
  }

  const [
    ,
    yearText,
    monthText,
    dayText,
    hourText = "0",
    minuteText = "0",
    secondText = "0",
  ] = match;
  const year = Number(yearText);
  const month = Number(monthText);
  const day = Number(dayText);
  const hour = Number(hourText);
  const minute = Number(minuteText);
  const second = Number(secondText);
  const date = new Date(year, month - 1, day, hour, minute, second, 0);

  return isValidLocalDateTime(date, year, month, day, hour, minute, second)
    ? date
    : null;
};

const tryParseNativeDate = (value) => {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? null : date;
};

export const tryParseDateTime = (value) => {
  if (value === null || value === undefined || value === "") {
    return null;
  }

  if (value instanceof Date) {
    return Number.isNaN(value.getTime()) ? null : new Date(value.getTime());
  }

  if (typeof value === "number") {
    return tryParseNativeDate(value);
  }

  const text = String(value).trim();
  if (!text) {
    return null;
  }

  const localDate = tryParseLocalDateTime(text);
  if (localDate) {
    return localDate;
  }

  const candidates = [text];
  if (text.includes(" ")) {
    candidates.push(text.replace(" ", "T"));
  }
  if (text.includes("-")) {
    candidates.push(text.replace(/-/g, "/"));
  }

  for (const candidate of candidates) {
    const date = tryParseNativeDate(candidate);
    if (date) {
      return date;
    }
  }

  return null;
};

export const formatDateTimeToSeconds = (value) => {
  if (value === null || value === undefined || value === "") {
    return "-";
  }

  const date = tryParseDateTime(value);
  if (!date) {
    const text = String(value).trim();
    return text || "-";
  }

  return [
    `${date.getFullYear()}-${padNumber(date.getMonth() + 1)}-${padNumber(date.getDate())}`,
    `${padNumber(date.getHours())}:${padNumber(date.getMinutes())}:${padNumber(date.getSeconds())}`,
  ].join(" ");
};
