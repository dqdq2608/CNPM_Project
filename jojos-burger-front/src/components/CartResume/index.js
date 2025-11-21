import React, { useState, useEffect } from "react";
import { useHistory } from "react-router-dom";
import { toast } from "react-toastify";

import { useCart } from "../../hooks/CartContext";
import {
  createOrderFromCart,
  fetchDeliveryQuote,
} from "../../services/api/order";
import checkout from "../../services/api/checkout";
import formatCurrency from "../../utils/formatCurrency";
import { Button } from "../Button";
import { Container } from "./styles";

export function CartResume() {
  const [finalPrice, setFinalPrice] = useState(0);
  // phí giao hàng tính từ BFF
  const [deliveryFee, setDeliveryFee] = useState(0);
  const [distanceKm, setDistanceKm] = useState(null);
  const [loadingQuote, setLoadingQuote] = useState(false);
  const [quoteError, setQuoteError] = useState("");

  // địa chỉ giao hàng (demo: 1 địa chỉ match FakeGeocodingService)
  const [deliveryAddress, setDeliveryAddress] = useState(
    "12 Nguyễn Huệ, Quận 1, Hồ Chí Minh"
  );

  const { push } = useHistory();

  const { cartProducts, clearCart, selectedRestaurant } = useCart();

  useEffect(() => {
    const sumPrice = cartProducts.reduce((acc, current) => {
      return current.price * current.quantity + acc;
    }, 0);
    setFinalPrice(sumPrice);
  }, [cartProducts]);

  useEffect(() => {
    // chưa chọn chi nhánh hoặc chưa nhập địa chỉ => không tính phí
    if (!selectedRestaurant || !deliveryAddress.trim()) {
      setDeliveryFee(0);
      setDistanceKm(null);
      setQuoteError("");
      return;
    }

    const timeoutId = setTimeout(async () => {
      try {
        setLoadingQuote(true);
        setQuoteError("");

        const data = await fetchDeliveryQuote(
          selectedRestaurant,
          deliveryAddress
        );
        // BFF trả về { distanceKm, deliveryFee }
        setDistanceKm(data.distanceKm);
        setDeliveryFee(data.deliveryFee);
      } catch (err) {
        console.error("Fetch delivery quote failed:", err);
        setQuoteError("Không tính được phí giao hàng. Vui lòng thử lại.");
        setDeliveryFee(0);
        setDistanceKm(null);
      } finally {
        setLoadingQuote(false);
      }
    }, 500); // debounce 500ms

    return () => clearTimeout(timeoutId);
  }, [selectedRestaurant, deliveryAddress]);

  const submitOrder = async () => {
    if (!cartProducts.length) {
      toast.error("Giỏ hàng trống");
      return;
    }

    if (!selectedRestaurant) {
      toast.error("Vui lòng chọn chi nhánh giao hàng ở trên.");
      return;
    }

    if (!deliveryAddress.trim()) {
      toast.error("Vui lòng nhập địa chỉ giao hàng.");
      return;
    }

    try {
      await toast.promise(
        createOrderFromCart(cartProducts, selectedRestaurant, deliveryAddress), // ✅ giờ gửi đủ products + restaurantId + deliveryAddress
        {
          pending: "Đang tạo đơn hàng...",
          success: "Đặt hàng thành công! Đồ ăn đang trên đường tới bạn 🚀",
          error:
            "Xử lý đơn hàng thất bại. Vui lòng thử lại sau hoặc kiểm tra kết nối.",
        }
      );

      const items = cartProducts.map((product) => ({
        productId: product.id,
        productName: product.name,
        quantity: product.quantity,
        unitPrice: product.price,
        pictureUrl: product.pictureUrl,
      }));

      const payload = { items };

      // 3) Tạo order qua BFF
      const checkoutRes = await toast.promise(
        checkout.checkoutOnline(payload),
        {
          pending: "Creating order...",
          success: "Order created!",
          error: "Could not create order",
        }
      );

      console.log("checkoutRes at FE =", checkoutRes);

      const orderId = checkoutRes.orderId ?? checkoutRes.OrderId;
      if (!orderId) {
        toast.error("Order ID not found");
        return;
      }

      // 4) Lấy paymentUrl
      const payRes = await toast.promise(
        checkout.fetchPaymentLink(orderId),
        {
          pending: "Retrieving payment link...",
          success: "Redirecting to PayOS...",
          error: "Could not obtain payment link",
        }
      );

      console.log("payRes at FE =", payRes);

      const paymentUrl = payRes.paymentUrl ?? payRes.PaymentUrl;
      if (!paymentUrl) {
        toast.error("Payment link unavailable");
        return;
      }

      await clearCart();
      window.location.href = paymentUrl;
    } catch (e) {
      console.error("Checkout error:", e);

      if (e?.response?.status === 401) {
        toast.error("Vui lòng đăng nhập trước khi thanh toán.");
        push("/login");
      } else {
        console.error("Order error:", e);
        // nếu lỗi do thiếu restaurant/address ở FE, createOrderFromCart sẽ throw Error thường:
        if (!e.response) {
          toast.error(e.message || "Có lỗi xảy ra khi tạo đơn.");
        }
      }
    }
  };

  const total = finalPrice + deliveryFee;

  return (
    <div>
      <Container>
        <div className="container-top">
          <h2 className="title">Order Checkout</h2>

          <p className="items">Items</p>
          <p className="items-price">{formatCurrency(finalPrice)}</p>

          <p className="delivery-fee">Delivery fee</p>
          <p className="delivery-price">{formatCurrency(deliveryFee)}</p>

          {/* Thông tin chi nhánh đang chọn */}
          <div style={{ marginTop: 12 }}>
            {selectedRestaurant ? (
              <>
                <p style={{ marginBottom: 4 }}>
                  Chi nhánh: <strong>{selectedRestaurant.name}</strong>
                </p>
                <p style={{ fontSize: 12, color: "#666", margin: 0 }}>
                  {selectedRestaurant.address}
                </p>
              </>
            ) : (
              <p style={{ fontSize: 12, color: "#e67e22", marginTop: 8 }}>
                Vui lòng chọn chi nhánh ở phần “Chọn chi nhánh giao hàng” phía
                trên.
              </p>
            )}
          </div>

          {/* Địa chỉ giao hàng */}
          <div style={{ marginTop: 12 }}>
            <label
              htmlFor="delivery-address"
              style={{ fontSize: 13, fontWeight: 500 }}
            >
              Địa chỉ giao hàng
            </label>
            <input
              id="delivery-address"
              type="text"
              value={deliveryAddress}
              onChange={(e) => setDeliveryAddress(e.target.value)}
              placeholder="Ví dụ: 12 Nguyễn Huệ, Quận 1, Hồ Chí Minh"
              style={{
                marginTop: 4,
                width: "100%",
                padding: "6px 8px",
                borderRadius: 8,
                border: "1px solid #ccc",
                fontSize: 13,
              }}
            />
          </div>
          {/* Trạng thái tính phí giao hàng */}
          <div style={{ marginTop: 4 }}>
            {loadingQuote && (
              <p style={{ fontSize: 12, color: "#555" }}>
                Đang tính phí giao hàng...
              </p>
            )}

            {!loadingQuote && quoteError && (
              <p style={{ fontSize: 12, color: "#e74c3c" }}>{quoteError}</p>
            )}

            {!loadingQuote && !quoteError && distanceKm != null && (
              <p style={{ fontSize: 12, color: "#2c3e50" }}>
                Khoảng cách ~ {distanceKm.toFixed(1)} km – Phí giao hàng:{" "}
                {formatCurrency(deliveryFee)}
              </p>
            )}
          </div>
        </div>

        <div className="container-bot">
          <p className="total">Total</p>
          <p className="price-total">{formatCurrency(total)}</p>
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
