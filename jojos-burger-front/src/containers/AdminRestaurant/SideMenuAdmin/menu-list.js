// src/containers/AdminRestaurant/SideMenuAdmin/menu-list.js
import AddBoxIcon from "@mui/icons-material/AddBox";
import EditIcon from "@mui/icons-material/Edit";
import RestaurantIcon from "@mui/icons-material/Restaurant";

import paths from "../../../constants/paths";

const listLinks = [
  {
    id: 1,
    label: "Restaurants",
    link: paths.Restaurants,     // /admin-restaurant
    icon: RestaurantIcon,
  },
  {
    id: 2,
    label: "New Restaurant",
    link: paths.NewRestaurant,  // /admin-restaurant/new
    icon: AddBoxIcon,
  },
  {
    id: 3,
    label: "Edit Restaurant",
    link: paths.EditRestaurant, // /admin-restaurant/edit
    icon: EditIcon,
  },
  {
    id: 4,
    label: "Delete Restaurant",
    link: paths.DeleteRestaurant, // /admin-restaurant/delete
    icon: EditIcon,
  },
  {
    id: 5,
    label: "Stats",
    link: paths.Stats, // /admin-restaurant/delete
    icon: EditIcon,
  },
];

export default listLinks;
