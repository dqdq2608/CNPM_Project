import L from "leaflet";
import PropTypes from "prop-types";
import React, { useEffect, useState, useRef } from "react";
import { MapContainer, TileLayer, Marker, Popup, useMap } from "react-leaflet";
import "leaflet/dist/leaflet.css";

// --- Assets (Icon) ---
const droneIcon = new L.Icon({
  iconUrl: "https://cdn-icons-png.flaticon.com/512/3063/3063822.png", // Icon Drone
  iconSize: [40, 40],
  iconAnchor: [20, 20],
});

const pinIcon = new L.Icon({
  iconUrl: "https://unpkg.com/leaflet@1.7.1/dist/images/marker-icon.png",
  iconSize: [25, 41],
  iconAnchor: [12, 41],
});

// --- Sub-component: Tự động zoom vừa khít 2 điểm ---
function FitBounds({ start, end }) {
  const map = useMap();
  useEffect(() => {
    if (start && end) {
      const bounds = L.latLngBounds([start, end]);
      map.fitBounds(bounds, { padding: [50, 50] }); // Cách lề 50px
    }
  }, [start, end, map]);
  return null;
}

// ✅ THÊM ĐOẠN NÀY ĐỂ SỬA LỖI ESLINT CHO FITBOUNDS
FitBounds.propTypes = {
  start: PropTypes.array.isRequired,
  end: PropTypes.array.isRequired,
};

// --- Main Component ---
const DroneDeliveryMap = ({ originLat, originLng, destLat, destLng }) => {
  const [dronePos, setDronePos] = useState([originLat, originLng]);
  const requestRef = useRef();
  const startTimeRef = useRef();
  const DURATION = 8000; // Bay trong 8 giây

  // Hàm nội suy tuyến tính (Linear Interpolation)
  const lerp = (start, end, t) => start + (end - start) * t;

  useEffect(() => {
    const animate = (time) => {
      if (!startTimeRef.current) startTimeRef.current = time;
      const progress = Math.min((time - startTimeRef.current) / DURATION, 1);

      setDronePos([
        lerp(originLat, destLat, progress),
        lerp(originLng, destLng, progress),
      ]);

      if (progress < 1) requestRef.current = requestAnimationFrame(animate);
    };

    requestRef.current = requestAnimationFrame(animate);
    return () => cancelAnimationFrame(requestRef.current);
  }, [originLat, originLng, destLat, destLng]);

  return (
    <MapContainer
      center={[originLat, originLng]}
      zoom={13}
      style={{ height: "300px", width: "100%", borderRadius: "8px" }}
      scrollWheelZoom={false}
    >
      <TileLayer url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" />

      {/* Gọi component phụ để zoom */}
      <FitBounds start={[originLat, originLng]} end={[destLat, destLng]} />

      <Marker position={[originLat, originLng]} icon={pinIcon}>
        <Popup>Nhà hàng</Popup>
      </Marker>
      <Marker position={[destLat, destLng]} icon={pinIcon}>
        <Popup>Khách hàng</Popup>
      </Marker>
      <Marker position={dronePos} icon={droneIcon} zIndexOffset={1000}>
        <Popup>Đang giao hàng...</Popup>
      </Marker>
    </MapContainer>
  );
};

DroneDeliveryMap.propTypes = {
  originLat: PropTypes.number.isRequired,
  originLng: PropTypes.number.isRequired,
  destLat: PropTypes.number.isRequired,
  destLng: PropTypes.number.isRequired,
};

export default DroneDeliveryMap;
