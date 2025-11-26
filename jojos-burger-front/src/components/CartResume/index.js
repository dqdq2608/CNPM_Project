import React, { useState, useEffect } from "react";
import { useHistory } from "react-router-dom";
import { toast } from "react-toastify";

import { useCart } from "../../hooks/CartContext";
import { fetchPaymentLink } from "../../services/api/checkout";
import {
  fetchDeliveryQuote,
  createOrderFromCart,
} from "../../services/api/order";
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
      // 1) Tạo Order (và Delivery) qua BFF
      const orderResult = await toast.promise(
        createOrderFromCart(cartProducts, selectedRestaurant, deliveryAddress),
        {
          pending: "Đang tạo đơn & giao hàng...",
          success: "Tạo đơn hàng thành công!",
          error: "Tạo đơn thất bại, vui lòng thử lại.",
        }
      );

      console.log("orderResult from BFF =", orderResult);

      const orderId =
        orderResult.orderId ?? orderResult.OrderId ?? orderResult.orderID; // phòng khi BE map khác

      if (!orderId) {
        toast.error("Không tìm được Order ID từ server.");
        return;
      }

      // Nếu BFF đã trả sẵn paymentUrl thì dùng luôn
      let paymentUrl = orderResult.paymentUrl ?? orderResult.PaymentUrl ?? null;

      // 2) Nếu chưa có paymentUrl thì gọi endpoint /payments/{orderId}
      if (!paymentUrl) {
        const payRes = await toast.promise(fetchPaymentLink(orderId), {
          pending: "Đang lấy link thanh toán...",
          success: "Chuẩn bị chuyển tới cổng thanh toán...",
          error: "Không lấy được link thanh toán.",
        });

        console.log("payRes at FE =", payRes);
        paymentUrl = payRes.paymentUrl ?? payRes.PaymentUrl ?? null;
      }

      if (!paymentUrl) {
        toast.error("Không tìm được link thanh toán cho đơn hàng này.");
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
        if (!e.response) {
          toast.error(e.message || "Có lỗi xảy ra khi tạo đơn.");
        } else {
          toast.error("Có lỗi xảy ra khi xử lý đơn hàng.");
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
                Distance ~ {distanceKm.toFixed(1)}
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
