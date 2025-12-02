// src/services/api/drone.js
import axios from "axios";

// Gợi ý: đặt BASE_URL bằng env, hoặc sửa trực tiếp cho khớp gateway của bạn
// Nếu Delivery.API chạy độc lập: http://localhost:<port>
// Nếu qua Kong / API gateway: http://localhost:8000/delivery
const BFF_BASE = process.env.REACT_APP_API_BASE || "https://localhost:7082";

// Dùng /bff-api giống basket
const droneHttp = axios.create({
  baseURL: `${BFF_BASE}/bff-api`,
  withCredentials: true, // rất quan trọng để gửi cookie login
});

export const DroneStatus = {
  Idle: 0,
  Delivering: 1,
  Charging: 2,
  Maintenance: 3,
  Offline: 4,
};

// ===== DRONE APIS (thêm phía dưới file order.js) =====

// Lấy danh sách drone cho trang Drone Management
export async function fetchDrones() {
  const { data } = await droneHttp.get("/drones"); // BFF route: GET /drones
  return data;
}

// Cập nhật trạng thái Drone (Idle / Delivering / Maintenance / Offline)
export async function updateDroneStatus(id, status) {
  const { data } = await droneHttp.put(`/drones/${id}/status`, { status });
  return data;
}

// (Optional) tạo Drone mới từ UI Admin
export async function createDrone(payload) {
  // payload: { code, initialLatitude, initialLongitude }
  const { data } = await droneHttp.post("/drones", payload);
  return data;
}
