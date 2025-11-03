import React, { useEffect } from "react";
import { userManager } from "../services/authService";
import { useNavigate } from "react-router-dom";

export default function AuthCallback() {
  const navigate = useNavigate();

  useEffect(() => {
    userManager.signinRedirectCallback().then(() => {
      navigate("/");
    });
  }, [navigate]);

  return <div>Signing in...</div>;
}
