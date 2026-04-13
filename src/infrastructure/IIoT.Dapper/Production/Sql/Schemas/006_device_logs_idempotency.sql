alter table device_logs
    add column if not exists idempotency_key varchar(64);

create unique index if not exists ux_device_logs_idempotency
    on device_logs (device_id, log_time, idempotency_key)
    where idempotency_key is not null;
