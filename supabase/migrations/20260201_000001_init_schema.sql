-- =========================
-- Extensions
-- =========================
create extension if not exists pgcrypto;

-- =========================
-- USERS
-- =========================
create table if not exists users (
  user_id           uuid primary key default gen_random_uuid(),
  email             text unique not null,
  password_hash     text not null,
  display_name      text not null,
  created_at        timestamptz not null default now()
);

-- =========================
-- GROUPS
-- =========================
create table if not exists groups (
  group_id          uuid primary key default gen_random_uuid(),
  name              text not null,
  created_by_user   uuid not null references users(user_id),
  created_at        timestamptz not null default now()
);

-- =========================
-- GROUP MEMBERS
-- =========================
create table if not exists group_members (
  group_id          uuid not null references groups(group_id) on delete cascade,
  user_id           uuid not null references users(user_id) on delete cascade,
  role              text not null default 'member',
  joined_at         timestamptz not null default now(),
  primary key (group_id, user_id)
);

-- =========================
-- PANTRIES
-- =========================
create table if not exists pantries (
  pantry_id         uuid primary key default gen_random_uuid(),
  group_id          uuid not null references groups(group_id) on delete cascade,
  name              text not null,
  created_at        timestamptz not null default now()
);

-- =========================
-- PANTRY LOCATIONS
-- =========================
create table if not exists pantry_locations (
  location_id       uuid primary key default gen_random_uuid(),
  pantry_id         uuid not null references pantries(pantry_id) on delete cascade,
  name              text not null,
  notes             text,
  created_by_user   uuid references users(user_id),
  created_at        timestamptz not null default now(),
  unique (pantry_id, name)
);

-- =========================
-- ITEM CATEGORIES (scoped to group)
-- =========================
create table if not exists item_categories (
  category_id       uuid primary key default gen_random_uuid(),
  group_id          uuid not null references groups(group_id) on delete cascade,
  name              text not null,
  created_by_user   uuid references users(user_id),
  created_at        timestamptz not null default now(),
  unique (group_id, name)
);

-- =========================
-- ITEM CATALOG
-- =========================
create table if not exists item_catalog (
  item_id           uuid primary key default gen_random_uuid(),
  name              text not null,
  brand             text,
  description       text,
  default_unit      text,
  barcode           text,
  category_id       uuid references item_categories(category_id) on delete set null,
  unique (name, brand)
);

-- =========================
-- TAGS (scoped to group)
-- =========================
create table if not exists item_tags (
  tag_id            uuid primary key default gen_random_uuid(),
  group_id          uuid not null references groups(group_id) on delete cascade,
  name              text not null,
  created_by_user   uuid references users(user_id),
  created_at        timestamptz not null default now(),
  unique (group_id, name)
);

-- Many-to-many: item_catalog <-> tags
create table if not exists item_catalog_tags (
  item_id           uuid not null references item_catalog(item_id) on delete cascade,
  tag_id            uuid not null references item_tags(tag_id) on delete cascade,
  primary key (item_id, tag_id)
);

-- =========================
-- INVENTORY ITEMS
-- =========================
create table if not exists inventory_items (
  inventory_id      uuid primary key default gen_random_uuid(),
  pantry_id         uuid not null references pantries(pantry_id) on delete cascade,
  location_id       uuid references pantry_locations(location_id) on delete set null,

  item_id           uuid references item_catalog(item_id),
  custom_name       text,

  quantity          numeric(12, 3) not null default 0,
  unit              text not null,

  expires_on        date,
  notes             text,

  created_by_user   uuid references users(user_id),
  created_at        timestamptz not null default now(),
  updated_at        timestamptz not null default now(),

  check (
    (item_id is not null and custom_name is null)
    or
    (item_id is null and custom_name is not null)
  )
);

-- =========================
-- LISTS
-- =========================
create table if not exists lists (
  list_id           uuid primary key default gen_random_uuid(),
  group_id          uuid not null references groups(group_id) on delete cascade,
  name              text not null,
  list_type         text not null default 'shopping',
  created_by_user   uuid not null references users(user_id),
  created_at        timestamptz not null default now(),
  archived_at       timestamptz
);

-- =========================
-- LIST ITEMS
-- =========================
create table if not exists list_items (
  list_item_id      uuid primary key default gen_random_uuid(),
  list_id           uuid not null references lists(list_id) on delete cascade,

  item_id           uuid references item_catalog(item_id),
  custom_name       text,

  quantity          numeric(12, 3) not null default 1,
  unit              text,

  is_checked        boolean not null default false,
  added_by_user     uuid references users(user_id),
  created_at        timestamptz not null default now(),

  check (
    (item_id is not null and custom_name is null)
    or
    (item_id is null and custom_name is not null)
  )
);

-- =========================
-- Indexes
-- =========================
create index if not exists idx_group_members_user on group_members(user_id);
create index if not exists idx_pantries_group on pantries(group_id);
create index if not exists idx_locations_pantry on pantry_locations(pantry_id);
create index if not exists idx_inventory_pantry on inventory_items(pantry_id);
create index if not exists idx_inventory_location on inventory_items(location_id);
create index if not exists idx_inventory_item on inventory_items(item_id);
create index if not exists idx_categories_group on item_categories(group_id);
create index if not exists idx_tags_group on item_tags(group_id);
create index if not exists idx_item_catalog_tags_tag on item_catalog_tags(tag_id);
create index if not exists idx_lists_group on lists(group_id);
create index if not exists idx_list_items_list on list_items(list_id);

-- =========================
-- updated_at trigger for inventory_items
-- =========================
create or replace function set_updated_at()
returns trigger as $$
begin
  new.updated_at = now();
  return new;
end;
$$ language plpgsql;

drop trigger if exists trg_inventory_items_updated_at on inventory_items;
create trigger trg_inventory_items_updated_at
before update on inventory_items
for each row
execute function set_updated_at();