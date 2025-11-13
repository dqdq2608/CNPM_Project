// src/services/api/catalog.js
import { catalogHttp } from "../http";

const BASE = process.env.REACT_APP_CATALOG_API_BASE || "http://localhost:7002";

// build ảnh từ endpoint BE
const buildPicUrl = (id) => `${BASE}/api/catalog/items/${id}/pic`;

// format tiền VND
const toVnd = (value) =>
  new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(
    value ?? 0
  );

// Chuẩn hoá item để FE dùng đồng nhất
const normalizeItem = (i) => ({
  id: i.id,
  name: i.name,
  description: i.description,
  price: i.price,
  formatedPrice: toVnd(i.price),
  url: i.pictureFileName ? buildPicUrl(i.id) : undefined,
  raw: i,
});

/** Lấy danh sách CatalogTypes (categories) */
async function fetchCatalogTypes() {
  const res = await catalogHttp.get("/catalogtypes");
  return res.data;
}

async function createCatalogType(payload) {
  const res = await catalogHttp.post("/catalogtypes", payload, {
    headers: {
      "Content-Type": "application/json",
    },
  });
  return res.data;
}

/** Cập nhật CatalogType */
async function updateCatalogType(id, payload) {
  const body = { id, ...payload }; // payload: { type: "Burger" } chẳng hạn
  const res = await catalogHttp.put("/catalogtypes", body);
  return res.data;
}

/** Xoá CatalogType */
async function deleteCatalogType(id) {
  await catalogHttp.delete(`/catalogtypes/${id}`);
}

async function fetchRestaurants() {
  const url = `${BASE}/api/catalog/restaurants`;
  const { data } = await catalogHttp.get(url);
  // data: [{ restaurantId, name, address, lat, lng }]
  return data;
}

/** Lấy danh sách items (có phân trang + filter) */
async function fetchCatalog({
  pageIndex = 0,
  pageSize = 12,
  typeId,
  restaurantId,
  onlyAvailable = true,
} = {}) {
  const params = { pageIndex, pageSize };
  if (typeof typeId === "number") params.typeId = typeId;
  if (restaurantId) params.restaurantId = restaurantId;
  if (onlyAvailable) params.onlyAvailable = true;

  const url = `${BASE}/api/catalog/items`;
  const { data } = await catalogHttp.get(url, { params });

  // BE trả kiểu { pageIndex, pageSize, count/totalItems, data/items }
  const items = data?.data ?? data?.items ?? data?.results ?? [];
  const total = data?.count ?? data?.totalItems ?? items.length;

  return {
    total,
    items: items.map(normalizeItem),
    pageIndex: data?.pageIndex ?? pageIndex,
    pageSize: data?.pageSize ?? pageSize,
  };
}

/** Tìm theo tên (có phân trang + filter) */
async function searchCatalogByName({
  name,
  pageIndex = 0,
  pageSize = 12,
  typeId,
  restaurantId,
} = {}) {
  const params = { pageIndex, pageSize };
  if (typeof typeId === "number") params.typeId = typeId;
  if (restaurantId) params.restaurantId = restaurantId;

  const url = `${BASE}/api/catalog/items/by/${encodeURIComponent(name)}`;
  const { data } = await catalogHttp.get(url, { params });

  const items = data?.data ?? data?.items ?? data ?? [];
  const total = data?.count ?? data?.totalItems ?? items.length;

  return {
    total,
    items: items.map(normalizeItem),
    pageIndex: data?.pageIndex ?? pageIndex,
    pageSize: data?.pageSize ?? pageSize,
  };
}

/** Lấy chi tiết 1 item */
async function fetchCatalogItemById(id) {
  const url = `${BASE}/api/catalog/items/${id}`;
  const { data } = await catalog.get(url);
  return normalizeItem(data);
}

/* ===== Default export để giữ tương thích với code cũ (import catalog from ...) =====
   - getCategories: alias của fetchCatalogTypes
   - getProducts: alias của fetchCatalog
   - getProductById: alias của fetchCatalogItemById
*/

async function createCatalogItem(productPayload) {
  await catalogHttp.post("/items", productPayload);
}

async function updateCatalogItem(productPayload) {
  await catalogHttp.put("/items", productPayload);
}

async function deleteCatalogItem(id) {
  await catalogHttp.delete(`/items/${id}`);
}

/* ===== Default export để giữ tương thích với code cũ (import catalog from ...) =====
   - getCategories: alias của fetchCatalogTypes
   - getProducts: alias của fetchCatalog
   - getProductById: alias của fetchCatalogItemById
*/

const catalog = {
  fetchCatalogTypes,
  fetchRestaurants,
  fetchCatalog,
  searchCatalogByName,
  fetchCatalogItemById,
  createCatalogItem,
  updateCatalogItem,
  deleteCatalogItem,
  createCatalogType,
  updateCatalogType,
  deleteCatalogType,

  // Aliases cho code cũ
  getCategories: fetchCatalogTypes,
  getProducts: fetchCatalog,
  getProductById: fetchCatalogItemById,
};

export default catalog;

// Đồng thời export named để dùng cú pháp { fetchCatalog, ... } nếu cần
export {
  fetchCatalogTypes,
  fetchRestaurants,
  fetchCatalog,
  searchCatalogByName,
  fetchCatalogItemById,
  createCatalogItem,
  updateCatalogItem,
  deleteCatalogItem,
  createCatalogType,
  updateCatalogType,
  deleteCatalogType,
};
