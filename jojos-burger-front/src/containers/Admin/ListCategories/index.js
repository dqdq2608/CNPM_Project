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

import { fetchCatalogTypes } from "../../../services/api/catalog";
import { Container, Img } from "./styles";

export function ListCategories() {
  const [categories, setCategories] = useState([]);

  useEffect(() => {
    async function loadCategories() {
      try {
        // Lấy catalog types từ Catalog API
        const types = await fetchCatalogTypes();

        // Map dữ liệu
        const mapped = (types || []).map((t) => ({
          id: t.id,
          name: t.type,
          url: "/images/category-placeholder.png", // ảnh mặc định
        }));

        setCategories(mapped);
      } catch (err) {
        console.error("Error loading catalog types:", err);
        toast.error("Failed to load categories.");
      }
    }

    loadCategories();
  }, []);

  return (
    <Container>
      <TableContainer component={Paper}>
        <Table sx={{ minWidth: 650 }} aria-label="categories table">
          <TableHead>
            <TableRow>
              <TableCell>Category</TableCell>
              <TableCell align="center">Image</TableCell>
            </TableRow>
          </TableHead>

          <TableBody>
            {categories &&
              categories.map((cat) => (
                <TableRow
                  key={cat.id}
                  sx={{ "&:last-child td, &:last-child th": { border: 0 } }}
                >
                  <TableCell>{cat.name}</TableCell>

                  <TableCell align="center">
                    <Img src={cat.url} alt="category" />
                  </TableCell>
                </TableRow>
              ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Container>
  );
}

export default ListCategories;
