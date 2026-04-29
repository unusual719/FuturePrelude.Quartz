export const LOGIN_PATH = "/login";
export const AUTHENTICATED_HOME_PATH = "/dashboard";
const REFRESH_TOKEN_ENDPOINT = "/api/auth/refresh";

const AUTH_STORAGE_KEYS = [
  "scheduler_token",
  "scheduler_refresh_token",
  "scheduler_user",
];

let authRedirectInProgress = false;
let refreshPromise = null;

const getDefaultStorage = () =>
  typeof localStorage !== "undefined" ? localStorage : null;

const isNonEmptyString = (value) =>
  typeof value === "string" && value.trim() !== "";

const getDefaultFetch = () =>
  typeof fetch === "function" ? fetch.bind(globalThis) : null;

const normalizeStoredAccessToken = (value) => {
  if (!isNonEmptyString(value)) {
    return "";
  }

  const token = value.trim();
  return token.startsWith("Bearer ") ? token.slice("Bearer ".length) : token;
};

const readResponsePayload = async (response) => {
  try {
    return await response.json();
  } catch {
    return {};
  }
};

const unwrapAuthPayload = (result) => {
  if (!result || typeof result !== "object") {
    return {};
  }

  return result.Data ?? result.data ?? result;
};

const extractAuthTokens = (result) => {
  const payload = unwrapAuthPayload(result);

  return {
    accessToken: payload.AccessToken ?? payload.accessToken ?? "",
    refreshToken: payload.RefreshToken ?? payload.refreshToken ?? "",
    tokenType: payload.TokenType ?? payload.tokenType ?? "",
    expiresIn: Number(payload.ExpiresIn ?? payload.expiresIn ?? 0) || 0,
  };
};

const buildAuthError = (message, status) => {
  const error = new Error(message);

  if (status) {
    error.status = status;
    error.statusCode = status;
  }

  return error;
};

const saveAuthTokens = (tokens, storage = getDefaultStorage()) => {
  if (!storage) {
    return;
  }

  storage.setItem("scheduler_token", tokens.accessToken);
  storage.setItem("scheduler_refresh_token", tokens.refreshToken);
};

export const clearAuthSession = (storage = getDefaultStorage()) => {
  if (!storage) {
    return;
  }

  AUTH_STORAGE_KEYS.forEach((key) => {
    storage.removeItem(key);
  });
};

export const sanitizeRedirectPath = (value) => {
  if (!isNonEmptyString(value)) {
    return "";
  }

  const path = value.trim();

  if (!path.startsWith("/") || path.startsWith("//")) {
    return "";
  }

  if (
    path === LOGIN_PATH ||
    path.startsWith(`${LOGIN_PATH}?`) ||
    path.startsWith(`${LOGIN_PATH}#`)
  ) {
    return "";
  }

  return path;
};

export const getPostLoginRedirectPath = (value) =>
  sanitizeRedirectPath(value) || AUTHENTICATED_HOME_PATH;

export const getCurrentRelativePath = (location = globalThis.location) => {
  if (!location) {
    return AUTHENTICATED_HOME_PATH;
  }

  const path =
    `${location.pathname || ""}${location.search || ""}${location.hash || ""}`;

  return getPostLoginRedirectPath(path);
};

export const buildLoginRedirectUrl = (redirectPath) =>
  `${LOGIN_PATH}?redirect=${encodeURIComponent(
    getPostLoginRedirectPath(redirectPath),
  )}`;

export const handleUnauthorizedSessionExpiry = ({
  location = globalThis.location,
  storage = getDefaultStorage(),
  replace,
} = {}) => {
  clearAuthSession(storage);

  const loginUrl = buildLoginRedirectUrl(getCurrentRelativePath(location));
  if (authRedirectInProgress) {
    return loginUrl;
  }

  authRedirectInProgress = true;

  const navigate =
    replace ||
    (typeof location?.replace === "function" ? location.replace.bind(location) : null) ||
    (typeof globalThis.location?.replace === "function"
      ? globalThis.location.replace.bind(globalThis.location)
      : null);

  if (navigate) {
    navigate(loginUrl);
  }

  return loginUrl;
};

export const isUnauthorizedError = (error) => {
  if (!error) {
    return false;
  }

  if (error.status === 401 || error.statusCode === 401) {
    return true;
  }

  const message = isNonEmptyString(error.message)
    ? error.message
    : isNonEmptyString(String(error))
      ? String(error)
      : "";

  return /(^|\b)401(\b|$)|unauthorized|未授权/i.test(message);
};

export const refreshAuthTokens = ({
  location = globalThis.location,
  storage = getDefaultStorage(),
  replace,
  fetchImpl = getDefaultFetch(),
} = {}) => {
  if (refreshPromise) {
    return refreshPromise;
  }

  refreshPromise = (async () => {
    const accessToken = normalizeStoredAccessToken(
      storage?.getItem("scheduler_token") || "",
    );
    const refreshToken = storage?.getItem("scheduler_refresh_token") || "";

    if (!accessToken || !refreshToken || !fetchImpl) {
      const error = buildAuthError("刷新令牌不可用，请重新登录", 401);
      handleUnauthorizedSessionExpiry({ location, storage, replace });
      throw error;
    }

    let response;
    let result;

    try {
      response = await fetchImpl(REFRESH_TOKEN_ENDPOINT, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          AccessToken: accessToken,
          RefreshToken: refreshToken,
        }),
      });
      result = await readResponsePayload(response);
    } catch (error) {
      handleUnauthorizedSessionExpiry({ location, storage, replace });
      throw error instanceof Error
        ? error
        : buildAuthError("刷新令牌失败，请重新登录", 401);
    }

    const tokens = extractAuthTokens(result);
    const hasWrappedCode =
      Object.prototype.hasOwnProperty.call(result ?? {}, "Code") ||
      Object.prototype.hasOwnProperty.call(result ?? {}, "code");
    const isSuccessCode = !hasWrappedCode || result.Code === 200 || result.code === 200;

    if (
      !response.ok ||
      !isSuccessCode ||
      !isNonEmptyString(tokens.accessToken) ||
      !isNonEmptyString(tokens.refreshToken)
    ) {
      const message =
        result?.Message ??
        result?.message ??
        "刷新令牌失败，请重新登录";
      handleUnauthorizedSessionExpiry({ location, storage, replace });
      throw buildAuthError(message, response.status || 401);
    }

    saveAuthTokens(tokens, storage);
    return tokens;
  })().finally(() => {
    refreshPromise = null;
  });

  return refreshPromise;
};

export const resetAuthRedirectState = () => {
  authRedirectInProgress = false;
  refreshPromise = null;
};
