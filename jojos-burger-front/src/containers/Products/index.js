// src/containers/Products/index.js
import PropTypes from "prop-types";
import React, { useEffect, useState, useMemo } from "react";

import ProductsCover from "../../assets/productsCover.jpg";
import { CardProduct } from "../../components";
import { fetchCatalogTypes, fetchCatalog } from "../../services/api/catalog"; // <= đổi import
import {
  CategoryButton,
  Container,
  CoverImg,
  ContainerCategory,
  ContainerProducts,
} from "./styles";

export function Products({ location: { state } }) {
  let initialCategoryId = 0;
  if (state?.categoryId) initialCategoryId = state.categoryId;

  const [categories, setCategories] = useState([]);
  const [products, setProducts] = useState([]);
  const [activeCategory, setActiveCategory] = useState(initialCategoryId);

  useEffect(() => {
    async function loadAll() {
      // categories
      const types = await fetchCatalogTypes();
      setCategories([{ id: 0, name: "All" }, ...types]);

      // items
      const { items } = await fetchCatalog({
        pageIndex: 0,
        pageSize: 1000,
        onlyAvailable: true,
      });
      // map về shape cũ để CardProduct vẫn dùng được
      const shaped = items.map((item) => ({
        ...item,
        category_id: item.raw?.catalogTypeId, // giữ key cũ FE đang dùng
        imageUrl: item.url, // CardProduct thường đọc imageUrl
      }));
      setProducts(shaped);
    }
    loadAll();
  }, []);

  const filteredProducts = useMemo(() => {
    if (activeCategory === 0) return products;
    return products.filter((p) => p.category_id === activeCategory);
  }, [activeCategory, products]);

  return (
    <Container>
      <CoverImg src={ProductsCover} alt="cover" />
      <ContainerCategory>
        {categories.map((category) => (
          <CategoryButton
            type="button"
            isActiveCategory={activeCategory === category.id}
            key={category.id}
            onClick={() => setActiveCategory(category.id)}
          >
            {category.name}
          </CategoryButton>
        ))}
      </ContainerCategory>

      <ContainerProducts>
        {filteredProducts.map((product) => (
          <CardProduct key={product.id} product={product} />
        ))}
      </ContainerProducts>
    </Container>
  );
}

Products.propTypes = {
  location: PropTypes.object,
};
