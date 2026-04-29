import * as signalR from "@microsoft/signalr";
import {
  handleUnauthorizedSessionExpiry,
  isUnauthorizedError,
  refreshAuthTokens,
} from "./authSession.js";

const OVERVIEW_EVENT = "dashboard-overview-updated";
const HEALTH_ENDPOINT = "/health";
const HEALTH_TIMEOUT_MS = 5000;
const RECONNECT_BASE_DELAY_MS = 1000;
const RECONNECT_MAX_DELAY_MS = 30000;

const infiniteReconnectPolicy = {
  nextRetryDelayInMilliseconds({ previousRetryCount }) {
    return Math.min(
      RECONNECT_BASE_DELAY_MS * 2 ** Math.min(previousRetryCount, 5),
      RECONNECT_MAX_DELAY_MS,
    );
  },
};

const wait = (ms) =>
  new Promise((resolve) => {
    setTimeout(resolve, ms);
  });

const getRetryDelay = (attempt) =>
  Math.min(
    RECONNECT_BASE_DELAY_MS * 2 ** Math.min(attempt, 5),
    RECONNECT_MAX_DELAY_MS,
  );

const getAccessToken = () => {
  const token = localStorage.getItem("scheduler_token") || "";

  if (!token) {
    return "";
  }

  return token.startsWith("Bearer ") ? token.slice("Bearer ".length) : token;
};

const getRecoveryError = (error, fallbackMessage) =>
  error instanceof Error ? error : new Error(fallbackMessage);

const checkDashboardHealth = async () => {
  const abortController = new AbortController();
  const timeoutId = setTimeout(() => {
    abortController.abort();
  }, HEALTH_TIMEOUT_MS);

  try {
    const response = await fetch(HEALTH_ENDPOINT, {
      method: "GET",
      cache: "no-store",
      signal: abortController.signal,
    });

    return response.ok;
  } catch {
    return false;
  } finally {
    clearTimeout(timeoutId);
  }
};

export function createDashboardConnection({
  onOverview,
  onReconnected,
  onReconnecting,
  onClosed,
} = {}) {
  let stoppedByClient = false;
  let recoveryPromise = null;
  let recoveryAttempt = 0;

  const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/dashboard", {
      accessTokenFactory: () => getAccessToken(),
    })
    .withAutomaticReconnect(infiniteReconnectPolicy)
    .configureLogging(signalR.LogLevel.Warning)
    .build();

  const resetRecoveryState = () => {
    recoveryAttempt = 0;
  };

  const tryRecoverUnauthorizedConnection = async () => {
    try {
      await refreshAuthTokens();
      await connection.start();
      resetRecoveryState();
      await onReconnected?.();
      return true;
    } catch (error) {
      if (isUnauthorizedError(error)) {
        stoppedByClient = true;
        handleUnauthorizedSessionExpiry();
      }

      return false;
    }
  };

  const ensureBackgroundRecovery = (error) => {
    if (stoppedByClient || recoveryPromise) {
      return recoveryPromise;
    }

    recoveryPromise = (async () => {
      let recoveryError = getRecoveryError(
        error,
        "SignalR connection closed",
      );

      if (isUnauthorizedError(recoveryError)) {
        const recovered = await tryRecoverUnauthorizedConnection();
        if (recovered || stoppedByClient) {
          return;
        }

        recoveryError = new Error("Dashboard SignalR reconnect failed");
      }

      while (!stoppedByClient) {
        if (connection.state !== signalR.HubConnectionState.Disconnected) {
          return;
        }

        await onReconnecting?.(recoveryError);

        const isHealthy = await checkDashboardHealth();
        if (!isHealthy) {
          recoveryError = new Error("Dashboard service health check failed");
          await wait(getRetryDelay(recoveryAttempt));
          recoveryAttempt += 1;
          continue;
        }

        try {
          await connection.start();
          resetRecoveryState();
          await onReconnected?.();
          return;
        } catch (startError) {
          recoveryError = getRecoveryError(
            startError,
            "Dashboard SignalR reconnect failed",
          );

          if (isUnauthorizedError(recoveryError)) {
            const recovered = await tryRecoverUnauthorizedConnection();
            if (recovered || stoppedByClient) {
              return;
            }

            recoveryError = new Error("Dashboard SignalR reconnect failed");
          }

          await wait(getRetryDelay(recoveryAttempt));
          recoveryAttempt += 1;
        }
      }
    })().finally(() => {
      recoveryPromise = null;
    });

    return recoveryPromise;
  };

  connection.on(OVERVIEW_EVENT, (payload) => {
    onOverview?.(payload);
  });
  connection.onreconnected(async () => {
    resetRecoveryState();
    await onReconnected?.();
  });
  connection.onreconnecting(async (error) => {
    if (isUnauthorizedError(error)) {
      try {
        await refreshAuthTokens();
      } catch {
        stoppedByClient = true;
      }
      return;
    }

    await onReconnecting?.(error);
  });
  connection.onclose(async (error) => {
    if (stoppedByClient) {
      return;
    }

    if (isUnauthorizedError(error)) {
      void ensureBackgroundRecovery(error);
      return;
    }

    await onClosed?.(error);
    void ensureBackgroundRecovery(error);
  });

  return {
    async start() {
      stoppedByClient = false;

      try {
        await connection.start();
        resetRecoveryState();
      } catch (error) {
        if (isUnauthorizedError(error)) {
          const recovered = await tryRecoverUnauthorizedConnection();
          if (recovered) {
            return;
          }
        } else {
          void ensureBackgroundRecovery(error);
        }

        throw error;
      }
    },
    async stop() {
      stoppedByClient = true;
      connection.off(OVERVIEW_EVENT);
      await connection.stop();
    },
  };
}
