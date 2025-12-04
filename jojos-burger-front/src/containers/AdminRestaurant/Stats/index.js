import React, { useEffect, useState } from "react";

const KONG_CATALOG_BASE =
  process.env.REACT_APP_KONG_CATALOG_BASE ||
  "https://localhost:8443/api/catalog";

const RESTAURANT_STATUS_LABEL = {
  0: "Đang hoạt động",
  1: "Tạm ngưng",
  2: "Đã đóng cửa",
};

const AdminRestaurantOrders = () => {
  const [restaurants, setRestaurants] = useState([]);
  const [selectedRestaurantId, setSelectedRestaurantId] = useState("");
  const [orders, setOrders] = useState([]);
  const [loadingRestaurants, setLoadingRestaurants] = useState(false);
  const [loadingOrders, setLoadingOrders] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

  // ======================= LOAD RESTAURANTS (kể cả đã ẩn) =======================
  useEffect(() => {
    async function load() {
      try {
        setLoadingRestaurants(true);
        setError("");

        const res = await fetch(
          `${KONG_CATALOG_BASE}/admin/restaurants?includeDeleted=true`
        );
        if (!res.ok) throw new Error("Fetch restaurants failed");

        const data = await res.json();

        const parsed = data.map((r) => ({
          id: r.restaurantId,
          name: r.name,
          address: r.address,
          status: r.status,
          statusText: RESTAURANT_STATUS_LABEL[r.status] ?? "Không rõ",
          isDeleted: r.isDeleted,
          deletedAt: r.deletedAt,
        }));

        setRestaurants(parsed);
      } catch (err) {
        console.error(err);
        setError("Không thể tải danh sách nhà hàng.");
      } finally {
        setLoadingRestaurants(false);
      }
    }

    load();
  }, []);

  // ======================= LOAD ORDERS THEO NHÀ HÀNG =======================
  async function handleLoadOrders() {
    if (!selectedRestaurantId) {
      setError("Vui lòng chọn nhà hàng.");
      return;
    }

    try {
      setLoadingOrders(true);
      setError("");
      setMessage("");

      const res = await fetch(
        `${KONG_CATALOG_BASE}/admin/restaurant-orders/${selectedRestaurantId}`
      );

      if (!res.ok) {
        const text = await res.text();
        throw new Error(text || "Fetch orders failed");
      }

      const data = await res.json();
      setOrders(data);

      if (data.length === 0) {
        setMessage("Nhà hàng này chưa có đơn nào.");
      }
    } catch (err) {
      console.error(err);
      setError("Không thể tải danh sách đơn hàng.");
    } finally {
      setLoadingOrders(false);
    }
  }

  const selectedRestaurant = restaurants.find(
    (r) => r.id === selectedRestaurantId
  );

  return (
    <div style={{ padding: 24 }}>
      <h1>Thống kê đơn hàng theo Nhà hàng</h1>

      {error && (
        <div
          style={{
            padding: 10,
            background: "#ffe6e6",
            marginBottom: 16,
            borderRadius: 4,
            border: "1px solid #ff4d4f",
          }}
        >
          {error}
        </div>
      )}

      {message && (
        <div
          style={{
            padding: 10,
            background: "#e6ffe6",
            marginBottom: 16,
            borderRadius: 4,
            border: "1px solid #52c41a",
          }}
        >
          {message}
        </div>
      )}

      {/* ========== Chọn nhà hàng ========== */}
      <div
        style={{
          marginBottom: 16,
          display: "flex",
          gap: 12,
          alignItems: "center",
        }}
      >
        <label>Chọn nhà hàng:</label>
        {loadingRestaurants ? (
          <span>Đang tải danh sách nhà hàng...</span>
        ) : (
          <select
            value={selectedRestaurantId}
            onChange={(e) => {
              setSelectedRestaurantId(e.target.value);
              setOrders([]);
              setMessage("");
            }}
          >
            <option value="">-- Chọn nhà hàng --</option>
            {restaurants.map((r) => (
              <option key={r.id} value={r.id}>
                {r.name}
                {r.isDeleted ? " (ĐÃ ẨN)" : ""} - {r.statusText}
              </option>
            ))}
          </select>
        )}

        <button
          onClick={handleLoadOrders}
          disabled={!selectedRestaurantId || loadingOrders}
        >
          {loadingOrders ? "Đang tải đơn..." : "Xem đơn hàng"}
        </button>
      </div>

      {/* Info nhà hàng */}
      {selectedRestaurant && (
        <div
          style={{
            padding: 12,
            marginBottom: 16,
            borderRadius: 4,
            border: "1px solid #ddd",
            background: "#fafafa",
          }}
        >
          <b>{selectedRestaurant.name}</b>{" "}
          {selectedRestaurant.isDeleted && (
            <span style={{ color: "red" }}>(ĐÃ ẨN)</span>
          )}
          <br />
          Địa chỉ: {selectedRestaurant.address}
          <br />
          Trạng thái: {selectedRestaurant.statusText}
          {selectedRestaurant.deletedAt && (
            <>
              <br />
              Thời điểm ẩn:{" "}
              {new Date(selectedRestaurant.deletedAt).toLocaleString()}
            </>
          )}
        </div>
      )}

      {/* ========== Bảng đơn hàng ========== */}
      {orders.length > 0 && (
        <table
          border="1"
          cellPadding="8"
          style={{
            width: "100%",
            borderCollapse: "collapse",
            marginTop: 16,
          }}
        >
          <thead>
            <tr>
              <th>Mã đơn</th>
              <th>Ngày đặt</th>
              <th>Trạng thái</th>
              <th>Tổng tiền</th>
            </tr>
          </thead>
          <tbody>
            {orders.map((o) => (
              <tr key={o.orderNumber}>
                <td>{o.orderNumber}</td>
                <td>
                  {o.date
                    ? new Date(o.date).toLocaleString()
                    : "Không rõ"}
                </td>
                <td>{o.status}</td>
                <td>
                  {typeof o.total === "number"
                    ? o.total.toLocaleString("vi-VN", {
                        style: "currency",
                        currency: "VND",
                      })
                    : o.total}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {orders.length === 0 && !loadingOrders && selectedRestaurantId && !message && (
        <p>Chưa có dữ liệu đơn hàng.</p>
      )}
    </div>
  );
};

export default AdminRestaurantOrders;
