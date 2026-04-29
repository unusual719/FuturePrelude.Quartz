import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import { createBackendProxyConfig } from "./devProxyConfig.js";

export default defineConfig({
  plugins: [vue()],
  root: ".",
  build: {
    outDir: "../wwwroot", // 输出到 ASP.NET Core 的静态文件目录
    emptyOutDir: true,
  },
  server: {
    port: 5173,
    proxy: createBackendProxyConfig(),
  },
});
