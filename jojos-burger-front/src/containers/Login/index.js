import React, { useState, useEffect } from "react";
import { yupResolver } from "@hookform/resolvers/yup";
import { useForm } from "react-hook-form";
import { Link } from "react-router-dom";
import { toast, Bounce } from "react-toastify";
import * as Yup from "yup";

import api from "../../services/api";
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

const BFF_BASE = process.env.REACT_APP_API_BASE || "https://localhost:7082";
await fetch(`${BFF_BASE}/bff/antiforgery`, { credentials: "include" });

export function Login() {
  const [submitting, setSubmitting] = useState(false);
  const [bffOk, setBffOk] = useState(null);

  useEffect(() => {
    (async () => {
      // 1) buộc server set cookie BffCsrf
      fetch(`${BFF_BASE}/bff/antiforgery`, { credentials: "include" })
        .then(() => console.log("✅ CSRF cookie set"))
        .catch(() => console.warn("⚠️ Failed to fetch CSRF cookie"));

      // 2) test /bff/user
      const r = await api.get("/bff/user");
      console.log("[BFF TEST] /bff/user:", r.status, r.data);
    })();
  }, []);

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

  // Xử lý login
  const onSubmit = async (clientData) => {
    if (bffOk === false) {
      toast.error("BFF is not reachable.", {
        position: "top-center",
        autoClose: 2000,
        theme: "dark",
        transition: Bounce,
      });
      return;
    }

    try {
      setSubmitting(true);

      const res = await api.post(
        "/auth/password-login",
        {
          username: clientData.email,
          password: clientData.password,
        },
        { validateStatus: () => true },
      );

      if (res.status === 200) {
        toast.success("Login successful!", {
          position: "top-center",
          autoClose: 2000,
          theme: "dark",
          transition: Bounce,
        });

        // Nếu BFF đã set cookie session, chỉ cần reload / redirect
        setTimeout(() => {
          window.location.assign("/");
        }, 1500);

        return;
      }

      if (res.status === 401) {
        toast.error("Incorrect e-mail or password.", {
          position: "top-center",
          autoClose: 2000,
          theme: "dark",
          transition: Bounce,
        });
      } else {
        toast.error(`Login failed (${res.status}).`, {
          position: "top-center",
          autoClose: 2000,
          theme: "dark",
          transition: Bounce,
        });
      }
    } catch (err) {
      console.error("❌ Login request error:", err);
      toast.error("Cannot connect to BFF. Please try again later.", {
        position: "top-center",
        autoClose: 2000,
        theme: "dark",
        transition: Bounce,
      });
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Container>
      <LoginImage src={LoginImg} alt="login-image" />

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
            disabled={submitting || bffOk === false}
            style={{ marginTop: "4.1875rem", marginBottom: "1.8125rem" }}
          >
            Sign In
          </Button>
        </form>

        <SignInLink>
          Don&apos;t have an account? <Link to="/register">Sign Up!</Link>{" "}
        </SignInLink>
      </ContainerItems>
    </Container>
  );
}
