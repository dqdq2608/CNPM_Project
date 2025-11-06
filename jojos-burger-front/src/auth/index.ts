import api, { ensureCsrfToken } from "../services/api";

export async function bffLogin(username: string, password: string) {
  await ensureCsrfToken(); // xin CSRF ban đầu
  await api.post("/auth/password-login", { username, password });
  await ensureCsrfToken(); // làm tươi CSRF theo phiên mới
}

export async function bffLogout() {
  await api.post("/auth/logout", {}); // interceptor tự gắn X-CSRF
}

export async function getBffUser(): Promise<{ [k: string]: string } | null> {
  try {
    const r = await api.get("/bff/user");
    const arr: Array<{ type: string; value: string }> = r.data;
    return Object.fromEntries(arr.map((x) => [x.type, x.value]));
  } catch {
    return null;
  }
}
