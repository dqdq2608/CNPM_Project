import { yupResolver } from "@hookform/resolvers/yup";
import UploadIcon from "@mui/icons-material/Upload";
import React, { useEffect, useState } from "react";
import { useForm, Controller } from "react-hook-form";
import { useHistory } from "react-router-dom/";
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

export function NewProduct() {
  const [fileName, setFileName] = useState(null);
  const [categories, setCategories] = useState([]);
  const [restaurants, setRestaurants] = useState([]);
  const { push } = useHistory();

  const schema = Yup.object().shape({
    name: Yup.string().required("The product must have a name."),
    price: Yup.number()
      .typeError("Price must be a number.")
      .positive("Price must be greater than 0.")
      .required("The product must have a price."),
    category: Yup.object().required("You must choose a category."),
    restaurant: Yup.object().required("You must choose a restaurant."),
    file: Yup.mixed().nullable(), // ảnh không bắt buộc
  });

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
      description: "", // có thể thêm input Description nếu muốn
      price: Number(data.price),
      catalogTypeId: data.category.id,
      restaurantId: data.restaurant.id,
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
        // Categories (catalog types)
        const types = await fetchCatalogTypes(); // [{ id, type }]
        const mappedCats = (types || []).map((t) => ({
          id: t.id,
          name: t.type,
        }));
        setCategories(mappedCats);

        // Restaurants
        const rData = await fetchRestaurants(); // [{ restaurantId, name, ... }]
        const mappedRes = (rData || []).map((r) => ({
          id: r.restaurantId,
          name: r.name,
        }));
        setRestaurants(mappedRes);
      } catch (e) {
        console.error(e);
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

        <Button>Add Product</Button>
      </form>
    </Container>
  );
}

export default NewProduct;
