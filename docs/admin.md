# Admin app

## What changed

Add a Units screen, and change the cement product create and edit forms.

Units, new screen at `/api/admin/units`:

- List every unit, including inactive ones.
- Create and edit with `name` and `isActive`.
- Delete only when no product uses that unit. If products use it, set `isActive` to false instead.
- Bag and Ton are already seeded.

Cement product create `POST /api/admin/cement/products` and edit `PUT /api/admin/cement/products/{id}` now send only:

- `imageUrl` — optional URL, show a preview
- `name`
- `unitId` — dropdown of active units from `GET /api/admin/units`
- `price`
- `isActive`

Remove `grade` from the form. Do not send `unit` as the text Bag or Ton. Do not send `stockQuantity` on create or edit. A new product starts at stock 0.

Stock stays a separate action: `PATCH /api/admin/cement/products/{id}/stock`.

The product list still shows image, name, `unitName`, price, stock, and active status. Inactive products stay in this list and are hidden on the client site.

Orders, equipment, rentals, projects, and workers are unchanged.

Internal app for the company. Staff manage cement stock and orders, equipment and rentals, construction projects, and the workers sent to those sites.

Customers use the client web app. This app uses the same API with an `Admin` token. Every route below is under `/api/admin` and returns 401 or 403 for a client token.

## API

Base URL in development: `http://localhost:5205`

CORS already allows `http://localhost:3000` and `http://localhost:5173`. Run this app on a different port from the client web app, and add that origin to `AllowedOrigins` in `Construction.API/appsettings.json` if it is not already listed.

Every response has this shape:

```json
{
  "success": true,
  "message": null,
  "data": {}
}
```

Send `Authorization: Bearer <token>` on every call after login. JSON uses camelCase.

Seed admin for local testing:

- Email: `admin@construction.local`
- Password: `Admin@12345`

`POST /api/auth/login` is the same endpoint as the client app. After login, allow the session only when `role` is `Admin`. If the role is `Client`, clear the token and show an error.

`GET /api/auth/me` returns the signed-in profile.

## Pages

1. Dashboard
2. Units
3. Cement products
4. Cement orders
5. Equipment
6. Rentals
7. Projects list and project detail
8. Workers list and worker detail

## Dashboard

`GET /api/admin/stats`

```json
{
  "openCementOrders": 1,
  "activeProjects": 1,
  "workersOnSite": 1,
  "equipmentOutOnRent": 0,
  "lowStockProducts": [
    { "id": "...", "name": "White Cement", "stockQuantity": 8 }
  ]
}
```

Open orders are everything except `Delivered` and `Cancelled`. Active projects are `InProgress`. Workers on site are assignments that include today. Low stock is an active product with quantity of 10 or less.

Show the four counts as cards and the low-stock list under them.

## Units

Unit options change over time, so they are their own screen. Bag and Ton are not fixed in the product form. Admin adds, renames, and turns units on or off here. The cement product form loads this list into a dropdown.

`GET /api/admin/units` lists every unit, including inactive ones.

```json
{
  "id": "...",
  "name": "Bag",
  "isActive": true
}
```

`POST /api/admin/units`

```json
{
  "name": "Ton",
  "isActive": true
}
```

`PUT /api/admin/units/{id}` updates `name` and `isActive`.

`DELETE /api/admin/units/{id}` removes a unit only when no cement product uses it. If products still use it, turn `isActive` off instead of deleting. Inactive units stay on old products, but they do not appear in the product form dropdown.

Suggested screen: a table of name and status, plus Add, Edit, and Delete.

## Cement products

Create and edit use the same fields: image, name, unit, price, and active or inactive. Grade is not on the form. Stock is not on the form. Stock stays on a separate action so confirming an order can still reduce quantity.

`GET /api/admin/cement/products` includes inactive products. Each row shows image, name, unit name, price, stock, and active status.

`POST /api/admin/cement/products`

```json
{
  "imageUrl": "https://example.com/ppc.jpg",
  "name": "PPC Cement",
  "unitId": "...",
  "price": 18500,
  "isActive": true
}
```

`PUT /api/admin/cement/products/{id}` sends the same fields: `imageUrl`, `name`, `unitId`, `price`, and `isActive`.

Product form:

- Image: text field for `imageUrl`. Show a preview when the URL is present. Empty is allowed.
- Name: required.
- Unit: dropdown filled from `GET /api/admin/units`, active units only. Store `unitId`, display `name`.
- Price: required, greater than 0.
- Active: switch for `isActive`. Inactive products stay in this list and disappear from the client catalog.

`PATCH /api/admin/cement/products/{id}/stock` sets or adjusts stock. Send one of these, not both:

```json
{ "stockQuantity": 100 }
```

```json
{ "adjustment": -5 }
```

Stock cannot go below 0.

## Cement orders

`GET /api/admin/cement/orders` lists every customer order. Optional query: `?status=Pending`.

`GET /api/admin/cement/orders/{id}` includes the customer name, line items, and linked project.

