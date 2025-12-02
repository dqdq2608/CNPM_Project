import React, { useEffect, useState } from "react";

const KONG_CATALOG_BASE =
  process.env.REACT_APP_KONG_CATALOG_BASE ||
  "https://localhost:8443/api/catalog"; // sửa nếu Kong chạy port khác

// Map enum số sang text dễ đọc
const RESTAURANT_STATUS_LABEL = {
  0: "Đang hoạt động", // Active
  1: "Tạm ngưng",      // Inactive
  2: "Đã đóng cửa",    // Closed
};

async function fetchRestaurantsDirect() {
  const url = `${KONG_CATALOG_BASE}/admin/restaurants`;

  const response = await fetch(url, {
    method: "GET",
    headers: {
      "Content-Type": "application/json",
    },
    credentials: "omit",
  });

  if (!response.ok) {
    throw new Error(`Failed to fetch restaurants: ${response.status}`);
  }

  const data = await response.json();

  // Backend trả: restaurantId, name, address, lat, lng, adminEmail, status
  return data.map((r) => {
    const status = r.status; // enum dạng số 0/1/2
    return {
      id: r.restaurantId,
      name: r.name,
      address: r.address,
      latitude: r.lat ?? 0,
      longitude: r.lng ?? 0,
      email: r.adminEmail || "",
      status, // giữ raw value
      statusText: RESTAURANT_STATUS_LABEL[status] || "Không rõ",
      raw: r,
    };
  });
}

// Style màu cho từng trạng thái
const getStatusStyle = (status) => {
  switch (status) {
    case 0: // Active
      return {
        backgroundColor: "#e6ffed",
        color: "#0f8a2b",
        padding: "2px 8px",
        borderRadius: 12,
        fontSize: 12,
        fontWeight: 600,
      };
    case 1: // Inactive
      return {
        backgroundColor: "#fff4e5",
        color: "#b45d00",
        padding: "2px 8px",
        borderRadius: 12,
        fontSize: 12,
        fontWeight: 600,
      };
    case 2: // Closed
    default:
      return {
        backgroundColor: "#f5f5f5",
        color: "#666",
        padding: "2px 8px",
        borderRadius: 12,
        fontSize: 12,
        fontWeight: 600,
      };
  }
};

const ListRestaurants = () => {
  const [restaurants, setRestaurants] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    async function load() {
      try {
        setLoading(true);
        setError("");

        const data = await fetchRestaurantsDirect();
        setRestaurants(data);
      } catch (err) {
        console.error(err);
        setError("Không thể tải danh sách nhà hàng");
      } finally {
        setLoading(false);
      }
    }

    load();
  }, []);

  const formatCoord = (value) =>
    value === 0 || value === null || value === undefined
      ? "Chưa có"
      : Number(value).toFixed(6);

  return (
    <div style={{ padding: 24 }}>
      <h1>Danh sách Nhà hàng</h1>

      {error && (
        <div
          style={{
            padding: 10,
            background: "#ffe5e5",
            marginBottom: 12,
            borderRadius: 4,
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
          style={{ width: "100%", borderCollapse: "collapse", marginTop: 16 }}
        >
          <thead>
            <tr>
              <th>Tên</th>
              <th>Email</th>
              <th>Địa chỉ</th>
              <th>Vĩ độ (Lat)</th>
              <th>Kinh độ (Lng)</th>
              <th>Trạng thái</th>
            </tr>
          </thead>
          <tbody>
            {restaurants.map((r) => (
              <tr key={r.id}>
                <td>{r.name}</td>
                <td>{r.email || "Chưa có"}</td>
                <td>{r.address}</td>
                <td>{formatCoord(r.latitude)}</td>
                <td>{formatCoord(r.longitude)}</td>
                <td>
                  <span style={getStatusStyle(r.status)}>{r.statusText}</span>
                </td>
              </tr>
            ))}
            {restaurants.length === 0 && (
              <tr>
                <td colSpan="6" style={{ textAlign: "center" }}>
                  Chưa có nhà hàng nào
                </td>
              </tr>
            )}
          </tbody>
        </table>
      )}
    </div>
  );
};

export default ListRestaurants;
