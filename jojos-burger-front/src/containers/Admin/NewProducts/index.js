import { yupResolver } from "@hookform/resolvers/yup";
import UploadIcon from "@mui/icons-material/Upload";
import React, { useEffect, useState } from "react";
import { useForm, Controller } from "react-hook-form";
import { useHistory } from "react-router-dom";
import ReactSelect from "react-select";
import { toast } from "react-toastify";
import * as Yup from "yup";

import { ErrorMessage } from "../../../components";
import { Container, Label, Input, Button, LabelUpload } from "./styles";

import {
  fetchCatalogTypes,
  fetchRestaurants,
  createCatalogItem,
} from "../../../services/api/catalog";

const schema = Yup.object().shape({
  name: Yup.string().required("The product must have a name."),
  price: Yup.number()
    .typeError("Price must be a number.")
    .positive("Price must be greater than 0.")
    .required("The product must have a price."),
  category: Yup.object().required("You must choose a category."),
  restaurant: Yup.object().required("You must choose a restaurant."),
  file: Yup.mixed().nullable(),
});

// Ép toàn bộ text của ReactSelect thành màu đen
const selectStyles = {
  control: (base) => ({
    ...base,
    color: "#000",
    fontSize: 14,
  }),
  menu: (base) => ({
    ...base,
    zIndex: 9999,
  }),
  menuList: (base) => ({
    ...base,
    color: "#000",
    fontSize: 14,
  }),
  option: (base, state) => ({
    ...base,
    color: "#000",
    backgroundColor: state.isSelected
      ? "#e0e7ff"
      : state.isFocused
      ? "#f1f5f9"
      : "#fff",
    fontSize: 14,
  }),
  singleValue: (base) => ({
    ...base,
    color: "#000",
    fontSize: 14,
  }),
  input: (base) => ({
    ...base,
    color: "#000",
    fontSize: 14,
  }),
  placeholder: (base) => ({
    ...base,
    color: "#666",
    fontSize: 14,
  }),
};

export function NewProduct() {
  const [fileName, setFileName] = useState(null);
  const [categories, setCategories] = useState([]);
  const [restaurants, setRestaurants] = useState([]);
  const { push } = useHistory();

  const {
    register,
    handleSubmit,
    control,
    formState: { errors },
  } = useForm({ resolver: yupResolver(schema) });

  const onSubmit = async (data) => {
    const picName = data?.file?.[0]?.name ?? null;

    const payload = {
      name: data.name,
      description: "",
      price: Number(data.price),
      // lấy id từ option { value, label }
      catalogTypeId: data.category.value,
      restaurantId: data.restaurant.value,
      pictureFileName: picName,
      isAvailable: true,
      estimatedPrepTime: 10,
      availableStock: 0,
      restockThreshold: 0,
      maxStockThreshold: 0,
      onReorder: false,
    };

    const req = createCatalogItem(payload);

    await toast.promise(req, {
      pending: "Creating new product...",
      success: "Product was successfully created.",
      error: "Error while creating product, try again later...",
    });

    setTimeout(() => {
      push("/list-products");
    }, 1200);
  };

  useEffect(() => {
    async function loadLookups() {
      try {
        // ======================= CATEGORIES =======================
        const types = await fetchCatalogTypes();
        console.log("raw types:", types);

        const mappedCats = (types || []).map((t) => ({
          value: t.id ?? t.Id ?? t.value,
          label:
            t.type ??
            t.Type ??
            t.name ??
            t.Name ??
            t.label ??
            "",
        }));
        console.log("categories mapped:", mappedCats);

        setCategories(mappedCats);

        // ======================= RESTAURANTS =======================
        const rData = await fetchRestaurants();
        console.log("raw restaurants:", rData);

        const mappedRes = (rData || []).map((r) => ({
          value: r.restaurantId ?? r.id ?? r.value,
          label: r.name ?? r.Name ?? r.label ?? "",
        }));
        console.log("restaurants mapped:", mappedRes);

        setRestaurants(mappedRes);
      } catch (e) {
        console.error("Failed to load lookups:", e);
        toast.error("Failed to load categories/restaurants.");
      }
    }

    loadLookups();
  }, []);


  return (
    <Container>
      <form noValidate onSubmit={handleSubmit(onSubmit)}>
        <div>
          <Label>Name</Label>
          <Input type="text" {...register("name")} />
          <ErrorMessage>{errors.name?.message}</ErrorMessage>
        </div>

        <div>
          <Label>Price</Label>
          <Input type="number" step="1" min="0" {...register("price")} />
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
                setFileName(f ? f.name : null);
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
                options={categories}     // [{ value, label }]
                placeholder="Select Category"
                isClearable
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
                styles={selectStyles}
                options={restaurants}     // [{ value, label }]
                placeholder="Select Restaurant"
                isClearable
              />
            )}
          />
          <ErrorMessage>{errors.restaurant?.message}</ErrorMessage>
        </div>

        <Button>Add Product</Button>
      </form>
    </Container>
  );
}

export default NewProduct;
