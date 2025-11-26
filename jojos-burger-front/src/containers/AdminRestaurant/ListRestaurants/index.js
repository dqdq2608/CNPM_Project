import React, { useEffect, useState } from "react";

// ======================= FETCH QUA KONG =======================

// ⭐ Gọi trực tiếp Kong Gateway (bỏ BFF)
const KONG_CATALOG_BASE =
  process.env.REACT_APP_KONG_CATALOG_BASE ||
  "https://localhost:8443/api/catalog"; // sửa nếu Kong chạy port khác

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

  return data.map((r) => ({
    id: r.restaurantId || r.id,
    name: r.name,
    address: r.address,
    latitude: r.lat ?? r.latitude ?? 0,
    longitude: r.lng ?? r.longitude ?? 0,
    email: r.adminEmail || r.admin_email || r.email || "", // ⭐ thêm email
    raw: r,
  }));
}
// ===============================================================


const ListRestaurants = () => {
  const [restaurants, setRestaurants] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    async function load() {
      try {
        setLoading(true);
        setError(""); // ⭐ clear lỗi trước mỗi lần fetch

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
              <th>Email</th>        {/* ⭐ thêm cột Email */}
              <th>Địa chỉ</th>
              <th>Vĩ độ (Lat)</th>
              <th>Kinh độ (Lng)</th>
            </tr>
          </thead>
          <tbody>
            {restaurants.map((r) => (
              <tr key={r.id}>
                <td>{r.name}</td>
                <td>{r.email}</td>    {/* ⭐ hiển thị email */}
                <td>{r.address}</td>
                <td>{formatCoord(r.latitude)}</td>
                <td>{formatCoord(r.longitude)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
};

export default ListRestaurants;
