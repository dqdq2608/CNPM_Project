import React, { useEffect, useState } from "react";

import { useCart } from "../../hooks/CartContext";
import { fetchRestaurants } from "../../services/api/catalog";
import {
  Container,
  Title,
  List,
  Item,
  Radio,
  Info,
  Address,
  Small,
} from "./styles";

export function RestaurantSelector() {
  const { selectedRestaurant, selectRestaurant } = useCart();

  const [restaurants, setRestaurants] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    const loadRestaurants = async () => {
      try {
        setLoading(true);
        setError("");

        const data = await fetchRestaurants();
        setRestaurants(data);
      } catch (err) {
        console.error("Fetch restaurants failed:", err);
        setError("Không tải được danh sách chi nhánh. Vui lòng thử lại.");
      } finally {
        setLoading(false);
      }
    };

    loadRestaurants();
  }, []);

  const handleSelect = (restaurant) => {
    // lưu full object vào CartContext + localStorage
    selectRestaurant(restaurant);
  };

  return (
    <Container>
      <Title>Chọn chi nhánh giao hàng</Title>

      {loading && <Small>Đang tải danh sách chi nhánh...</Small>}
      {error && <Small style={{ color: "#e74c3c" }}>{error}</Small>}

      {!loading && !error && restaurants.length === 0 && (
        <Small>Hiện chưa có chi nhánh nào khả dụng.</Small>
      )}

      <List>
        {restaurants.map((r) => {
          const isActive = selectedRestaurant?.id === r.id;

          return (
            <Item
              key={r.id}
              isActive={isActive}
              onClick={() => handleSelect(r)}
            >
              <Radio type="radio" readOnly checked={isActive} />
              <Info>
                <strong>{r.name}</strong>
                <Address>{r.address}</Address>
                <Small>
                  Lat: {r.latitude?.toFixed?.(5) ?? r.latitude} – Lon:{" "}
                  {r.longitude?.toFixed?.(5) ?? r.longitude}
                </Small>
              </Info>
            </Item>
          );
        })}
      </List>
    </Container>
  );
}
