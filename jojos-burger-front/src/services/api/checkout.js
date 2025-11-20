// src/services/checkout.js
import http from "../http";

/**
 * Gửi yêu cầu checkout online:
 *  - BFF: POST /api/checkoutonline
 *  - Body: tuỳ theo BE yêu cầu (ví dụ: items, address,...)
 *  - Response: giả sử BE trả { orderId, total, ... }
 */
async function checkoutOnline(payload) {
  const { data } = await http.post("/checkoutonline", payload, {
    headers: {
      "Content-Type": "application/json",
    },
  });
  return data; // { orderId, total, ... }
}

/**
 * Lấy payment link đã được PaymentProcessor cache:
 *  - BFF: GET /api/payments/{orderId}
 *  - Response: giả sử trả { orderId, paymentUrl }
 */
async function fetchPaymentLink(orderId) {
  const { data } = await http.get(`/payments/${orderId}`);
  return data; // { orderId, paymentUrl }
}

const checkout = {
  checkoutOnline,
  fetchPaymentLink,
};

export default checkout;
export { checkoutOnline, fetchPaymentLink };
