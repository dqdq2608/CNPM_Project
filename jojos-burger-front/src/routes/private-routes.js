import PropTypes from "prop-types";
import React from "react";
import { Route, Redirect } from "react-router-dom";

import { Header, Footer } from "../components";
import { useUser } from "../hooks/UserContext";

function extractClaims(user) {
  if (!user?.raw) return [];
  return user.raw.map((c) => ({
    type: c.type.toLowerCase(),
    value: c.value,
  }));
}

// FIX: Nhận diện admin đúng cho hệ thống của bạn
function hasAdminRole(user) {
  const claims = extractClaims(user);
  return claims.some((c) => {
    const v = String(c.value).toLowerCase();
    return v === "admin" || v === "restaurantadmin";
  });
}

export default function PrivateRoute({ component, isAdmin, ...rest }) {
  const { user, loading } = useUser();
  const C = component;

  if (loading) return null;

  if (!user) return <Redirect to="/login" />;

  // FIX: Cho phép RestaurantAdmin
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
