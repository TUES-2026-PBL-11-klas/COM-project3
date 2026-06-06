# NEXORA — Project Architecture

NEXORA is an eco trash-reporting web platform. Citizens can report trash locations on a map, form cleanup teams, share images of trash, and track cleanup progress.

---

## 1. Frontend (Vanilla JS)

**Purpose:** User interface for reporting trash, managing teams, and viewing the clean map.

### Components

- **teams.html** — List of teams, create team modal
- **team-detail.html** — Team members management, image gallery, image upload
- **cleanmap/** — Interactive map for trash reports
- **simple-map/** — Simplified map view

### Technologies

- HTML5, CSS3, Vanilla JavaScript
- Served via Live Server (development)
- Communicates with backend via REST API (`fetch`)
- Configured via `config.js` (`API_BASE`, `CURRENT_USER_ID`)

### Data Flow

1. User opens `teams.html` → JS calls `GET /api/teams/my?userId=`
2. Backend returns team list → JS renders team cards
3. Owner clicks **Create Team** → `POST /api/teams`
4. Owner clicks **Delete Team** → `DELETE /api/teams/{id}`
5. Member uploads image → `POST /api/teams/{id}/images` (multipart)

---

## 2. Backend (ASP.NET Core 10 — Minimal API)

**Purpose:** Process HTTP requests, enforce business logic, interact with MongoDB.

### Layered Structure

```
server/
├── API/              → Endpoints, Middleware, Program.cs
├── Core/
│   ├── Teams/        → Team, TeamMember, TeamImage models + ITeamRepository, ITeamService
│   └── CleanMap/     → CleanMapReport models + interfaces
└── Data/
    ├── Teams/        → MongoTeamRepository, MongoTeamImageRepository, TeamMapper
    └── CleanMap/     → MongoDB data access for clean map reports
```

1. **Endpoints** — Minimal API route definitions (`TeamEndpoints.cs`, `CleanMapEndpoints.cs`)
2. **Services (Business Logic)** — Permission checks, validation, orchestration
3. **Repositories (Data Access)** — Direct MongoDB communication
4. **Models** — `Team`, `TeamMember`, `TeamImage`, `CleanMapReport`

### Teams API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/health` | Health check (used by k8s probes) |
| GET | `/api/teams/my?userId=` | Get my teams |
| GET | `/api/teams/{id}` | Get team by ID |
| POST | `/api/teams?userId=` | Create team |
| POST | `/api/teams/{id}/members?userId=` | Add member (owner only) |
| DELETE | `/api/teams/{id}/members/{targetUserId}?userId=` | Remove member (owner only) |
| DELETE | `/api/teams/{id}?userId=` | Delete team (owner only) |
| GET | `/api/teams/{id}/images` | Get team images |
| POST | `/api/teams/{id}/images?userId=` | Upload image (multipart) |

### OOP Principles Applied

- `MongoRepositoryBase<TDocument>` — abstract generic base class (inheritance, encapsulation)
- `ITeamRepository`, `ITeamService` — interfaces injected via DI (polymorphism)
- `TeamMember` embedded in `Team` — composition
- `static class TeamRole` — encapsulation of domain constants

---

## 3. Database (MongoDB Atlas)

**Purpose:** Store all persistent data in the cloud.

### Collections

- `teams` — Team documents with embedded `members` array
- `team_images` — Image metadata (teamId, uploadedBy, imageUrl, notes)
- `cleanmap_reports` — Trash report locations

### Document Design

```json
// teams collection
{
  "_id": ObjectId,
  "name": "Green Warriors",
  "description": "We clean parks",
  "createdBy": "user-id",
  "members": [
    { "userId": "user-id", "role": "owner", "joinedAt": ISODate }
  ],
  "createdAt": ISODate,
  "updatedAt": ISODate
}
```

### Features

- **Embedded documents** — `TeamMember` inside `Team` (MongoDB best practice)
- **Indexes** — `members.userId` on `teams`, `teamId` on `team_images` (no full collection scans)
- **Cloud hosted** — MongoDB Atlas, replica set cluster

---

## 4. Infrastructure & DevOps

**Purpose:** Containerization, orchestration, CI/CD, and secrets management.

### Docker

```bash
# Build image
docker build -t nexora-api:latest .

# Run with env var (no hardcoded credentials)
docker run -p 5087:8080 \
  -e MongoDB__ConnectionString="mongodb+srv://..." \
  nexora-api:latest

# Or with docker-compose
docker-compose up
```

### Kubernetes (`k8s/`)

| File | Purpose |
|------|---------|
| `deployment.yaml` | 2 replicas, readiness + liveness probes on `/api/health` |
| `service.yaml` | ClusterIP, port 80 → 8080 |
| `configmap.yaml` | Non-sensitive config (`ASPNETCORE_ENVIRONMENT`, DB name) |
| `secret.yaml` | MongoDB connection string (base64 encoded) |
| `hpa.yaml` | Auto-scale 2–5 pods at >70% CPU usage |

```bash
# Deploy to Kubernetes
kubectl apply -f k8s/
kubectl get pods
kubectl get svc
```

### CI/CD (GitHub Actions)

Pipeline triggers on push and PRs to `main`:

1. **Build & Test** — restore, build, run backend unit tests
2. **Docker Build** — build image (runs only if tests pass)

### Security

- MongoDB credentials stored as **Kubernetes Secret** (base64)
- Connection string passed via **environment variable** (not hardcoded)
- `appsettings.Development.json` and `.env` are **gitignored**

---

## 5. Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- MongoDB Atlas account

### Run locally

```powershell
# Set credentials
$env:MongoDB__ConnectionString = "mongodb+srv://<user>:<pass>@cluster0.xxx.mongodb.net/"

# Start API
cd server
dotnet run --project API

# Run tests
dotnet test Tests/Tests.csproj
```

### Run with Docker Compose

```bash
# Create .env file first
echo "MONGODB_CONNECTION_STRING=mongodb+srv://..." > .env

docker-compose up
# API available at http://localhost:5087
```

---

## 6. Data Flow Example

**Create Team Flow:**

1. User clicks "Create Team" → frontend sends `POST /api/teams?userId=`
2. `TeamEndpoints` receives request → calls `TeamService.CreateTeamAsync()`
3. `TeamService` creates `Team` object, adds creator as `owner`
4. `TeamRepository.CreateAsync()` inserts document into MongoDB Atlas `teams` collection
5. Response flows back → frontend re-renders team list

**Upload Image Flow:**

1. Member selects image → frontend sends `POST /api/teams/{id}/images` (multipart)
2. `TeamImageService` validates file type, size, checks membership
3. File saved to `wwwroot/uploads/teams/{teamId}/`
4. Metadata inserted into `team_images` collection
5. Frontend refreshes image gallery
