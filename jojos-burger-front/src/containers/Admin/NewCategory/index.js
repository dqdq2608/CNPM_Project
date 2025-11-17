import { yupResolver } from "@hookform/resolvers/yup";
import React from "react";
import { useForm } from "react-hook-form";
import * as Yup from "yup";
import { useHistory } from "react-router-dom"; // bỏ dấu / ở cuối
import { toast } from "react-toastify";

import { ErrorMessage } from "../../../components";
import { Container, Label, Input, Button } from "./styles";

// gọi API từ catalog.js
import { createCatalogType } from "../../../services/api/catalog";

const schema = Yup.object({
  name: Yup.string()
    .trim()
    .required("Category name is required.")
    .max(100, "Max 100 characters."),
});

export default function NewCategory() {
  const { push } = useHistory();

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    reset,
  } = useForm({ resolver: yupResolver(schema) });

  const onSubmit = async (data) => {
    // payload khớp với BE: /api/catalog/catalogtypes (CreateCatalogType)
    const payload = { type: data.name.trim() };

    try {
      const req = createCatalogType(payload); // catalogHttp.post('/api/catalog/catalogtypes', payload)

      await toast.promise(req, {
        pending: "Creating category...",
        success: "Category was successfully created.",
        error: "Error while creating category, try again later...",
      });

      reset();
      setTimeout(() => {
        push("/list-categories");
      }, 800);
    } catch (err) {
      console.error("Create category failed:", err);
      toast.error("Unexpected error while creating category.");
    }
  };

  return (
    <Container>
      <form noValidate onSubmit={handleSubmit(onSubmit)}>
        <div>
          <Label>Category name</Label>
          <Input
            type="text"
            placeholder="e.g. Burgers"
            {...register("name")}
          />
          <ErrorMessage>{errors.name?.message}</ErrorMessage>
        </div>

        <Button type="submit" disabled={isSubmitting}>
          Add Category
        </Button>
      </form>
    </Container>
  );
}
