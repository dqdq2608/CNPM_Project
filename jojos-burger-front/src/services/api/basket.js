// src/services/api/basket.js
import axios from "axios";

const BFF_BASE = process.env.REACT_APP_API_BASE || "https://localhost:7082";

// Dùng trực tiếp root của BFF (vì Basket endpoint là /bff-api/* chứ không phải /api/*)
const basketHttp = axios.create({
  baseURL: BFF_BASE + "/bff-api",
  withCredentials: true, // gửi cookie __Host-bff
});

export async function fetchBasket() {
  const res = await basketHttp.get("/basket");
  // res.data = { buyerId, items: [...] } theo Basket.API
  return res.data;
}

export async function saveBasketFromCart(cartProducts) {
  // cartProducts hiện tại có dạng { id, name, price, url, quantity }
  const items = cartProducts.map((p) => ({
    id: p.id.toString(), // hoặc GUID, nếu bạn có
    productId: p.id,
    productName: p.name,
    unitPrice: p.price,
    oldUnitPrice: p.price,
    quantity: p.quantity,
    pictureUrl: p.url,
  }));

  const payload = { items }; // BFF sẽ tự thêm buyerId
  const res = await basketHttp.post("/basket", payload);
  return res.data; // CustomerBasket
}

export async function clearBasketApi() {
  await basketHttp.delete("/basket");
}
