// src/services/api/drone.js
import axios from "axios";

// Gợi ý: đặt BASE_URL bằng env, hoặc sửa trực tiếp cho khớp gateway của bạn
// Nếu Delivery.API chạy độc lập: http://localhost:<port>
// Nếu qua Kong / API gateway: http://localhost:8000/delivery
const DELIVERY_BASE_URL =
  process.env.REACT_APP_DELIVERY_API_BASE_URL || "http://localhost:5010";

export const DroneStatus = {
  Idle: 0,
  Delivering: 1,
  Charging: 2,
  Maintenance: 3,
  Offline: 4,
};

export async function fetchDrones() {
  const res = await axios.get(`${DELIVERY_BASE_URL}/api/drones`);
  return res.data;
}

export async function updateDroneStatus(id, status) {
  const res = await axios.put(`${DELIVERY_BASE_URL}/api/drones/${id}/status`, {
    status,
  });
  return res.data;
}

// (Optional) nếu sau này cần tạo drone từ UI
export async function createDrone(payload) {
  const res = await axios.post(`${DELIVERY_BASE_URL}/api/drones`, payload);
  return res.data;
}
