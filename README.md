# NEXORA — Teams Module

Eco trash-reporting platform. This module handles team management and image sharing.

## Stack

- **Backend**: ASP.NET Core 10 Minimal API
- **Database**: MongoDB Atlas
- **Containerization**: Docker
- **Orchestration**: Kubernetes (k8s/)
- **CI/CD**: GitHub Actions

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- MongoDB Atlas account (or local MongoDB)

### 1. Set connection string

Copy the example and fill in your credentials:

```bash
cp NexoraAPI/appsettings.Development.json.example NexoraAPI/appsettings.Development.json
```

Or set via environment variable (overrides appsettings):

```powershell
$env:MongoDB__ConnectionString = "mongodb+srv://<user>:<password>@cluster0.xxx.mongodb.net/?appName=Cluster0"
```

### 2. Run the API

```powershell
cd NexoraAPI
dotnet run
# → http://localhost:5087
```

### 3. Run unit tests

```powershell
cd NexoraAPI.Tests
dotnet test
```

## API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/health` | Health check |
| GET | `/api/teams/my?userId=` | Get my teams |
| GET | `/api/teams/{id}` | Get team by ID |
| POST | `/api/teams?userId=` | Create team |
| POST | `/api/teams/{id}/members?userId=` | Add member |
| DELETE | `/api/teams/{id}/members/{targetUserId}?userId=` | Remove member |
| GET | `/api/teams/{id}/images` | Get team images |
| POST | `/api/teams/{id}/images?userId=` | Upload image |

## Docker

```bash
# Build
docker build -t nexora-api:latest .

# Run
docker run -p 8080:8080 \
  -e MongoDB__ConnectionString="your-connection-string" \
  -e MongoDB__DatabaseName="nexora" \
  nexora-api:latest
```

## Kubernetes

```bash
# 1. Create secret (replace placeholder with real base64 value)
# PowerShell:
# [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes("your-connection-string"))
# Edit k8s/secret.yaml and replace REPLACE_WITH_BASE64_ENCODED_CONNECTION_STRING

# 2. Apply all manifests
kubectl apply -f k8s/

# 3. Verify
kubectl get pods
kubectl get svc
```

### k8s Manifests

| File | Purpose |
|------|---------|
| `deployment.yaml` | 2 replicas, health probes |
| `service.yaml` | ClusterIP, port 80 → 8080 |
| `configmap.yaml` | Non-sensitive config |
| `secret.yaml` | MongoDB connection string |
| `hpa.yaml` | Auto-scale 2–5 pods at >70% CPU |
