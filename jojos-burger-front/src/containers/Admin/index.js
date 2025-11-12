import PropTypes from "prop-types";
import React, { useEffect } from "react";
import { useHistory, useLocation } from "react-router-dom";

import { SideMenuAdmin } from "../../components";
import paths from "../../constants/paths";
import EditCategory from "./EditCategory";
import EditProduct from "./EditProduct";
import ListCategories from "./ListCategories";
import ListProducts from "./ListProducts";
import NewCategory from "./NewCategory";
import NewProduct from "./NewProducts";
import Orders from "./Orders";
import { Container, ContainerItems } from "./styles";

export function Admin() {
  const history = useHistory();
  const { pathname } = useLocation();

  useEffect(() => {
    if (pathname === "/admin") {
      const defaultAdminPath =
        (typeof paths?.Products === "string" && paths.Products) ||
        "/admin/products";
      history.replace(defaultAdminPath);
    }
  }, [pathname, history]);

  return (
    <Container>
      <SideMenuAdmin path={pathname} />
      <ContainerItems>
        {pathname === paths.Order && <Orders />}
        {pathname === paths.Products && <ListProducts />}
        {pathname === paths.NewProduct && <NewProduct />}
        {pathname === paths.EditProduct && <EditProduct />}
        {pathname === paths.Categories && <ListCategories />}
        {pathname === paths.NewCategory && <NewCategory />}
        {pathname === paths.EditCategory && <EditCategory />}
      </ContainerItems>
    </Container>
  );
}

Admin.propTypes = {
  match: PropTypes.shape({
    path: PropTypes.string,
  }),
};
