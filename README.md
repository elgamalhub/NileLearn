# NileLearn — Learning Management System API

A production-shaped, bilingual (English / Arabic) learning management system backend built with **ASP.NET Core 8**, **Entity Framework Core**, **ASP.NET Core Identity**, **JWT**, and a clean layered architecture.

NileLearn covers the full learning commerce loop: course authoring and publishing, course discovery, subscriptions and coupons, Egyptian payment gateways, secure chunked video delivery, progress tracking, certificates, quizzes, audit logging, and role-based admin analytics.

[![CI](https://github.com/elgamalhub/NileLearn/actions/workflows/ci.yml/badge.svg)](https://github.com/elgamalhub/NileLearn/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-supported-CC2927?logo=microsoftsqlserver)](https://www.microsoft.com/sql-server)
[![SQLite](https://img.shields.io/badge/SQLite-demo%20default-003B57?logo=sqlite)](https://www.sqlite.org/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

---

## Table of contents

- [Why this project](#why-this-project)
- [Features](#features)
- [Architecture](#architecture)
- [Tech stack](#tech-stack)
- [Project structure](#project-structure)
- [Getting started](#getting-started)
- [Demo accounts](#demo-accounts)
- [API surface](#api-surface)
- [Configuration](#configuration)
- [Running with Docker](#running-with-docker)
- [Tests](#tests)
- [Security notes](#security-notes)
- [Frontend status](#frontend-status)
- [License](#license)

---

## Why this project

Most LMS demos stop at CRUD. NileLearn is built around the parts that make a learning business work:

- **Money is handled explicitly.** Course checkout, subscriptions, coupons and invoices share one order model, with webhook verification and an idempotent payment state machine.
- **Video is protected.** Uploads are chunked and playback is authorised with short-lived signed URLs plus heartbeat-based progress.
- **Access is role-aware.** SuperAdmin, Admin, Instructor, TeachingAssistant, Student, CustomerSupport and FinanceManager roles gate the API surface.
- **The domain is tested.** Unit tests cover business rules; `WebApplicationFactory` integration tests exercise the real HTTP pipeline, seeding, auth and the full register → login → checkout → webhook → enrolment flow.

---

## Features

### Identity & security
- ASP.NET Core Identity with custom `ApplicationUser` / `ApplicationRole`
- JWT access tokens + rotating refresh tokens
- Session listing and revocation
- Profile management and password change
- Role-based authorization across admin, instructor and student surfaces
- Audit logging for privileged actions

### Courses & learning
- Course authoring with draft, review, published, scheduled, archived states
- Categories, sections, lessons (video, article, quiz, assignment, downloadable resource)
- Free previews and level/price/language filtering
- Search, sorting (popular, newest, price, rating) and pagination
- Enrolments, lesson progress, course progress and resume position
- Quizzes with attempts, grading and pass marks
- Certificates with public verification codes

### Commerce
- Subscription plans (monthly, quarterly, annual, lifetime) with EGP pricing
- Course purchases, coupons and discounts
- Invoices and order history
- Payment gateway abstraction with:
  - **Paymob** (card + wallet)
  - **Fawry** reference payments
  - **Kashier**
  - **Stripe** (payment intents + webhooks)
  - **Sandbox** gateway for local demos and automated tests
- Webhook signature verification (HMAC-SHA512 for Paymob, gateway-specific checks)

### Video
- Chunked upload sessions with resume support
- Storage-provider abstraction (Cloudflare Stream, Bunny Stream, S3, R2, external embed, local simulator)
- Short-lived signed playback URLs
- Playback heartbeat tracking and position persistence

### Admin & analytics
- User management, suspend/reactivate, role assignment
- Revenue, enrolment and completion analytics
- Audit log browsing
- Content moderation hooks

---

## Architecture

```
NileLearn.Api            HTTP layer: controllers, auth wiring, Swagger, health checks
        |
NileLearn.Application    Use cases, DTOs, service interfaces, validation
        |
NileLearn.Domain         Entities, enums, invariants — no framework dependencies
        ^
NileLearn.Infrastructure EF Core, Identity, JWT, payments, video, persistence, seeding
```

Dependencies point inward. The domain has no dependency on EF Core or ASP.NET Core; infrastructure implements the interfaces declared by the application layer. Controllers stay thin and delegate to services.

---

## Tech stack

| Area | Technology |
| --- | --- |
| Runtime | .NET 8 / ASP.NET Core 8 |
| Data | EF Core 8, SQL Server, SQLite |
| Identity | ASP.NET Core Identity, JWT Bearer, rotating refresh tokens |
| Validation | FluentValidation |
| Docs | Swagger / OpenAPI with Bearer auth |
| Payments | Paymob, Fawry, Kashier, Stripe, Sandbox |
| Video | Signed playback, chunked upload, provider abstraction |
| Tests | xUnit, FluentAssertions, Moq, `WebApplicationFactory`, EF Core InMemory |
| DevOps | Docker, Docker Compose, GitHub Actions |

---

## Project structure

```
.
├── NileLearn.slnx
├── Dockerfile
├── docker-compose.yml
├── .env.example
├── src
│   ├── NileLearn.Api
│   │   ├── Controllers
│   │   ├── Program.cs
│   │   └── appsettings.json
│   ├── NileLearn.Application
│   │   ├── Common
│   │   └── Features (Auth, Courses, Learning, Payments, Billing, Videos, Admin)
│   ├── NileLearn.Domain
│   │   ├── Entities
│   │   └── Enums
│   └── NileLearn.Infrastructure
│       ├── Persistence
│       ├── Services (Auth, Courses, Learning, Payments, Videos, Admin)
│       └── DependencyInjection.cs
└── tests
    ├── NileLearn.UnitTests
    └── NileLearn.IntegrationTests
```

---

## Getting started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Optional: SQL Server 2019+ (the demo uses SQLite by default)
- Optional: Docker Desktop

### Run locally

```bash
git clone https://github.com/elgamalhub/NileLearn.git
cd NileLearn

# SQLite is the default; no database server required
dotnet run --project src/NileLearn.Api
```

The API listens on `http://localhost:5241`.

- Swagger UI: <http://localhost:5241/swagger>
- Health check: <http://localhost:5241/health>

On first start the database is created and seeded with roles, demo users, categories, courses, plans and coupons.

### Use SQL Server instead

Update `appsettings.json` (or use environment variables):

```json
{
  "UseSqlite": false,
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=NileLearnDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

Then delete `AppData/nilelearn.db` if it exists and restart.

---

## Demo accounts

| Role | Email | Password |
| --- | --- | --- |
| Super Admin | `superadmin@nilelearn.com` | `AdminPassword123!` |
| Instructor | `instructor@nilelearn.com` | `Instructor123!` |
| Student | `student@nilelearn.com` | `Student123!` |

These accounts are seeded for local evaluation only. Change or remove them before any real deployment.

### Quick API smoke test

```bash
# Health
curl http://localhost:5241/health

# Browse categories and courses
curl http://localhost:5241/api/v1/courses/categories
curl "http://localhost:5241/api/v1/courses?pageIndex=1&pageSize=6"

# Log in and capture the access token
curl -X POST http://localhost:5241/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"student@nilelearn.com","password":"Student123!"}'
```

---

## API surface

Base route: `/api/v1`

| Controller | Highlights |
| --- | --- |
| `Auth` | register, login, refresh-token, profile, change-password, sessions |
| `Courses` | list/search, get by slug/id, categories, create, update, publish, sections, lessons, delete |
| `Learning` | my-courses, progress, certificates (claim / verify / list), quizzes (start / submit) |
| `Payments` | checkout course/subscription, webhooks, status, invoices |
| `Subscriptions` | plans, my-subscription, create plan, cancel |
| `Videos` | upload init/chunk/complete/status/delete, playback authorize, heartbeat |
| `Admin` | analytics, users, suspend/restore, role assignment, audit logs |

All authenticated endpoints accept `Authorization: Bearer <access-token>`. Swagger includes a Bearer scheme so you can authorise once and try every endpoint.

---

## Configuration

Configuration is standard ASP.NET Core: `appsettings.json`, `appsettings.{Environment}.json`, environment variables, and user-secrets.

The checked-in `appsettings.json` contains **development-only placeholders**. Override them in any real environment:

```bash
export JWT__SECRETKEY="a-long-random-secret-at-least-32-characters"
export USESQLITE="false"
export CONNECTIONSTRINGS__DEFAULTCONNECTION="Server=...;Database=...;User Id=...;Password=..."
export PAYMENTGATEWAYS__PAYMOB__APIKEY="..."
```

A complete list lives in [`.env.example`](.env.example). Environment variable names use double underscores (`__`) for nesting.

> **Never commit real gateway credentials.** The sandbox gateway needs no keys and is what the demo and tests use.

---

## Running with Docker

```bash
docker compose up --build
```

The API is exposed at <http://localhost:8080>. SQLite data and uploads persist in the `nilelearn-data` volume.

Build the image directly:

```bash
docker build -t nilelearn-api .
docker run --rm -p 8080:8080 -v nilelearn-data:/app/AppData nilelearn-api
```

---

## Tests

```bash
# Everything (use explicit projects for the broadest SDK compatibility)
dotnet test tests/NileLearn.UnitTests && dotnet test tests/NileLearn.IntegrationTests

# Unit tests only
dotnet test tests/NileLearn.UnitTests

# Integration tests only
dotnet test tests/NileLearn.IntegrationTests
```

Coverage includes:

- Seeded data and health endpoint through the real HTTP pipeline
- Registration, login and JWT issuance
- Course catalogue filtering and pagination
- Subscription plans and EGP pricing
- Full checkout → sandbox webhook → enrolment flow
- Domain rules for discounts, progress and certificates

CI runs both suites on every push and pull request via GitHub Actions.

---

## Security notes

- Real payment keys and JWT secrets are **not** committed; placeholders are labelled as development-only.
- Passwords are hashed by ASP.NET Core Identity.
- Refresh tokens are stored hashed and rotated on use.
- Playback URLs are short-lived and signed.
- Webhooks are verified before orders are marked paid.
- For production: use HTTPS, rotate the JWT key, restrict CORS to your real frontend origin, enable rate limiting, and store configuration in a secret manager.

---

## Frontend status

This repository is **API-first**. The Angular client is a separate scaffold and is intentionally not included here so the backend can be reviewed, run and tested on its own. The API is fully documented in Swagger and is ready to be consumed by any SPA or mobile client.

---

## License

MIT — see [LICENSE](LICENSE).
