# Backend

The backend follows Clean Architecture:

```text
API -> Application -> Domain
API -> Infrastructure -> Application -> Domain
Infrastructure -> Domain
```

`API` hosts HTTP endpoints, `Application` contains feature DTOs, use cases, and ports, `Domain` contains entities and domain exceptions, and `Infrastructure` implements repositories, PostgreSQL persistence, password hashing, and JWT creation.

## Requirements and local configuration

- .NET 8 SDK.
- PostgreSQL.
- EF CLI 8.0.15 (`dotnet tool install --global dotnet-ef --version 8.0.15`).

From the repository root, copy `.env.example` to `.env` and set your local PostgreSQL password and a private JWT signing key:

```powershell
if (-not (Test-Path .env)) { Copy-Item .env.example .env }
```

The API loads `.env` from the repository root (or a parent of its working directory) before reading configuration. The keys use .NET environment-variable naming, so `ConnectionStrings__DefaultConnection` supplies `ConnectionStrings:DefaultConnection` and `Jwt__SigningKey` supplies `Jwt:SigningKey`. User Secrets can be used as an alternative; process environment variables (including values loaded from `.env`) take precedence. Keep `.env` private; only `.env.example` belongs in source control.

## Database

The initial migration creates the identity tables and seeds the `User` and `Admin` roles. `IdentityTokenLifecycle` adds refresh/reset token storage and `TokenVersion`; `ContactsModule` adds customers, passengers, and the initial reservation/trip tables. `DestinationsModule` adds destinations and images; `TourismProductsModule` evolves products and adds product-destination assignments and images. `ItinerariesModule` adds itineraries, days, and activities related to products and destinations. `DeparturesModule` adds departures, prices, and the optional departure link on trips. `ReservationsModule` expands reservations, adds reservation passengers, cancellations, and the read-side payment model, and links trips to reservation passengers. It refuses to overwrite legacy reservation rows that lack the new required departure and booking data. The departures migration also refuses to delete non-empty legacy `ProductDepartures`. Reconcile either legacy table before applying pending migrations:

Before applying migrations, verify that `ConnectionStrings:DefaultConnection` points to the intended development database. The command below uses the Development configuration and applies all pending migrations; it can change or create database objects:

```powershell
$previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
$env:ASPNETCORE_ENVIRONMENT = "Development"
try {
    dotnet ef database update --project .\Backend\ARTravel.Infrastructure\ARTravel.Infrastructure.csproj --startup-project .\Backend\ARTravel.API\ARTravel.API.csproj
    if ($LASTEXITCODE -ne 0) { throw "Migration failed with exit code $LASTEXITCODE." }
}
finally {
    if ($null -eq $previousEnvironment) {
        Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue
    }
    else {
        $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
    }
}
```

### Run from Visual Studio 2022

Open `Backend\ARTravel.sln`, select the **ARTravel.API** project as the startup project, choose the **ARTravel.API** launch profile (not IIS Express), and press **F5** or **Ctrl+F5**. The shared `launchSettings.json` profile sets `ASPNETCORE_ENVIRONMENT=Development` and opens Swagger at `http://localhost:52107/swagger`.

Use Visual Studio 2022 17.8 or later with the .NET 8 SDK installed. If the launch profile does not appear, close and reopen the solution so Visual Studio reloads `ARTravel.API\Properties\launchSettings.json`.

### Run from VS Code or a terminal

From the repository root, run:

```powershell
dotnet run --project .\Backend\ARTravel.API\ARTravel.API.csproj --launch-profile ARTravel.API
```

The same profile opens Swagger at `http://localhost:52107/swagger`. You can also use **Run and Debug** in VS Code and select the `ARTravel.API` launch profile.

`GET /health` checks the API process; `GET /health/database` checks PostgreSQL connectivity.

## Identity API

