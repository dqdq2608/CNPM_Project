import React, { useEffect, useState } from "react";
import axios from "axios";

const KONG_CATALOG_BASE =
  process.env.REACT_APP_KONG_CATALOG_BASE ||
  "https://localhost:8443/api/catalog";

const RESTAURANT_STATUS_LABEL = {
  0: "Đang hoạt động",
  1: "Tạm ngưng",
  2: "Đã đóng cửa",
};

const STATUS_COLOR = {
  0: "green",
  1: "orange",
  2: "red",
};

const DeleteRestaurant = () => {
  const [restaurants, setRestaurants] = useState([]);
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [deletingId, setDeletingId] = useState(null);

  // ====================== helper: lấy số đơn theo nhà hàng ======================
  async function fetchOrderCount(restaurantId, restaurantName) {
    try {
      const res = await fetch(
        `${KONG_CATALOG_BASE}/restaurants/${restaurantId}/order-count`
      );

      if (!res.ok) {
        console.warn(
          `[DeleteRestaurant] Không lấy được số đơn cho nhà hàng ${restaurantName} (id=${restaurantId}), status=${res.status}`
        );
        return 0;
      }

      const data = await res.json();
      const count = data?.orderCount ?? 0;

      console.log(
        `[DeleteRestaurant] Nhà hàng "${restaurantName}" (id=${restaurantId}) có ${count} đơn.`
      );

      return count;
    } catch (err) {
      console.error(
        `[DeleteRestaurant] Lỗi khi gọi /order-count cho nhà hàng ${restaurantName} (id=${restaurantId})`,
        err
      );
      return 0;
    }
  }

  // ========================= LOAD danh sách =========================
  useEffect(() => {
    loadRestaurants();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function loadRestaurants() {
    try {
      setLoading(true);
      setError("");
      setMessage("");

      const res = await fetch(`${KONG_CATALOG_BASE}/admin/restaurants`);
      if (!res.ok) throw new Error("Fetch failed");

      const data = await res.json();

      // parse cơ bản
      const baseList = data.map((r) => ({
        id: r.restaurantId,
        name: r.name,
        address: r.address,
        status: r.status,
        statusText: RESTAURANT_STATUS_LABEL[r.status] ?? "Không rõ",
        isDeleted: r.isDeleted,
        deletedAt: r.deletedAt,
        orderCount: 0,
      }));

      // gọi thêm API lấy số đơn cho từng nhà hàng (tuần tự cho đơn giản)
      const withOrders = [];
      for (const rest of baseList) {
        const count = await fetchOrderCount(rest.id, rest.name);
        withOrders.push({ ...rest, orderCount: count });
      }

      setRestaurants(withOrders);
    } catch (err) {
      console.error(err);
      setError("Không thể tải danh sách nhà hàng.");
    } finally {
      setLoading(false);
    }
  }

  // ========================= XOÁ =========================
  async function handleDelete(r) {
    setMessage("");
    setError("");

    const ok = window.confirm(
      `Bạn có chắc muốn xoá nhà hàng: ${r.name}?\n\n` +
        "• Nếu có đơn hàng → chỉ đóng cửa (soft delete).\n" +
        "• Nếu không có đơn → xoá hoàn toàn."
    );
    if (!ok) return;

    try {
      setDeletingId(r.id);

      const response = await axios.delete(
        `${KONG_CATALOG_BASE}/restaurants/${r.id}`,
        {
          validateStatus: () => true, // luôn nhận body kể cả 4xx
        }
      );

      const data = response.data;

      // ======================== HARD DELETE ========================
      if (response.status === 204) {
        setMessage(`Đã xoá hoàn toàn nhà hàng: ${r.name}.`);
        // remove khỏi UI
        setRestaurants((prev) => prev.filter((x) => x.id !== r.id));
        return;
      }

      // ======================== SOFT DELETE ========================
      if (response.status === 400 && data && data.softDeleted) {
        const count = data.orderCount ?? r.orderCount ?? 0;

        setMessage(
          `Nhà hàng "${r.name}" có ${count} đơn → đã chuyển sang trạng thái "Đã đóng cửa" (soft delete).`
        );

        console.log(
          `[DeleteRestaurant] SOFT DELETE: nhà hàng "${r.name}" có ${count} đơn.`
        );

        // API /admin/restaurants đã không trả IsDeleted=true nữa,
        // nên ta xoá khỏi UI cho khớp với backend.
        setRestaurants((prev) => prev.filter((x) => x.id !== r.id));
        return;
      }

      // ======================== Lỗi khác =========================
      setError(
        (data && data.message) || "Không xoá được nhà hàng. Vui lòng thử lại."
      );
    } catch (err) {
      console.error(err);
      setError("Lỗi khi xoá nhà hàng.");
    } finally {
      setDeletingId(null);
    }
  }

  return (
    <div style={{ padding: 24 }}>
      <h1>Xoá Nhà hàng</h1>

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

      {loading ? (
        <p>Đang tải...</p>
      ) : (
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
              <th>Tên</th>
              <th>Địa chỉ</th>
              <th>Trạng thái</th>
              <th>Số đơn hàng</th>
              <th>Hành động</th>
            </tr>
          </thead>
          <tbody>
            {restaurants.map((r) => (
              <tr key={r.id}>
                <td>{r.name}</td>
                <td>{r.address}</td>
                <td>
                  <b style={{ color: STATUS_COLOR[r.status] }}>
                    {r.statusText}
                  </b>
                </td>
                <td style={{ textAlign: "center" }}>
                  {r.orderCount ?? 0}
                </td>
                <td>
                  <button
                    style={{
                      color: "white",
                      background: "red",
                      padding: "6px 12px",
                      borderRadius: "4px",
                      border: "none",
                      cursor: deletingId === r.id ? "not-allowed" : "pointer",
                      opacity: deletingId === r.id ? 0.6 : 1,
                    }}
                    disabled={deletingId === r.id}
                    onClick={() => handleDelete(r)}
                  >
                    {deletingId === r.id ? "Đang xoá..." : "Xoá"}
                  </button>
                </td>
              </tr>
            ))}
            {restaurants.length === 0 && !loading && (
              <tr>
                <td colSpan="5" style={{ textAlign: "center" }}>
                  Chưa có nhà hàng nào.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      )}
    </div>
  );
};

export default DeleteRestaurant;
