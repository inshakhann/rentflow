# RentFlow

<div align="center">

**Modern property operations platform built with ASP.NET Core + Blazor WebAssembly**

[Live Demo](https://rentflow-app.azurewebsites.net) • [Local Setup](#quick-start-local-development) • [API Overview](#api-overview)

</div>

---

## Table of Contents

- [What Is RentFlow?](#what-is-rentflow)
- [Core Capabilities](#core-capabilities)
- [Tech Stack](#tech-stack)
- [Solution Architecture](#solution-architecture)
- [Quick Start (Local Development)](#quick-start-local-development)
- [Test Accounts](#test-accounts)
- [Configuration](#configuration)
- [Running in Production (Azure)](#running-in-production-azure)
- [API Overview](#api-overview)
- [Project Structure](#project-structure)
- [Troubleshooting](#troubleshooting)
- [Security Notes](#security-notes)
- [Contributing](#contributing)

---

## What Is RentFlow?

RentFlow is a full-stack, role-based property management platform designed for:

- **Admins** managing platform-wide operations
- **Landlords** managing units, leases, maintenance, and revenue
- **Tenants** managing rent payments, maintenance requests, and negotiations

It combines operational workflows with analytics, weather awareness, notifications, and AI-assisted support into one unified web app.

---

## Core Capabilities

### Admin

- Platform dashboard with aggregate stats
- Manage landlords and tenants
- View all properties
- Configure system settings

### Landlord

- Portfolio dashboard and occupancy insights
- Property and unit management
- Tenant lease assignment and invitations
- Revenue and arrears tracking
- Maintenance ticket workflow and assignments
- Weather alert visibility for managed properties

### Tenant

- Personal dashboard with due status
- Payment history + receipt generation
- Maintenance bot + ticket creation
- Rent negotiation workflow
- Unit and lease details

### Shared Features

- JWT-based authentication and role authorization
- Notification center
- Theme toggle (light/dark)
- Responsive layout + redesigned UI shell
- Seeded data for immediate demo/testing

---

## Tech Stack

- **Backend:** ASP.NET Core 8 (Web API + hosting Blazor WASM)
- **Frontend:** Blazor WebAssembly (.NET 8)
- **Database:** SQLite (EF Core)
- **Auth:** JWT Bearer
- **Styling/UI:** Bootstrap 5 + custom design system
- **Charts/Visuals:** Chart.js + custom JS interop
- **Mail/Infra services:** MailKit, background services
- **Deployment:** Azure App Service (Linux)

---

## Solution Architecture

```text
Browser (Blazor WASM)
    │
    ├── RentFlow.Client (Pages, Layouts, Services)
    │       │
    │       └── HTTP calls to /api/*
    │
ASP.NET Core Host (RentFlow.Server)
    ├── Controllers (Auth, Properties, Payments, Leases, Maintenance, etc.)
    ├── Services (Token, Email, LateFee background job)
    ├── EF Core DbContext
    └── SQLite database
            │
Shared Contracts
    └── RentFlow.Shared (DTOs + Models)
```

---

## Quick Start (Local Development)

### Prerequisites

- .NET **8 SDK** (recommended)
- Git
- macOS / Linux / Windows

> Note: .NET 10 runtime can coexist, but the project targets **net8.0**.

### 1) Clone the Repository

```bash
git clone https://github.com/inshakhann/rentflow.git
cd rentflow
```

### 2) Restore + Build

```bash
dotnet restore Rentflow_agent/RentFlow.slnx
dotnet build Rentflow_agent/RentFlow.slnx -v minimal
```

### 3) Run the App

```bash
dotnet run --project Rentflow_agent/RentFlow.Server/RentFlow.Server.csproj
```

Default local URLs:

- `http://localhost:5131`
- `https://localhost:7119` (when HTTPS profile is active)

### 4) Open in Browser

- Visit: `http://localhost:5131`
- Sign in using one of the seeded test accounts below.

---

## Test Accounts

All seeded accounts use the same password:

- Password: `RentFlow@2024`

### Admin

- `admin@rentflow.io`

### Landlords

- `ahmed.landlord@rentflow.io`
- `sara.landlord@rentflow.io`

### Tenants

- `ali.tenant@rentflow.io`
- `fatima.tenant@rentflow.io`
- `bilal.tenant@rentflow.io`
- `zara.tenant@rentflow.io`

---

## Configuration

Main config file:

- `Rentflow_agent/RentFlow.Server/appsettings.json`

### Important keys

- `ConnectionStrings:DefaultConnection`
- `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`
- `OpenWeatherMap:ApiKey`
- `Groq:ApiKey`
- `GoogleMaps:ApiKey`
- `EmailSettings:*`

### Development defaults

The app seeds data automatically if the DB is empty.

### Production recommendation

- Move secrets to environment variables or Azure App Settings
- Use a strong JWT signing key
- Use managed DB (or persistent SQLite path if staying file-based)

---

## Running in Production (Azure)

Current deployment:

- `https://rentflow-app.azurewebsites.net`

### Azure App Settings used

- `ConnectionStrings__DefaultConnection=Data Source=/home/rentflow.db`
- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS=http://+:8080`
- `WEBSITES_PORT=8080`

> Why this matters: Linux App Service requires writable DB paths. A non-writable SQLite path can cause container startup failure.

---

## API Overview

All APIs are served by `RentFlow.Server` under `/api/*`.

### Authentication

- `POST /api/auth/login`
- `POST /api/auth/register`

### Admin

- `GET /api/admin/dashboard`
- `GET /api/admin/landlords`
- `GET /api/admin/tenants`
- `GET /api/admin/properties`

### Properties & Units

- `GET /api/properties`
- `GET /api/properties/{id}`
- `POST /api/properties`
- `POST /api/properties/{propertyId}/units`
- `PUT /api/properties/units/{unitId}`

### Leases

- `GET /api/leases/landlord`
- `GET /api/leases/available-tenants`
- `POST /api/leases`
- `GET /api/leases/tenant`
- `GET /api/leases/tenant/countdown`
- `GET /api/leases/landlord/occupancy-heatmap`

### Payments & Reports

- `GET /api/payments/landlord/revenue`
- `GET /api/payments/landlord/late`
- `GET /api/payments/tenant/history`
- `POST /api/payments/{id}/pay`
- `GET /api/payments/tenant/receipt/{id}`
- `GET /api/payments/tenant/due-status`
- `GET /api/payments/score/{tenantId}`
- `GET /api/report/monthly`

### Maintenance

- `GET /api/maintenance/landlord`
- `GET /api/maintenance/tenant`
- `POST /api/maintenance`
- `PUT /api/maintenance/{id}`
- `POST /api/maintenance/upload`

### Weather / AI / Utility

- `GET /api/weather`
- `GET /api/weather/landlord/alerts`
- `PUT /api/weather/alerts/{id}/read`
- `GET /api/weather/tenant/alerts`
- `GET /api/geocode`
- `POST /api/groq/chat`
- `POST /api/groq/negotiate`
- `POST /api/groq/suggest-rent`
- `GET /api/qrcode/tenant`
- `GET /api/notifications`
- `PUT /api/notifications/{id}/read`
- `PUT /api/notifications/read-all`

---

## Project Structure

```text
Rentflow_agent/
├── RentFlow.Client/     # Blazor WASM UI, layouts, pages, client services
├── RentFlow.Server/     # ASP.NET Core host, APIs, EF Core, background services
├── RentFlow.Shared/     # DTOs and domain models shared between client/server
├── RentFlow.slnx        # solution file
└── run.bat              # convenience run script (Windows)
```

---

## Troubleshooting

### App boots but crashes with SQLite file error

Symptom:

- `SQLite Error 14: 'unable to open database file'`

Fix:

- Ensure DB path is writable.
- Local default is `Data Source=data/rentflow.db`.
- Azure App Service should use a writable path like `/home/rentflow.db`.

### Root URL briefly shows error/reload UI

Fixes already included:

- Removed forced full page reloads on auth navigation
- Replaced default Blazor loader/error flash with custom startup shell

### Icons (notification/theme) not visible

Fixes already included:

- Bootstrap Icons CDN added to host page
- Header icon contrast adjusted across themes

### Dark mode table/readability issues

Fixes already included:

- Global dark-mode overrides for cards/tables/muted text/badges
- Cross-page contrast tuning through CSS variables

---

## Security Notes

- Do not use seeded passwords in production.
- Replace placeholder keys in `appsettings.json`.
- Store secrets in secure config providers (Azure App Settings / Key Vault).
- Restrict admin creation paths (already enforced at register endpoint).

---

## Contributing

1. Fork and clone
2. Create a feature branch
3. Make focused commits
4. Open a pull request with:
   - summary
   - screenshots (if UI changes)
   - test notes

---

<div align="center">

Built with care for real-world property operations workflows.

</div>
