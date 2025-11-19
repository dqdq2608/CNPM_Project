// src/components/CartResume/index.js
import React, { useState, useEffect } from "react";
import { useHistory } from "react-router-dom";
import { toast } from "react-toastify";

import { useCart } from "../../hooks/CartContext";
import { createOrderFromCart } from "../../services/api/order";
import formatCurrency from "../../utils/formatCurrency";
import { Button } from "../Button";
import { Container } from "./styles";

export function CartResume() {
  const [finalPrice, setFinalPrice] = useState(0);
  const [deliveryFee] = useState(3);

  const { push } = useHistory();

  const { cartProducts, clearCart } = useCart();

  useEffect(() => {
    const sumPrice = cartProducts.reduce((acc, current) => {
      return current.price * current.quantity + acc;
    }, 0);
    setFinalPrice(sumPrice);
  }, [cartProducts, deliveryFee]);

  const submitOrder = async () => {
    if (!cartProducts.length) {
      toast.error("Giỏ hàng trống");
      return;
    }

    try {
      await toast.promise(
        createOrderFromCart(cartProducts), // ✅ POST /bff-api/order
        {
          pending: "Registering order...",
          success: "Order done! Food is on the way!",
          error: "Error when processing request. Please try again later... :(",
        }
      );

      // Order thành công → clear giỏ (FE + Redis qua BFF)
      await clearCart();
      setTimeout(() => {
        push("/");
      }, 1000);
    } catch (e) {
      if (e?.response?.status === 401) {
        toast.error("Please login before checkout.");
        push("/login");
      } else {
        console.error("Order error:", e);
      }
    }
  };

  return (
    <div>
      <Container>
        <div className="container-top">
          <h2 className="title">Order Checkout</h2>
          <p className="items">Items</p>
          <p className="items-price">{formatCurrency(finalPrice)}</p>
          <p className="delivery-fee">Delivery fee</p>
          <p className="delivery-price">{formatCurrency(deliveryFee)}</p>
        </div>
        <div className="container-bot">
          <p className="total">Total</p>
          <p className="price-total">
            {formatCurrency(finalPrice + deliveryFee)}
          </p>
        </div>
      </Container>
      <Button
        style={{ width: "100%", marginTop: 30, marginBottom: 30 }}
        onClick={submitOrder}
      >
        Checkout
      </Button>
    </div>
  );
}
