# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

WholeCareInsurance: an insurance administration system with a REST API and a React admin panel. Spanish is the working language for UI copy, entity/field names in Spanish-facing contexts (`Nombre`, `Rol`), and commit messages — see conventions below.

Two independent projects in this repo:
- `WholeCareInsurance.api/` — ASP.NET Core (.NET 9) + EF Core 9 + SQL Server, port `5279`
- `wholecare-admin-vs/` — React 19 + React Router v7 + Vite 8, port `5173`

## Commands

### Backend (`WholeCareInsurance.api/`)
```bash
cd WholeCareInsurance.api
dotnet ef database update      # apply migrations / create the DB
dotnet run                     # start API (seeds admin user on startup)
dotnet ef migrations add <Name>  # after changing a Model or Configuration
```
Swagger UI at `http://localhost:5279/swagger` (dev only). There is no test project in this repo — verify backend changes by running the API and exercising endpoints via Swagger/curl, not via `dotnet test`.

Default seeded admin (created by `Services/AdminUserSeeder.cs` on every startup if missing): `admin@wholecare.com` / `Admin123!`, role `Admin`.

Local secrets go in `WholeCareInsurance.api/appsettings.Development.json` (gitignored), overriding `Jwt:Key`/`Jwt:Issuer`/`Jwt:Audience`/`Jwt:AccessTokenMinutes` and `ConnectionStrings:DefaultConnection`. `Jwt:Key` must be ≥32 chars.

### Frontend (`wholecare-admin-vs/`)
```bash
cd wholecare-admin-vs
npm install
npm run dev       # Vite dev server, http://localhost:5173
npm run build
npm run lint
```
No test runner is configured for the frontend either.

## Architecture

### Backend layering
`Controllers` → `Services` (business logic, interfaces in `Repositories/Interfaces`) → `Data/AppDbContext` (EF Core). Entity configurations (fluent API) live in `Data/Configurations/*Configuration.cs` and are picked up automatically via `modelBuilder.ApplyConfigurationsFromAssembly` in `AppDbContext`.

All DTOs live under `DTOs/<Area>/` (`DTOs/Auth/`, `DTOs/Users/`, `DTOs/Customers/`, `DTOs/Policies/`) as standalone classes — no AutoMapper, mapping is hand-written in each controller (`ToResponse`/`MapFromDto`-style private methods). `PolicyResponseDto` is shared between `PoliciesController` and `CustomersController.GetPoliciesForCustomer`.
`Mappings/`, `Middlewares/`, `Repositories/Implementations/`, and `Utils/` are currently empty placeholder folders (kept via `.csproj` `<Folder Include>` entries) — don't assume code lives there.

Route prefixes are inconsistent: `CustomersController`/`PoliciesController` are under `api/customers` and `api/policies`; `AuthController`/`UsersController` are under `auth` and `users` (no `api/` prefix). Match the existing prefix for the controller you're touching.

### Auth flow
JWT bearer auth (`AuthService`, `Program.cs`). On login, `AuthService` issues a short-lived JWT (`Jwt:AccessTokenMinutes`, default 60 min) with claims `Sub`, `Name`, `GivenName`, `Email`, `Role`, plus a random refresh token. Refresh tokens are never stored raw — only their SHA-256 hash (`User.RefreshTokenHash`) with an expiry (`User.RefreshTokenExpiresAt`) and a filtered unique index for lookup. `POST /auth/refresh` rotates both tokens; `POST /auth/logout` clears the hash/expiry. Two roles: `Admin` (full access, can register users) and `Agente` (customers/policies only), enforced via `[Authorize(Roles = "...")]`.

### Frontend
No global state library, but there is a centralized API client: `src/api.js` exports `apiFetch(path, options)`, which reads the base URL from `VITE_API_URL` (`.env`, gitignored — see `.env.example`), attaches the `Authorization: Bearer` header from `localStorage` automatically, and on a 401 transparently refreshes the access token (deduplicated at the module level — the refresh token rotates on each use, so concurrent 401s must share a single in-flight refresh) and retries the original request once. If refresh fails it calls `logout()` (also deduplicated), which hits `POST /auth/logout` and redirects to `/login`. All authenticated calls in `Customers.jsx`/`Policies.jsx` go through `apiFetch`; `Login.jsx` talks to `/auth/login` directly via `VITE_API_URL` since there's no token yet. `App.jsx` still reads `localStorage.getItem("accessToken")` directly for the initial route-gating check (not an API call, so it doesn't go through `apiFetch`).

### Roadmap
`PENDIENTE.md` at the repo root tracks the current backlog (policy search/filter, detail view, e-signature consent — blocked on a provider decision, DTO file reorg). Check it at the start of a session before proposing new work.

## Conventions
- Commit messages and UI-facing strings are in Spanish; code identifiers (classes, variables, endpoints, DB columns) are in English except where existing models already use Spanish (`User.Nombre`, `User.Rol`).
- Verify backend/frontend changes by actually running the app (`dotnet run` + `npm run dev`, or curl/Swagger) — this repo has caught real bugs (CORS, post-login redirect, duplicate unique index) that only surfaced at runtime, not from reading the code or a successful build.
