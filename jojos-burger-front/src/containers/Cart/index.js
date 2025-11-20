// src/containers/Cart/index.js
import React from "react";

import CartCover from "../../assets/homecover.jpg";
import { CartItems, CartResume } from "../../components";
import { RestaurantSelector } from "../../components/RestaurantSelector";
import { Container, CoverImg, Wrapper } from "./styles";

export function Cart() {
  return (
    <Container>
      <CoverImg src={CartCover} />
      <Wrapper>
        <CartItems />
        <RestaurantSelector />
        <CartResume />
      </Wrapper>
    </Container>
  );
}
