import React, { useEffect, useRef } from "react";
import PropTypes from "prop-types";
import L from "leaflet";
import "leaflet/dist/leaflet.css";

// Fix icon mặc định
const DefaultIcon = L.icon({
  iconUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png",
  shadowUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png",
  iconSize: [25, 41],
  iconAnchor: [12, 41],
});
L.Marker.prototype.options.icon = DefaultIcon;

/**
 * MapPicker
 *  - value: { lat, lng }
 *  - onChange: ({ lat, lng }) => void
 */
const MapPicker = ({ value, onChange }) => {
  const mapContainerRef = useRef(null);
  const mapRef = useRef(null);
  const markerRef = useRef(null);

  useEffect(() => {
    // Khởi tạo map lần đầu
    if (!mapRef.current && mapContainerRef.current) {
      const initialCenter =
        value?.lat && value?.lng
          ? [value.lat, value.lng]
          : [10.776389, 106.701944]; // Default: HCM

      mapRef.current = L.map(mapContainerRef.current).setView(
        initialCenter,
        13
      );

      L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
        maxZoom: 19,
        attribution:
          '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
      }).addTo(mapRef.current);

      // Click để chọn vị trí
      mapRef.current.on("click", (e) => {
        const { lat, lng } = e.latlng;

        if (!markerRef.current) {
          markerRef.current = L.marker([lat, lng]).addTo(mapRef.current);
        } else {
          markerRef.current.setLatLng([lat, lng]);
        }

        onChange && onChange({ lat, lng });
      });
    }

    // Nếu value thay đổi từ cha → update marker + view
    if (
      mapRef.current &&
      value &&
      typeof value.lat === "number" &&
      typeof value.lng === "number"
    ) {
      const latlng = [value.lat, value.lng];

      mapRef.current.setView(latlng, mapRef.current.getZoom() || 13);

      if (!markerRef.current) {
        markerRef.current = L.marker(latlng).addTo(mapRef.current);
      } else {
        markerRef.current.setLatLng(latlng);
      }
    }
  }, [value, onChange]);

  return (
    <div
      ref={mapContainerRef}
      style={{
        height: 500,
        width: "100%",
        marginTop: 10,
        borderRadius: 8,
        overflow: "hidden",
      }}
    />
  );
};

MapPicker.propTypes = {
  value: PropTypes.shape({
    lat: PropTypes.oneOfType([PropTypes.number, PropTypes.string]),
    lng: PropTypes.oneOfType([PropTypes.number, PropTypes.string]),
  }),
  onChange: PropTypes.func,
};

export default MapPicker;
