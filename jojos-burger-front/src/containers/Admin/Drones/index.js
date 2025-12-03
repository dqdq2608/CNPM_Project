// src/pages/restaurant/drones/index.js
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Modal from "@mui/material/Modal";
import Paper from "@mui/material/Paper";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import PropTypes from "prop-types";
import React, { useEffect, useMemo, useState } from "react";
import { MapContainer, TileLayer, Marker, Popup } from "react-leaflet";

import {
  createDrone,
  DroneStatus,
  fetchDrones,
  updateDroneStatus,
} from "../../../services/api/drone";
import { Container, Menu, LinkMenu } from "../Orders/styles";
import droneStatusTabs from "./drone-status";
const RESTAURANT_LAT = 10.8231;
const RESTAURANT_LNG = 106.6297;

const DRONE_STATUS_LABEL = {
  [DroneStatus.Idle]: "Idle",
  [DroneStatus.Delivering]: "Delivering",
  [DroneStatus.Charging]: "Charging",
  [DroneStatus.Maintenance]: "Maintenance",
  [DroneStatus.Offline]: "Offline",
};

// Màu text đơn giản theo trạng thái
const DRONE_STATUS_COLOR = {
  Idle: "#2e7d32", // green
  Delivering: "#ed6c02", // orange
  Charging: "#0288d1", // blue
  Maintenance: "#6d4c41", // brown
  Offline: "#9e9e9e", // gray
};

function getStatusTextFromNumeric(statusNumber) {
  return DRONE_STATUS_LABEL[statusNumber] || "Unknown";
}

// Tạo điểm random quanh nhà hàng, để drone Delivering bay tới đó (giả)
function createRandomTarget() {
  const deltaLat = (Math.random() - 0.5) * 0.02; // ~2km
  const deltaLng = (Math.random() - 0.5) * 0.02;
  return {
    lat: RESTAURANT_LAT + deltaLat,
    lng: RESTAURANT_LNG + deltaLng,
  };
}

