// src/services/api/order.js
import axios from "axios";

const BFF_BASE = process.env.REACT_APP_API_BASE || "https://localhost:7082";

// Dùng /bff-api giống basket
const orderHttp = axios.create({
  baseURL: `${BFF_BASE}/bff-api`,
  withCredentials: true, // rất quan trọng để gửi cookie login
});

export async function createOrderFromCart(
  cartProducts,
  selectedRestaurant,
  deliveryAddress
) {
  if (!selectedRestaurant || !selectedRestaurant.id) {
    throw new Error("Missing restaurant selection");
  }

  if (!deliveryAddress || !deliveryAddress.trim()) {
    throw new Error("Missing delivery address");
  }

  const products = cartProducts.map((p) => ({
    id: p.id,
    quantity: p.quantity,
  }));

  const payload = {
    products,
    restaurantId: selectedRestaurant.id,
    deliveryAddress: deliveryAddress.trim(),
  };

  const res = await orderHttp.post("/order", payload);
  return res.data;
}

// Lấy danh sách orders của user hiện tại
export async function fetchMyOrders() {
  const res = await orderHttp.get("/orders");
  return res.data; // mảng OrderSummary từ Ordering.API
}

export async function fetchOrderDetail(orderId) {
  const res = await orderHttp.get(`/orders/${orderId}`);
  return res.data; // chi tiết đơn (có items)
}

export async function fetchDeliveryQuote(selectedRestaurant, deliveryAddress) {
  if (!selectedRestaurant || !selectedRestaurant.id) {
    throw new Error("Missing restaurant selection");
  }

  if (!deliveryAddress || !deliveryAddress.trim()) {
    throw new Error("Missing delivery address");
  }

  const payload = {
    restaurantId: selectedRestaurant.id,
    deliveryAddress: deliveryAddress.trim(),
  };

  const res = await orderHttp.post("/delivery/quote", payload);
  // BE trả về { distanceKm, deliveryFee }
  return res.data;
}
