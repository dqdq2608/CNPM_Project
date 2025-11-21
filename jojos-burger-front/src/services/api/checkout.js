// src/services/checkout.js
import http from "../http";

/**
 * Gửi yêu cầu checkout online
 */
async function checkoutOnline(payload) {
  const { data } = await http.post("/checkoutonline", payload, {
    headers: {
      "Content-Type": "application/json",
    },
  });

  console.log("checkoutOnline res.data = ", data);
  return data; // { orderId, total, ... }
}

/**
 * Lấy payment link, có retry vài lần để chờ PaymentProcessor tạo link
 */
async function fetchPaymentLink(orderId, maxRetries = 5, delayMs = 500) {
  let lastError;

  for (let attempt = 0; attempt < maxRetries; attempt++) {
    try {
      console.log(
        `fetchPaymentLink: try #${attempt + 1} for orderId =`,
        orderId
      );

      const { data } = await http.get(`/payments/${orderId}`);
      console.log("fetchPaymentLink data =", data);

      // Nếu đã có url thì trả về luôn
      if (data?.paymentUrl || data?.PaymentUrl) {
        return data;
      }

      // Nếu không có url nhưng cũng không lỗi → cứ coi như chưa sẵn sàng
      lastError = new Error("Payment URL missing in response");
    } catch (err) {
      lastError = err;

      // Nếu không phải 404 thì ném lỗi luôn (500, 401,...)
      if (err?.response?.status !== 404) {
        throw err;
      }

      // 404 → có thể do link chưa tạo xong, thử lại
      console.warn(
        `Payment link not ready yet for order ${orderId} (404). Retry...`
      );
    }

    // đợi 1 chút rồi thử lại
    await new Promise((resolve) => setTimeout(resolve, delayMs));
  }

  // Hết retry vẫn không được
  throw lastError ?? new Error("Cannot retrieve payment link");
}

const checkout = {
  checkoutOnline,
  fetchPaymentLink,
};

export default checkout;
export { checkoutOnline, fetchPaymentLink };
