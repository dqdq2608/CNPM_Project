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

export async function fetchRestaurantOrders() {
  const res = await orderHttp.get("/restaurant/orders");
  return res.data; // mảng OrderSummary
}

export async function fetchOrderDetail(orderId) {
  const res = await orderHttp.get(`/orders/${orderId}`);
  return res.data; // chi tiết đơn (có items)
}

export async function confirmOrderDelivery(orderId) {
  const res = await orderHttp.post(`/orders/${orderId}/confirm-delivery`);
  return res.data; // { success: true }
}

export async function fetchOrderDelivery(orderId) {
  const res = await orderHttp.get(`/orders/${orderId}/delivery`);
  return res.data;
  // => { id, orderId, restaurantLat, restaurantLon, customerLat, customerLon, distanceKm, deliveryFee, status }
}

export async function tickDelivery(orderId) {
  const res = await orderHttp.post(`/orders/${orderId}/delivery/tick`);
  return res.data; // DeliveryResponse từ BFF
}

// Gọi BFF để restaurant bắt đầu giao bằng drone
export async function startDelivery(orderId) {
  const res = await orderHttp.post(
    `/restaurant/orders/${orderId}/start-delivery`
  );
  return res.data; // { success: true }
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
