// src/components/CartResume/index.js
import React, { useState, useEffect } from "react";
import { useHistory } from "react-router-dom";
import { toast } from "react-toastify";

import { useCart } from "../../hooks/CartContext";
import checkout from "../../services/api/checkout";
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
    try {
      if (cartProducts.length === 0) {
        toast.error("Your cart is empty!");
        return;
      }

      // 1) Build items gửi lên BFF
      const items = cartProducts.map((product) => ({
        productId: product.id,
        productName: product.name,
        quantity: product.quantity,
        unitPrice: product.price
      }));

      const payload = { items };

      // 2) Create order qua BFF
      const checkoutRes = await toast.promise(
        checkout.checkoutOnline(payload),
        {
          pending: "Creating order...",
          success: "Order created!",
          error: "Could not create order"
        }
      );

      const orderId = checkoutRes.orderId ?? checkoutRes.OrderId;
      if (!orderId) {
        toast.error("Order ID not found");
        return;
      }

      // 3) Lấy paymentUrl từ BFF
      const payRes = await toast.promise(
        checkout.fetchPaymentLink(orderId),
        {
          pending: "Retrieving payment link...",
          success: "Redirecting to PayOS...",
          error: "Could not obtain payment link"
        }
      );

      const paymentUrl = payRes.paymentUrl ?? payRes.PaymentUrl;
      if (!paymentUrl) {
        toast.error("Payment link unavailable");
        return;
      }

      // 4) Redirect sang PayOS
      window.location.href = paymentUrl;

      // (tuỳ bạn)
      // clearCart();

    } catch (err) {
      console.error(err);
      toast.error("Checkout failed!");
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
