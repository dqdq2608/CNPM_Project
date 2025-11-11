// src/containers/Login/index.js
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

// ❗ Dùng UserContext thay vì gọi trực tiếp bffLogin/bffGetUser
import { useUser } from "../../hooks/UserContext";

// ✅ Nếu bạn đã có constants/paths, dùng để nhận biết các route admin
import paths from "../../constants/paths";

function pickClaims(me) {
  if (Array.isArray(me?.raw))
    return me.raw.map((c) => ({ type: c.type, value: c.value }));
  if (Array.isArray(me?.claims))
    return me.claims.map((c) => ({ type: c.type, value: c.value }));
  return [];
}

function hasAdminRole(me) {
  const claims = pickClaims(me);
  return claims.some(
    (c) => c.type === "role" && String(c.value).toLowerCase() === "admin",
  );
}

function buildAdminPathList() {
  // Tập các đường dẫn admin khả dụng—lọc cái nào thật sự có giá trị string
  const candidates = [
    paths?.Order,
    paths?.Products,
    paths?.NewProduct,
    paths?.EditProduct,
    paths?.NewCategory,
    paths?.EditCategory,
    paths?.Categories,
    "/admin", // fallback nếu dự án có route /admin
  ];
  return candidates.filter((p) => typeof p === "string" && p.length > 0);
}

function isAdminUrl(url) {
  if (typeof url !== "string") return false;
  const admins = buildAdminPathList();
  return admins.some((p) => url === p || url.startsWith(p + "/"));
}

export function Login() {
  const history = useHistory();
  const location = useLocation();
  const { login } = useUser(); // <— lấy hàm login từ context

  const params = new URLSearchParams(location.search);
  const rawReturnUrl = params.get("returnUrl") || "/";

  // Chỉ cho phép điều hướng nội bộ, tránh //host
  const safeReturnUrl =
    rawReturnUrl.startsWith("/") && !rawReturnUrl.startsWith("//")
      ? rawReturnUrl
      : "/";

  const [submitting, setSubmitting] = useState(false);

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

  const onSubmit = async ({ email, password }) => {
    try {
      setSubmitting(true);

      // ❗ Gọi login của context – hàm này đã ensure CSRF, gọi BFF, và setUser()
      const me = await login(email, password);

      if (me) {
        const isAdmin = hasAdminRole(me);

        // Đích mặc định cho admin: ưu tiên paths.Products, nếu không có dùng "/admin"
        const defaultAdminPath = "/admin";

        let target = safeReturnUrl;

        if (isAdmin) {
          // Nếu returnUrl không phải admin, chuyển sang admin default
          if (!isAdminUrl(safeReturnUrl)) {
            target = defaultAdminPath;
          }
        } else {
          // User thường mà returnUrl lại là admin => đưa về "/"
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
        return;
      }

      toast.error("Đăng nhập xong nhưng không đọc được phiên.", {
        position: "top-center",
        autoClose: 1500,
        theme: "dark",
        transition: Bounce,
      });
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
