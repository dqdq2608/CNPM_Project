// src/containers/MyOrders/styles.js
import styled from "styled-components";

export const Container = styled.div`
  max-width: 900px;
  margin: 32px auto;
  padding: 0 16px;
  display: flex;
  flex-direction: column;
  gap: 16px;
`;

export const OrderCard = styled.div`
  background: #fbeee0;
  border-radius: 12px;
  padding: 16px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.08);
`;

export const OrderHeader = styled.div`
  display: flex;
  justify-content: space-between;
  margin-bottom: 8px;
  cursor: pointer; /* 👈 click để expand/collapse */

  & > div:first-child strong {
    display: block;
    font-size: 16px;
    color: #422800;
  }

  & > div:first-child span {
    font-size: 12px;
    color: #777;
  }

  & > div:last-child span {
    font-size: 12px;
    font-weight: bold;
    color: #422800;
  }

  & > div:last-child button {
    border: none;
    background: #f57c00;
    color: #fff;
    font-size: 12px;
    padding: 4px 8px;
    border-radius: 8px;
    cursor: pointer;
  }

  & > div:last-child button:hover {
    opacity: 0.9;
  }
`;

export const OrderItems = styled.ul`
  list-style: none;
  padding: 0;
  margin: 8px 0 0 0;

  li {
    display: flex;
    justify-content: space-between;
    font-size: 14px;
    padding: 4px 0;
    border-bottom: 1px dashed #d3b8a5;
  }

  li:last-child {
    border-bottom: none;
  }
`;

export const OrderFooter = styled.div`
  margin-top: 8px;
  text-align: right;
  font-weight: bold;
  color: #422800;
`;
