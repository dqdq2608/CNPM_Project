// containers/MyOrders/index.js
import React, { useEffect, useState } from "react";
import { useHistory } from "react-router-dom";
import { toast } from "react-toastify";

// 1. IMPORT COMPONENT BẢN ĐỒ
import DroneDeliveryMap from "../../components/DroneDeliveryMap";
import {
  fetchMyOrders,
  fetchOrderDetail,
  fetchOrderDelivery,
  confirmOrderDelivery,
  tickDelivery,
} from "../../services/api/order";
import {
  Container,
  OrderCard,
  OrderHeader,
  OrderItems,
  OrderFooter,
} from "./styles";

export function MyOrders() {
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);
  const [expandedId, setExpandedId] = useState(null);
  const [details, setDetails] = useState({});
  const [loadingDetailId, setLoadingDetailId] = useState(null);

  const { push } = useHistory();

  useEffect(() => {
    async function load() {
      try {
        const data = await fetchMyOrders();
        setOrders(data || []);
      } catch (e) {
        if (e?.response?.status === 401) {
          toast.error("Vui lòng đăng nhập để xem đơn hàng.");
          push("/login");
        } else {
          console.error("Fetch orders error:", e);
          toast.error("Lỗi khi tải danh sách đơn hàng.");
        }
      } finally {
        setLoading(false);
      }
    }

    load();
  }, [push]);

  const toggleOrder = async (id) => {
    if (expandedId === id) {
      setExpandedId(null);
      return;
    }

    setExpandedId(id);

    if (details[id]) return;

    try {
      setLoadingDetailId(id);

      // Gọi song song cho nhanh
      const [detail, delivery] = await Promise.all([
        fetchOrderDetail(id),
        fetchOrderDelivery(id),
      ]);

      // Map dữ liệu delivery sang các field DroneDeliveryMap đang dùng
      const mergedDetail = {
        ...detail,
        originLat: delivery?.restaurantLat ?? delivery?.RestaurantLat,
        originLon: delivery?.restaurantLon ?? delivery?.RestaurantLon,
        destLat: delivery?.customerLat ?? delivery?.CustomerLat,
        destLon: delivery?.customerLon ?? delivery?.CustomerLon,
      };

      setDetails((prev) => ({ ...prev, [id]: mergedDetail }));
    } catch (e) {
      console.error("Fetch order detail/delivery error:", e);
      toast.error("Lỗi khi tải chi tiết đơn hàng / lộ trình giao.");
    } finally {
      setLoadingDetailId(null);
    }
  };

  if (loading) {
    return <p style={{ padding: 16 }}>Đang tải danh sách đơn hàng...</p>;
  }

  if (!orders.length) {
    return <p style={{ padding: 16 }}>Bạn chưa có đơn hàng nào.</p>;
  }

  const handleConfirmDelivery = async (orderId) => {
    try {
      await confirmOrderDelivery(orderId);
      toast.success("Thank you for confirming delivery!");

      // Cập nhật list orders (nếu bạn có state orders)
      setOrders((prev) =>
        prev.map((o) => (o.id === orderId ? { ...o, status: "Completed" } : o))
      );

      // Cập nhật detail
      setDetails((prev) => ({
        ...prev,
        [orderId]: {
          ...prev[orderId],
          status: "Completed",
        },
      }));
    } catch (e) {
      console.error("Confirm delivery error:", e);
      toast.error(
        e?.response?.data?.message || "Cannot confirm delivery at this time."
      );
    }
  };

  const handleReportNotReceived = (orderId) => {
    toast.info(
      "If you have not received your order, please contact our support at"
    );
  };

  const handleDroneFlightCompleted = async (orderId) => {
    try {
      // Gọi BFF -> Delivery tick (Delivery + Ordering sẽ set Delivered)
      const delivery = await tickDelivery(orderId);

      // Cập nhật list orders: set status = Delivered
      setOrders((prev) =>
        prev.map((o) =>
          (o.orderNumber ?? o.id) === orderId
            ? { ...o, status: "Delivered", orderStatus: "Delivered" }
            : o
        )
      );

      // Cập nhật detail của đơn đó
      setDetails((prev) => ({
        ...prev,
        [orderId]: {
          ...prev[orderId],
          status: "Delivered",
          deliveryStatus: delivery?.status ?? delivery?.Status ?? "Delivered",
        },
      }));
    } catch (e) {
      console.error("tickDelivery error:", e);
      toast.error("Không cập nhật được trạng thái giao hàng từ drone.");
    }
  };

  return (
    <Container>
      {orders.map((order) => {
        const id = order.orderNumber ?? order.id;
        const orderDate = order.date || order.orderDate;
        const status = order.status || order.orderStatus;
        const isOpen = expandedId === id;

        const detail = details[id];
        const items =
          detail?.orderItems ||
          detail?.orderitems ||
          detail?.orderitemsDto ||
          [];

        // --- PHẦN TÍNH TOÁN ---
        const deliveryFee =
          detail?.deliveryFee ??
          detail?.DeliveryFee ??
          order.deliveryFee ??
          order.DeliveryFee ??
          0;

        const finalTotal = order.total ?? order.Total ?? 0;

        let subTotal = 0;
        if (items.length > 0) {
          subTotal = items.reduce((sum, item) => {
            const price = item.unitPrice ?? item.UnitPrice ?? 0;
            const qty = item.units ?? item.quantity ?? item.Units ?? 0;
            return sum + price * qty;
          }, 0);
        } else {
          subTotal = finalTotal - deliveryFee;
        }
        // ---------------------

        // LOGIC CHECK TỌA ĐỘ
        // Kiểm tra xem đã có dữ liệu tọa độ chưa
        // Sử dụng toán tử ?? để bắt cả trường hợp viết hoa/thường
        const hasCoords =
          detail &&
          (detail.originLat ?? detail.OriginLat) &&
          (detail.destLat ?? detail.DestLat);

        return (
          <OrderCard key={id}>
            <OrderHeader onClick={() => toggleOrder(id)}>
              <div>
                <strong>Order #{id}</strong>
                <span>
                  {orderDate ? new Date(orderDate).toLocaleString() : ""}
                </span>
              </div>

              <div>
                <span style={{ marginRight: 8 }}>Status: {status}</span>
                <button
                  type="button"
                  onClick={(e) => {
                    e.stopPropagation();
                    toggleOrder(id);
                  }}
                >
                  {isOpen ? "Hide Details" : "View Details"}
                </button>
              </div>
            </OrderHeader>

            {isOpen && (
              <>
                {loadingDetailId === id && !detail && (
                  <p style={{ padding: "8px 0", fontSize: 13 }}>
                    Đang tải chi tiết đơn hàng...
                  </p>
                )}

                {/* --- HIỂN THỊ MAP --- */}
                {detail &&
                  (status === "Delivering" || status === "Delivered") && (
                    <div style={{ marginBottom: 16, marginTop: 8 }}>
                      {hasCoords ? (
                        <div
                          style={{
                            border: "1px solid #ddd",
                            borderRadius: 8,
                            overflow: "hidden",
                          }}
                        >
                          <DroneDeliveryMap
                            originLat={detail.originLat ?? detail.OriginLat}
                            originLng={detail.originLon ?? detail.OriginLon}
                            destLat={detail.destLat ?? detail.DestLat}
                            destLng={detail.destLon ?? detail.DestLon}
                            status={status}
                            onFlightCompleted={() =>
                              handleDroneFlightCompleted(id)
                            }
                          />
                        </div>
                      ) : (
                        <p
                          style={{
                            fontSize: 13,
                            fontStyle: "italic",
                            color: "#666",
                          }}
                        >
                          Chưa có thông tin lộ trình bay (Thiếu tọa độ).
                        </p>
                      )}
                    </div>
                  )}
                {/* ------------------------------------------------ */}

                {detail && (
                  <OrderItems>
                    {items.map((item, idx) => (
                      <li key={item.productId ?? idx}>
                        <span>{item.productName ?? item.ProductName}</span>
                        <span>
                          {item.units ?? item.quantity ?? item.Units} x $
                          {(item.unitPrice ?? item.UnitPrice ?? 0).toFixed(2)}
                        </span>
                      </li>
                    ))}
                  </OrderItems>
                )}
                {/* ------------------------------------------------ */}

                {detail && status === "Delivered" && (
                  <div className="mt-3 flex gap-2">
                    <button
                      className="btn btn-primary"
                      onClick={() => handleConfirmDelivery(id)}
                    >
                      Received
                    </button>

                    <button
                      className="btn btn-outline"
                      onClick={() => handleReportNotReceived(id)}
                    >
                      Not Received
                    </button>
                  </div>
                )}
              </>
            )}
            <OrderFooter>
              {isOpen ? (
                <div
                  style={{
                    display: "flex",
                    flexDirection: "column",
                    alignItems: "flex-end",
                    gap: "4px",
                  }}
                >
                  <span style={{ fontSize: "14px", color: "#555" }}>
                    Subtotal: ${subTotal.toFixed(2)}
                  </span>
                  <span style={{ fontSize: "14px", color: "#555" }}>
                    Delivery Fee: ${Number(deliveryFee).toFixed(2)}
                  </span>
                  <div
                    style={{
                      borderTop: "1px solid #ccc",
                      marginTop: "4px",
                      paddingTop: "4px",
                      width: "100%",
                      textAlign: "right",
                    }}
                  >
                    <span
                      style={{
                        fontWeight: "bold",
                        fontSize: "16px",
                        color: "#d32f2f",
                      }}
                    >
                      Total: ${Number(finalTotal).toFixed(2)}
                    </span>
                  </div>
                </div>
              ) : (
                <span style={{ fontWeight: "bold" }}>
                  Total: ${Number(finalTotal).toFixed(2)}
                </span>
              )}
            </OrderFooter>
          </OrderCard>
        );
      })}
    </Container>
  );
}
