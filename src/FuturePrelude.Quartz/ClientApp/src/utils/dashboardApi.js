import { get } from "./request.js";

export function getDashboardOverview() {
  return get("/api/dashboard/overview", {
    skipLoading: true,
  });
}
