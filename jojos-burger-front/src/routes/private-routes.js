import PropTypes from "prop-types";
import React from "react";
import { Route, Redirect } from "react-router-dom";
import { useUser } from "../hooks/UserContext";
import { Header, Footer } from "../components";

export default function PrivateRoute({ component, isAdmin, ...rest }) {
  const { user, loading } = useUser();
  const C = component;

  if (loading) return null; // hoặc spinner

  if (!user) return <Redirect to="/login" />;

  // nếu có role admin thì bạn tự kiểm tra từ claim user.role (nếu dùng)
  if (isAdmin && user.role !== "admin") {
    return <Redirect to="/orders" />;
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
