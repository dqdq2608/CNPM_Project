import axios from "axios";

// BFF
const API_BASE = process.env.REACT_APP_API_BASE || "";

// HTTP chính dùng cho các route /api/* (qua BFF)
const http = axios.create({
  baseURL: API_BASE + "/api",
  withCredentials: true,
});

// HTTP riêng cho Catalog service (hình ảnh, catalogtypes,...)
// Ưu tiên REACT_APP_CATALOG_API_BASE, nếu không có thì fallback về /api/catalog
export const catalogHttp = axios.create({
  baseURL:
    process.env.REACT_APP_CATALOG_API_BASE || API_BASE + "/api/catalog",
  withCredentials: false, // thường không cần cookie cho public catalog
});

export default http;
