// src/components/RestaurantSelector/styles.js
import styled from "styled-components";

export const Container = styled.div`
  margin-top: 24px;
  margin-bottom: 24px;
  padding: 16px 20px;
  border-radius: 16px;
  background-color: #fbeee0;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.08);
`;

export const Title = styled.h3`
  margin: 0 0 12px;
  font-size: 18px;
  font-weight: 600;
  color: #333;
`;

export const List = styled.div`
  display: flex;
  flex-direction: column;
  gap: 8px;
`;

export const Item = styled.button`
  display: flex;
  align-items: flex-start;
  width: 100%;
  text-align: left;
  border: none;
  border-radius: 12px;
  padding: 10px 12px;
  cursor: pointer;
  background-color: ${(props) => (props.isActive ? "#f1c40f33" : "#ffffff")};
  border: 1px solid ${(props) => (props.isActive ? "#f1c40f" : "#ddd")};

  &:hover {
    background-color: #fdf2c0;
  }
`;

export const Radio = styled.input`
  margin-top: 4px;
  margin-right: 10px;
`;

export const Info = styled.div`
  display: flex;
  flex-direction: column;
  gap: 2px;

  strong {
    font-size: 14px;
  }
`;

export const Address = styled.p`
  margin: 0;
  font-size: 13px;
  color: #555;
`;

export const Small = styled.p`
  margin: 6px 0 0;
  font-size: 12px;
  color: #777;
`;
