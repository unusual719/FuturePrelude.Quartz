const BACKEND_TARGET = "http://localhost:5153";

export function createBackendProxyConfig(target = BACKEND_TARGET) {
  return {
    "/api": {
      target,
      changeOrigin: true,
      secure: false,
    },
    "/hubs": {
      target,
      changeOrigin: true,
      secure: false,
      ws: true,
    },
  };
}

export { BACKEND_TARGET };
