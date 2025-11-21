import PropTypes from "prop-types";
import React, {
  createContext,
  useContext,
  useEffect,
  useState,
  useCallback,
} from "react";

import {
  fetchBasket,
  saveBasketFromCart,
  clearBasketApi,
} from "../services/api/basket";
import { useUser } from "./UserContext";

const CartContext = createContext({});

export const CartProvider = ({ children }) => {
  const [cartProducts, setCartProducts] = useState([]);
  const [selectedRestaurant, setSelectedRestaurant] = useState(null);
  const { user } = useUser();

  const updateLocalStorage = useCallback(
    async (products) => {
      if (user?.sub) {
        await localStorage.setItem(
          `jojosburger:cartInfo:${user.sub}`,
          JSON.stringify(products)
        );
      } else {
        await localStorage.setItem(
          "jojosburger:cartInfo:guest",
          JSON.stringify(products)
        );
      }
    },
    [user?.sub] // 👈 hàm này chỉ thay đổi khi user.sub thay đổi
  );

  const updateRestaurantLocalStorage = useCallback(
    async (restaurant) => {
      const key = user?.sub
        ? `jojosburger:selectedRestaurant:${user.sub}`
        : "jojosburger:selectedRestaurant:guest";

      if (restaurant) {
        await localStorage.setItem(key, JSON.stringify(restaurant));
      } else {
        await localStorage.removeItem(key);
      }
    },
    [user?.sub]
  );

  const selectRestaurant = async (restaurant) => {
    setSelectedRestaurant(restaurant || null);
    await updateRestaurantLocalStorage(restaurant || null);
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
        setSelectedRestaurant(null);
        await updateLocalStorage([]);
        await updateRestaurantLocalStorage(null);
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

        const key = user?.sub
          ? `jojosburger:selectedRestaurant:${user.sub}`
          : "jojosburger:selectedRestaurant:guest";

        const stored = localStorage.getItem(key);
        if (stored) {
          try {
            const parsed = JSON.parse(stored);
            setSelectedRestaurant(parsed);
          } catch {
            setSelectedRestaurant(null);
          }
        } else {
          setSelectedRestaurant(null);
        }
      } catch (e) {
        console.error("Load basket from server failed:", e);
        // Lỗi backend -> vẫn để giỏ rỗng, tránh lẫn giỏ user cũ
        setCartProducts([]);
        await updateLocalStorage([]);
      }
    };

    loadUserData();
  }, [user?.sub, updateLocalStorage, updateRestaurantLocalStorage]);

  return (
    <CartContext.Provider
      value={{
        putProductInCart,
        cartProducts,
        increaseQuantity,
        decreaseQuantity,
        deleteProduct,
        clearCart,
        selectedRestaurant,
        selectRestaurant,
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
