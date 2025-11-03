import React from "react";
import { Switch, Route, BrowserRouter as Router } from "react-router-dom";

import paths from "../constants/paths";
// import { Home, Login, Products, Register, Cart, Admin } from '../containers'
import PrivateRoute from "./private-routes";
// import AuthCallback from "./pages/AuthCallback";
import { Home, Login, Products, Register, Cart } from "../containers";

function Routes() {
  return (
    <Router>
      <Switch>
        <Route component={Login} path="/login" />
        <Route component={Register} path="/register" />
        <PrivateRoute exact component={Home} path="/" />
        <PrivateRoute component={Products} path="/products" />
        <PrivateRoute component={Cart} path="/cart" />

        {/* <PrivateRoute component={Admin} path={paths.Order} isAdmin />
        <PrivateRoute component={Admin} path={paths.Products} isAdmin />
        <PrivateRoute component={Admin} path={paths.NewProduct} isAdmin />
        <PrivateRoute component={Admin} path={paths.EditProduct} isAdmin />
        <PrivateRoute component={Admin} path={paths.NewCategory} isAdmin />
        <PrivateRoute component={Admin} path={paths.EditCategory} isAdmin />
        <PrivateRoute component={Admin} path={paths.Categories} isAdmin /> */}
      </Switch>
    </Router>
  );
}

export default Routes;
