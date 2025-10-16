-- Tạo schema riêng cho từng microservice
CREATE SCHEMA IF NOT EXISTS user_service;
CREATE SCHEMA IF NOT EXISTS order_service;
CREATE SCHEMA IF NOT EXISTS product_service;
CREATE SCHEMA IF NOT EXISTS payment_service;

-- Nếu có dùng PostGIS (sau này cho drone service)
-- CREATE EXTENSION IF NOT EXISTS postgis;

-- Gán quyền cho user 'app'
GRANT ALL PRIVILEGES ON DATABASE foodappdb TO postgres;

-- (Tuỳ chọn) Cài sẵn extension UUID
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
