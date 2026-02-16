## Database Setup (Supabase)

1. Install Supabase CLI
2. Run `supabase login`
3. Run `supabase link --project-ref ofxwrxjhxkufiilrxzav`
4. Run `supabase db push`

# Kitchen Inventory Tracking System (KITS)

A Blazor Server app for tracking household pantry inventory and maintaining a shared shopping list.  
Authentication and data storage are handled by **Supabase**.

---

## What you can do

- **Create an account / log in** (Supabase Auth)
- **Households (groups)**
  - On sign-up, a household is created automatically ("<Display Name>'s Household")
  - Household data (inventory + shopping list) is shared across members
- **Inventory**
  - Create **pantries** (e.g., *Kitchen*, *Garage*, *Laundry*)
  - Create **containers** inside a pantry (e.g., *Top shelf*, *Drawer 2*)
  - Add items with quantity/amount, description/notes, and optional category
  - Edit or delete items
  - Browse inventory by pantry or container using the sidebar
- **Shopping List**
  - Add items to the shopping list directly from inventory
  - Check items off, update quantity/unit, or remove items
  - A default "Weekly Shopping" list is created automatically for each household
- **Settings**
  - Update profile display name / email
  - Manage household details
  - Manage categories (add/edit/delete)

---

## Tech stack

- **.NET 10** + **Blazor Server** (Interactive Server Components)
- **Supabase** (Auth + Postgres + Row Level Security)
- **Supabase CLI** for migrations (optional but recommended)

---

## Getting started (local)

### Prerequisites

- .NET SDK **10.0** installed
- A Supabase project (or local Supabase via CLI)

### 1) Clone and restore

```bash
git clone <https://github.com/zwsut/cse325-project.git>
cd cse325-project
dotnet restore
