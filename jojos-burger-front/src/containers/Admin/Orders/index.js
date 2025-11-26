import Paper from "@mui/material/Paper";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import PropTypes from "prop-types";
import React, { useEffect, useState } from "react";

import { fetchOrdersByRestaurant } from "../../../services/api/order";
import formatDate from "../../../utils/formatDate";

function Orders({ restaurantId }) {
  const [orders, setOrders] = useState([]);
  const [filteredOrders, setFilteredOrders] = useState([]);
  const [activeStatus, setActiveStatus] = useState("All");

  useEffect(() => {
    async function loadOrders() {
      try {
        const data = await fetchOrdersByRestaurant(restaurantId);

        setOrders(data);
        setFilteredOrders(data);
      } catch (err) {
        console.error("Failed to load restaurant orders:", err);
      }
    }

    if (restaurantId) {
      loadOrders();
    }
  }, [restaurantId]);

  function createData(order) {
    return {
      name: order.user.name,
      orderId: order._id,
      date: formatDate(order.createdAt),
      status: order.status,
      products: order.products,
    };
  }

  useEffect(() => {
    const newRows = filteredOrders.map((order) => createData(order));

    setRows(newRows);
  }, [filteredOrders]);

  useEffect(() => {
    if (activeStatus === 0) {
      setFilteredOrders(orders);
    } else {
      const statusIndex = status.findIndex(
        (status) => status.id === activeStatus
      );
      const newFilteredOrders = orders.filter(
        (order) => order.status === status[statusIndex].value
      );
      setFilteredOrders(newFilteredOrders);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [orders]);

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

Orders.propTypes = {
  restaurantId: PropTypes.oneOfType([PropTypes.string, PropTypes.number]),
};

export default Orders;
