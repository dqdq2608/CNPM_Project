import L from "leaflet";
import PropTypes from "prop-types";
import React, { useEffect } from "react";
import { MapContainer, TileLayer, Marker, Popup, useMap } from "react-leaflet";
import "leaflet/dist/leaflet.css";

// --- Assets (Icon) ---
const droneIcon = new L.Icon({
  iconUrl: "/drone.png",
  iconSize: [40, 40],
  iconAnchor: [20, 20],
});

const pinIcon = new L.Icon({
  iconUrl: "https://unpkg.com/leaflet@1.7.1/dist/images/marker-icon.png",
  iconSize: [25, 41],
  iconAnchor: [12, 41],
});

// --- Fit bounds ---
function FitBounds({ start, end }) {
  const map = useMap();
  useEffect(() => {
    if (start && end) {
      const bounds = L.latLngBounds([start, end]);
      map.fitBounds(bounds, { padding: [50, 50] });
    }
  }, [start, end, map]);
  return null;
}

FitBounds.propTypes = {
  start: PropTypes.array.isRequired,
  end: PropTypes.array.isRequired,
};

// --- Main Component ---
const DroneDeliveryMap = ({
  originLat,
  originLng,
  destLat,
  destLng,
  droneLat,
  droneLng,
  status,
}) => {
  // Nếu chưa có toạ độ drone từ BE, cho nó đứng tại nhà hàng
  const dronePos =
    typeof droneLat === "number" && typeof droneLng === "number"
      ? [droneLat, droneLng]
      : [originLat, originLng];

  return (
    <MapContainer
      center={[originLat, originLng]}
      zoom={13}
      style={{ height: "300px", width: "100%", borderRadius: "8px" }}
      scrollWheelZoom={false}
    >
      <TileLayer url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" />

      <FitBounds start={[originLat, originLng]} end={[destLat, destLng]} />

      <Marker position={[originLat, originLng]} icon={pinIcon}>
        <Popup>Nhà hàng</Popup>
      </Marker>

      <Marker position={[destLat, destLng]} icon={pinIcon}>
        <Popup>Khách hàng</Popup>
      </Marker>

      <Marker position={dronePos} icon={droneIcon} zIndexOffset={1000}>
        <Popup>
          {status === "Delivered"
            ? "Đã giao tới khách hàng"
            : "Đang giao hàng..."}
        </Popup>
      </Marker>
    </MapContainer>
  );
};

DroneDeliveryMap.propTypes = {
  originLat: PropTypes.number.isRequired,
  originLng: PropTypes.number.isRequired,
  destLat: PropTypes.number.isRequired,
  destLng: PropTypes.number.isRequired,
  droneLat: PropTypes.number, // 👈 mới
  droneLng: PropTypes.number, // 👈 mới
  status: PropTypes.string.isRequired,
};

export default DroneDeliveryMap;
