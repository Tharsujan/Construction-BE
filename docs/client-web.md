# Client web app

## What changed

Cement products no longer use `grade` or a fixed unit of `Bag` or `Ton`. Update the catalog and product detail to the new response.

- Show `imageUrl`. Use a placeholder when it is empty.
- Show `name`, `unitName`, and `price`. Example: `Rs 1,250 / Bag`.
- Read the unit from `unitName`. Do not hard-code Bag or Ton.
- Keep using `stockQuantity` only to hide Add to cart when it is 0.
- Inactive products are not returned. A direct link to one returns 404.
- Do not add a Units page. Customers cannot create or edit units.

Orders, rentals, and projects are unchanged.

Public site for customers. They browse cement, place orders, browse equipment, request rentals, and view the company's upcoming and ongoing projects.

The admin app manages workers, stock, and project assignment. This app does not show worker names, wages, order payments of other customers, or rental money of other customers.

## API

Base URL in development: `http://localhost:5205`

CORS already allows `http://localhost:3000` and `http://localhost:5173`.

Every response has this shape:

```json
{
  "success": true,
  "message": null,
  "data": {}
}
```

Send `Authorization: Bearer <token>` on signed-in calls. Store the token from login or register. JSON uses camelCase. Dates are `YYYY-MM-DD`. Date-times are UTC ISO strings.

Seed client for local testing:

- Email: `client@construction.local`
- Password: `Client@12345`

## Pages

1. Home
2. Cement catalog and product detail
3. Cart and checkout
4. My orders and order detail
5. Equipment catalog and equipment detail
6. Rental request
7. My rentals and rental detail
8. Projects: upcoming, ongoing, and project detail
9. Register, login, and account

## Auth

`POST /api/auth/register`

```json
{
  "name": "Amal Perera",
  "email": "amal@example.com",
  "phone": "0771234567",
  "password": "secret12"
}
```

`POST /api/auth/login`

```json
{
  "email": "amal@example.com",
  "password": "secret12"
}
```

Both return:

```json
{
  "token": "...",
  "userId": "...",
  "name": "Amal Perera",
  "email": "amal@example.com",
  "role": "Client"
}
```

`GET /api/auth/me` returns `{ id, name, email, phone, role }`. Call it on app load when a token exists. If it returns 401, clear the token and send the user to login.

Register and login are public. Catalog and project pages are public. Orders, rentals, and account require a token.

## Cement

The catalog shows the product image, name, unit name, and price. Grade is not a field. The unit label comes from the Units list that admin manages, so the site must display `unitName` from the product. Do not hard-code Bag or Ton.

`GET /api/cement/products` lists active products only.

`GET /api/cement/products/{id}` returns one active product. An inactive product returns 404.

```json
{
  "id": "...",
  "imageUrl": "https://example.com/opc-43.jpg",
  "name": "OPC 43 Grade",
  "unitId": "...",
  "unitName": "Bag",
  "price": 1250,
  "stockQuantity": 500,
  "isActive": true
}
```

Catalog card:

- Show `imageUrl`. If it is empty, show a placeholder.
- Show name, `unitName`, and price. Example: `Rs 1,250 / Bag`.
- Hide Add to cart when `stockQuantity` is 0.

The customer does not create products and does not see the Units management page. If admin renames a unit, the next product load shows the new name.

`POST /api/cement/orders`

```json
{
  "deliveryAddress": "12 Galle Road, Colombo",
  "items": [
    { "productId": "...", "quantity": 2 }
  ]
}
```

The order starts as `Pending` and `Unpaid`. `total` is calculated by the API. The client cannot set the price.

`GET /api/cement/orders` returns only this customer's orders. Optional query: `?status=Pending`.

`GET /api/cement/orders/{id}` returns the order and line items. Another customer's order returns 404.

Order statuses: `Pending`, `Confirmed`, `OutForDelivery`, `Delivered`, `Cancelled`.

Show status as a label. The customer does not change it.

## Equipment rental

`GET /api/equipment` lists machines that are not in maintenance.

`GET /api/equipment/{id}` returns one machine.

```json
{
  "id": "...",
  "name": "Concrete Mixer",
  "code": "CM-01",
  "dailyRate": 3500,
  "status": "Available"
}
```

`status` is `Available` or `Rented`. Allow a rental request only when status is `Available`.

`POST /api/rentals`

```json
{
  "equipmentId": "...",
  "startDate": "2026-10-10",
  "expectedReturn": "2026-10-12",
  "siteAddress": "Kandy Road, Kadawatha",
  "deposit": 1000
}
```

`expectedReturn` must be on or after `startDate`. `deposit` can be `0`. The rental starts as `Requested`. `total` stays `0` until admin marks it returned.

`GET /api/rentals` returns only this customer's rentals. Optional query: `?status=Requested`.

`GET /api/rentals/{id}` returns one rental.

Rental statuses: `Requested`, `Active`, `Returned`, `Cancelled`.

## Projects

These pages are public. Do not send a token requirement for them.

`GET /api/projects?status=upcoming` returns public projects with status `Planned`.

`GET /api/projects?status=ongoing` returns public projects with status `InProgress`.

`GET /api/projects/{id}` returns:

```json
{
  "id": "...",
  "name": "Kandy Road Bridge Repair",
  "summary": "Ongoing bridge repair on Kandy Road.",
  "description": "Deck repair and approach road work while traffic is managed.",
  "location": "Kandy Road, Kadawatha",
  "status": "InProgress",
  "startDate": "2026-09-01",
  "endDate": "2026-12-15"
}
```

A project with `isPublic` false returns 404. Do not add a worker list, wage, cement order, or rental section on this page.

Suggested layout:

- Projects page with two sections: Upcoming and Ongoing
- A card shows name, summary, location, and dates
- Detail page shows description, location, status, and dates

## Suggested build order

1. API client, token storage, login, and register
2. Cement catalog, cart, and checkout
3. My orders
4. Equipment list and rental request
5. My rentals
6. Upcoming projects, ongoing projects, and project detail