`PATCH /api/admin/cement/orders/{id}/status`

```json
{ "status": "Confirmed" }
```

Allowed moves:

- `Pending` to `Confirmed` or `Cancelled`
- `Confirmed` to `OutForDelivery` or `Cancelled`
- `OutForDelivery` to `Delivered` or `Cancelled`

`Confirmed` reduces stock. `Cancelled` after that puts the stock back. Show the API error if stock is not enough.

`PATCH /api/admin/cement/orders/{id}/payment`

```json
{ "paymentStatus": "Paid" }
```

`paymentStatus` is `Unpaid` or `Paid`.

`PATCH /api/admin/cement/orders/{id}/project`

```json
{ "projectId": "..." }
```

Send `{ "projectId": null }` to clear the link. Use this when a delivery is going to one of the company's sites.

## Equipment

`GET /api/admin/equipment` lists every machine, including maintenance.

`POST /api/admin/equipment`

```json
{
  "name": "Concrete Mixer",
  "code": "CM-01",
  "dailyRate": 3500
}
```

New equipment starts as `Available`. `code` is unique.

`PUT /api/admin/equipment/{id}` updates name, code, and daily rate.

`PATCH /api/admin/equipment/{id}/status`

```json
{ "status": "Maintenance" }
```

Admin can set `Available` or `Maintenance`. `Rented` is set only when a rental is approved. A rented machine must be returned before its status can change.

## Rentals

`GET /api/admin/rentals` lists every request. Optional query: `?status=Requested`.

`GET /api/admin/rentals/{id}` includes the customer and the machine.

`PATCH /api/admin/rentals/{id}/approve` with an empty body `{}`.

This moves `Requested` to `Active` and marks the machine `Rented`. It fails if the machine is not `Available`.

`PATCH /api/admin/rentals/{id}/return`

```json
{ "actualReturn": "2026-10-12" }
```

`actualReturn` can be omitted. The API then uses today's date. Total is daily rate times the number of days, with a minimum of 1 day. Status becomes `Returned` and the machine becomes `Available`.

`PATCH /api/admin/rentals/{id}/cancel` with `{}`. A returned rental cannot be cancelled. Cancelling an active rental frees the machine.

`PATCH /api/admin/rentals/{id}/payment`

```json
{ "paymentStatus": "Paid" }
```

`PATCH /api/admin/rentals/{id}/project`

```json
{ "projectId": "..." }
```

Send `null` to clear the project. Use this when the machine is going to a company site.

## Projects

`GET /api/admin/projects` lists every project, including hidden ones. Optional query: `?status=InProgress`.

Statuses:

- `Planned` shows as Upcoming on the client site
- `InProgress` shows as Ongoing on the client site
- `OnHold`
- `Completed`

`POST /api/admin/projects`

```json
{
  "name": "Colombo Housing Block A",
  "summary": "Upcoming residential block on Baseline Road.",
  "siteAddress": "Baseline Road, Colombo",
  "description": "Foundation and structural work for a four-storey housing block.",
  "startDate": "2026-11-01",
  "endDate": "2027-04-30",
  "isPublic": true
}
```

A new project starts as `Planned`. `isPublic` controls whether the client site can see it.

`PUT /api/admin/projects/{id}` updates the same fields. It does not change status.

`PATCH /api/admin/projects/{id}/status`

```json
{ "status": "InProgress" }
```

`GET /api/admin/projects/{id}` is the project workspace. It returns:

- `project`
- `assignments` with worker name, trade, dates, and daily rate
- `cementOrders` linked to the site
- `rentals` linked to the site

The detail page should show those three lists under the project header.

## Workers

`GET /api/admin/workers` lists the crew. Optional query: `?active=true`.

`POST /api/admin/workers`

```json
{
  "name": "Ravi Perera",
  "trade": "Mason",
  "phone": "0771000001",
  "dailyRate": 4500
}
```

New workers are active.

`GET /api/admin/workers/{id}` returns the worker plus current and past assignments, including the project name.

`PUT /api/admin/workers/{id}`

```json
{
  "trade": "Mason",
  "phone": "0771000001",
  "dailyRate": 4800,
  "isActive": true
}
```

The name is not changed by this call.

## Assignments

Send a worker to a project from the project detail page.

`GET /api/admin/projects/{id}/assignments`

`POST /api/admin/projects/{id}/assignments`

```json
{
  "workerId": "...",
  "startDate": "2026-11-01",
  "endDate": "2026-11-20"
}
```

The worker must be active. The API rejects dates that overlap another assignment for that worker. The daily rate is copied from the worker at assignment time.

`PATCH /api/admin/assignments/{id}/end`

```json
{ "endDate": "2026-11-05" }
```

Omit `endDate` to end the assignment today. The end date cannot be before the start date.

## Suggested build order

1. Admin login and role check
2. Dashboard stats
3. Units, then cement products using that unit dropdown
4. Cement stock, orders, status, payment, and project link
5. Equipment and rentals, including approve and return
6. Projects, public flag, and status
7. Workers and assignment to a project
