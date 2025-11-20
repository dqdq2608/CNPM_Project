// containers/MyOrders/index.js
import React, { useEffect, useState } from "react";
import { useHistory } from "react-router-dom";
import { toast } from "react-toastify";

import { fetchMyOrders, fetchOrderDetail } from "../../services/api/order";
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
  const [details, setDetails] = useState({}); // { [orderId]: detail }
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
    // nếu đang mở -> đóng lại
    if (expandedId === id) {
      setExpandedId(null);
      return;
    }

    // mở order này
    setExpandedId(id);

    // nếu đã có detail rồi thì không gọi lại
    if (details[id]) return;

    try {
      setLoadingDetailId(id);
      const detail = await fetchOrderDetail(id);
      setDetails((prev) => ({ ...prev, [id]: detail }));
    } catch (e) {
      console.error("Fetch order detail error:", e);
      toast.error("Lỗi khi tải chi tiết đơn hàng.");
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

  return (
    <Container>
      {orders.map((order) => {
        const id = order.orderNumber ?? order.id;
        const orderDate = order.date || order.orderDate;
        const status = order.status || order.orderStatus;
        const isOpen = expandedId === id;

        const detail = details[id];
        // tuỳ vào DTO chi tiết mà Ordering trả về
        const items =
          detail?.orderItems ||
          detail?.orderitems ||
          detail?.orderitemsDto ||
          [];

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

                {detail && (
                  <OrderItems>
                    {items.map((item, idx) => (
                      <li key={item.productId ?? idx}>
                        <span>{item.productName}</span>
                        <span>
                          {item.units ?? item.quantity} x $
                          {(item.unitPrice ?? 0).toFixed(2)}
                        </span>
                      </li>
                    ))}
                  </OrderItems>
                )}
              </>
            )}

            <OrderFooter>
              <span>
                Total: ${(order.total ?? order.Total ?? 0).toFixed(2)}
              </span>
            </OrderFooter>
          </OrderCard>
        );
      })}
    </Container>
  );
}
