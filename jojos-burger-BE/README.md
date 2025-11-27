Lệnh chạy seed account:
docker compose run --rm ids /seed

Lệnh kiểm tra bảng order:
docker exec -it ordering-db bash
psql -U ordering -d orderingdb

    -- Trong psql:
    \dn           -- xem schema, phải thấy 'ordering'
    \dt ordering.* -- xem bảng trong schema ordering, phải có 'ordering.orders'

Lệnh tạo schema thủ công trên supabase:

-- Tạo schema 'delivery' nếu chưa có
create schema if not exists delivery;

-- Tạo bảng 'deliveryorders' trong schema 'delivery'
create table if not exists delivery.deliveryorders (
"Id" serial primary key,
"OrderId" integer not null,
"RestaurantLat" double precision not null,
"RestaurantLon" double precision not null,
"CustomerLat" double precision not null,
"CustomerLon" double precision not null,
"DistanceKm" double precision not null,
"DeliveryFee" numeric not null,
"Status" integer not null,
"CreatedAt" timestamptz not null,
"UpdatedAt" timestamptz null
);
