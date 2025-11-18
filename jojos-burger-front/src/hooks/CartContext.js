import PropTypes from "prop-types";
import React, { createContext, useContext, useEffect, useState } from "react";

import {
  fetchBasket,
  saveBasketFromCart,
  clearBasketApi,
} from "../services/api/basket";
import { useUser } from "./UserContext";

const CartContext = createContext({});

export const CartProvider = ({ children }) => {
  const [cartProducts, setCartProducts] = useState([]);
  const { user } = useUser();

  const updateLocalStorage = async (products) => {
    if (user?.sub) {
      // user đã login → lưu theo từng user ID
      await localStorage.setItem(
        `jojosburger:cartInfo:${user.sub}`,
        JSON.stringify(products)
      );
    } else {
      // user chưa login → lưu 1 key chung
      await localStorage.setItem(
        "jojosburger:cartInfo:guest",
        JSON.stringify(products)
      );
    }
  };

  const putProductInCart = async (product) => {
    const cartIndex = cartProducts.findIndex((prd) => prd.id === product.id);

    let newCartProducts = [];
    if (cartIndex >= 0) {
      // tạo mảng mới, không mutate trực tiếp
      newCartProducts = cartProducts.map((prd, index) =>
        index === cartIndex ? { ...prd, quantity: prd.quantity + 1 } : prd
      );
      setCartProducts(newCartProducts);
    } else {
      const newProduct = { ...product, quantity: 1 };
      newCartProducts = [...cartProducts, newProduct];
      setCartProducts(newCartProducts);
    }

    await updateLocalStorage(newCartProducts);
    try {
      await saveBasketFromCart(newCartProducts);
    } catch (e) {
      console.error("Sync basket (putProductInCart) failed:", e);
    }
  };

  const deleteProduct = async (productId) => {
    const newCart = cartProducts.filter((product) => product.id !== productId);

    setCartProducts(newCart);

    await updateLocalStorage(newCart);
    try {
      await saveBasketFromCart(newCart);
    } catch (e) {
      console.error("Sync basket (deleteProduct) failed:", e);
    }
  };

  const increaseQuantity = async (productId) => {
    const newCart = cartProducts.map((product) => {
      return product.id === productId
        ? { ...product, quantity: product.quantity + 1 }
        : product;
    });

    setCartProducts(newCart);

    await updateLocalStorage(newCart);
    try {
      await saveBasketFromCart(newCart);
    } catch (e) {
      console.error("Sync basket (increaseQuantity) failed:", e);
    }
  };

  const decreaseQuantity = async (productId) => {
    const cartIndex = cartProducts.findIndex(
      (product) => product.id === productId
    );

    if (cartIndex >= 0 && cartProducts[cartIndex].quantity > 1) {
      const newCart = cartProducts.map((product) => {
        return product.id === productId
          ? { ...product, quantity: product.quantity - 1 }
          : product;
      });
      setCartProducts(newCart);

      await updateLocalStorage(newCart);
      try {
        await saveBasketFromCart(newCart);
      } catch (e) {
        console.error("Sync basket (decreaseQuantity) failed:", e);
      }
    }
  };

  const clearCart = async () => {
    setCartProducts([]);

    await updateLocalStorage([]);
    try {
      await clearBasketApi();
    } catch (e) {
      console.error("Clear basket on server failed:", e);
    }
  };

  useEffect(() => {
    const loadUserData = async () => {
      // Nếu CHƯA login → luôn cho giỏ rỗng
      if (!user?.sub) {
        setCartProducts([]);
        await updateLocalStorage([]);
        return;
      }

      // Nếu đã login → load giỏ từ Basket.API cho đúng user hiện tại
      try {
        const basket = await fetchBasket();
        if (basket && basket.items) {
          const mapped = basket.items.map((it) => ({
            id: it.productId,
            name: it.productName,
            price: it.unitPrice,
            url: it.pictureUrl,
            quantity: it.quantity,
          }));

          setCartProducts(mapped);
          await updateLocalStorage(mapped);
        } else {
          // Không có giỏ trên server -> để rỗng
          setCartProducts([]);
          await updateLocalStorage([]);
        }
      } catch (e) {
        console.error("Load basket from server failed:", e);
        // Lỗi backend -> vẫn để giỏ rỗng, tránh lẫn giỏ user cũ
        setCartProducts([]);
        await updateLocalStorage([]);
      }
    };

    loadUserData();
  }, [user?.sub]);

  return (
    <CartContext.Provider
      value={{
        putProductInCart,
        cartProducts,
        increaseQuantity,
        decreaseQuantity,
        deleteProduct,
        clearCart,
      }}
    >
      {children}
    </CartContext.Provider>
  );
};

export const useCart = () => {
  const context = useContext(CartContext);

  if (!context) {
    throw new Error("useCart must be used with UserContext.");
  }
  return context;
};

CartProvider.propTypes = {
  children: PropTypes.node,
};
