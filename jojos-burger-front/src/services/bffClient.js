// src/services/bffClient.js (JS)
const BFF_BASE = process.env.REACT_APP_API_BASE || "";

function getCookie(name) {
  return document.cookie
    .split("; ")
    .find((r) => r.startsWith(name + "="))
    ?.split("=")[1];
}

// Đăng nhập/đăng xuất qua BFF
export function login(returnUrl = "/") {
  window.location.href = `${BFF_BASE}/bff/login?returnUrl=${encodeURIComponent(returnUrl)}`;
}
export function logout() {
  window.location.href = `${BFF_BASE}/bff/logout`;
}

// Lấy user hiện tại
export async function getUser() {
  const r = await fetch(`${BFF_BASE}/bff/user`, { credentials: "include" });
  return r.ok ? r.json() : null;
}

// Gọi API qua BFF
export async function apiGet(path) {
  const r = await fetch(`${BFF_BASE}${path}`, { credentials: "include" });
  if (!r.ok) throw new Error(await r.text());
  return r.json();
}
export async function apiJson(method, path, body) {
  const csrf = getCookie("BffCsrf") || "";
  const r = await fetch(`${BFF_BASE}${path}`, {
    method,
    credentials: "include",
    headers: { "Content-Type": "application/json", "X-CSRF": csrf },
    body: JSON.stringify(body),
  });
  if (!r.ok) throw new Error(await r.text());
  return r.json();
}
