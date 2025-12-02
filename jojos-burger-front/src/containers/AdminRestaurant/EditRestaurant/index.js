// src/containers/AdminRestaurant/EditRestaurant/index.js
import React, { useEffect, useState } from "react";
import axios from "axios";
import { useHistory } from "react-router-dom";
import paths from "../../../constants/paths";
import MapPicker from "../../../components/MapPicker";

// Base URL qua Kong
const KONG_CATALOG_BASE =
  process.env.REACT_APP_KONG_CATALOG_BASE ||
  "https://localhost:8443/api/catalog";

// Map enum status ↔ text
const RESTAURANT_STATUS_LABEL = {
  0: "Đang hoạt động", // Active
  1: "Tạm ngưng", // Inactive
  2: "Đã đóng cửa", // Closed
};

const RESTAURANT_STATUS_OPTIONS = [
  { value: 0, label: "Đang hoạt động" },
  { value: 1, label: "Tạm ngưng" },
  { value: 2, label: "Đã đóng cửa" },
];

// ======================= Fetch danh sách =======================
async function fetchRestaurantsForAdmin() {
  const url = `${KONG_CATALOG_BASE}/admin/restaurants`;

  const response = await fetch(url, {
    method: "GET",
    headers: { "Content-Type": "application/json" },
    credentials: "omit",
  });

  if (!response.ok) {
    throw new Error(`Failed to fetch: ${response.status}`);
  }

  const data = await response.json();

  return data.map((r) => ({
    id: r.restaurantId || r.id,
    name: r.name,
    address: r.address,
    latitude: r.lat ?? r.latitude ?? 0,
    longitude: r.lng ?? r.longitude ?? 0,
    adminEmail: r.adminEmail || r.email || "",
    status: r.status ?? 0, // enum từ backend
  }));
}

// =================== Forward geocode (địa chỉ → toạ độ) ===================
async function geocodeAddress(address) {
  if (!address || !address.trim()) return null;

  const url = `https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(
    address
  )}&limit=1`;

  const res = await fetch(url, {
    headers: { "Accept-Language": "vi" },
  });

  if (!res.ok) return null;

  const data = await res.json();
  if (!data || !data.length) return null;

  const first = data[0];
  return {
    lat: Number(first.lat),
    lng: Number(first.lon),
    displayName: first.display_name,
  };
}

// =================== Reverse geocode (toạ độ → địa chỉ) ===================
async function reverseGeocode(lat, lng) {
  const url = `https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lng}&zoom=18&addressdetails=1`;

  const res = await fetch(url, {
    headers: { "Accept-Language": "vi" },
  });

  if (!res.ok) return null;

  const data = await res.json();
  return data.display_name || null;
}