| Method | Route | Access |
|---|---|---|
| `POST` | `/api/auth/register` | Public |
| `POST` | `/api/auth/login` | Public |
| `POST` | `/api/auth/refresh-token` | Public (refresh token required) |
| `POST` | `/api/auth/forgot-password` | Public |
| `POST` | `/api/auth/reset-password` | Public (one-time reset token required) |
| `POST` | `/api/auth/logout` | Authenticated |
| `PUT` | `/api/auth/change-password` | Authenticated |
| `GET` | `/api/auth/me` | Authenticated |
| `GET` | `/api/users?page=1&pageSize=20` | Admin |
| `GET` | `/api/users/{userId}` | Admin |
| `POST` | `/api/users` | Admin |
| `PUT` | `/api/users/{userId}` | Admin |
| `PATCH` | `/api/users/{userId}/status` | Admin |
| `GET` | `/api/users/{userId}/roles` | Admin |
| `POST` | `/api/users/{userId}/roles/{roleId}` | Admin |
| `DELETE` | `/api/users/{userId}/roles/{roleId}` | Admin |
| `GET` | `/api/roles?page=1&pageSize=20` | Admin; paginated |
| `GET` | `/api/roles/{roleId}` | Admin |
| `POST` | `/api/roles` | Admin |
| `PUT` | `/api/roles/{roleId}` | Admin |
| `PATCH` | `/api/roles/{roleId}/status` | Admin |
| `DELETE` | `/api/roles/{roleId}` | Admin |

## Customers and passengers API

These endpoints require the `Administrator` policy. Customer, passenger, passenger-trip, and customer-reservation collection endpoints accept `page` (default `1`) and `pageSize` (default `20`, maximum `100`); customer and passenger collection endpoints also accept `search` and `isActive` filters. Responses include `Items`, `Page`, `PageSize`, and `TotalCount`. Invalid pagination or customer type returns `400`; missing resources return `404` in ProblemDetails format. Request validation errors also use ProblemDetails.

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/customers` | Search/filter customers with pagination |
| `GET` | `/api/customers/{id}` | Get customer |
| `POST` | `/api/customers` | Create customer |
| `PUT` | `/api/customers/{id}` | Update customer details |
| `PATCH` | `/api/customers/{id}/status` | Change `isActive` |
| `GET` | `/api/customers/{id}/reservations` | List a customer's reservations with pagination |
| `GET` | `/api/customers/{id}/summary` | Customer details, reservation count, and total reservation amount |
| `GET` | `/api/passengers` | Search/filter passengers with pagination |
| `GET` | `/api/passengers/{id}` | Get passenger |
| `POST` | `/api/passengers` | Create passenger |
| `PUT` | `/api/passengers/{id}` | Update passenger details |
| `PATCH` | `/api/passengers/{id}/status` | Change `isActive` |
| `GET` | `/api/passengers/{id}/trips?page=1&pageSize=20` | List a passenger's trips with pagination |

`CustomerType` uses `1` for `Individual` and `2` for `Organization`. An individual requires `FirstName` and `LastName`; an organization requires `BusinessName`. `Trip` history is read-only in the current API; the reservations/travel module supplies its reservation passenger snapshots and departure manifest endpoints.

## Destinations API

These endpoints require the `Administrator` policy. List, children, and product endpoints are paginated (`page=1`, `pageSize=20`, maximum `100`). Destination listing also supports `search`, numeric `destinationType` (`1` Region through `7` Other), `country`, and `isActive` filters. Child destinations are direct children only. Destination details include their ordered images. Invalid input returns `400` and missing destinations or images return `404` in ProblemDetails format.

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/destinations` | Search/filter destinations with pagination |
| `GET` | `/api/destinations/{id}` | Get destination and its images |
| `POST` | `/api/destinations` | Create destination (optional `parentDestinationId`) |
| `PUT` | `/api/destinations/{id}` | Update destination and parent |
| `PATCH` | `/api/destinations/{id}/status` | Change `isActive` |
| `GET` | `/api/destinations/{id}/children` | List direct child destinations |
| `GET` | `/api/destinations/{id}/products` | List destination products |
| `GET` | `/api/destinations/{id}/summary` | Destination details, child/product/image counts |
| `POST` | `/api/destinations/{id}/images` | Add an image; the first image is primary by default |
| `DELETE` | `/api/destinations/{id}/images/{imageId}` | Remove an image |
| `PATCH` | `/api/destinations/{id}/images/{imageId}/primary` | Set the primary image |

Coordinates must be supplied together and must fall within geographic ranges; altitude cannot be negative. Parent updates are checked to prevent self-parenting and hierarchy cycles. Setting a primary image clears the previous primary; removing the primary selects the next image by display order. Tourism products are associated with destinations through the product-destination assignment endpoint.

## Tourism products API

