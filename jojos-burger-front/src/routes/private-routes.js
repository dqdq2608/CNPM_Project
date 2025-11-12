// src/routes/private-routes.js
import PropTypes from "prop-types";
import React from "react";
import { Route, Redirect } from "react-router-dom";

import { Header, Footer } from "../components";
import { useUser } from "../hooks/UserContext";

function extractClaims(user) {
  if (Array.isArray(user?.raw))
    return user.raw.map((c) => ({ type: c.type, value: c.value }));
  if (Array.isArray(user?.claims)) return user.claims;
  return [];
}

function hasAdminRole(user) {
  const claims = extractClaims(user);
  return claims.some(
    (c) => c.type === "role" && String(c.value).toLowerCase() === "admin",
  );
}

export default function PrivateRoute({ component, isAdmin, ...rest }) {
  const { user, loading } = useUser();
  const C = component;

  if (loading) return null;
  if (!user) return <Redirect to="/login" />;

  if (isAdmin && !hasAdminRole(user)) {
    return <Redirect to="/" />;
  }

  return (
    <>
      {!isAdmin && <Header />}
      <Route {...rest} render={(props) => <C {...props} />} />
      {!isAdmin && <Footer />}
    </>
  );
}

PrivateRoute.propTypes = {
  component: PropTypes.oneOfType([PropTypes.func, PropTypes.element]),
  isAdmin: PropTypes.bool,
};
