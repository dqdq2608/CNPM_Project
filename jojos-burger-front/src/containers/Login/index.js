import React, { useState } from "react";
import { useForm } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import * as Yup from "yup";
import { Link, useHistory, useLocation } from "react-router-dom";
import { toast, Bounce } from "react-toastify";

import bgHome from "../../assets/bg-home.jpg";
import LoginImg from "../../assets/login-body.svg";
import { Button, ErrorMessage } from "../../components";
import { useUser } from "../../hooks/UserContext";
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

export function Login() {
  const history = useHistory();
  const location = useLocation();
  const { login } = useUser();

  // hỗ trợ returnUrl=/path (nếu có)
  const params = new URLSearchParams(location.search);
  const rawReturnUrl = params.get("returnUrl") || "/";
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

  const onSubmit = async (clientData) => {
    try {
      setSubmitting(true);

      // gọi login từ UserContext (đã lo /auth/password-login + CSRF)
      await login(clientData.email, clientData.password);

      toast.success("Login successful!", {
        position: "top-center",
        autoClose: 600,
        theme: "dark",
        transition: Bounce,
      });

      // điều hướng mượt (SPA)
      const notLogin = (p) => p && p !== "/login" && !p.startsWith("/login?");
      const finalUrl = notLogin(safeReturnUrl) ? safeReturnUrl : "/";
      history.replace(finalUrl);
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
