import axios from "axios";

export const BFF_BASE =
  process.env.REACT_APP_API_BASE || "https://localhost:7082";

const api = axios.create({
  baseURL: BFF_BASE,
  withCredentials: true, // bắt buộc để gửi/nhận cookie __Host-bff
});

// xin CSRF: BFF set __Host-bff-af (HttpOnly) + __Host-bff-csrf (FE đọc được)
export async function ensureCsrfToken() {
  await api.get("/bff/antiforgery");
}

// auto gắn X-CSRF từ cookie __Host-bff-csrf
function readCookie(name) {
  return document.cookie
    .split(";")
    .map((s) => s.trim())
    .find((x) => x.startsWith(name + "="))
    ?.split("=")[1];
}

api.interceptors.request.use((cfg) => {
  const csrf = readCookie("__Host-bff-csrf");
  if (csrf) {
    cfg.headers = cfg.headers || {};
    cfg.headers["X-CSRF"] = csrf;
  }
  return cfg;
});

export default api;
