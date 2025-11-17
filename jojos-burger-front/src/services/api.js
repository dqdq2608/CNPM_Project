import http from "./http"; // axios cho /api/*
import { fetchCatalogTypes, fetchCatalog } from "./api/catalog";

// Adapter cho Home:
export async function getCategories() {
  const types = await fetchCatalogTypes();
  return types.map((t) => ({ id: t.id, name: t.name, image: null }));
}

export async function getProducts() {
  const { items } = await fetchCatalog({
    pageIndex: 0,
    pageSize: 20,
    onlyAvailable: true,
  });
  return items.map((it) => ({
    id: it.id,
    name: it.name,
    description: it.description,
    price: it.price,
    image: it.url,
  }));
}

export default http;
