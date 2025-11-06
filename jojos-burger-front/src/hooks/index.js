// src/hooks/index.js
import React from "react";
import PropTypes from "prop-types";

// ⬇️ PHẢI import để dùng trong JSX
import { UserProvider } from "./UserContext";
import { CartProvider } from "./CartContext";

// Re-export cho nơi khác có thể import theo tên
export * from "./UserContext";
export * from "./CartContext";

// Export mặc định: AppProvider bọc toàn app
const AppProvider = ({ children }) => (
  <UserProvider>
    <CartProvider>{children}</CartProvider>
  </UserProvider>
);

AppProvider.propTypes = {
  children: PropTypes.node,
};

export default AppProvider;
