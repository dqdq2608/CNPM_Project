import CancelIcon from "@mui/icons-material/Cancel";
import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import Paper from "@mui/material/Paper";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import * as React from "react";
import { useEffect, useState } from "react";
import { toast } from "react-toastify";

// API lấy sản phẩm từ Catalog
import { fetchCatalog } from "../../../services/api/catalog";

import { Container, Img } from "./styles";

export function ListProducts() {
  const [products, setProducts] = useState([]);

  useEffect(() => {
    async function loadProducts() {
      try {
        const { items } = await fetchCatalog({
          pageIndex: 0,
          pageSize: 50,
          onlyAvailable: true,
        });

        setProducts(items);
      } catch (err) {
        console.error(err);
        toast.error("Không tải được danh sách sản phẩm.");
      }
    }
    loadProducts();
  }, []);

  function isOffer(offerStatus) {
    if (offerStatus) {
      return <CheckCircleIcon style={{ color: "#00AA00" }} />;
    }
    return <CancelIcon style={{ color: "#CC1717" }} />;
  }

  return (
    <Container>
      <TableContainer component={Paper}>
        <Table sx={{ minWidth: 650 }} aria-label="products table">
          <TableHead>
            <TableRow>
              <TableCell>Product</TableCell>
              <TableCell>Price</TableCell>
              <TableCell align="center">Offer</TableCell>
              <TableCell align="center">Image</TableCell>
            </TableRow>
          </TableHead>

          <TableBody>
            {products &&
              products.map((product) => (
                <TableRow
                  key={product.id}
                  sx={{ "&:last-child td, &:last-child th": { border: 0 } }}
                >
                  <TableCell component="th" scope="row">
                    {product.name}
                  </TableCell>

                  <TableCell>{product.formatedPrice}</TableCell>

                  <TableCell align="center">
                    {isOffer(product?.raw?.available ?? true)}
                  </TableCell>

                  <TableCell align="center">
                    {product.url ? (
                      <Img src={product.url} alt="product" />
                    ) : (
                      "-"
                    )}
                  </TableCell>
                </TableRow>
              ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Container>
  );
}

export default ListProducts;
