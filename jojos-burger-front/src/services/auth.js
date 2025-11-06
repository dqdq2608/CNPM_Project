import api, { ensureCsrfToken } from "./api";

// /bff/user trả mảng [{ type, value }]
export async function fetchCurrentUser() {
  await ensureCsrfToken(); // chắc có CSRF
  const r = await api.get("/bff/user"); // cần cookie __Host-bff
  const claims = r.data || [];
  const map = Object.fromEntries(claims.map((c) => [c.type, c.value]));
  // Ưu tiên: name → email → sub
  const displayName = map["name"] || map["email"] || map["sub"] || "User";
  return { claims: map, displayName };
}
