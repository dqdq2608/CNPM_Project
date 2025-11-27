import KeyboardArrowDownIcon from "@mui/icons-material/KeyboardArrowDown";
import KeyboardArrowUpIcon from "@mui/icons-material/KeyboardArrowUp";
import Box from "@mui/material/Box";
import Collapse from "@mui/material/Collapse";
import IconButton from "@mui/material/IconButton";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Typography from "@mui/material/Typography";
import PropTypes from "prop-types";
import React from "react";
import { toast } from "react-toastify";

import { fetchOrderDetail, startDelivery } from "../../../services/api/order";
import formatCurrency from "../../../utils/formatCurrency";
import { ProductImg } from "./styles";
function Row({ row }) {
  const [open, setOpen] = React.useState(false);
  const [items, setItems] = React.useState([]);
  const [itemsLoading, setItemsLoading] = React.useState(false);
  const [starting, setStarting] = React.useState(false); // 👈 trạng thái đang gọi API

  const handleStartDelivery = async () => {
    try {
      setStarting(true);
      await startDelivery(row.orderId);
      toast.success("Đã bắt đầu giao hàng bằng drone");

      // cập nhật status local cho đẹp UI (optional)
      row.status = "Delivering";
    } catch (e) {
      console.error("startDelivery error", e);
      toast.error("Không thể bắt đầu giao bằng drone, vui lòng thử lại.");
    } finally {
      setStarting(false);
    }
  };

  const handleToggleOpen = async () => {
    const nextOpen = !open;
    setOpen(nextOpen);

    // Lần đầu mở thì mới fetch chi tiết
    if (nextOpen && items.length === 0) {
      setItemsLoading(true);
      try {
        const detail = await fetchOrderDetail(row.orderId);
        const mappedItems =
          detail.orderItems?.map((oi) => ({
            quantity: oi.units,
            name: oi.productName,
            price: oi.unitPrice,
            url: oi.pictureUrl,
          })) ?? [];

        setItems(mappedItems);
      } catch (e) {
        console.error("fetchOrderDetail error", e);
      } finally {
        setItemsLoading(false);
      }
    }
  };
  return (
    <React.Fragment>
      <TableRow sx={{ "& > *": { borderBottom: "unset" } }}>
        <TableCell>
          <IconButton
            aria-label="expand row"
            size="small"
            onClick={handleToggleOpen}
          >
            {open ? <KeyboardArrowUpIcon /> : <KeyboardArrowDownIcon />}
          </IconButton>
        </TableCell>
        <TableCell component="th" scope="row">
          {row.orderId}
        </TableCell>
        <TableCell>{row.name}</TableCell>
        <TableCell>{row.date}</TableCell>
        <TableCell>
          {formatCurrency(row.total)} {/* 👈 HIỂN THỊ TỔNG TIỀN */}
        </TableCell>
        <TableCell>{row.status}</TableCell> {/* 👈 STATUS TEXT */}
        <TableCell>
          {row.status === "Paid" && ( // hoặc "StockConfirmed" tuỳ flow của bạn
            <button
              className="btn btn-primary"
              onClick={handleStartDelivery}
              disabled={starting}
            >
              {starting ? "Đang bắt đầu..." : "Bắt đầu giao bằng drone"}
            </button>
          )}
        </TableCell>
      </TableRow>
      <TableRow>
        <TableCell style={{ paddingBottom: 0, paddingTop: 0 }} colSpan={6}>
          <Collapse in={open} timeout="auto" unmountOnExit>
            <Box sx={{ margin: 1 }}>
              <Typography variant="h6" gutterBottom component="div">
                Order History
              </Typography>
              <Table size="small" aria-label="purchases">
                <TableHead>
                  <TableRow>
                    <TableCell></TableCell>
                    <TableCell>Amount</TableCell>
                    <TableCell>Product</TableCell>
                    <TableCell>Total price (€)</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {itemsLoading && (
                    <TableRow>
                      <TableCell colSpan={5}>
                        <Typography variant="body2">
                          Đang tải chi tiết đơn hàng...
                        </Typography>
                      </TableCell>
                    </TableRow>
                  )}

                  {!itemsLoading &&
                    items.map((productRow, index) => (
                      <TableRow key={index}>
                        <TableCell>
                          <ProductImg
                            src={productRow.url}
                            alt="product-image"
                          />
                        </TableCell>
                        <TableCell component="th" scope="row">
                          {productRow.quantity}
                        </TableCell>
                        <TableCell>{productRow.name}</TableCell>
                        <TableCell>
                          {formatCurrency(
                            productRow.quantity * productRow.price
                          )}
                        </TableCell>
                      </TableRow>
                    ))}
                </TableBody>
              </Table>
            </Box>
          </Collapse>
        </TableCell>
      </TableRow>
    </React.Fragment>
  );
}

Row.propTypes = {
  row: PropTypes.shape({
    name: PropTypes.string.isRequired,
    orderId: PropTypes.oneOfType([PropTypes.string, PropTypes.number])
      .isRequired,
    date: PropTypes.string.isRequired,
    status: PropTypes.string.isRequired,
    total: PropTypes.number.isRequired,
  }).isRequired,
};

export default Row;
