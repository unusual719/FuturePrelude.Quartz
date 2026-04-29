import { defineConfig } from "vitest/config";
import vue from "@vitejs/plugin-vue";

export default defineConfig({
  plugins: [vue()],
  test: {
    environment: "jsdom",
    setupFiles: "./vitest.setup.js",
    include: ["src/**/*.spec.js"],
    passWithNoTests: true,
    globals: true,
  },
});
