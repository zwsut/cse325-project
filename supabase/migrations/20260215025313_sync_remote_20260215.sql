drop extension if exists "pg_net";

alter table "public"."group_members" enable row level security;

alter table "public"."groups" enable row level security;

alter table "public"."list_items" enable row level security;

alter table "public"."lists" enable row level security;

alter table "public"."users" drop column "password_hash";

alter table "public"."users" enable row level security;

alter table "public"."users" add constraint "users_user_id_fkey" FOREIGN KEY (user_id) REFERENCES auth.users(id) ON DELETE CASCADE not valid;

alter table "public"."users" validate constraint "users_user_id_fkey";

set check_function_bodies = off;

CREATE OR REPLACE FUNCTION public.handle_new_auth_user()
 RETURNS trigger
 LANGUAGE plpgsql
 SECURITY DEFINER
AS $function$
begin
  insert into public.users (user_id, email, display_name)
  values (new.id, new.email, coalesce(new.raw_user_meta_data->>'display_name', new.email))
  on conflict (user_id) do nothing;
  return new;
end;
$function$
;


  create policy "gm_insert_self"
  on "public"."group_members"
  as permissive
  for insert
  to authenticated
with check ((user_id = auth.uid()));



  create policy "gm_select_self"
  on "public"."group_members"
  as permissive
  for select
  to authenticated
using ((user_id = auth.uid()));



  create policy "groups_insert_own"
  on "public"."groups"
  as permissive
  for insert
  to authenticated
with check ((created_by_user = auth.uid()));



  create policy "groups_select_member"
  on "public"."groups"
  as permissive
  for select
  to authenticated
using ((EXISTS ( SELECT 1
   FROM public.group_members gm
  WHERE ((gm.group_id = groups.group_id) AND (gm.user_id = auth.uid())))));



  create policy "groups_select_own"
  on "public"."groups"
  as permissive
  for select
  to authenticated
using ((created_by_user = auth.uid()));



  create policy "groups_update_owner"
  on "public"."groups"
  as permissive
  for update
  to authenticated
using ((created_by_user = auth.uid()))
with check ((created_by_user = auth.uid()));



  create policy "list_items_delete_member"
  on "public"."list_items"
  as permissive
  for delete
  to authenticated
using ((EXISTS ( SELECT 1
   FROM (public.lists l
     JOIN public.group_members gm ON ((gm.group_id = l.group_id)))
  WHERE ((l.list_id = list_items.list_id) AND (gm.user_id = auth.uid())))));



  create policy "list_items_insert_member"
  on "public"."list_items"
  as permissive
  for insert
  to authenticated
with check ((EXISTS ( SELECT 1
   FROM (public.lists l
     JOIN public.group_members gm ON ((gm.group_id = l.group_id)))
  WHERE ((l.list_id = list_items.list_id) AND (gm.user_id = auth.uid())))));



  create policy "list_items_select_member"
  on "public"."list_items"
  as permissive
  for select
  to authenticated
using ((EXISTS ( SELECT 1
   FROM (public.lists l
     JOIN public.group_members gm ON ((gm.group_id = l.group_id)))
  WHERE ((l.list_id = list_items.list_id) AND (gm.user_id = auth.uid())))));



  create policy "list_items_update_member"
  on "public"."list_items"
  as permissive
  for update
  to authenticated
using ((EXISTS ( SELECT 1
   FROM (public.lists l
     JOIN public.group_members gm ON ((gm.group_id = l.group_id)))
  WHERE ((l.list_id = list_items.list_id) AND (gm.user_id = auth.uid())))))
with check ((EXISTS ( SELECT 1
   FROM (public.lists l
     JOIN public.group_members gm ON ((gm.group_id = l.group_id)))
  WHERE ((l.list_id = list_items.list_id) AND (gm.user_id = auth.uid())))));



  create policy "lists_insert_member"
  on "public"."lists"
  as permissive
  for insert
  to authenticated
with check ((EXISTS ( SELECT 1
   FROM public.group_members gm
  WHERE ((gm.group_id = lists.group_id) AND (gm.user_id = auth.uid())))));



  create policy "lists_select_member"
  on "public"."lists"
  as permissive
  for select
  to authenticated
using ((EXISTS ( SELECT 1
   FROM public.group_members gm
  WHERE ((gm.group_id = lists.group_id) AND (gm.user_id = auth.uid())))));



  create policy "lists_update_member"
  on "public"."lists"
  as permissive
  for update
  to authenticated
using ((EXISTS ( SELECT 1
   FROM public.group_members gm
  WHERE ((gm.group_id = lists.group_id) AND (gm.user_id = auth.uid())))))
with check ((EXISTS ( SELECT 1
   FROM public.group_members gm
  WHERE ((gm.group_id = lists.group_id) AND (gm.user_id = auth.uid())))));



  create policy "Users can insert own profile"
  on "public"."users"
  as permissive
  for insert
  to authenticated
with check ((user_id = auth.uid()));



  create policy "Users can read own profile"
  on "public"."users"
  as permissive
  for select
  to authenticated
using ((user_id = auth.uid()));



  create policy "Users can update own profile"
  on "public"."users"
  as permissive
  for update
  to authenticated
using ((user_id = auth.uid()))
with check ((user_id = auth.uid()));



  create policy "users_insert_own"
  on "public"."users"
  as permissive
  for insert
  to authenticated
with check ((user_id = auth.uid()));



  create policy "users_select_own"
  on "public"."users"
  as permissive
  for select
  to authenticated
using ((user_id = auth.uid()));



  create policy "users_update_own"
  on "public"."users"
  as permissive
  for update
  to authenticated
using ((user_id = auth.uid()))
with check ((user_id = auth.uid()));


CREATE TRIGGER on_auth_user_created AFTER INSERT ON auth.users FOR EACH ROW EXECUTE FUNCTION public.handle_new_auth_user();


