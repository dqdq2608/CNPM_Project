import { yupResolver } from "@hookform/resolvers/yup";
import CancelIcon from "@mui/icons-material/Cancel";
import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import UploadIcon from "@mui/icons-material/Upload";
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
import { useForm, Controller } from "react-hook-form";
import ReactSelect from "react-select";
import { toast } from "react-toastify";
import * as Yup from "yup";

import { ErrorMessage } from "../../../components";
import { Container, Label, Input, Button, LabelUpload } from "./styles";

import {
  fetchCatalog,
  fetchCatalogTypes,
  fetchRestaurants,
  updateCatalogItem,
  deleteCatalogItem,
} from "../../../services/api/catalog";

// dùng lại style ảnh + icon edit/delete của ListProducts
import {
  Img,
  EditIconImg,
  DeleteForever as DeleteIcon,
} from "../ListProducts/styles";

const schema = Yup.object().shape({
  name: Yup.string().required("The product must have a name."),
  price: Yup.number()
    .typeError("Price must be a number.")
    .positive("Price must be greater than 0.")
    .required("The product must have a price."),
  category: Yup.object().required("Choose a category."),
  restaurant: Yup.object().required("Choose a restaurant."),
  file: Yup.mixed().nullable(),
});

export default function EditProduct() {
  const [products, setProducts] = useState([]);
  const [selectedProduct, setSelectedProduct] = useState(null);
  const [categories, setCategories] = useState([]);
  const [restaurants, setRestaurants] = useState([]);
  const [fileName, setFileName] = useState(null);
  const [loading, setLoading] = useState(true);
  const [open, setOpen] = useState(false); // popup

  const {
    register,
    handleSubmit,
    control,
    reset,
    formState: { errors },
  } = useForm({ resolver: yupResolver(schema) });

  // helper: điền dữ liệu form từ 1 product
  function fillFormFromProduct(prod, cats = categories, rests = restaurants) {
    if (!prod) return;

    reset({
      name: prod.name,
      price: prod.price,
      category: cats.find((c) => c.id === prod.raw.catalogTypeId) || null,
      restaurant: rests.find((r) => r.id === prod.raw.restaurantId) || null,
      file: null,
    });

    setFileName(prod.raw.pictureFileName || null);
  }

  useEffect(() => {
    async function load() {
      try {
        const [types, rests, catalogResult] = await Promise.all([
          fetchCatalogTypes(),
          fetchRestaurants(),
          fetchCatalog({ pageIndex: 0, pageSize: 100, onlyAvailable: false }),
        ]);

        const mappedCats = (types || []).map((t) => ({
          id: t.id,
          name: t.type,
        }));
        const mappedRests = (rests || []).map((r) => ({
          id: r.restaurantId,
          name: r.name,
        }));

        setCategories(mappedCats);
        setRestaurants(mappedRests);

        const list = catalogResult.items || [];
        setProducts(list);
      } catch (e) {
        console.error("Failed to load data:", e);
        toast.error("Failed to load products / categories / restaurants.");
      } finally {
        setLoading(false);
      }
    }

    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const onSubmit = async (data) => {
    if (!selectedProduct) {
      toast.warn("Please choose a product to edit.");
      return;
    }

    const picName =
      data?.file?.[0]?.name ?? selectedProduct.raw.pictureFileName ?? null;

    const payload = {
      id: selectedProduct.id,
      name: data.name,
      description: selectedProduct.raw.description || "",
      price: Number(data.price),
      catalogTypeId: data.category.id,
      restaurantId: data.restaurant.id,
      pictureFileName: picName,
      isAvailable: selectedProduct.raw.isAvailable ?? true,
      estimatedPrepTime: selectedProduct.raw.estimatedPrepTime ?? 10,
      availableStock: selectedProduct.raw.availableStock ?? 0,
      restockThreshold: selectedProduct.raw.restockThreshold ?? 0,
      maxStockThreshold: selectedProduct.raw.maxStockThreshold ?? 0,
      onReorder: selectedProduct.raw.onReorder ?? false,
    };

    const req = updateCatalogItem(payload);

    await toast.promise(req, {
      pending: "Updating product...",
      success: "Product updated successfully.",
      error: "Failed to update product.",
    });

    // Cập nhật list bên ngoài bảng
    setProducts((prev) =>
      prev.map((p) => (p.id === selectedProduct.id ? { ...p, ...payload } : p))
    );

    setOpen(false);
  };

  const handleDelete = async (product) => {
    if (!product) return;

    if (!window.confirm(`Delete product "${product.name}"?`)) {
      return;
    }

    const req = deleteCatalogItem(product.id);

    await toast.promise(req, {
      pending: "Deleting product...",
      success: "Product deleted successfully.",
      error: "Failed to delete product.",
    });

    setProducts((prev) => prev.filter((p) => p.id !== product.id));

    // nếu đang mở popup cho sản phẩm này thì đóng lại
    if (selectedProduct && selectedProduct.id === product.id) {
      setOpen(false);
      setSelectedProduct(null);
    }
  };

  function isOffer(offerStatus) {
    if (offerStatus) {
      return <CheckCircleIcon style={{ color: "#00AA00" }} />;
    }
    return <CancelIcon style={{ color: "#CC1717" }} />;
  }

  if (loading) {
    return <div style={{ padding: 20 }}>Loading...</div>;
  }

  if (products.length === 0) {
    return (
      <div style={{ padding: 20 }}>
        There is no product yet. Please create one in{" "}
        <strong>New Product</strong>.
      </div>
    );
  }

  return (
    <Container>
      {/* KHÔNG còn chữ "Edit / Delete Products" to nữa */}

      {/* BẢNG DANH SÁCH SẢN PHẨM */}
      <TableContainer component={Paper}>
        <Table sx={{ minWidth: 650 }} aria-label="products table">
          <TableHead>
            <TableRow>
              <TableCell>Product</TableCell>
              <TableCell>Price</TableCell>
              <TableCell align="center">Offer</TableCell>
              <TableCell align="center">Image</TableCell>
              <TableCell align="center">Actions</TableCell>
            </TableRow>
          </TableHead>

          <TableBody>
            {products.map((product) => (
              <TableRow
                key={product.id}
                sx={{
                  "&:last-child td, &:last-child th": { border: 0 },
                }}
              >
                <TableCell component="th" scope="row">
                  {product.name}
                </TableCell>

                <TableCell>{product.formatedPrice}</TableCell>

                <TableCell align="center">
                  {isOffer(product?.raw?.isAvailable ?? true)}
                </TableCell>

                <TableCell align="center">
                  {product.url ? <Img src={product.url} alt="product" /> : "-"}
                </TableCell>

                <TableCell align="center">
                  <EditIconImg
                    style={{ cursor: "pointer", marginRight: 8 }}
                    onClick={() => {
                      setSelectedProduct(product);
                      fillFormFromProduct(product);
                      setOpen(true); // mở popup
                    }}
                  />
                  <DeleteIcon
                    style={{ cursor: "pointer" }}
                    onClick={() => handleDelete(product)}
                  />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      {/* POPUP EDIT PRODUCT */}
      <Dialog
        open={open}
        onClose={() => setOpen(false)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>
          {selectedProduct
            ? `Editing: ${selectedProduct.name}`
            : "Edit Product"}
        </DialogTitle>

        <DialogContent dividers>
          {selectedProduct && (
            <form
              id="edit-product-form"
              noValidate
              onSubmit={handleSubmit(onSubmit)}
            >
              <div>
                <Label>Name</Label>
                <Input type="text" {...register("name")} />
                <ErrorMessage>{errors.name?.message}</ErrorMessage>
              </div>

              <div>
                <Label>Price</Label>
                <Input type="number" {...register("price")} />
                <ErrorMessage>{errors.price?.message}</ErrorMessage>
              </div>

              <div>
                <LabelUpload>
                  {fileName || (
                    <>
                      <UploadIcon />
                      Choose image (optional)
                    </>
                  )}
                  <Input
                    type="file"
                    accept="image/png, image/jpeg, image/jpg"
                    {...register("file")}
                    onChange={(e) => {
                      const f = e.target.files?.[0];
                      setFileName(
                        f ? f.name : selectedProduct.raw.pictureFileName
                      );
                    }}
                  />
                </LabelUpload>
                <ErrorMessage>{errors.file?.message}</ErrorMessage>
              </div>

              <div>
                <Label>Category</Label>
                <Controller
                  name="category"
                  control={control}
                  render={({ field }) => (
                    <ReactSelect
                      {...field}
                      options={categories}
                      getOptionLabel={(cat) => cat.name}
                      getOptionValue={(cat) => String(cat.id)}
                      placeholder="Select Category"
                    />
                  )}
                />
                <ErrorMessage>{errors.category?.message}</ErrorMessage>
              </div>

              <div>
                <Label>Restaurant</Label>
                <Controller
                  name="restaurant"
                  control={control}
                  render={({ field }) => (
                    <ReactSelect
                      {...field}
                      options={restaurants}
                      getOptionLabel={(r) => r.name}
                      getOptionValue={(r) => String(r.id)}
                      placeholder="Select Restaurant"
                    />
                  )}
                />
                <ErrorMessage>{errors.restaurant?.message}</ErrorMessage>
              </div>
            </form>
          )}
        </DialogContent>

        <DialogActions>
          <Button type="button" onClick={() => setOpen(false)}>
            Cancel
          </Button>
          <Button
            type="submit"
            form="edit-product-form"
            style={{ marginLeft: "8px" }}
          >
            Update
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
}
