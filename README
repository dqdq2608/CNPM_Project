# 🍔 Jojo’s Burger – Microservices E-Commerce System

Một hệ thống thương mại điện tử food-ordering được thiết kế theo kiến trúc microservices hiện đại, tích hợp xác thực, API Gateway, xử lý đơn hàng và thanh toán đa luồng.

---

## 📚 Mục lục

1. [Giới thiệu](#1-giới-thiệu)
2. [Các dịch vụ](#2-các-dịch-vụ)
3. [API Gateway (Kong)](#3-api-gateway-kong)
4. [Frontend React](#4-frontend-react)
5. [Chạy hệ thống](#5-chạy-hệ-thống)
6. [Mở rộng & Ghi chú thêm](#6-mở-rộng--ghi-chú-thêm)
7. [Nguồn tham khảo](#7-nguồn-tham-khảo)

---

## 1. Giới thiệu

Jojo's Burger giúp người dùng đặt món ăn online và thanh toán nhanh chóng. Hệ thống được chia thành nhiều microservices độc lập, mỗi service có database riêng (PostgreSQL, Redis) và được giao tiếp qua Kong Gateway & BFF.

- FE giao tiếp thông qua BFF
- BFF xử lý login + cookie + CSRF
- Kong định tuyến tới các service nội bộ
- Các service có thể giao tiếp qua event bus (RabbitMQ)

---
## 2. Các dịch vụ

| Service           | DB          | Giao tiếp         | Mô tả |
|-------------------|-------------|-------------------|-------|
| Catalog.API       | PostgreSQL  | /catalog/api      | Quản lý sản phẩm |
| Basket.API        | Redis       | /basket/api       | Quản lý giỏ hàng theo user |
| Order.API         | PostgreSQL  | /order/api        | Tạo và xử lý đơn hàng |
| Payment.API       | _           | /payment/api      | Giao tiếp với payment provider |
| IdentityServer    | PostgreSQL  | /connect/token    | Xác thực user & cấp token |
| BFF (Duende)      | —           | /bff-api/*        | Proxy API, xử lý CSRF & auth |
| IdentityServer (Duende) | PostgreSQL  | /connect/token, /connect/authorize | Cấp phát access token, refresh token và xác thực người dùng thông qua OpenID Connect (OIDC) |
| Webhook Service   | —           | qua RabbitMQ      | Nhận callback thanh toán |

---

## 3. API Gateway (Kong)

Kong định tuyến request từ BFF/FE đến các service nội bộ:

```yaml
- name: catalog
  url: http://catalog-api:8080
  routes:
    - paths: [ /catalog/api/catalog ]
      strip_path: true
```

Tương tự cho các route khác như `/basket`, `/order`, `/payment`.

---

## 4. Frontend React

```bash
npm install
npm start
```

Môi trường `.env` ví dụ:
```env
REACT_APP_API_BASE=https://localhost:7082
REACT_APP_CATALOG_API_BASE=https://localhost:8443/catalog
REACT_APP_BASKET_API_BASE=https://localhost:7082/bff-api/basket
REACT_APP_ORDER_API_BASE=https://localhost:7082/bff-api/order
```

---

## 5. Chạy hệ thống

Chạy toàn bộ backend + gateway + databases:

```bash
docker compose up --build
```

### Port Mặc định:

| Service        | URL |
|----------------|-----|
| BFF            | https://localhost:7082 |
| Kong           | https://localhost:8443 |
| IdentityServer | https://localhost:5001 |
| Catalog API    | http://localhost:7002 |
| Basket API     | http://localhost:5005 |
| Order API      | http://localhost:5010 |
| Payment API    | http://localhost:5015 |
| FE React       | https://localhost:3000 |

---

## 6. Mở rộng & Ghi chú thêm

- Thêm Notification Service / Email Service
- Tách frontend & BFF deploy riêng nếu cần
- Scale các service độc lập bằng Docker Swarm hoặc Kubernetes

---
## 7. Nguồn tham khảo

- 🌐 Frontend (React):  
  [https://github.com/jhschier/jojos-burger-front.git](https://github.com/jhschier/jojos-burger-front.git)

- 🏗 Backend (Microservices):  
  [https://github.com/dotnet/eShop.git](https://github.com/dotnet/eShop.git)

---

> Đây là hệ thống mô phỏng  dùng để học kiến trúc microservices, bảo mật web (Cookie Auth + CSRF).