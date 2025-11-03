// src/services/api.js
import axios from "axios";

const BFF_BASE = process.env.REACT_APP_API_BASE || "https://localhost:7082";

function getCookie(name) {
  return document.cookie
    .split("; ")
    .find((r) => r.startsWith(name + "="))
    ?.split("=")[1];
}

const api = axios.create({
  baseURL: BFF_BASE,
  withCredentials: true,
  validateStatus: () => true,
  // đặt lại tên để axios khỏi cảnh báo XSRF-TOKEN
  xsrfCookieName: "BffCsrf",
  xsrfHeaderName: "X-CSRF",
});

// tự gắn X-CSRF cho các method ghi
api.interceptors.request.use((config) => {
  const m = (config.method || "get").toLowerCase();
  if (["post", "put", "patch", "delete"].includes(m)) {
    const csrf = getCookie("BffCsrf");
    if (csrf) {
      config.headers = config.headers || {};
      config.headers["X-CSRF"] = csrf;
      console.log("[axios] attach X-CSRF:", csrf.slice(0, 8) + "...");
    }
  }
  if (config.headers?.Authorization) delete config.headers.Authorization;
  return config;
});

export default api;
