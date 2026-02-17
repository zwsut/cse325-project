-- =========================
-- Fix RLS for group_members during signup
-- =========================
-- The original policy only allowed users to add themselves, which doesn't work
-- during signup flow. This adds a policy allowing group owners to add members.

drop policy if exists "gm_insert_self" on "public"."group_members";

-- Policy 1: Users can add themselves to groups
create policy "gm_insert_self"
  on "public"."group_members"
  as permissive
  for insert
  to authenticated
with check ((user_id = auth.uid()));

-- Policy 2: Group owners can add members to their groups
create policy "gm_insert_by_group_owner"
  on "public"."group_members"
  as permissive
  for insert
  to authenticated
with check (
  EXISTS (
    SELECT 1 FROM public.groups g
    WHERE g.group_id = group_members.group_id
    AND g.created_by_user = auth.uid()
  )
);