// ============================ Component ============================
const EditRestaurant = () => {
  const history = useHistory();

  const [restaurants, setRestaurants] = useState([]);
  const [listLoading, setListLoading] = useState(false);
  const [error, setError] = useState("");

  const [selected, setSelected] = useState(null);
  const [geocodeLoading, setGeocodeLoading] = useState(false);
  const [savingLocation, setSavingLocation] = useState(false);
  const [savingStatusId, setSavingStatusId] = useState(null); // id nhà hàng đang đổi status

  const [form, setForm] = useState({
    address: "",
    lat: "",
    lng: "",
    adminEmail: "",
  });

  // ==== Tự load danh sách khi vào trang ====
  useEffect(() => {
    async function load() {
      try {
        setListLoading(true);
        setError("");
        const data = await fetchRestaurantsForAdmin();
        setRestaurants(data);
      } catch (err) {
        console.error(err);
        setError("Không thể tải danh sách nhà hàng");
      } finally {
        setListLoading(false);
      }
    }
    load();
  }, []);

  const handleSelect = (r) => {
    setSelected(r);
    setForm({
      address: r.address || "",
      lat: r.latitude ? String(r.latitude) : "",
      lng: r.longitude ? String(r.longitude) : "",
      adminEmail: r.adminEmail,
    });
  };

  const handleChange = (e) => {
    const { name, value } = e.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  };

  // ================== Khi chọn vị trí trên bản đồ ==================
  const handleMapChange = ({ lat, lng }) => {
    const latStr = lat.toFixed(6);
    const lngStr = lng.toFixed(6);

    setForm((prev) => ({
      ...prev,
      lat: latStr,
      lng: lngStr,
    }));

    reverseGeocode(latStr, lngStr)
      .then((addr) => {
        if (!addr) return;
        setForm((prev) => ({
          ...prev,
          address: addr,
        }));
      })
      .catch((err) => console.error("Reverse geocode error:", err));
  };

  // ================== Khi bấm “Tìm vị trí từ địa chỉ” ==================
  const handleFindOnMap = async () => {
    if (!form.address) return;

    try {
      setGeocodeLoading(true);
      const result = await geocodeAddress(form.address);
      if (!result) {
        setError("Không tìm được vị trí từ địa chỉ này");
        return;
      }

      setForm((prev) => ({
        ...prev,
        lat: result.lat.toFixed(6),
        lng: result.lng.toFixed(6),
      }));
    } finally {
      setGeocodeLoading(false);
    }
  };

  // ================== Lưu vị trí mới ==================
  const handleSaveLocation = async () => {
    if (!selected) return;

    const payload = {
      restaurantId: selected.id,
      name: selected.name,
      address: form.address,
      lat: form.lat === "" ? 0 : Number(form.lat),
      lng: form.lng === "" ? 0 : Number(form.lng),
    };

    try {
      setSavingLocation(true);
      setError("");

      await axios.put(`${KONG_CATALOG_BASE}/restaurants/${selected.id}`, payload);

      const updatedList = restaurants.map((r) =>
        r.id === selected.id
          ? {
              ...r,
              address: payload.address,
              latitude: payload.lat,
              longitude: payload.lng,
            }
          : r
      );
      setRestaurants(updatedList);

      setSelected((prev) =>
        prev
          ? {
              ...prev,
              address: payload.address,
              latitude: payload.lat,
              longitude: payload.lng,
            }
          : prev
      );
    } catch (err) {
      console.error(err);
      setError("Lỗi khi cập nhật vị trí nhà hàng");
    } finally {
      setSavingLocation(false);
    }
  };

  // ================== Đổi trạng thái nhà hàng ==================
  const handleChangeStatus = async (r, newStatus) => {
    const newStatusNum = Number(newStatus);

    // Nếu đóng cửa thì confirm cho chắc
    if (newStatusNum === 2) {
      const ok = window.confirm(
        "Bạn chắc chắn muốn chuyển nhà hàng này sang trạng thái 'Đã đóng cửa'?\n" +
          "Nhà hàng sẽ không nhận đơn mới nữa."
      );
      if (!ok) return;
    }

    try {
      setSavingStatusId(r.id);
      setError("");

      await axios.put(
        `${KONG_CATALOG_BASE}/restaurants/${r.id}/status`,
        { status: newStatusNum },
        { headers: { "Content-Type": "application/json" } }
      );

      // Cập nhật UI
      setRestaurants((prev) =>
        prev.map((x) =>
          x.id === r.id
            ? {
                ...x,
                status: newStatusNum,
              }
            : x
        )
      );

      // Nếu đang chọn nhà hàng này ở form, cập nhật selected luôn
      setSelected((prev) =>
        prev && prev.id === r.id
          ? {
              ...prev,
              status: newStatusNum,
            }
          : prev
      );
    } catch (err) {
      console.error("Update status error", err);
      console.error("Response:", err.response?.status, err.response?.data);
      setError("Lỗi khi cập nhật trạng thái nhà hàng");
    } finally {
      setSavingStatusId(null);
    }
  };

  const mapValue =
    form.lat && form.lng
      ? { lat: Number(form.lat), lng: Number(form.lng) }
      : null;

  const formatCoord = (v) =>
    v === 0 || v === null || v === undefined ? "Chưa có" : Number(v).toFixed(6);

  return (
    <div style={{ padding: 24 }}>
      <h1>Quản lý Nhà hàng (chuyển chi nhánh / trạng thái)</h1>

      {error && (
        <div
          style={{
            padding: 10,
            background: "#ffe5e5",
            borderRadius: 4,
            marginBottom: 12,
          }}
        >
          {error}
        </div>
      )}

      <button onClick={() => history.push(paths.Restaurants)}>
        Quay lại danh sách
      </button>

      {/* ================= Bảng danh sách ================= */}
      {listLoading ? (
        <p>Đang tải danh sách...</p>
      ) : (
        <table
          border="1"
          cellPadding="8"
          style={{
            width: "100%",
            borderCollapse: "collapse",
            marginTop: 16,
            marginBottom: 24,
          }}
        >
          <thead>
            <tr>
              <th>Tên</th>
              <th>Địa chỉ</th>
              <th>Lat</th>
              <th>Lng</th>
              <th>Email admin</th>
              <th>Trạng thái</th>
              <th>Hành động</th>
            </tr>
          </thead>
          <tbody>
            {restaurants.map((r) => (
              <tr key={r.id}>
                <td>{r.name}</td>
                <td>{r.address}</td>
                <td>{formatCoord(r.latitude)}</td>
                <td>{formatCoord(r.longitude)}</td>
                <td>{r.adminEmail || "Chưa có"}</td>
                <td>
                  <span style={{ marginRight: 8 }}>
                    {RESTAURANT_STATUS_LABEL[r.status] || "Không rõ"}
                  </span>
                  <br />
                  <select
                    value={r.status}
                    onChange={(e) => handleChangeStatus(r, e.target.value)}
                    disabled={savingStatusId === r.id}
                  >
                    {RESTAURANT_STATUS_OPTIONS.map((opt) => (
                      <option key={opt.value} value={opt.value}>
                        {opt.label}
                      </option>
                    ))}
                  </select>
                </td>
                <td>
                  <button onClick={() => handleSelect(r)}>Chuyển chi nhánh</button>
                </td>
              </tr>
            ))}
            {restaurants.length === 0 && (
              <tr>
                <td colSpan="7" style={{ textAlign: "center" }}>
                  Chưa có nhà hàng nào
                </td>
              </tr>
            )}
          </tbody>
        </table>
      )}

      {/* ================= Form chuyển chi nhánh ================= */}
      {selected && (
        <div
          style={{
            border: "1px solid #ccc",
            padding: 20,
            borderRadius: 6,
            background: "#fff",
            display: "grid",
            gridTemplateColumns: "1fr 1.4fr",
            gap: 24,
          }}
        >
          <div>
            <h3>Chuyển chi nhánh cho: {selected.name}</h3>

            <p>
              <b>Trạng thái hiện tại:</b>{" "}
              {RESTAURANT_STATUS_LABEL[selected.status] || "Không rõ"}
            </p>

            <p>
              <b>Vị trí hiện tại:</b>
              <br />
              Lat: {formatCoord(selected.latitude)} | Lng:{" "}
              {formatCoord(selected.longitude)}
            </p>

            <label>Địa chỉ mới</label>
            <textarea
              name="address"
              value={form.address}
              onChange={handleChange}
              rows={3}
              style={{ width: "100%", marginBottom: 8 }}
            />

            <button
              type="button"
              onClick={handleFindOnMap}
              disabled={!form.address || geocodeLoading}
              style={{ marginBottom: 16 }}
            >
              {geocodeLoading ? "Đang tìm..." : "Tìm vị trí từ địa chỉ"}
            </button>

            <label>Vĩ độ mới (Lat)</label>
            <input
              name="lat"
              type="number"
              step="0.000001"
              value={form.lat}
              onChange={handleChange}
              style={{ width: "100%", marginBottom: 10 }}
            />

            <label>Kinh độ mới (Lng)</label>
            <input
              name="lng"
              type="number"
              step="0.000001"
              value={form.lng}
              onChange={handleChange}
              style={{ width: "100%", marginBottom: 10 }}
            />

            <label>Email admin</label>
            <input
              readOnly
              value={form.adminEmail}
              style={{ width: "100%", background: "#f5f5f5" }}
            />

            <div style={{ marginTop: 16 }}>
              <button onClick={handleSaveLocation} disabled={savingLocation}>
                {savingLocation ? "Đang lưu..." : "Lưu vị trí mới"}
              </button>
              <button
                type="button"
                onClick={() => setSelected(null)}
                style={{ marginLeft: 12 }}
              >
                Bỏ chọn
              </button>
            </div>
          </div>

          <div>
            <p>Chọn vị trí nhà hàng trên bản đồ:</p>
            <MapPicker value={mapValue} onChange={handleMapChange} />
          </div>
        </div>
      )}
    </div>
  );
};

export default EditRestaurant;
