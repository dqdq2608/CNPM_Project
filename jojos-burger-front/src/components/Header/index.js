import MenuIcon from "@mui/icons-material/Menu";
import IconButton from "@mui/material/IconButton";
import Menu from "@mui/material/Menu";
import MenuItem from "@mui/material/MenuItem";
import React, { useState } from "react";
import { useHistory } from "react-router-dom";

import CartLogo from "../../assets/cart.png";
import UserLogo from "../../assets/user.png";
import { useUser } from "../../hooks/UserContext";
import {
  Container,
  ContainerLeft,
  ContainerRight,
  ContainerText,
  PageLink,
  PageLinkExit,
  Line,
} from "./styles";

export function Header() {
  const { user, loading, logout } = useUser();
  const {
    push,
    location: { pathname },
  } = useHistory();

  const [anchorEl, setAnchorEL] = useState(null);

  const handleClickIcon = (event) => setAnchorEL(event.currentTarget);
  const handleCloseIcon = () => setAnchorEL(null);

  const logoutUser = async () => {
    await logout();
    push("/login");
  };

  // kiểm tra quyền admin (tuỳ claim bạn trả về)
  const isAdmin =
    user?.role === "admin" ||
    user?.isAdmin === "true" ||
    user?.admin === "true";

  const handleAdminClick = () => {
    if (isAdmin) push("/orders");
  };

  // Ưu tiên hiển thị displayName (được tính trong UserContext)
  const displayName = !loading
    ? user?.displayName || user?.name || user?.email || "Guest"
    : "…";

  return (
    <Container>
      <ContainerLeft>
        {window.innerWidth > 950 ? (
          <>
            <PageLink onClick={() => push("/")} isActive={pathname === "/"}>
              Home
            </PageLink>
            <PageLink
              onClick={() => push("/products")}
              isActive={pathname.includes("/products")}
            >
              Products
            </PageLink>
          </>
        ) : (
          <>
            <IconButton
              aria-label="menu"
              aria-controls="basic-menu"
              aria-haspopup="true"
              onClick={handleClickIcon}
              color="inherit"
            >
              <MenuIcon />
            </IconButton>

            <Menu
              id="basic-menu"
              anchorEl={anchorEl}
              open={Boolean(anchorEl)}
              onClose={handleCloseIcon}
              slotProps={{
                paper: { style: { backgroundColor: "#fbeee0" } },
              }}
            >
              <MenuItem onClick={() => push("/")}>Home</MenuItem>
              <MenuItem onClick={() => push("/products")}>Products</MenuItem>
            </Menu>
          </>
        )}
      </ContainerLeft>

      <ContainerRight>
        <PageLink>
          <img
            src={CartLogo}
            onClick={() => push("/cart")}
            style={{ width: "25px" }}
            alt="cart-logo"
          />
        </PageLink>

        <Line />

        <PageLink>
          <img
            src={UserLogo}
            style={{ width: "25px", cursor: isAdmin ? "pointer" : "default" }}
            alt="user-logo"
            onClick={handleAdminClick}
            title={isAdmin ? "Go to admin" : ""}
          />
        </PageLink>

        <ContainerText>
          <p>Welcome {displayName}!</p>
          {!loading && user && (
            <PageLinkExit onClick={logoutUser}>Logout</PageLinkExit>
          )}
        </ContainerText>
      </ContainerRight>
    </Container>
  );
}
