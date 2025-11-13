// src/services/api/basket.js
import http from "./http";

// Lấy giỏ hàng user hiện tại
export async function getBasket() {
  const res = await http.get("/bff-api/basket");
  return res.data; // { buyerId, items: [...] }
}

// Ghi giỏ hàng (ghi đè toàn bộ items)
export async function saveBasket(items) {
  const res = await http.post("/bff-api/basket", {
    items: items || [],
  });
  return res.data;
}

// Xóa giỏ hàng
export async function deleteBasket() {
  await http.delete("/bff-api/basket");
}
