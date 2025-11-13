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

const FALLBACK_IMG = "/images/category-placeholder.png";

export function ListCategories() {
  const [categories, setCategories] = useState([]);

  useEffect(() => {
    async function loadCategories() {
      try {
        const types = await fetchCatalogTypes(); // [{ id, type, pictureUri }]
        console.log("types from API:", types);

        const mapped = (types || []).map((t) => ({
          id: t.id,
          // cố gắng lấy tên từ nhiều field khác nhau cho chắc
          name: t.name || t.type || t.Type || t.category || t.Category || "",
          url: t.pictureUri || FALLBACK_IMG,
        }));

        console.log("mapped categories:", mapped);
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
            {categories.map((cat) => (
              <TableRow
                key={cat.id}
                sx={{ "&:last-child td, &:last-child th": { border: 0 } }}
              >
                {/* Hiển thị tên */}
                <TableCell sx={{ color: "#000" }}>{cat.name}</TableCell>

                <TableCell align="center">
                  <Img
                    src={cat.url}
                    alt={cat.name || "category"}
                    onError={(e) => {
                      e.currentTarget.src = FALLBACK_IMG;
                    }}
                  />
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
