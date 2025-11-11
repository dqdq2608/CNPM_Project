// src/components/CategoryCarousel/index.js
import React, { useEffect, useState } from "react";
import Carousel from "react-elastic-carousel";

import { fetchCatalogTypes } from "../../services/api/catalog";
import {
  Container,
  H2Categories,
  ContainerItems,
  Image,
  Button,
} from "./styles";

export function CategoryCarousel() {
  const [categories, setCategories] = useState([]);

  useEffect(() => {
    async function loadCategories() {
      try {
        const types = await fetchCatalogTypes();
        const mapped = (types || []).map((t) => ({
          id: t.id,
          name: t.type,
          // BE chưa có ảnh -> dùng placeholder tạm
          url: "/images/category-placeholder.png",
        }));
        setCategories(mapped);
      } catch (e) {
        console.error("[CategoryCarousel] loadCategories failed:", e);
        setCategories([]);
      }
    }
    loadCategories();
  }, []);

  const breakPoints = [
    { width: 1, itemsToShow: 1 },
    { width: 400, itemsToShow: 2 },
    { width: 600, itemsToShow: 3 },
    { width: 900, itemsToShow: 4 },
    { width: 1300, itemsToShow: 5 },
  ];

  return (
    <Container>
      <H2Categories>Categories</H2Categories>
      <Carousel
        itemsToShow={5}
        style={{ width: "90%" }}
        breakPoints={breakPoints}
      >
        {categories.map((category) => (
          <ContainerItems key={category.id}>
            <Image src={category.url} alt={category.name} />
            <Button
              to={{
                pathname: "/products",
                state: { categoryId: category.id }, // Router v5: state truyền qua location
              }}
            >
              {category.name}
            </Button>
          </ContainerItems>
        ))}
      </Carousel>
    </Container>
  );
}
