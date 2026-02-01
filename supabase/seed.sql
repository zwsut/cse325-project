-- =========================
-- Seed data
-- =========================
with
u as (
  insert into users (email, password_hash, display_name)
  values
    ('zach@example.com', '$2b$10$exampleexampleexampleexampleexampleexampleexampleex', 'Zach'),
    ('alex@example.com', '$2b$10$exampleexampleexampleexampleexampleexampleexampleex', 'Alex')
  returning user_id, email, display_name
),
g as (
  insert into groups (name, created_by_user)
  select 'Sutherland Household', (select user_id from u where email='zach@example.com')
  returning group_id, name, created_by_user
),
gm as (
  insert into group_members (group_id, user_id, role)
  values
    ((select group_id from g), (select user_id from u where email='zach@example.com'), 'owner'),
    ((select group_id from g), (select user_id from u where email='alex@example.com'), 'member')
  returning group_id, user_id
),
p as (
  insert into pantries (group_id, name)
  values
    ((select group_id from g), 'Kitchen Pantry'),
    ((select group_id from g), 'Garage Shelf')
  returning pantry_id, name
),
loc as (
  insert into pantry_locations (pantry_id, name, notes, created_by_user)
  values
    ((select pantry_id from p where name='Kitchen Pantry'), 'Cupboard A', 'Top shelf', (select user_id from u where email='zach@example.com')),
    ((select pantry_id from p where name='Kitchen Pantry'), 'Fridge Door', 'Condiments', (select user_id from u where email='zach@example.com')),
    ((select pantry_id from p where name='Garage Shelf'), 'Garage Bin 2', 'Bulk storage', (select user_id from u where email='alex@example.com'))
  returning location_id, pantry_id, name
),
cat as (
  insert into item_categories (group_id, name, created_by_user)
  values
    ((select group_id from g), 'Bakery', (select user_id from u where email='zach@example.com')),
    ((select group_id from g), 'Canned', (select user_id from u where email='zach@example.com')),
    ((select group_id from g), 'Dairy',  (select user_id from u where email='alex@example.com'))
  returning category_id, name
),
items as (
  insert into item_catalog (name, brand, description, default_unit, barcode, category_id)
  values
    ('Bread', 'Stop & Shop', 'Sliced sandwich bread', 'loaf', null, (select category_id from cat where name='Bakery')),
    ('Milk', 'Hood', '2% milk', 'gallon', null, (select category_id from cat where name='Dairy')),
    ('Black Beans', 'Goya', 'Canned black beans', 'can', null, (select category_id from cat where name='Canned'))
  returning item_id, name
),
tags as (
  insert into item_tags (group_id, name, created_by_user)
  values
    ((select group_id from g), 'Bulk', (select user_id from u where email='alex@example.com')),
    ((select group_id from g), 'Emergency', (select user_id from u where email='zach@example.com')),
    ((select group_id from g), 'Kids', (select user_id from u where email='zach@example.com'))
  returning tag_id, name
),
itemtag as (
  insert into item_catalog_tags (item_id, tag_id)
  values
    ((select item_id from items where name='Black Beans'), (select tag_id from tags where name='Bulk')),
    ((select item_id from items where name='Black Beans'), (select tag_id from tags where name='Emergency')),
    ((select item_id from items where name='Milk'), (select tag_id from tags where name='Kids'))
  returning item_id, tag_id
),
inv as (
  insert into inventory_items (
    pantry_id, location_id, item_id, custom_name, quantity, unit, expires_on, notes, created_by_user
  )
  values
    (
      (select pantry_id from p where name='Kitchen Pantry'),
      (select location_id from loc where name='Cupboard A'),
      (select item_id from items where name='Bread'),
      null,
      2, 'loaf',
      null,
      'Use for lunches',
      (select user_id from u where email='zach@example.com')
    ),
    (
      (select pantry_id from p where name='Kitchen Pantry'),
      (select location_id from loc where name='Fridge Door'),
      (select item_id from items where name='Milk'),
      null,
      1, 'gallon',
      current_date + 7,
      'Opened yesterday',
      (select user_id from u where email='alex@example.com')
    ),
    (
      (select pantry_id from p where name='Garage Shelf'),
      (select location_id from loc where name='Garage Bin 2'),
      (select item_id from items where name='Black Beans'),
      null,
      12, 'can',
      current_date + 365,
      'Backstock',
      (select user_id from u where email='alex@example.com')
    ),
    (
      (select pantry_id from p where name='Kitchen Pantry'),
      (select location_id from loc where name='Cupboard A'),
      null,
      'Soy Sauce',
      1, 'bottle',
      null,
      'Custom item example',
      (select user_id from u where email='zach@example.com')
    )
  returning inventory_id
),
l as (
  insert into lists (group_id, name, list_type, created_by_user)
  values ((select group_id from g), 'Weekly Shopping', 'shopping', (select user_id from u where email='zach@example.com'))
  returning list_id
)
insert into list_items (list_id, item_id, custom_name, quantity, unit, is_checked, added_by_user)
values
  ((select list_id from l), (select item_id from items where name='Milk'), null, 1, 'gallon', false, (select user_id from u where email='zach@example.com')),
  ((select list_id from l), null, 'Pickles', 1, 'jar', false, (select user_id from u where email='alex@example.com'));
