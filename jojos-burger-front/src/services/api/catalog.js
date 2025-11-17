import { catalogHttp } from "../http";

// build URL ảnh item
const buildPicUrl = (id) => `${catalogHttp.defaults.baseURL}/items/${id}/pic`;

// Chuẩn hoá item
const normalizeItem = (i) => ({
  id: i.id,
  name: i.name,
  description: i.description,
  price: i.price,
  formatedPrice: new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
  }).format(i.price ?? 0),
  url: i.pictureFileName ? buildPicUrl(i.id) : undefined,
  raw: i,
});

// 🔹 Lấy CatalogTypes kèm ảnh (1 call, fail thì ném lỗi luôn)
async function fetchCatalogTypes() {
  const { data } = await catalogHttp.get("/catalogtypes-with-pics");
  return (data || []).map((t) => ({
    id: t.id,
    name: t.type,
    pictureUri: t.pictureUri || "/images/category-placeholder.png",
  }));
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
  const { data } = await catalogHttp.get('/restaurants');
  return data;
}

// 🔹 Danh sách items
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

  const { data } = await catalogHttp.get("/items", { params });
  const items = data?.data ?? data?.items ?? data?.results ?? [];
  const total = data?.count ?? data?.totalItems ?? items.length;

  return {
    total,
    items: items.map(normalizeItem),
    pageIndex: data?.pageIndex ?? pageIndex,
    pageSize: data?.pageSize ?? pageSize,
  };
}

// 🔹 Tìm theo tên
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

  const { data } = await catalogHttp.get(
    `/items/by/${encodeURIComponent(name)}`,
    { params }
  );
  const items = data?.data ?? data?.items ?? data ?? [];
  const total = data?.count ?? data?.totalItems ?? items.length;

  return {
    total,
    items: items.map(normalizeItem),
    pageIndex: data?.pageIndex ?? pageIndex,
    pageSize: data?.pageSize ?? pageSize,
  };
}

// 🔹 Chi tiết item
async function fetchCatalogItemById(id) {
  const { data } = await catalogHttp.get(`/items/${id}`);
  return normalizeItem(data);
}

/* ===== Default export để giữ tương thích với code cũ (import catalog from ...) =====
   - getCategories: alias của fetchCatalogTypes
   - getProducts: alias của fetchCatalog
   - getProductById: alias của fetchCatalogItemById
*/

async function createCatalogItem(productPayload) {
  await catalogHttp.post('/items', productPayload);
}

/** Cập nhật CatalogItem (v1: PUT /items, id nằm trong body) */
async function updateCatalogItem(productPayload) {
  await catalogHttp.put('/items', productPayload);
}


/** Xoá CatalogItem: DELETE /items/{id} */
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
  // alias cũ
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