function DronePage({ restaurantId }) {
  const [openCreate, setOpenCreate] = useState(false);
  const [newCode, setNewCode] = useState("");

  const [drones, setDrones] = useState([]);
  const [activeStatus, setActiveStatus] = useState("All");
  const [loading, setLoading] = useState(false);

  // state mô phỏng vị trí drone: { [droneId]: { lat, lng, startTime, durationMs, origin, target, progress } }
  const [simPositions, setSimPositions] = useState({});

  async function loadDrones() {
    setLoading(true);
    try {
      const data = await fetchDrones();
      setDrones(data || []);
    } catch (err) {
      console.error(err);
      // có thể toast error nếu bạn đã dùng react-toastify
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadDrones();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Filter cho table + map
  const filteredDrones = useMemo(() => {
    if (activeStatus === "All") return drones;

    return drones.filter((d) => {
      const text = getStatusTextFromNumeric(d.status);
      return text === activeStatus;
    });
  }, [activeStatus, drones]);

  // Simulate vị trí drone Delivering: mỗi 1s update
  useEffect(() => {
    const intervalId = setInterval(() => {
      const now = Date.now();

      setSimPositions((prev) => {
        const next = { ...prev };

        drones.forEach((d) => {
          const statusText = getStatusTextFromNumeric(d.status);

          // chỉ simulate với drone Delivering
          if (statusText !== "Delivering") {
            return;
          }

          const existing = prev[d.id] || {
            startTime: now,
            durationMs: 60 * 1000, // 60s "bay"
            origin: { lat: RESTAURANT_LAT, lng: RESTAURANT_LNG },
            target: createRandomTarget(),
          };

          const elapsed = now - existing.startTime;
          const t = Math.max(0, Math.min(1, elapsed / existing.durationMs));

          const lat =
            existing.origin.lat +
            (existing.target.lat - existing.origin.lat) * t;
          const lng =
            existing.origin.lng +
            (existing.target.lng - existing.origin.lng) * t;

          next[d.id] = {
            ...existing,
            lat,
            lng,
            progress: t,
          };

          // Khi bay xong (t >= 1) bạn có thể:
          // - Gọi API updateDroneStatus(d.id, DroneStatus.Idle)
          // - Gọi API complete assignment
          // Để đơn giản, demo chỉ dừng lại ở việc giữ vị trí ở target.
        });

        return next;
      });
    }, 1000);

    return () => clearInterval(intervalId);
  }, [drones]);

  // Lấy vị trí hiển thị trên map: ưu tiên simPositions, fallback sang currentLatitude/currentLongitude, cuối cùng là nhà hàng
  function getDronePosition(d) {
    const sim = simPositions[d.id];
    if (sim && typeof sim.lat === "number" && typeof sim.lng === "number") {
      return [sim.lat, sim.lng];
    }

    if (
      typeof d.currentLatitude === "number" &&
      typeof d.currentLongitude === "number"
    ) {
      return [d.currentLatitude, d.currentLongitude];
    }

    return [RESTAURANT_LAT, RESTAURANT_LNG];
  }

  async function handleSetStatus(id, status) {
    try {
      await updateDroneStatus(id, status);
      await loadDrones();
    } catch (err) {
      console.error(err);
    }
  }

  return (
    <Container>
      {/* Menu filter trạng thái giống Orders */}
      <Menu>
        {droneStatusTabs.map((item) => (
          <LinkMenu
            key={item.id}
            isActiveStatus={activeStatus === item.value}
            onClick={() => setActiveStatus(item.value)}
          >
            {item.label}
          </LinkMenu>
        ))}
      </Menu>

      {/* Map hiển thị vị trí drone */}
      <div style={{ height: "400px", margin: "0 20px 20px" }}>
        <MapContainer
          center={[RESTAURANT_LAT, RESTAURANT_LNG]}
          zoom={13}
          style={{ height: "100%", width: "100%" }}
        >
          <TileLayer
            attribution="&copy; OpenStreetMap contributors"
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          />
          {filteredDrones.map((d) => {
            const [lat, lng] = getDronePosition(d);
            const statusText = getStatusTextFromNumeric(d.status);
            const sim = simPositions[d.id];
            const progressPercent = sim
              ? Math.round((sim.progress || 0) * 100)
              : null;

            return (
              <Marker key={d.id} position={[lat, lng]}>
                <Popup>
                  <div>
                    <div>
                      <strong>Drone:</strong> {d.code}
                    </div>
                    <div>
                      <strong>Status:</strong> {statusText}
                    </div>
                    <div>
                      <strong>Lat / Lng:</strong> {lat.toFixed(5)},{" "}
                      {lng.toFixed(5)}
                    </div>
                    {statusText === "Delivering" &&
                      progressPercent !== null && (
                        <div>
                          <strong>Progress:</strong> {progressPercent}%
                        </div>
                      )}
                  </div>
                </Popup>
              </Marker>
            );
          })}
        </MapContainer>
      </div>

      {/* Bảng danh sách drone giống Orders table */}
      <div
        style={{
          display: "flex",
          justifyContent: "flex-end",
          padding: "0 20px 20px",
        }}
      >
        <Button
          variant="contained"
          color="primary"
          onClick={() => setOpenCreate(true)}
        >
          Add New Drone
        </Button>
      </div>
      <Modal open={openCreate} onClose={() => setOpenCreate(false)}>
        <Box
          sx={{
            position: "absolute",
            top: "50%",
            left: "50%",
            transform: "translate(-50%, -50%)",
            bgcolor: "white",
            p: 4,
            width: 400,
            borderRadius: 2,
            boxShadow: 24,
          }}
        >
          <h2>Create New Drone</h2>

          <div style={{ marginBottom: "16px" }}>
            <label>Code:</label>
            <input
              style={{ width: "100%", padding: 8 }}
              value={newCode}
              onChange={(e) => setNewCode(e.target.value)}
              placeholder="DR-001"
            />
          </div>
          <Button
            variant="contained"
            fullWidth
            onClick={async () => {
              if (!newCode.trim()) {
                alert("Please enter drone code");
                return;
              }

              try {
                await createDrone(newCode.trim());
                setOpenCreate(false);
                setNewCode("");
                await loadDrones();
              } catch (err) {
                console.error(err);
                alert("Failed to create drone");
              }
            }}
          >
            Create
          </Button>
        </Box>
      </Modal>

      <TableContainer
        component={Paper}
        sx={{ maxWidth: "90%", margin: "0 auto 40px" }}
      >
        <Table aria-label="drones table">
          <TableHead>
            <TableRow>
              <TableCell>ID</TableCell>
              <TableCell>Code</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Location</TableCell>
              <TableCell>Last heartbeat</TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {loading && (
              <TableRow>
                <TableCell colSpan={6}>Loading...</TableCell>
              </TableRow>
            )}
            {!loading && filteredDrones.length === 0 && (
              <TableRow>
                <TableCell colSpan={6}>No drones</TableCell>
              </TableRow>
            )}
            {!loading &&
              filteredDrones.map((d) => {
                const statusText = getStatusTextFromNumeric(d.status);
                const statusColor = DRONE_STATUS_COLOR[statusText] || "#000000";
                const [lat, lng] = getDronePosition(d);

                return (
                  <TableRow key={d.id}>
                    <TableCell>{d.id}</TableCell>
                    <TableCell>{d.code}</TableCell>
                    <TableCell>
                      <span style={{ color: statusColor }}>{statusText}</span>
                    </TableCell>
                    <TableCell>
                      {lat.toFixed(5)}, {lng.toFixed(5)}
                    </TableCell>
                    <TableCell>
                      {d.lastHeartbeatAt
                        ? new Date(d.lastHeartbeatAt).toLocaleString()
                        : "-"}
                    </TableCell>
                    <TableCell align="right">
                      <button
                        style={{ marginRight: 8 }}
                        onClick={() => handleSetStatus(d.id, DroneStatus.Idle)}
                      >
                        Set Idle
                      </button>
                      <button
                        style={{ marginRight: 8 }}
                        onClick={() =>
                          handleSetStatus(d.id, DroneStatus.Maintenance)
                        }
                      >
                        Maintenance
                      </button>
                      <button
                        onClick={() =>
                          handleSetStatus(d.id, DroneStatus.Offline)
                        }
                      >
                        Set Offline
                      </button>
                    </TableCell>
                  </TableRow>
                );
              })}
          </TableBody>
        </Table>
      </TableContainer>
    </Container>
  );
}

DronePage.propTypes = {
  restaurantId: PropTypes.oneOfType([PropTypes.string, PropTypes.number]),
};

export default DronePage;