These endpoints require the `Administrator` policy. Product and departure collections accept `page` (default `1`) and `pageSize` (default `20`, maximum `100`). Product listing also accepts `search`, numeric `productType` (`1` Excursion or `2` Package), `isPublished`, and `isActive`. Product details include assigned destinations and ordered images. `POST /api/products/{id}/destinations` replaces the complete destination assignment list; destination IDs and visit orders must be unique, and at most one destination may be marked main. Duplicate product codes return `409`; invalid input returns `400`; missing products, images, or destination assignments return `404` in ProblemDetails format.

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/products` | Search/filter products with pagination |
| `GET` | `/api/products/{id}` | Get product, destination assignments, and images |
| `POST` | `/api/products` | Create product |
| `PUT` | `/api/products/{id}` | Update product details |
| `PATCH` | `/api/products/{id}/status` | Change `isActive` |
| `PATCH` | `/api/products/{id}/publication` | Change `isPublished` |
| `GET` | `/api/products/{id}/destinations` | List assigned destinations |
| `POST` | `/api/products/{id}/destinations` | Replace assigned destinations |
| `DELETE` | `/api/products/{id}/destinations/{destinationId}` | Remove an assignment |
| `POST` | `/api/products/{id}/images` | Add an image; the first image is primary by default |
| `DELETE` | `/api/products/{id}/images/{imageId}` | Remove an image |
| `PATCH` | `/api/products/{id}/images/{imageId}/primary` | Set the primary image |
| `GET` | `/api/products/{id}/departures` | List product departures (paginated) |
| `POST` | `/api/products/{id}/departures` | Create a departure for the product |

`ProductType` uses `1` for `Excursion` and `2` for `Package`. Product codes are normalized to uppercase and unique; currency uses a three-letter code. See the Departures API below for schedule, availability, passenger, and price management.

## Itineraries API

These endpoints require the `Administrator` policy. `GET /api/itineraries` is paginated (`page=1`, `pageSize=20`, maximum `100`) and accepts `productId`, `isActive`, and `search` filters. Itinerary details include days and their activities, ordered by `DayNumber` and `DisplayOrder`. New itineraries start at version `1`; updates to the itinerary or its days/activities increment the version. Duplicating creates an active copy with the next version number and cloned days/activities. Concurrent edits return `409`; invalid input returns `400`; missing itineraries, days, activities, products, or destinations return `404` in ProblemDetails format.

The reorder endpoints accept the complete ordered list of existing IDs (`{"dayIds":[12,10,11]}` or `{"activityIds":[8,6,7]}`); omitted, duplicate, or foreign IDs are rejected. Day numbers are one-based; activity display order is zero-based.

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/itineraries` | Search/filter itineraries with pagination |
| `GET` | `/api/itineraries/{id}` | Get itinerary with days and activities |
| `POST` | `/api/itineraries` | Create itinerary (version `1`) |
| `PUT` | `/api/itineraries/{id}` | Update itinerary and increment version |
| `PATCH` | `/api/itineraries/{id}/status` | Change `isActive` |
| `POST` | `/api/itineraries/{id}/duplicate` | Clone itinerary as active version `source + 1` |
| `GET` | `/api/itineraries/{id}/days` | List days and activities |
| `POST` | `/api/itineraries/{id}/days` | Append a day |
| `PUT` | `/api/itineraries/{id}/days/{dayId}` | Update day title/description |
| `DELETE` | `/api/itineraries/{id}/days/{dayId}` | Remove a day and renumber remaining days |
| `PATCH` | `/api/itineraries/{id}/days/reorder` | Reorder all days |
| `GET` | `/api/itinerary-days/{dayId}/activities` | List day activities |
| `POST` | `/api/itinerary-days/{dayId}/activities` | Add activity |
| `PUT` | `/api/itinerary-days/{dayId}/activities/{activityId}` | Update activity |
| `DELETE` | `/api/itinerary-days/{dayId}/activities/{activityId}` | Remove activity and compact order |
| `PATCH` | `/api/itinerary-days/{dayId}/activities/reorder` | Reorder all activities |

`ItineraryActivityType` uses values `1`–`8` for `Visit`, `Excursion`, `Transfer`, `Meal`, `FreeTime`, `Accommodation`, `Recreational`, and `Other`. Start and end times must be provided together, and end time must be later than start time.

## Departures API

