-- =========================
-- Alternative RLS fix: Create a function for signup
-- =========================
-- This function handles group and group_member creation with proper elevated privileges

create or replace function public.ensure_signup_household(
  p_user_id uuid,
  p_display_name text
)
returns void
language plpgsql
security definer
set search_path = public
as $$
declare
  v_group_id uuid;
begin
  -- Create the group
  insert into public.groups (group_id, name, created_by_user, created_at)
  values (
    gen_random_uuid(),
    p_display_name || '''s Household',
    p_user_id,
    now()
  )
  returning group_id into v_group_id;

  -- Add user as group member (owner)
  insert into public.group_members (group_id, user_id, role, joined_at)
  values (v_group_id, p_user_id, 'owner', now());
end;
$$;

-- Grant execute permission to authenticated users
grant execute on function public.ensure_signup_household(uuid, text) to authenticated;
