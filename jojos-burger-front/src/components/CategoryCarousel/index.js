// src/components/CategoryCarousel/index.js
import React, { useEffect, useState, useMemo } from "react";
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
    let mounted = true;
    (async () => {
      try {
        const types = await fetchCatalogTypes(); // [{ id, name, pictureUri }]
        if (mounted) setCategories(types ?? []);
      } catch (e) {
        console.error("[CategoryCarousel] loadCategories failed:", e);
        if (mounted) setCategories([]);
      }
    })();
    return () => {
      mounted = false;
    };
  }, []);

  const breakPoints = useMemo(
    () => [
      { width: 1, itemsToShow: 1 },
      { width: 400, itemsToShow: 2 },
      { width: 600, itemsToShow: 3 },
      { width: 900, itemsToShow: 4 },
      { width: 1300, itemsToShow: 5 },
    ],
    []
  );

  const fallbackImg = "/images/category-placeholder.png";

  return (
    <Container>
      <H2Categories>Categories</H2Categories>
      <Carousel
        itemsToShow={5}
        style={{ width: "90%" }}
        breakPoints={breakPoints}
      >
        {categories.map((c) => (
          <ContainerItems key={c.id}>
            <Image
              src={c.pictureUri || fallbackImg}
              alt={c.name}
              onError={(e) => {
                e.currentTarget.onerror = null;
                e.currentTarget.src = fallbackImg;
              }}
            />
            <Button
              to={{
                pathname: "/products",
                state: { categoryId: c.id }, // React Router v5
              }}
            >
              {c.name}
            </Button>
          </ContainerItems>
        ))}
      </Carousel>
    </Container>
  );
}