These endpoints require the `Administrator` policy. Departure and passenger lists are paginated (`page=1`, `pageSize=20`, maximum `100`). Departure listing can filter by `productId` and numeric `status` (`1` Scheduled through `7` Suspended). A departure must reference an itinerary belonging to its product; dates must be UTC and `ReturnDate` must be later than `DepartureDate`. Capacity and minimum passengers must be positive, with the minimum no greater than capacity. Prices require non-negative amounts and a three-letter currency code. Invalid input returns `400`, missing departures/prices/products/itineraries return `404`, and reducing capacity below assigned passengers returns `409` in ProblemDetails format.

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/departures` | Search departures with pagination |
| `GET` | `/api/departures/{id}` | Get departure |
| `POST` | `/api/departures` | Create departure |
| `PUT` | `/api/departures/{id}` | Update departure details |
| `PATCH` | `/api/departures/{id}/status` | Change status |
| `POST` | `/api/departures/{id}/cancel` | Set status to `Cancelled` |
| `GET` | `/api/products/{productId}/departures` | List departures for a product |
| `POST` | `/api/products/{productId}/departures` | Create departure for a product |
| `GET` | `/api/departures/{id}/availability` | Get capacity, assigned passengers, and available seats |
| `GET` | `/api/departures/{id}/passengers` | List non-cancelled passenger trips with pagination |
| `GET` | `/api/departures/{id}/prices` | List departure prices |
| `POST` | `/api/departures/{id}/prices` | Add a price |
| `PUT` | `/api/departures/{id}/prices/{priceId}` | Update a price |
| `DELETE` | `/api/departures/{id}/prices/{priceId}` | Remove a price |

`DepartureStatus` values are `1` Scheduled, `2` Open, `3` Full, `4` InProgress, `5` Completed, `6` Cancelled, and `7` Suspended. `DeparturePriceType` values are `1` Standard, `2` Child, `3` Adult, and `4` Promotional. Passenger counts and availability use passengers in pending, confirmed, partially paid, or paid reservations for that departure. Legacy trips without a departure remain unchanged. The departure passenger endpoint returns reservation passenger snapshots; `Trip` remains a separate travel record associated through reservation, passenger, and departure keys.

## Reservations API

These endpoints require the `Administrator` policy. Reservation, customer-reservation, departure-reservation, and passenger collections are paginated (`page=1`, `pageSize=20`, maximum `100`). `GET /api/reservations` accepts `search` (reservation number), `customerId`, `departureId`, `status`, `reservationDateFrom`, and `reservationDateTo`. New reservations require an active customer and an `Open` departure, start as `Pending`, and receive a generated unique reservation number and the departure currency. Passenger count and prices are derived from the passenger snapshots. The standard passenger price uses a configured `Standard` `DeparturePrice`, or falls back to `Departure.BasePrice`; other selected price types require a matching departure price in the departure currency. Unit-price and currency values are snapshotted for the reservation.

Only pending reservations may be edited or have passengers added, changed, or removed. Passenger documents must be unique within a reservation. Adding a passenger locks the departure row while checking capacity; reservations in pending, confirmed, partially paid, or paid status consume seats. Confirming requires the departure to remain open and its aggregate passenger count to meet `Departure.MinimumPassengers`. `Discount` is a flat amount in the reservation currency and cannot exceed `Subtotal`. Only the following status transitions are allowed through `/status`: Pending to Confirmed or Expired; Confirmed to PartiallyPaid or Paid; PartiallyPaid to Paid; Paid to Completed. Cancellation is handled by `/cancel`, which requires a reason, records the acting user, and does not allow refunds above recorded paid payments. Invalid input returns `400`, missing entities return `404`, and disallowed transitions/capacity conflicts return `409` in ProblemDetails format.

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/reservations` | Filter reservations with pagination |
| `GET` | `/api/reservations/{id}` | Get reservation with passenger snapshots and cancellation |
| `POST` | `/api/reservations` | Create a pending reservation |
| `PUT` | `/api/reservations/{id}` | Update notes and discount while pending |
| `PATCH` | `/api/reservations/{id}/confirm` | Confirm when departure minimum is met |
| `PATCH` | `/api/reservations/{id}/cancel` | Cancel with reason and optional refund amount |
| `PATCH` | `/api/reservations/{id}/status` | Apply an allowed status transition |
| `GET` | `/api/reservations/{id}/payments` | List payment records |
| `GET` | `/api/customers/{customerId}/reservations` | List customer reservations with pagination |
| `GET` | `/api/departures/{departureId}/reservations` | List departure reservations with pagination |
| `GET` | `/api/reservations/{reservationId}/passengers` | List passenger snapshots with pagination |
| `GET` | `/api/reservations/{reservationId}/passengers/{passengerId}` | Get a passenger snapshot |
| `POST` | `/api/reservations/{reservationId}/passengers` | Add a passenger and reserve capacity |
| `PUT` | `/api/reservations/{reservationId}/passengers/{passengerId}` | Update a passenger and price snapshot |
| `DELETE` | `/api/reservations/{reservationId}/passengers/{passengerId}` | Remove a passenger while pending |
| `GET` | `/api/departures/{departureId}/passengers` | List passengers from active reservations |

