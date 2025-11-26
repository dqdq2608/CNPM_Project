Lệnh chạy seed account:
docker compose run --rm ids /seed

Lệnh kiểm tra bảng order:
docker exec -it ordering-db bash
psql -U ordering -d orderingdb

    -- Trong psql:
    \dn           -- xem schema, phải thấy 'ordering'
    \dt ordering.* -- xem bảng trong schema ordering, phải có 'ordering.orders'
