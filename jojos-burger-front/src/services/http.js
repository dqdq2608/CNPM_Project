import axios from "axios";

// BFF
const API_BASE = process.env.REACT_APP_API_BASE || "";

// HTTP chính dùng cho các route /api/* (qua BFF)
const http = axios.create({
  baseURL: API_BASE + "/api",
  withCredentials: true,
});

// HTTP riêng cho Catalog service (hình ảnh, catalogtypes,...)
export const catalogHttp = axios.create({
  baseURL: API_BASE + "/api/catalog",
  withCredentials: true, // BFF yêu cầu auth -> cần cookie
});

export default http;
