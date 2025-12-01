import AddBoxIcon from "@mui/icons-material/AddBox";
import AddShoppingCartIcon from "@mui/icons-material/AddShoppingCart";
import EditIcon from "@mui/icons-material/Edit";
import ListIcon from "@mui/icons-material/List";
import MopedIcon from "@mui/icons-material/Moped";
import ShoppingBagIcon from "@mui/icons-material/ShoppingBag";
import ShoppingCartIcon from "@mui/icons-material/ShoppingCart";

import paths from "../../constants/paths";

const listLinks = [
  {
    id: 1,
    label: "Orders",
    link: paths.Order,
    icon: ShoppingBagIcon,
  },
  {
    id: 2,
    label: "List Products",
    link: paths.Products,
    icon: ShoppingCartIcon,
  },
  {
    id: 3,
    label: "New Product",
    link: paths.NewProduct,
    icon: AddShoppingCartIcon,
  },
  {
    id: 4,
    label: "List Categories",
    link: paths.Categories,
    icon: ListIcon,
  },
  {
    id: 5,
    label: "New Category",
    link: paths.NewCategory,
    icon: AddBoxIcon,
  },

  {
    id: 6,
    label: "Edit Product",
    link: paths.EditProduct,
    icon: EditIcon,
  },
  {
    id: 7,
    label: "Edit Category",
    link: paths.EditCategory,
    icon: EditIcon,
  },
  {
    id: 8,
    label: "Drones",
    link: paths.Drones,
    icon: MopedIcon,
  },
];

export default listLinks;
