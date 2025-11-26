import React, { useState, useEffect } from "react";
import PropTypes from "prop-types"; // ⭐ thêm dòng này
import {
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Chip,
  Collapse,
  IconButton,
  Typography,
  Box,
  Button,
} from "@mui/material";
import {
  KeyboardArrowDown,
  KeyboardArrowUp,
  Edit as EditIcon,
} from "@mui/icons-material";

import formatDate from "../../../utils/formatDate";

// 👉 dùng BFF Ordering API
import { fetchMyOrders } from "../../../services/api/order";

function createData(order) {
  return {
    name: order.customerName ?? order.user?.name ?? "Unknown",
    orderId: order.orderId ?? order.id ?? order._id,
    date: formatDate(order.createdAt ?? order.orderDate ?? order.date),
    status: order.status,
    products: order.items ?? order.products ?? [],
  };
}

function Row({ row }) {
  const [open, setOpen] = useState(false);

  return (
    <>
      <TableRow sx={{ "& > *": { borderBottom: "unset" } }}>
        <TableCell>
          <IconButton aria-label="expand row" size="small" onClick={() => setOpen(!open)}>
            {open ? <KeyboardArrowUp /> : <KeyboardArrowDown />}
          </IconButton>
        </TableCell>

        <TableCell component="th" scope="row">{row.name}</TableCell>
        <TableCell>{row.orderId}</TableCell>
        <TableCell>{row.date}</TableCell>
        <TableCell>
          <Chip
            label={row.status}
            color={
              row.status === "Delivered"
                ? "success"
                : row.status === "Pending"
                ? "warning"
                : row.status === "Canceled"
                ? "error"
                : "default"
            }
          />
        </TableCell>
        <TableCell align="right">
          <IconButton color="primary">
            <EditIcon />
          </IconButton>
        </TableCell>
      </TableRow>

      <TableRow>
        <TableCell style={{ paddingBottom: 0, paddingTop: 0 }} colSpan={6}>
          <Collapse in={open} timeout="auto" unmountOnExit>
            <Box margin={2}>
              <Typography variant="h6" gutterBottom>
                Products
              </Typography>

              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Name</TableCell>
                    <TableCell>Quantity</TableCell>
                    <TableCell>Price</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {row.products.map((product, index) => (
                    <TableRow key={index}>
                      <TableCell>{product.name}</TableCell>
                      <TableCell>{product.quantity}</TableCell>
                      <TableCell>${product.price}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </Box>
          </Collapse>
        </TableCell>
      </TableRow>
    </>
  );
}

/* ⭐ Thêm PropTypes fix lỗi ESLint */
Row.propTypes = {
  row: PropTypes.shape({
    name: PropTypes.string,
    orderId: PropTypes.oneOfType([PropTypes.string, PropTypes.number]),
    date: PropTypes.string,
    status: PropTypes.string,
    products: PropTypes.arrayOf(
      PropTypes.shape({
        name: PropTypes.string,
        quantity: PropTypes.number,
        price: PropTypes.number,
      })
    ),
  }).isRequired,
};

export default function Orders() {
  const [orders, setOrders] = useState([]);
  const [filteredOrders, setFilteredOrders] = useState([]);
  const [activeStatus, setActiveStatus] = useState("All");

  useEffect(() => {
    async function loadOrders() {
      try {
        const data = await fetchMyOrders();
        const rows = data.map((order) => createData(order));
        setOrders(rows);
        setFilteredOrders(rows);
      } catch (error) {
        console.error("Failed to load orders:", error);
        setOrders([]);
        setFilteredOrders([]);
      }
    }

    loadOrders();
  }, []);

  const handleFilter = (status) => {
    setActiveStatus(status);

    if (status === "All") {
      setFilteredOrders(orders);
    } else {
      setFilteredOrders(orders.filter((o) => o.status === status));
    }
  };

  return (
    <div style={{ padding: 20 }}>
      <Typography variant="h5" gutterBottom>
        Orders
      </Typography>

      <Box mb={2} display="flex" gap={1}>
        {["All", "Pending", "Delivered", "Canceled"].map((status) => (
          <Button
            key={status}
            variant={activeStatus === status ? "contained" : "outlined"}
            onClick={() => handleFilter(status)}
          >
            {status}
          </Button>
        ))}
      </Box>

      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell />
              <TableCell>Name</TableCell>
              <TableCell>Order ID</TableCell>
              <TableCell>Date</TableCell>
              <TableCell>Status</TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>

          <TableBody>
            {filteredOrders.map((row, index) => (
              <Row key={index} row={row} />
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </div>
  );
}

/* ⭐ Nếu ESLint vẫn complain, bạn thêm: */
Orders.propTypes = {};
