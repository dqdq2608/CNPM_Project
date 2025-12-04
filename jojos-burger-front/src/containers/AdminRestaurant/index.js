// src/containers/AdminRestaurant/index.js
import PropTypes from "prop-types";
import React from "react";
import { useLocation } from "react-router-dom";

import paths from "../../constants/paths";
import { Container, ContainerItems } from "../Admin/styles";
import { SideMenuAdmin } from "./SideMenuAdmin"; // <- named

import ListRestaurants from "./ListRestaurants";   // <- default
import NewRestaurant from "./NewRestaurant";       // <- default
import EditRestaurant from "./EditRestaurant";     // <- default
import DeleteRestaurant from "./DeleteRestaurant";
import AdminRestaurantOrders from "./Stats";

export function AdminRestaurant() {
  const { pathname } = useLocation();

  return (
    <Container>
      <SideMenuAdmin path={pathname} />

      <ContainerItems>
        {pathname === paths.Restaurants && <ListRestaurants />}
        {pathname === paths.NewRestaurant && <NewRestaurant />}
        {pathname === paths.EditRestaurant && <EditRestaurant />}
        {pathname === paths.DeleteRestaurant && <DeleteRestaurant />}
        {pathname === paths.Stats && <AdminRestaurantOrders />}
      </ContainerItems>
    </Container>
  );
}

AdminRestaurant.propTypes = {
  match: PropTypes.shape({
    path: PropTypes.string,
  }),
};
