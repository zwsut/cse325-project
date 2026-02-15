-- Backfill app users and households for existing auth users.
-- Safe to run multiple times.

-- 1) Ensure every auth user has a matching public.users row.
insert into public.users (user_id, email, display_name, created_at)
select
    au.id,
    coalesce(au.email, ''),
    coalesce(
        nullif(trim(au.raw_user_meta_data->>'display_name'), ''),
        split_part(coalesce(au.email, ''), '@', 1),
        'User'
    ),
    now()
from auth.users au
left join public.users pu on pu.user_id = au.id
where pu.user_id is null;

-- 2) Repair missing email/display_name fields in existing public.users rows.
update public.users pu
set
    email = coalesce(nullif(pu.email, ''), au.email, pu.email),
    display_name = coalesce(
        nullif(trim(pu.display_name), ''),
        nullif(trim(au.raw_user_meta_data->>'display_name'), ''),
        split_part(coalesce(au.email, pu.email, ''), '@', 1),
        'User'
    )
from auth.users au
where au.id = pu.user_id
  and (
      pu.email is null or pu.email = ''
      or pu.display_name is null or trim(pu.display_name) = ''
  );

-- 3) Create one household (group) for auth users with no group membership.
with users_without_group as (
    select
        au.id as user_id,
        coalesce(
            nullif(trim(pu.display_name), ''),
            nullif(trim(au.raw_user_meta_data->>'display_name'), ''),
            split_part(coalesce(au.email, pu.email, ''), '@', 1),
            'User'
        ) as display_name
    from auth.users au
    left join public.users pu on pu.user_id = au.id
    left join public.group_members gm on gm.user_id = au.id
    where gm.user_id is null
),
created_groups as (
    insert into public.groups (group_id, name, created_by_user, created_at)
    select
        gen_random_uuid(),
        users_without_group.display_name || '''s Household',
        users_without_group.user_id,
        now()
    from users_without_group
    returning group_id, created_by_user
)
insert into public.group_members (group_id, user_id, role, joined_at)
select
    created_groups.group_id,
    created_groups.created_by_user,
    'owner',
    now()
from created_groups
on conflict (group_id, user_id) do nothing;
