import { yupResolver } from "@hookform/resolvers/yup";
import Paper from "@mui/material/Paper";
import Table from "@mui/material/Table";
import TableBody from "@mui/material/TableBody";
import TableCell from "@mui/material/TableCell";
import TableContainer from "@mui/material/TableContainer";
import TableHead from "@mui/material/TableHead";
import TableRow from "@mui/material/TableRow";
import Dialog from "@mui/material/Dialog";
import DialogTitle from "@mui/material/DialogTitle";
import DialogContent from "@mui/material/DialogContent";
import DialogActions from "@mui/material/DialogActions";
import React, { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { toast } from "react-toastify";
import * as Yup from "yup";

import { ErrorMessage } from "../../../components";
import {
  fetchCatalogTypes,
  updateCatalogType,
  deleteCatalogType,
} from "../../../services/api/catalog";
import { Container, Label, Input, Button } from "./styles";
import { EditIconImg, DeleteIcon } from "../ListCategories/styles";

const schema = Yup.object().shape({
  name: Yup.string().trim().required("Category name is required."),
});

export function EditCategory() {
  const [categories, setCategories] = useState([]);
  const [editingCategory, setEditingCategory] = useState(null); // category đang được edit (popup)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    reset,
  } = useForm({
    resolver: yupResolver(schema),
    defaultValues: { name: "" },
  });

  // Load list categories
  useEffect(() => {
    async function loadCategories() {
      try {
        const types = await fetchCatalogTypes(); // [{ id, type }]
        const mapped = (types || []).map((t) => ({
          id: t.id,
          name: t.type,
        }));
        setCategories(mapped);
      } catch (err) {
        console.error("Error loading catalog types:", err);
        toast.error("Failed to load categories.");
      }
    }
    loadCategories();
  }, []);

  // Mở popup edit
  function handleOpenEdit(cat) {
    setEditingCategory(cat);
    reset({ name: cat.name });
  }

  // Đóng popup
  function handleCloseDialog() {
    setEditingCategory(null);
    reset({ name: "" });
  }

  // Submit form trong popup
  const onSubmit = async (data) => {
    if (!editingCategory) return;

    const payload = { type: data.name.trim() };

    // updateCatalogType đang được định nghĩa kiểu: (id, payload) -> PUT /catalogtypes với { id, ...payload }
    const req = updateCatalogType(editingCategory.id, payload);

    await toast.promise(req, {
      pending: "Updating category...",
      success: "Category was successfully updated.",
      error: "Error while updating category, try again later...",
    });

    // Cập nhật lại list ở client
    setCategories((prev) =>
      prev.map((c) =>
        c.id === editingCategory.id ? { ...c, name: data.name.trim() } : c
      )
    );

    handleCloseDialog();
  };

  // Xoá category
  async function handleDelete(cat) {
    if (!window.confirm(`Delete category "${cat.name}"?`)) return;

    const req = deleteCatalogType(cat.id);

    await toast.promise(req, {
      pending: "Deleting category...",
      success: "Category was successfully deleted.",
      error: "Error while deleting category, try again later...",
    });

    setCategories((prev) => prev.filter((c) => c.id !== cat.id));

    if (editingCategory?.id === cat.id) {
      handleCloseDialog();
    }
  }

  return (
    <Container>
      {/* BẢNG CATEGORY */}
      <TableContainer component={Paper}>
        <Table sx={{ minWidth: 450 }} aria-label="categories table">
          <TableHead>
            <TableRow>
              <TableCell>Category</TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {categories.map((cat) => (
              <TableRow key={cat.id} hover>
                <TableCell>{cat.name}</TableCell>
                <TableCell align="right">
                  <EditIconImg onClick={() => handleOpenEdit(cat)} />
                  <DeleteIcon onClick={() => handleDelete(cat)} />
                </TableCell>
              </TableRow>
            ))}
            {categories.length === 0 && (
              <TableRow>
                <TableCell colSpan={2}>No categories yet.</TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </TableContainer>

      {/* POPUP EDIT */}
      <Dialog
        open={Boolean(editingCategory)}
        onClose={handleCloseDialog}
        fullWidth
        maxWidth="sm"
      >
        <DialogTitle>
          {editingCategory
            ? `Editing: ${editingCategory.name}`
            : "Edit Category"}
        </DialogTitle>

        <form noValidate onSubmit={handleSubmit(onSubmit)}>
          <DialogContent dividers>
            <Label>Category name</Label>
            <Input
              type="text"
              placeholder="Category name"
              {...register("name")}
            />
            <ErrorMessage>{errors.name?.message}</ErrorMessage>
          </DialogContent>

          <DialogActions>
            <Button type="button" onClick={handleCloseDialog}>
              Cancel
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              Update Category
            </Button>
          </DialogActions>
        </form>
      </Dialog>
    </Container>
  );
}

export default EditCategory;
