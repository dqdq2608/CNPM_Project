import axios from "axios";

export const BFF_BASE =
  process.env.REACT_APP_API_BASE || "https://localhost:7082";

// Axios dùng riêng cho BFF (root)
const bffHttp = axios.create({
  baseURL: BFF_BASE,
  withCredentials: true, // gửi/nhận cookie __Host-bff-*
});

// Đọc cookie tiện dụng
function readCookie(name) {
  const m = document.cookie.match(new RegExp(`(?:^|; )${name}=([^;]*)`));
  return m ? decodeURIComponent(m[1]) : null;
}

// Tự gắn X-CSRF cho mọi request nếu có
bffHttp.interceptors.request.use((cfg) => {
  const csrf = readCookie("__Host-bff-csrf");
  if (csrf) {
    cfg.headers = cfg.headers || {};
    cfg.headers["X-CSRF"] = csrf;
  }
  return cfg;
});

/**
 * IBffPublicApi "interface" cho FE.
 *
 * @typedef {Object} IBffAuthApi
 * @property {() => Promise<void>} initAntiforgery   Gọi /bff/public/antiforgery
 * @property {(username:string, password:string) => Promise<void>} login
 * @property {() => Promise<object|null>} getUser    Gọi /bff/public/user
 * @property {() => Promise<void>} logout            Gọi /bff/public/logout
 */

/**
 * @type {IBffAuthApi}
 */
export const bffAuthApi = {
  async initAntiforgery() {
    await bffHttp.get("/bff/public/antiforgery");
  },

  async login(username, password) {
    await bffHttp.get("/bff/public/antiforgery"); // chắc chắn có CSRF
    await bffHttp.post("/bff/public/login", { username, password });
    await bffHttp.get("/bff/public/antiforgery"); // refresh CSRF sau login
  },

  async getUser() {
    try {
      const res = await bffHttp.get("/bff/public/user");
      return res.data; // { sub, name, email, session_expires_in, raw }
    } catch (e) {
      if (e?.response?.status === 401) return null;
      throw e;
    }
  },

  async logout() {
    await bffHttp.get("/bff/public/antiforgery");
    try {
      await bffHttp.post("/bff/public/logout", {});
    } catch (e) {
      console.error("Logout failed:", e?.response?.status, e?.message);
    } finally {
      await bffHttp.get("/bff/public/antiforgery");
    }
  },
};
