# Rick and Morty Movie Application

A .NET 9 web application built with ASP.NET Core MVC that displays and manages Rick and Morty character data. The application supports both PostgreSQL and SQLite databases, with Redis caching.

## 📋 Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (version 9.0 or later)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (version 17.8 or later)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (**required for running tests**)

## 🏗️ Project Structure

```
RickAndMortyMovie/
├── src/
│   ├── WebApp/                    # ASP.NET Core MVC Web Application
│   │   ├── Controllers/           # MVC Controllers
│   │   ├── Views/                 # Razor Views
│   │   ├── Models/                # View Models
│   │   └── wwwroot/               # Static files (CSS, JS, images)
│   ├── ConsoleApp/                # Character import console application
│   ├── Application/               # Application layer (MediatR handlers, queries)
│   ├── Domain/                    # Domain entities and interfaces
│   ├── Infrastructure/            # Data access and external services
│   │   └── DataPersistence/       # EF Core contexts and configurations
│   └── Contracts/                 # Shared contracts and DTOs
├── tests/
│   ├── WebApp.Tests/              # Integration tests for WebApp
│   ├── ConsoleApp.Tests/          # Tests for ConsoleApp
│   └── Application.Tests/         # Unit tests for Application layer
├── docker-compose.yml             # Docker Compose orchestration
├── Dockerfile                     # Container definitions
└── RickAndMortyMovie.sln          # Solution file
```

## 💾 Database Configuration

This project supports two database providers:

### SQLite (Development)
for development purposes, the application uses SQLite by default.
- Database file: `rickandmorty.db` (created automatically)

### PostgreSQL (Test, Staging, Production)
The database provider is configured in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "SqlLiteConnection": "Data Source=rickandmorty.db",
    "PostgreSqlConnection": "Host=localhost;Port=5432;Database=rickandmorty_db;Username=postgres;Password=postgres_password"
  }
}
```

## 🚀 Running the Project in Visual Studio

### Option 1: Run Console and WebApp Directly From Visual Studio

### Option 2: Run with Docker Compose
**Access the application**:
   - Web Application: http://localhost:8080
   - Health Check: http://localhost:8080/health
