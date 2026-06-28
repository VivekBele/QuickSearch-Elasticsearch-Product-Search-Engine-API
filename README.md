# QuickSearch — Elasticsearch Product Search Engine API

A high-performance product search engine API built with **.NET 10**, **Elasticsearch**, and **SQL Server**. It supports full-text search, pagination, sorting, and CRUD operations across both Elasticsearch indices and a relational database.

---

## Tech Stack

| Layer            | Technology                                      |
| ---------------- | ----------------------------------------------- |
| **Runtime**      | .NET 10 (ASP.NET Core Web API)                  |
| **Search**       | Elasticsearch 8.x (via `Elastic.Clients.Elasticsearch`) |
| **Database**     | SQL Server (via Entity Framework Core — DB First) |
| **Logging**      | MongoDB (custom logger utility)                 |
| **Architecture** | Clean Architecture with Generic Repository Pattern |

---

## Solution Structure

```
QuickSearch.slnx
├── QuickSearch.Web            # Host / Entry point (Kestrel server, DI registration)
├── QuickSearch.Api            # Controllers, Service interfaces & implementations
├── QuickSearch.DataAccess     # Data access layer (ProductDbService, UserDbService)
├── QuickSearch.Data           # EF Core DbContext, Entity models, Generic Repository
├── QuickSearch.Model          # Shared DTOs (Request / Response models)
├── QuickSearch.LoggerUtility  # MongoDB-based logging utility
├── HelperUtilities            # Constants, extension methods
└── QuickSearch-WebAPIs        # (Legacy — standalone API project)
```

---

## API Endpoints

### Products — `api/products`

| Method   | Route                        | Description                                           |
| -------- | ---------------------------- | ----------------------------------------------------- |
| `GET`    | `/api/products/all`          | Get all products (paginated). Query: `pageNumber`, `pageSize`, `isFromElastic` |
| `GET`    | `/api/products/{id}`         | Get a single product by ID. Query: `isFromElastic`    |
| `GET`    | `/api/products/search`       | Search products by term, category, brand with sorting |
| `PUT`    | `/api/products/{id}`         | Update an existing product (DB + Elasticsearch)       |
| `DELETE` | `/api/products/{id}`         | Delete a product (DB + Elasticsearch)                 |

### Authentication — `api/auth`

| Method | Route              | Description                                      |
| ------ | ------------------ | ------------------------------------------------ |
| `POST` | `/api/auth/login`  | Authenticate with username & password (SHA-256)   |

### Users — `api/users`

| Method   | Route              | Description                                      |
| -------- | ------------------ | ------------------------------------------------ |
| `GET`    | `/api/users`       | Get all users with their roles                   |
| `DELETE` | `/api/users/{id}`  | Delete a user (cascades roles & logs)            |

---

## Architecture Flow

```
Client Request
    │
    ▼
┌──────────────────┐
│  QuickSearch.Web  │  ← Entry point, DI container, Kestrel host
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│  QuickSearch.Api  │  ← Controllers + Service Layer (business logic)
└────────┬─────────┘
         │
         ▼
┌────────────────────────┐
│  QuickSearch.DataAccess │  ← Data access services (ProductDbService, UserDbService)
└────────┬───────────────┘
         │
         ▼
┌──────────────────┐
│  QuickSearch.Data │  ← EF Core DbContext, Entities, Generic Repository<T>
└──────────────────┘
```

---

## Database Schema

### Tables

| Table        | Description                              |
| ------------ | ---------------------------------------- |
| `Products`   | Product catalog (Name, Price, Brand, Category, Rating) |
| `Users`      | Registered users (Username, PasswordHash, Email)       |
| `Roles`      | Role definitions (Admin, User)           |
| `UserRoles`  | Many-to-many mapping between Users and Roles           |
| `user_logs`  | Login audit trail (UserId, LoginTime)    |

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/sql-server) (LocalDB or full instance)
- [Elasticsearch 8.x](https://www.elastic.co/downloads/elasticsearch)
- [MongoDB](https://www.mongodb.com/try/download/community) (for logging)

### Run the API

```bash
cd QuickSearch
dotnet run --project QuickSearch.Web
```

The API will be available at:
- **HTTP**: `http://localhost:5094`
- **HTTPS**: `https://localhost:7142`
- **Swagger UI**: `https://localhost:7142/swagger`

### Configuration

All connection strings and settings are in `QuickSearch.Web/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "MongoDb": "mongodb://localhost:27017",
    "SqlDb": "Server=localhost;Database=QuickSearch;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Elasticsearch": {
    "Indices": {
      "ProductIndex": "products"
    }
  }
}
```

---

## Key Design Decisions

- **Generic Repository Pattern** — `IRepository<T>` provides a reusable data access abstraction for all entities.
- **Dual Data Source** — Products can be fetched from either Elasticsearch (fast search) or SQL Server (source of truth) via the `isFromElastic` flag.
- **Layered Architecture** — Strict separation: Controllers → Services → DataAccess → Data (EF Core).
- **SHA-256 Password Hashing** — Passwords are hashed before storage and comparison.

---

## Default Admin Credentials

| Field    | Value                |
| -------- | -------------------- |
| Username | `admin`              |
| Password | `admin123`           |

---

## License

This project is for educational and development purposes.
