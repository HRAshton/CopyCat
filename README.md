# CopyCat

Telegram channel duplication service built as a modular .NET application with:
- Blazor admin UI
- PostgreSQL persistence
- background workers
- Telegram MTProto integration through `WTelegramClient`

## Current State

This repository is now usable as a first-pass private admin tool.

You can:
- create Telegram sessions from the UI using real phone/API credentials
- start Telegram login from the UI
- submit verification code and 2FA password from the UI
- discover channels from connected sessions
- mark channels as sources
- create target channels from the UI
- create source-to-target mappings from the UI
- queue one-shot backfills from the UI
- run background discovery, control-operation, live-ingest, filtering, and forwarding workers

Still incomplete or MVP-grade:
- filter editor is not yet a full visual builder
- rewrite editor is not yet a full visual builder
- media/albums forwarding is simplified
- worker concurrency/idempotency is acceptable for a private prototype, not hardened production

## Stack

- .NET 9
- ASP.NET Core Blazor
- MudBlazor
- EF Core
- PostgreSQL
- WTelegramClient
- Docker Compose

## Run

### Docker Compose

```bash
docker compose up --build
```

Then open:

```text
http://localhost:8080
```

### Production Compose

Use the production file on the server:

```bash
cp .env.production.example .env.production
# edit .env.production
docker compose --env-file .env.production -f docker-compose.prod.yml up -d --build
```

Recommended environment variables:
- `COPYCAT_DB_NAME`
- `COPYCAT_DB_USER`
- `COPYCAT_DB_PASSWORD`
- `COPYCAT_APP_NAME`
- `COPYCAT_WEB_PORT`

### Local CLI

Start PostgreSQL first, then run:

```bash
dotnet run --project src/CopyCat.Web
dotnet run --project src/CopyCat.Worker
```

## Build And Test

```bash
dotnet restore CopyCat.slnx
dotnet build CopyCat.slnx --no-restore
dotnet test CopyCat.slnx --no-build
```

## Coverage

Use the test collector plus local `reportgenerator` for HTML:

```bash
dotnet tool restore
dotnet test CopyCat.slnx --no-build --collect:"XPlat Code Coverage" --results-directory coverage/test-results
dotnet tool run reportgenerator "-reports:coverage/test-results/**/coverage.cobertura.xml" "-targetdir:coverage/report" "-reporttypes:Html"
```

If you prefer to build and test in one step:

```bash
dotnet tool restore
dotnet test CopyCat.slnx --collect:"XPlat Code Coverage" --results-directory coverage/test-results -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura
dotnet tool run reportgenerator "-reports:coverage/test-results/**/coverage.cobertura.xml" "-targetdir:coverage/report" "-reporttypes:Html"
```

That produces:
- Cobertura files under `coverage/test-results`
- an HTML report at `coverage/report/index.html`

If you only need to rebuild the HTML from existing Cobertura files:

```bash
dotnet tool run reportgenerator "-reports:coverage/test-results/**/coverage.cobertura.xml" "-targetdir:coverage/report" "-reporttypes:Html"
```

## Database Migrations

The application now applies EF Core migrations on startup through:
- `src/CopyCat.Web/Program.cs`
- `src/CopyCat.Worker/Program.cs`

The baseline migration lives under:
- `src/CopyCat.Infrastructure/Data/Migrations`

To add a new migration locally:

```bash
dotnet ef migrations add <Name> --project src/CopyCat.Infrastructure --startup-project src/CopyCat.Web --context CopyCatDbContext
```

To update the database manually:

```bash
dotnet ef database update --project src/CopyCat.Infrastructure --startup-project src/CopyCat.Web --context CopyCatDbContext
```

## Telegram Credentials

You need Telegram MTProto user credentials, not a bot token.

Required values:
- phone number of the Telegram user account
- `api_id`
- `api_hash`

These are used by [src/CopyCat.Telegram/Clients/WTelegramGateway.cs](/A:/repos/CopyCat/src/CopyCat.Telegram/Clients/WTelegramGateway.cs).

## Session Workflow

Go to `/sessions`.

### Step 1: Create Session

Fill in:
- `Display Name`
- `Phone Number`
- `API ID`
- `API Hash`

Then click `Create Session`.

The new session row will appear in the table and its internal GUID will be shown in the `Id` column.

