import React, { useState } from "react";
import { useForm } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import * as Yup from "yup";
import { Link, useHistory, useLocation } from "react-router-dom";
import { toast, Bounce } from "react-toastify";

import bgHome from "../../assets/bg-home.jpg";
import LoginImg from "../../assets/login-body.svg";
import { Button, ErrorMessage } from "../../components";

import {
  LoginImage,
  Container,
  ContainerItems,
  HeaderName,
  HeaderBurger,
  H1Login,
  Label,
  Input,
  SignInLink,
} from "./styles";

import { useUser } from "../../hooks/UserContext";
import paths from "../../constants/paths";

/* =================== HELPERS ====================== */

// Trích claim từ BFF
function extractClaims(dto) {
  if (!dto?.raw) return {};
  return Object.fromEntries(dto.raw.map((c) => [c.type, c.value]));
}

// Kiểm tra role admin
function isAdminUser(dto) {
  const claims = extractClaims(dto);
  const role = claims.role?.toLowerCase?.();
  return role === "restaurantadmin" || role === "admin";
}

// Xác định URL admin trong hệ thống
function getAdminPaths() {
  return [
    paths?.Order,
    paths?.Products,
    paths?.NewProduct,
    paths?.EditProduct,
    paths?.NewCategory,
    paths?.EditCategory,
    paths?.Categories,
    "/admin",
  ].filter((x) => typeof x === "string" && x.length > 0);
}

// Kiểm tra URL có phải admin route không
function isAdminUrl(url) {
  if (!url) return false;
  const adminRoutes = getAdminPaths();
  return adminRoutes.some((p) => url === p || url.startsWith(p + "/"));
}

/* =================== LOGIN COMPONENT ====================== */

export function Login() {
  const history = useHistory();
  const location = useLocation();
  const { login } = useUser(); // ← FE chỉ gọi hàm login của context

  const params = new URLSearchParams(location.search);
  const rawReturnUrl = params.get("returnUrl") || "/";

  // Chỉ redirect nội bộ, chặn open redirect
  const safeReturnUrl =
    rawReturnUrl.startsWith("/") && !rawReturnUrl.startsWith("//")
      ? rawReturnUrl
      : "/";

  const [submitting, setSubmitting] = useState(false);

  // Validate form
  const schema = Yup.object().shape({
    email: Yup.string()
      .email("Please enter a valid e-mail.")
      .required("E-mail is required."),
    password: Yup.string().required("Password is required."),
  });

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm({ resolver: yupResolver(schema) });

  /* =================== HANDLE LOGIN ====================== */

  const onSubmit = async ({ email, password }) => {
    try {
      setSubmitting(true);

      // ❗ Gọi login của UserContext – context sẽ dùng bffPublicApi.login()
      const dto = await login(email, password);

      if (!dto) {
        toast.error("Login succeeded but cannot fetch session.", {
          position: "top-center",
          autoClose: 1500,
          theme: "dark",
          transition: Bounce,
        });
        return;
      }

      const admin = isAdminUser(dto);
      const adminFallback = "/admin";

      let target = safeReturnUrl;

      if (admin) {
        // Nếu user là admin nhưng returnUrl không nằm trong admin route → ép qua admin
        if (!isAdminUrl(safeReturnUrl)) {
          target = adminFallback;
        }
      } else {
        // User thường nhưng returnUrl lại là admin → chuyển về "/"
        if (isAdminUrl(safeReturnUrl)) {
          target = "/";
        }
      }

      toast.success("Login successful!", {
        position: "top-center",
        autoClose: 600,
        theme: "dark",
        transition: Bounce,
      });

      history.replace(target);
    } catch (e) {
      console.error("Login error:", e);
      toast.error("Cannot connect or wrong credentials.", {
        position: "top-center",
        autoClose: 1500,
        theme: "dark",
        transition: Bounce,
      });
    } finally {
      setSubmitting(false);
    }
  };

  /* =================== UI RENDER ====================== */

  return (
    <Container>
      <LoginImage src={LoginImg} alt="login" />

      <ContainerItems
        style={{
          backgroundImage: `url(${bgHome})`,
          backgroundRepeat: "no-repeat",
          backgroundSize: "cover",
          backgroundPosition: "center",
        }}
      >
        <HeaderName>JoJo&apos;s</HeaderName>
        <HeaderBurger>Burger</HeaderBurger>

        <H1Login>Login</H1Login>

        <form noValidate onSubmit={handleSubmit(onSubmit)}>
          <Label>Email</Label>
          <Input
            type="email"
            {...register("email")}
            error={errors.email?.message}
            autoComplete="username"
          />
          <ErrorMessage>{errors.email?.message}</ErrorMessage>

          <Label>Password</Label>
          <Input
            type="password"
            {...register("password")}
            error={errors.password?.message}
            autoComplete="current-password"
          />
          <ErrorMessage>{errors.password?.message}</ErrorMessage>

          <Button
            type="submit"
            disabled={submitting}
            style={{ marginTop: "4.1875rem", marginBottom: "1.8125rem" }}
          >
            Sign In
          </Button>
        </form>

        <SignInLink>
          Don&apos;t have an account? <Link to="/register">Sign Up!</Link>
        </SignInLink>
      </ContainerItems>
    </Container>
  );
}
