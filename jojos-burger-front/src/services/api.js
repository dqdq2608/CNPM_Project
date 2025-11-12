import axios from "axios";

import { fetchCatalogTypes, fetchCatalog } from "./api/catalog";
export const BFF_BASE =
  process.env.REACT_APP_API_BASE || "https://localhost:7082";

const api = axios.create({
  baseURL: BFF_BASE,
  withCredentials: true, // gửi/nhận cookie __Host-bff
});

// ---- CSRF helpers ----
export async function ensureCsrfToken() {
  // phát lại __Host-bff-csrf (FE đọc được) + __Host-bff-af (HttpOnly)
  await api.get("/bff/public/antiforgery");
}

function readCookie(name) {
  const m = document.cookie.match(new RegExp(`(?:^|; )${name}=([^;]*)`));
  return m ? decodeURIComponent(m[1]) : null;
}

// Tự gắn X-CSRF nếu có (cho TẤT CẢ request)
api.interceptors.request.use((cfg) => {
  const csrf = readCookie("__Host-bff-csrf");
  if (csrf) {
    cfg.headers = cfg.headers || {};
    cfg.headers["X-CSRF"] = csrf;
  }
  return cfg;
});

// Adapter cho Home:
export async function getCategories() {
  const types = await fetchCatalogTypes(); // /api/catalog/catalogtypes
  return types.map((t) => ({ id: t.id, name: t.name, image: null })); // shape tối thiểu Home cần
}

export async function getProducts() {
  const { items } = await fetchCatalog({
    pageIndex: 0,
    pageSize: 20,
    onlyAvailable: true,
  }); // /api/catalog/items
  // map về shape Home đang dùng cho "offers"
  return items.map((it) => ({
    id: it.id,
    name: it.name,
    description: it.description,
    price: it.price,
    image: it.url, // ảnh
  }));
}
// ---- PUBLIC INTERFACE cho FE ----
export async function bffLogin(username, password) {
  await ensureCsrfToken();
  await api.post("/bff/public/login", { username, password });
  await ensureCsrfToken(); // refresh CSRF gắn với session vừa tạo
}

export async function bffGetUser() {
  try {
    const r = await api.get("/bff/public/user");
    return r.data; // { sub, name, email, session_expires_in, raw }
  } catch (e) {
    if (e?.response?.status === 401) return null;
    throw e;
  }
}

export async function bffLogout() {
  // 🔴 refresh CSRF trước khi logout để chắc chắn đúng token
  await ensureCsrfToken();
  try {
    await api.post("/bff/public/logout", {});
  } catch (e) {
    // không chặn UI nếu server trả 500/403 — vẫn cho FE xóa phiên
    console.error("Logout failed:", e?.response?.status, e?.message);
  } finally {
    // token CSRF có thể bị đổi sau khi logout
    await ensureCsrfToken();
  }
}

export default api;
