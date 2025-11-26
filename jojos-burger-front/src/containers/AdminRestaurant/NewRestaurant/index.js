import React, { useState } from "react";
import axios from "axios";
import { useHistory } from "react-router-dom";
import paths from "../../../constants/paths";
import MapPicker from "../../../components/MapPicker";

const API_BASE =
  process.env.REACT_APP_RESTAURANT_API_BASE ||
  "https://localhost:8443/api/catalog";

const NewRestaurant = () => {
  const history = useHistory();
  const [error, setError] = useState("");
  const [addressLoading, setAddressLoading] = useState(false);

  const [form, setForm] = useState({
    name: "",
    address: "",
    lat: null,
    lng: null,
  });

  const handleChange = (e) => {
    const { name, value } = e.target;
    setError("");
    setForm((prev) => ({ ...prev, [name]: value }));
  };

  // 🔹 Nhập địa chỉ -> tìm trên bản đồ (forward geocoding)
  const handleFindOnMap = async () => {
    if (!form.address || !form.address.trim()) {
      setError("Hãy nhập địa chỉ trước khi tìm trên bản đồ");
      return;
    }

    try {
      setAddressLoading(true);
      setError("");

      const url = `https://nominatim.openstreetmap.org/search?format=json&limit=1&q=${encodeURIComponent(
        form.address
      )}`;

      const res = await fetch(url, {
        headers: { "Accept-Language": "vi" },
      });
      const data = await res.json();

      if (!Array.isArray(data) || data.length === 0) {
        setError("Không tìm thấy vị trí phù hợp với địa chỉ này");
        return;
      }

      const { lat, lon } = data[0];

      setForm((prev) => ({
        ...prev,
        lat: Number(lat),
        lng: Number(lon),
      }));
    } catch (err) {
      console.error(err);
      setError("Lỗi khi tìm vị trí trên bản đồ từ địa chỉ");
    } finally {
      setAddressLoading(false);
    }
  };

  // 🔹 Click map -> set lat/lng + tự điền lại địa chỉ (reverse geocoding)
  const handleMapSelect = async ({ lat, lng }) => {
    setForm((prev) => ({
      ...prev,
      lat,
      lng,
    }));

    try {
      const url = `https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lng}`;
      const res = await fetch(url, {
        headers: { "Accept-Language": "vi" },
      });
      const data = await res.json();
      const displayName = data?.display_name;

      if (displayName) {
        setForm((prev) => ({
          ...prev,
          address: displayName,
        }));
      }
    } catch (err) {
      console.error("Reverse geocode error", err);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");

    if (!form.lat || !form.lng) {
      setError("Bạn phải chọn vị trí trên bản đồ (hoặc tìm từ địa chỉ).");
      return;
    }

    const payload = {
      name: form.name,
      address: form.address,
      lat: Number(form.lat),
      lng: Number(form.lng),
    };

    try {
      // ✅ GỌI THÔNG QUA KONG: https://localhost:8443/api/catalog/restaurants-with-admin
      const res = await axios.post(
        `${API_BASE}/restaurants-with-admin`,
        payload
      );

      const admin = res.data?.admin || res.data?.Admin; // tuỳ backend trả về
      if (admin) {
        const email = admin.email || admin.Email;
        const tempPassword = admin.tempPassword || admin.TempPassword;

        alert(
          `Tạo nhà hàng & tài khoản admin thành công:\nEmail: ${email}\nMật khẩu: ${tempPassword}`
        );
      } else {
        alert("Tạo nhà hàng thành công");
      }

      history.push(paths.Restaurants); // path danh sách nhà hàng
    } catch (err) {
      console.error(err);
      setError("Lỗi khi thêm nhà hàng / tạo tài khoản admin");
    }
  };

  return (
    <div style={{ padding: 24 }}>
      <h1>Thêm Nhà hàng</h1>

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

      <form
        onSubmit={handleSubmit}
        style={{
          maxWidth: 700,
          border: "1px solid #ccc",
          padding: 20,
          borderRadius: 6,
          background: "#fff",
        }}
      >
        <label>Tên *</label>
        <input
          name="name"
          required
          value={form.name}
          onChange={handleChange}
          style={{ width: "100%", marginBottom: 10 }}
        />

        <label>Địa chỉ</label>
        <div style={{ display: "flex", gap: 8, marginBottom: 10 }}>
          <input
            name="address"
            value={form.address}
            onChange={handleChange}
            style={{ flex: 1 }}
            placeholder="Nhập địa chỉ cửa hàng"
          />
          <button
            type="button"
            onClick={handleFindOnMap}
            disabled={addressLoading}
          >
            {addressLoading ? "Đang tìm..." : "Tìm trên bản đồ"}
          </button>
        </div>

        <label>Chọn vị trí trên bản đồ *</label>
        <MapPicker
          value={
            form.lat && form.lng
              ? { lat: Number(form.lat), lng: Number(form.lng) }
              : null
          }
          onChange={handleMapSelect}
        />  

        <div style={{ marginTop: 15 }}>
          <strong>Lat:</strong> {form.lat || "Chưa chọn"} &nbsp; | &nbsp;
          <strong>Lng:</strong> {form.lng || "Chưa chọn"}
        </div>

        <button type="submit" style={{ marginTop: 20 }}>
          Thêm mới
        </button>
      </form>
    </div>
  );
};

export default NewRestaurant;
