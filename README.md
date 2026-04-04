# Coding Challenge - Sensor Analytics API

This repository contains a sample .NET 8 Web API that demonstrates an event-driven approach for collecting
and aggregating sensor events from stadium gates. It includes:

- A .NET 8 Web API (`Coding.Challenge.API`) using EF Core + SQLite for persistence.
- A background producer that simulates sensor events and writes them into an in-memory `Channel`.
- A background consumer that consumes events from the channel and persists them.
- An endpoint to query aggregated results grouped by gate and type with optional filters (gate, type, start, end).
- Unit tests for repository and controller logic.

How to run
----------
Prerequisites: .NET 8 SDK

1. Restore and build

   dotnet restore
   dotnet build

2. Run the API

   dotnet run --project Coding.Challenge.API

   The API will be available at `https://localhost:5001` (or the ports printed by the host).

3. Swagger

   When running in Development environment, Swagger UI is available at `/swagger`.

Useful endpoints
----------------
- POST `api/sensorevents` - Accepts a sensor event payload for manual testing. Example body:

  {
    "gate": "Gate A",
    "timestamp": "2023-04-01T08:00:00Z",
    "numberOfPeople": 10,
    "type": "enter"
  }

- GET `api/analytics` - Returns aggregated results grouped by gate and type. Supports query parameters `gate`, `type`, `start`, `end`.

Example response:

  [
    { "gate": "Gate A", "type": "enter", "numberOfPeople": 100 }
  ]

Database
--------
This project uses SQLite by default with a local file `analytics.db`. To change connection string, set `ConnectionStrings:Sqlite` in `appsettings.json` or environment variables.

Environment-specific configuration
----------------------------------
The application supports environment-specific configuration files using ASP.NET Core conventions. Files provided in this repo:

- `appsettings.Development.json` - defaults for development
- `appsettings.Staging.json` - example staging configuration
- `appsettings.Production.json` - production-like defaults
- `appsettings.Test.json` - test-specific configuration (uses in-memory provider by default)

The host environment is controlled by the `ASPNETCORE_ENVIRONMENT` environment variable. For example to run in Staging:

  $env:ASPNETCORE_ENVIRONMENT = "Staging"
  dotnet run --project Coding.Challenge.API

Database configuration can be overridden per environment by setting `ConnectionStrings:Sqlite` or by changing the `Database:UseInMemory` flag in the environment-specific appsettings file.

Docker
------
You can run the service using Docker and docker-compose. This will build the image from the included `Dockerfile` and mount a local `./data` folder for the SQLite DB file.

Build and run with docker-compose:

  docker compose up --build

The API will be available at `http://localhost:5000` and `https://localhost:5001` depending on your Docker host configuration.

Notes when running with Docker:
- The service expects the connection string `ConnectionStrings:Sqlite` to point to a writable path. The provided `docker-compose.yml` mounts `./data` into the container at `/data` and sets the connection string accordingly.

To run the service in Production mode via Docker, set the environment to Production and provide a writable path for the DB:

  docker compose up --build

Or override environment variables when running:

  docker run -e ASPNETCORE_ENVIRONMENT=Production -e ConnectionStrings__Sqlite="Data Source=/data/analytics.db" -v %cd%/data:/data -p 5000:80 coding.challenge.api:latest

Running tests
-------------
Run unit and integration tests locally with:

  dotnet test

Regenerating migrations
-----------------------
If you want to regenerate EF Core migrations locally (for example to update schema), a helper script is provided at `scripts/regenerate-migrations.ps1`.

Usage (PowerShell):

  pwsh ./scripts/regenerate-migrations.ps1

This will back up existing migrations and create a new `InitialCreate` migration in `Coding.Challenge.API/Migrations`.

Health checks and metrics
-------------------------
The application exposes a basic health endpoint at `/health` (HTTP 200 when healthy).

Metrics are available at `GET /api/metrics` and return a JSON payload like:

  { "totalEvents": 123, "deadLetters": 2 }

Dead-letter and retention
-------------------------
When the consumer fails to persist an event after configured retries, the event is stored in the `DeadLetters` table for manual inspection. Configure retention cleanup via `appsettings.{Environment}.json` using the `Retention:DaysToKeep` setting (default 30 days). The background service `RetentionCleanupService` runs once per day and removes events older than the configured retention window.

ECS Fargate deployment (template)
---------------------------------
The CI workflow includes an example job that builds and publishes a Docker image to GHCR. To deploy to AWS ECS Fargate, you'll need to add a deploy job with appropriate AWS credentials saved as GitHub Secrets (`AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `AWS_REGION`, `ECS_CLUSTER`, `ECS_SERVICE`, `ECR_REPOSITORY`).

An outline of the steps you would add to `.github/workflows/ci-deploy.yml`:

1. Login to ECR and build/push image to ECR repo.
2. Render an ECS task definition JSON (or use a template) referencing the new image tag.
3. Register the new task definition with `aws ecs register-task-definition`.
4. Update the service with `aws ecs update-service --cluster $ECS_CLUSTER --service $ECS_SERVICE --force-new-deployment`.

I can add a concrete implementation of the ECS deployment job if you provide target cluster/service names and confirm storing AWS credentials in GitHub Secrets.


Notes and improvements made
---------------------------
- Added server-side aggregation in repository to let the database perform grouping and summing for efficiency.
- Added DTO validation for incoming POST requests and return `400 Bad Request` when invalid.
- On startup the application attempts to apply EF Core migrations via `Database.Migrate()` and falls back to `EnsureCreated()` for easy reviewer runs.
- Included a `Dockerfile` to run the service in a container.

Contact
-------
This is a coding exercise submission. For questions, please contact Dharanesh.pashavula@gmail.com.
