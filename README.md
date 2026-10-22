# DevTrack

[![CI](https://github.com/MohammadYousef962/DevTrack/actions/workflows/ci.yml/badge.svg)](https://github.com/MohammadYousef962/DevTrack/actions/workflows/ci.yml)

A role-based project management platform: teams, projects, a Kanban board, tasks with comments, an activity feed and a dashboard. The back end is an ASP.NET Core Web API, the front end is a React + TypeScript single-page app.

<!--
Add two or three screenshots here, for example:
![Dashboard](docs/screenshots/dashboard.png)
![Board](docs/screenshots/board.png)
-->

## Features

- **Authentication and roles**: register and sign in with JWT bearer tokens. There are three roles (Admin, Project Manager, Developer). New accounts are always Developers, and Admins promote people from the Admin page.
- **Teams and projects**: teams have an owner and members. Projects belong to a team and have their own members and a status (Planning, Active, Completed, Archived). Archived projects are read-only.
- **Kanban board**: five columns (Backlog, To Do, In Progress, In Review, Done). Tasks have a status, priority, assignee and due date. The task detail panel is addressable by URL (`?task=12`), so a task can be linked to and survives a refresh.
- **Comments**: per-task comments. Authors can edit their own, and authors, team owners and admins can delete.
- **Activity feed**: one feed per project and one across all projects, recording 13 kinds of event (creations, edits, status, priority and assignee changes, deletions, comments, member changes). People only see events for projects they can open.
- **Dashboard**: totals, tasks by status, project progress, upcoming deadlines and recent activity, computed in SQL and scoped to what the signed-in user can access.
- **My Tasks**: everything assigned to you, with server-side status, priority and text filters.
- **Admin tools**: search users, change roles, deactivate and reactivate accounts. The last active admin can't be demoted or deactivated.
- **Profile pictures**: click your picture on the profile page to upload one. The browser crops it to a square and shrinks it, the server checks the file type and size, and it is stored in the database. You see it in the sidebar, and administrators see it in the user list.
- **Everyday polish**: responsive layout with a mobile navigation drawer, session-expiry handling that returns you to the page you were on, and loading and error states with retry buttons.

## Tech stack

| Area | Technology |
| --- | --- |
| API | .NET 10, ASP.NET Core Web API, Entity Framework Core, SQL Server |
| Auth | JWT bearer tokens, PBKDF2 password hashing |
| API docs | OpenAPI with the Scalar UI (Development only) |
| Front end | React, TypeScript, Vite, Tailwind CSS v4, React Router, Axios, lucide-react |
| Tests | xUnit, Moq, EF Core InMemory, `WebApplicationFactory` against real SQL Server |
| CI | GitHub Actions, Dependabot |

## Project structure

```
DevTrack/
├── DevTrack.Domain/          Entities and enums
├── DevTrack.Application/     DTOs, service interfaces, shared models and exceptions
├── DevTrack.Infrastructure/  EF Core DbContext, migrations and service implementations
├── DevTrack.API/             Controllers, middleware, dependency injection, OpenAPI
├── DevTrack.Tests/           Unit and integration tests
├── frontend/                 React + TypeScript app
└── .github/                  CI workflow and Dependabot configuration
```

Dependencies point inward: `API → Infrastructure → Application → Domain`. Controllers stay thin, and business rules (including who may do what on which project) live in the services.

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). `global.json` pins the repo to the 10.0 SDK line.
- [Node.js](https://nodejs.org/) (a current LTS release; CI uses Node 24).
- SQL Server. The default connection string uses **LocalDB**, which is Windows-only and installs with Visual Studio or SQL Server Express LocalDB. On other systems, point the connection string at any SQL Server instance (see [Configuration](#configuration)).

### 1. Clone and add a JWT signing key

The signing key is a secret, so it is not in the repo. Store it in [User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets), which keeps it outside the project folder:

```powershell
git clone https://github.com/MohammadYousef962/DevTrack.git
cd DevTrack

dotnet user-secrets init --project DevTrack.API

$bytes = New-Object byte[] 64
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
dotnet user-secrets set "Jwt:SigningKey" ([Convert]::ToBase64String($bytes)) --project DevTrack.API
```

On macOS or Linux, `openssl rand -base64 64` produces a suitable key.

### 2. Run the API

```powershell
dotnet run --project DevTrack.API
```

The API listens on `http://localhost:5055`. On startup it applies any pending EF Core migrations, and in the Development environment it creates a first admin account if none exists:

| Email | Password |
| --- | --- |
| `admin@devtrack.local` | `Admin123!` |

This account exists for local development only. Change or remove it before exposing an instance to anyone else.

Interactive API documentation is at <http://localhost:5055/scalar/v1> (Development only). Sign in through `POST /api/auth/login` and use the returned token as a bearer token.

### 3. Run the front end

In a second terminal:

```powershell
cd frontend
npm install
npm run dev
```

Open <http://localhost:5173> and sign in with the admin account above, or register a new user.

## Configuration

| Setting | Where | Notes |
| --- | --- | --- |
| `ConnectionStrings:DefaultConnection` | `DevTrack.API/appsettings.json` | LocalDB by default. Override with User Secrets or the `ConnectionStrings__DefaultConnection` environment variable. |
| `Jwt:SigningKey` | User Secrets, or the `Jwt__SigningKey` environment variable | Required. Never committed. |
| `Jwt:Issuer`, `Jwt:Audience` | `appsettings.json` | `DevTrackApi` and `DevTrackClient`. |
| `Jwt:AccessTokenExpirationMinutes` | `appsettings.json` | 60. |
| CORS origin | `DevTrack.API/Program.cs` | `http://localhost:5173`. Change it if the front end is served elsewhere. |
| API base URL | `frontend/src/api/client.ts` | `http://localhost:5055/api`. Currently hard-coded for local development. |

To run against a different SQL Server, for example the container image CI uses:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your connection string>" --project DevTrack.API
```

## Testing

```powershell
dotnet test
```

- **Unit tests** run the services against EF Core's InMemory provider with mocked collaborators.
- **Integration tests** start the real API with `WebApplicationFactory` and talk to a real SQL Server database named `DevTrackTestDb`, which is dropped and recreated on each run. Some of them check consistency across endpoints, for example that the dashboard totals match the task list and that the cross-project feed matches each project's own feed, for every kind of user.
- Set `DEVTRACK_TEST_CONNECTION` to use another server. The test harness refuses to run against any database whose name doesn't contain "Test".
- Tests run serially because they share one database.

The front end is checked with ESLint and the TypeScript build (`npm run lint`, `npm run build`). It has no automated UI tests yet.

## Continuous integration

`.github/workflows/ci.yml` runs on every push and pull request:

- **Backend job**: restores, builds and tests against a SQL Server 2022 service container. The SA password and JWT key are generated fresh for each run and masked in the logs, so the workflow needs no repository secrets.
- **Frontend job**: `npm ci`, lint, and a production build.

Dependabot checks NuGet, npm and GitHub Actions weekly (`.github/dependabot.yml`), grouping routine minor and patch updates into one pull request per ecosystem.

## API overview

All routes are under `/api` and require a bearer token except registration and login. The Scalar page lists every endpoint with its request and response shapes.

| Area | Routes |
| --- | --- |
| Auth | `POST /auth/register`, `POST /auth/login` |
| Users | `/users/me`, `/users/me/avatar`, `/users/{id}/avatar`, `/users/change-password`, `/users/lookup`, plus Admin-only role and status changes |
| Teams | `/teams` with member management at `/teams/{id}/members` |
| Projects | `/projects`, `/projects/{id}/status`, `/projects/{id}/members`, `/projects/{id}/activity` |
| Tasks | `/tasks` with filtering and paging, plus `/tasks/{id}/status`, `/priority` and `/assignee` |
| Comments | `/tasks/{id}/comments`, `/comments/{id}` |
| Activity and dashboard | `/activity`, `/dashboard/stats` |

## Design notes

- **Authorization lives in the services.** A role alone doesn't grant access to a project. Admins, team owners and project members each get exactly the access the rules allow, and the same rules back the dashboard totals and the activity feeds.
- **One error shape.** A global exception middleware maps domain exceptions to 400, 401, 403, 404, 409 and 500, and model validation errors use the same JSON structure, so the front end has one code path for displaying errors.
- **Work happens in the database.** Filtering, paging and aggregation are SQL queries rather than in-memory loops.
- **The activity log records changes, not saves.** Saving a form with nothing edited writes no entry, and comment text is never copied into the log.
- **Secrets stay out of the repo.** User Secrets locally, throwaway generated values in CI, and a test harness that refuses to touch a database that isn't clearly a test one.

## Known limitations

These are known and deliberate for now:

- **Sessions.** There are no refresh tokens, so sessions end after 60 minutes. Tokens can't be revoked, so a role change or deactivation takes effect at the next sign-in.
- **No rate limiting** on the login, register or change-password endpoints.
- **Seeded admin.** The development admin has a known password, and there is no production bootstrap flow yet.
- **Board size.** A project's board shows its newest 200 tasks. The page says so when more exist.
- **Memberships.** Removing someone from a team doesn't remove their project memberships, and removed project members keep edit rights on tasks and comments they created. People on a team but not on a project can open the project page but not its tasks or activity.
- **Paging.** The task list and project activity endpoints don't cap the page size, and `GET /api/users` isn't paginated.
- **Profile pictures** are uploaded and stored in the database. Only their owner and administrators (on the Admin page) see them; everywhere else shows initials. Uploads are checked for file type and size but not yet for pixel dimensions.
- **Configuration.** The API URL and CORS origin are hard-coded for local development.

## Roadmap

- A hosted demo with environment-based configuration
- Refresh tokens and token revocation
- Rate limiting on authentication endpoints
- Pixel-dimension checks for uploaded pictures, and showing pictures to teammates (assignees, comments, members)
- Paging on the board
- Front-end tests
