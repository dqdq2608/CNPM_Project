import axios from "axios";

const api = axios.create({
  baseURL: "https://localhost:7082", // BFF đang chạy HTTPS
  withCredentials: true,
});

api.interceptors.request.use((config) => {
  if (config?.url?.startsWith("/health")) {
    return config;
  }

  const raw = localStorage.getItem("jojosburger:userData");
  const token = raw ? JSON.parse(raw).token : null;

  if (token) {
    config.headers = config.headers || {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export default api;
