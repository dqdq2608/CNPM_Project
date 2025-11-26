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
import status from "./order-status";
import Row from "./row";
import { Container, Menu, LinkMenu } from "./styles";

function Orders({ restaurantId }) {
  const [orders, setOrders] = useState([]);
  const [filteredOrders, setFilteredOrders] = useState([]);
  const [activeStatus, setActiveStatus] = useState(0);
  const [rows, setRows] = useState([]);

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

  function handleStatus(status) {
    if (status.id === 0) {
      setFilteredOrders(orders);
    } else {
      const newOrders = orders.filter((order) => order.status === status.value);
      setFilteredOrders(newOrders);
    }
    setActiveStatus(status.id);
  }

  return (
    <Container>
      <Menu>
        {status &&
          status.map((status) => (
            <LinkMenu
              key={status.id}
              onClick={() => handleStatus(status)}
              isActiveStatus={activeStatus === status.id}
            >
              {status.label}
            </LinkMenu>
          ))}
      </Menu>

      <TableContainer component={Paper}>
        <Table aria-label="collapsible table">
          <TableHead>
            <TableRow>
              <TableCell />
              <TableCell>Order ID</TableCell>
              <TableCell>Client Name</TableCell>
              <TableCell>Order Date</TableCell>
              <TableCell>Order Status</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {rows.map((row) => (
              <Row
                key={row.orderId}
                row={row}
                setOrders={setOrders}
                orders={orders}
              />
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Container>
  );
}

Orders.propTypes = {
  restaurantId: PropTypes.oneOfType([PropTypes.string, PropTypes.number]),
};

export default Orders;