`ReservationDocumentType` uses `1` for `DNI`, `2` for `Passport`, and `3` for `Other`. Gender is optional free text. `ReservationPassenger` is a per-booking snapshot; its optional `PassengerId` links to the reusable passenger profile when one is known. `Trip` remains a separate travel/fulfillment record: `ReservationId` is required while `PassengerId`, `DepartureId`, and `ReservationPassengerId` are optional for legacy or snapshot-only trips. Composite foreign keys ensure a linked trip's departure matches its reservation and its reservation passenger belongs to that same reservation. A trip linked to a reservation passenger cannot outlive or be moved to another reservation. Reservation passenger snapshots, not `Trip`, are the source for manifests and availability.

`GET /api/reservations/{id}/payments` reads `ReservationPayment` records. Payment data has a minimal read model (`Amount`, `Currency`, `PaymentMethod`, status, transaction reference, timestamps, and notes); payment creation/processing endpoints are not part of this module.

Registration requires a password with at least 12 characters, including uppercase, lowercase, a number, and a symbol. Passwords are stored as salted PBKDF2-SHA256 hashes. Access tokens are signed JWTs and refresh tokens are opaque, stored hashed, single-use, and rotated. Logout, password changes, password resets, and account status changes invalidate sessions. Authorization reloads current active roles for each request. Built-in `User` and `Admin` roles cannot be renamed, deactivated, or deleted; a role assigned to a user cannot be deleted.

Password-reset token storage and the endpoint are implemented, but no email provider has been selected/configured. Until an `IPasswordResetNotifier` is configured, `POST /api/auth/forgot-password` returns `503` without looking up the submitted email or exposing a reset token. The reset email must deliver the one-time token to the account owner; tokens are stored only as hashes.

### First administrator

Public registration only assigns the standard `User` role. After registering the account that should administer the system, promote that account once using PostgreSQL (replace `1` with its user ID):

```sql
INSERT INTO "UserRoles" ("UserId", "RoleId")
VALUES (1, 2)
ON CONFLICT DO NOTHING;
```

The seed assigns role ID `2` to `Admin`. Administrator role removal/deactivation is blocked when it would leave no active administrators.

## Guía funcional para el frontend

Todas las rutas de negocio descritas aquí requieren un JWT de un usuario con rol `Admin`, salvo las rutas públicas de autenticación. Enviar el token como `Authorization: Bearer <accessToken>`. Las listas paginadas devuelven `{ "items": [], "page": 1, "pageSize": 20, "totalCount": 0 }`; usar `page` y `pageSize` (máximo `100`) para navegar. Los errores esperados se entregan en formato `ProblemDetails`: `400` para datos inválidos, `401/403` para acceso, `404` para recursos inexistentes y `409` para conflictos. Los errores inesperados devuelven `500` con un mensaje genérico y un identificador de seguimiento; los detalles completos solo quedan en los logs.

### Orden recomendado para preparar y cargar datos

1. **Crear una cuenta y obtener acceso.** Registrar la cuenta con `POST /api/auth/register`, habilitar el rol `Admin` para la primera cuenta mediante el procedimiento seguro de bootstrap documentado arriba, y luego iniciar sesión con `POST /api/auth/login`. El frontend guarda el access token de forma segura y usa `GET /api/auth/me` para recuperar el usuario actual. Los usuarios posteriores pueden administrarse con `/api/users`; los roles disponibles se consultan con `GET /api/roles?page=1&pageSize=20`.
2. **Cargar destinos.** Crear primero los destinos de nivel superior con `POST /api/destinations`; después crear sus destinos hijos usando `parentDestinationId`. Agregar imágenes con `POST /api/destinations/{id}/images` y seleccionar la principal con `PATCH /api/destinations/{id}/images/{imageId}/primary` cuando sea necesario.
3. **Crear productos turísticos.** Crear el producto con `POST /api/products`; después asignar o reemplazar sus destinos ordenados mediante `POST /api/products/{id}/destinations` y añadir imágenes. Marcarlo activo y publicado con los endpoints de estado/publicación cuando esté listo para ofrecerse.
4. **Definir el itinerario.** Crear una versión inicial con `POST /api/itineraries`, asociada al producto. Agregar los días con `POST /api/itineraries/{id}/days` y actividades con `POST /api/itinerary-days/{dayId}/activities`; cada actividad puede referenciar un destino. Si se reordena, enviar la lista completa de IDs actuales a los endpoints `/days/reorder` o `/activities/reorder`. Las modificaciones incrementan la versión del itinerario.
5. **Programar una salida.** Crear la salida con `POST /api/products/{productId}/departures` o `POST /api/departures`, usando un itinerario que pertenezca al mismo producto. Configurar las tarifas con `POST /api/departures/{id}/prices`. Para aceptar reservas, cambiar el estado a `Open` con `PATCH /api/departures/{id}/status`; la disponibilidad se consulta en `GET /api/departures/{id}/availability`.
6. **Preparar clientes y perfiles de pasajeros.** Buscar o crear el cliente en `/api/customers`. Opcionalmente, crear perfiles reutilizables con `/api/passengers`; una reserva conserva su propia copia de los datos del pasajero aunque se enlace a un perfil.
7. **Consultar y operar el catálogo.** Usar los endpoints paginados `/api/destinations`, `/api/products`, `/api/itineraries` y `/api/departures` para las pantallas de búsqueda. Consultar los recursos por ID para abrir sus detalles y usar los filtros documentados en cada módulo.

