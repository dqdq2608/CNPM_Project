// services/http.js
import axios from "axios";

const base = process.env.REACT_APP_API_BASE || "";
const http = axios.create({
  baseURL: base + "/api",
  withCredentials: true,
});

// 👇 Thêm instance riêng cho Catalog
export const catalogHttp = axios.create({
  baseURL: process.env.REACT_APP_CATALOG_API_BASE + "/api/catalog",
  withCredentials: false, // KHÔNG gửi cookie để tránh CORS lỗi
});

function getCookie(name) {
  return document.cookie
    .split("; ")
    .find((r) => r.startsWith(name + "="))
    ?.split("=")[1];
}

http.interceptors.request.use((cfg) => {
  const m = (cfg.method || "get").toUpperCase();
  if (!["GET", "HEAD", "OPTIONS"].includes(m)) {
    const csrf = getCookie("BffCsrf") || "";
    cfg.headers = {
      ...(cfg.headers || {}),
      "X-CSRF": csrf,
      "Content-Type": "application/json",
    };
  }
  return cfg;
});

export default http;
