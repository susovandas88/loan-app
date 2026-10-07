# Loan application (ASP.NET Core + React + Azure)

Customer portal for applying, uploading documents, and tracking status. The React SPA talks only to a versioned Web API. Verification runs in the background and posts to a bank-core simulator.

**System design (architecture, APIs, data, Azure, Docker):** [SYSTEM_DESIGN.md](SYSTEM_DESIGN.md)

## Local run

Use three terminals from the repo root.

1. Bank simulator: `dotnet run --project src/LoanApp.BankCore.Simulator`
2. API: `dotnet run --project src/LoanApp.Api` (http://localhost:5088)
3. Web: `cd apps/web && npm install && npm run dev` (http://localhost:5173)

OpenAPI: http://localhost:5088/openapi/v1.json

Development auth accepts any request and uses `X-User-Id` (default `dev-user`). Local files go to `blobs/` via a short-lived upload URL. An in-process worker handles verification so Azure Functions are not required locally.

Filename hints for testing rules: `unreadable`, `blur`, `expired`, `virus`, `eicar`, `.exe`.

## Docker Compose

From the repo root:

```bash
docker compose up --build
```

- Web: http://localhost:5173
- API: http://localhost:5088
- Bank simulator: http://localhost:5090

The API talks to the bank over the Compose network (`http://bank:8080`). The browser still calls the API at `http://localhost:5088`. SQLite and uploaded files persist in named volumes.

Stop with `docker compose down`. Add `-v` to drop the database and blob volumes.

## Azure

Bicep in `infra/` provisions App Service, Functions, Static Web Apps, SQL, Blob, Service Bus, Key Vault, and Application Insights with managed identities.

```bash
az deployment group create -g <rg> -f infra/main.bicep -p infra/main.bicepparam sqlAdminPassword=<password>
```

`azure.yaml` is ready for Azure Developer CLI (`azd`).

## AI later

v1 uses `IVerificationEngine` (rules) and a no-op `ILoanAssistant`. Feature flags `FeatureFlags:AiAssistant` and `FeatureFlags:AiVerificationAssist` stay off until Azure OpenAI is wired. Models must not call the bank; only `IBankCoreClient` does.