### Step 2: Request Telegram Login Code

Click `Login` on the session row.

That tells Telegram to start the login flow for that session's phone number.

The app will:
- store the session
- set its status to waiting for code or password
- show any backend error in the `Error` column

### Step 3: Submit The Code

In the authentication panel on the same page:
- paste the session GUID into `Session Id`
- paste the Telegram code into `Verification Code`
- click `Submit Code`

Shortcut:
- click `Use ID` on the row to fill the session ID field automatically

### Step 4: Submit 2FA Password If Needed

If the account has Telegram cloud password enabled:
- enter the password in `2FA Password`
- click `Submit Password`

If successful, the session should move to `Connected`.

## Channels Workflow

Go to `/channels`.

### Discover channels

Click `Discover Now`.

This requires at least one connected Telegram session.

### Mark a source channel

Click `Toggle Source` on a discovered channel.

### Create a target channel

Use the right-side panel:
1. select a source channel
2. enter or accept the suggested target title
3. click `Create Target`

This creates the Telegram target channel through the same connected Telegram user session and stores it locally.

## Mappings Workflow

Go to `/mappings`.

Use the form to select:
- source channel
- target channel
- forwarding mode
- enabled/live-forwarding flags

Then click `Save Mapping`.

That creates the route the workers will use.

### Run a one-time backfill

Use the `One-Time Backfill` panel on the same page:

1. select an existing mapping
2. choose how many messages to import
3. click `Run Backfill`

This queues a control operation that imports older messages once. After import, the normal filtering and
forwarding pipeline processes those stored messages.

## What Happens In The Background

The worker service contains:
- channel discovery worker
- Telegram control operation worker
- live ingest worker
- filtering worker
- forwarding worker

Relevant files:
- [src/CopyCat.Worker/Services/ChannelDiscoveryWorker.cs](/A:/repos/CopyCat/src/CopyCat.Worker/Services/ChannelDiscoveryWorker.cs)
- [src/CopyCat.Worker/Services/TelegramControlOperationWorker.cs](/A:/repos/CopyCat/src/CopyCat.Worker/Services/TelegramControlOperationWorker.cs)
- [src/CopyCat.Worker/Services/LiveMessageIngestWorker.cs](/A:/repos/CopyCat/src/CopyCat.Worker/Services/LiveMessageIngestWorker.cs)
- [src/CopyCat.Worker/Services/FilteringWorker.cs](/A:/repos/CopyCat/src/CopyCat.Worker/Services/FilteringWorker.cs)
- [src/CopyCat.Worker/Services/ForwardingWorker.cs](/A:/repos/CopyCat/src/CopyCat.Worker/Services/ForwardingWorker.cs)

## Important Limitations

### Telegram forwarding

Text forwarding works best right now.

The forwarding layer is still simplified for:
- albums
- attachment reconstruction
- exact copy-as-is semantics
- reliable Telegram target message ID capture for forwarded messages

### Filters and rewrites

The engine exists, but the UI is still basic. You can store sample drafts, but the editor is not yet a full admin experience.

## Recommended Next Work

1. Add richer session diagnostics in the UI.
2. Improve media forwarding and album handling.
3. Add stronger retry/error classification for Telegram API failures.
4. Add operational cleanup and retention jobs for old history and audit data.
5. Introduce a dedicated reverse-proxy deployment example for HTTPS.

## Useful Files

- [src/CopyCat.Web/Components/Pages/Sessions.razor](/A:/repos/CopyCat/src/CopyCat.Web/Components/Pages/Sessions.razor)
- [src/CopyCat.Web/Components/Pages/Channels.razor](/A:/repos/CopyCat/src/CopyCat.Web/Components/Pages/Channels.razor)
- [src/CopyCat.Web/Components/Pages/Mappings.razor](/A:/repos/CopyCat/src/CopyCat.Web/Components/Pages/Mappings.razor)
- [src/CopyCat.Telegram/Clients/WTelegramGateway.cs](/A:/repos/CopyCat/src/CopyCat.Telegram/Clients/WTelegramGateway.cs)
- [src/CopyCat.Infrastructure/Services](/A:/repos/CopyCat/src/CopyCat.Infrastructure/Services)
- [docker-compose.yml](/A:/repos/CopyCat/docker-compose.yml)