### Flujo frontend de reserva y operación

1. El usuario elige un cliente y una salida abierta. El frontend puede mostrar las plazas con `GET /api/departures/{departureId}/availability` y las reservas existentes con `GET /api/departures/{departureId}/reservations`.
2. Crear la reserva pendiente mediante `POST /api/reservations`, enviando `customerId`, `departureId` y notas opcionales. El backend genera el número de reserva y fija la moneda de la salida.
3. Agregar cada participante con `POST /api/reservations/{reservationId}/passengers`. Enviar sus datos como snapshot y, si existe, el `passengerId` del perfil global. Seleccionar `priceType`; el backend resuelve y conserva el precio aplicado. Las altas validan la capacidad bajo bloqueo de la salida.
4. Ajustar notas/descuento con `PUT /api/reservations/{id}` y consultar el detalle con `GET /api/reservations/{id}`. El subtotal, cantidad de pasajeros y total se calculan a partir de los snapshots; el descuento no puede superar el subtotal.
5. Confirmar con `PATCH /api/reservations/{id}/confirm` cuando la salida siga abierta y alcance el mínimo de pasajeros. Si aún no corresponde confirmar, mantenerla pendiente o expirar mediante la transición permitida.
6. Para una cancelación, solicitar motivo y reembolso en `PATCH /api/reservations/{id}/cancel`. El backend verifica que el reembolso no exceda los pagos registrados y conserva quién canceló.
7. Los pagos solo se consultan con `GET /api/reservations/{id}/payments`: el módulo actual no registra ni procesa pagos. El frontend no debe presentar un cambio de estado a `PartiallyPaid` o `Paid` como pago procesado; esos estados solo deben cambiarse cuando exista un flujo de pagos confiable. Las transiciones disponibles están restringidas por el backend.
8. Durante la operación, consultar `GET /api/departures/{departureId}/passengers` para el manifiesto y `GET /api/passengers/{id}/trips?page=1&pageSize=20` para el historial disponible del perfil. `Trip` es un registro de ejecución del viaje separado de la reserva; el API actual no expone endpoints para crearlo o editarlo.

### Flujo frontend para diseñar o modificar itinerarios

1. Elegir un producto y consultar `GET /api/itineraries?productId=...`; crear una versión nueva con `POST /api/itineraries` o abrir una existente con `GET /api/itineraries/{id}`.
2. Para edición por etapas, listar días mediante `GET /api/itineraries/{id}/days`, agregar/editar/eliminar días y volver a consultar el itinerario para refrescar la versión.
3. Para cada día, listar actividades con `GET /api/itinerary-days/{dayId}/activities`; agregar, editar o eliminar actividades y enviar el orden completo al endpoint de reordenamiento cuando corresponda.
4. Si se necesita una variante sin sobrescribir la original, usar `POST /api/itineraries/{id}/duplicate`; la copia queda activa y con la siguiente versión. Activar/desactivar una versión se hace con `PATCH /api/itineraries/{id}/status`.

El frontend debe tratar los IDs devueltos por el backend como referencias canónicas, refrescar recursos después de una respuesta `409` y no asumir que una solicitud de escritura exitosa reemplaza todos los datos relacionados: las asignaciones de destinos de un producto, por ejemplo, se reemplazan como colección completa.
