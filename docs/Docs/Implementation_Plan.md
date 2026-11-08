# TicTacToe Live - Senior Software Engineer Analysis & Implementation Plan

> Converted from the supplied 12-page `Implementation Plan.pdf`.
>
> **Security redaction:** The source PDF contained a hard-coded SQL Server credential in its configuration examples. The literal credential has intentionally been removed from this Markdown file. Do not commit real passwords or connection strings. If the exposed password is real and remains active, rotate it.

## 1. Goal Description

Build **TicTacToe Live**, a clean, robust, educational real-time multiplayer 3x3 web game in **.NET 10**, following:

- Clean Architecture
- CQRS using `Cortex.Mediator`
- EF Core 10 with SQL Server
- ASP.NET Core MVC Razor Views
- ASP.NET Core SignalR

The game enables two guest players to play together across separate browsers/devices with real-time updates, strict server-side rule verification, optimistic concurrency guards, and resilient refresh/reconnect state recovery.

## 2. Senior Engineer Assessment: Is Everything Okay?

### 2.1 Analysis of Documentation (`docs/Docs/`)

The four documents provided in the source workspace are:

- `TicTacToe_Live_PRD.md`
- `tech-stacks.md`
- `project-architecture.md`
- `features.md`

The source assessment describes these documents as well-defined, cohesive, and production-grade in design.

- **Requirements and boundaries:** The PRD scope is bounded to an MVP (FR-01 to FR-08), deliberately excluding premature complexity such as AI opponents, user accounts/passwords, chat, spectators, and leaderboards.
- **Architecture and CQRS:** Clean separation across Domain, Application, Infrastructure, and Web layers. CQRS is scoped to command/query separation through `Cortex.Mediator`; the plan explicitly rejects MediatR and separate read databases.
- **Concurrency and security:** EF Core shadow `RowVersion` for SQL write concurrency, application-level `Revision` tracking for client UI event ordering, and guest authentication using one-time cryptographically generated tokens stored as hashes in SQL and held in browser `sessionStorage`.
- **Implementation roadmap:** `features.md` provides a sequential 10-phase execution roadmap, P00 through P09, with strict exit gates and definitions of done.

### 2.2 Existing Workspace vs. Documentation: Discrepancies and Deficiencies

The source audit identified these issues for Phase P00. They must be rechecked against the current repository before making changes; some may already have been fixed by a previous AI agent.

| Item | Workspace state found by source audit | Architecture/documentation requirement | Impact and required action |
|---|---|---|---|
| `appsettings.json` | Corrupted/duplicate JSON syntax and hard-coded credentials. **Credential value redacted.** | Valid JSON; no committed credentials; use development secrets/environment variables. | Critical: fix JSON and remove/sanitize secrets. Rotate any real credential that was exposed. |
| Package bloat | `TicTacToe.Infrastructure` and Web referenced ASP.NET Core Identity, Identity UI, NuGet Protocol, and Dynamic LINQ packages. | Strict MVP: no user accounts, identity framework, or unnecessary packages. | Remove the unneeded package references, including `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.AspNetCore.Identity.UI`, `System.Linq.Dynamic.Core`, and `NuGet.Packaging`, if they are still present and unused. |
| Test projects | No test projects existed in the audited workspace/solution. | Three NUnit test projects: `Domain.Tests`, `Application.Tests`, and `IntegrationTests`. | Scaffold all three test projects with NUnit, Shouldly, and `Autofac.Extras.Moq` where specified (test-only). |
| Build configuration | Missing `Directory.Build.props`. | Root `Directory.Build.props` enforcing .NET 10 settings and nullable reference types. | Add the shared build properties file. |
| Solution file | `TicTacToe.slnx` failed MSBuild evaluation during restore. | A clean compiling solution (`TicTacToeLive.sln` or a fixed `TicTacToe.slnx`). | Fix the solution/project definitions or provide a canonical `.sln` so `dotnet build` succeeds. |
| Folder conventions | Infrastructure contained a `Data/Migrations` placeholder. | Infrastructure folders under `Persistence/Configurations`, `Repositories`, and `Migrations`. | Align the folder structure under `TicTacToe.Infrastructure/Persistence`. |

## 3. User Review Required: Open Questions

The source PDF asks for confirmation of the following configuration choices. A continuing AI agent should first inspect the current repository and local setup. It must not ask these questions again if the choices are already resolved in code/configuration or the user's environment.

### 3.1 SQL Server Environment

The source `appsettings.json` referenced a local SQL Server database named `TicTacToe` and SQL authentication. The credential is intentionally redacted here.

The source document lists these options for local development and integration tests:

- **Option A:** Local SQL Server on `localhost` with SQL authentication, using credentials held outside committed configuration.
- **Option B:** `(localdb)\mssqllocaldb`, described as the default for Visual Studio local development.
- **Option C:** `localhost` with Windows Authentication (`Trusted_Connection=True`).

Choose the option that matches the actual local SQL Server installation. Never place the password in source control.

### 3.2 Solution Format

The source document asks whether to maintain an XML-based `.slnx` solution or use a standard Visual Studio `.sln` alongside it for IDE and CI compatibility. The prior workspace audit found the `.slnx` file failed MSBuild evaluation. Inspect the current workspace and use a solution format that restores and builds reliably in the user's installed .NET/Visual Studio tooling.

### 3.3 Security Warning: Extraneous Identity Packages

The plan says to remove these package references from `TicTacToe.Infrastructure.csproj` and `TicTacToe.Web.csproj` if still present:

- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `Microsoft.AspNetCore.Identity.UI`
- `System.Linq.Dynamic.Core`
- `NuGet.Packaging`

The project should use the documented guest-token service rather than ASP.NET Core Identity.

## 4. Architecture and Data Flow

### 4.1 Main flow

```mermaid
flowchart TD
    A[Browser clients: Player A and Player B] --> B[Razor Views + lobby.js / game.js]
    B --> C[HTTP POST requests]
    B <--> D[SignalR WebSocket / long polling]
    C --> E[MatchesController]
    C --> F[LobbyController]
    D <--> G[GameHub: /hubs/game]
    E --> H[IMatchNotifier]
    H --> G
    E --> I[Cortex.Mediator commands and queries]
    F --> I
    G --> I
    I --> J[TicTacToe.Application]
    J --> K[IGuestTokenService]
    J --> L[IUnitOfWork]
    J --> M[IGameMatchRepository and IMatchReadRepository]
    K --> N[TicTacToe.Infrastructure]
    L --> N
    M --> N
    N --> O[GuestTokenService / EFUnitOfWork / repositories]
    O --> P[GameDbContext with shadow RowVersion]
    P --> Q[(SQL Server: GameMatches and MatchPlayers)]
    D -. SignalR groups .-> R[lobby and match:{matchId}]
```

### 4.2 Component responsibilities

- **Browser clients:** Keep the match token and assigned symbol in `sessionStorage`; render Razor views and update the lobby/game UI through JavaScript.
- **`MatchesController`:** Handle match creation and joining HTTP endpoints; dispatch application commands and notify the lobby/match after successful commits.
- **`LobbyController`:** Serve the lobby page and initial UI.
- **`GameHub` (`/hubs/game`):** Subscribe clients to the lobby or a match group, authorize match access using a token, and dispatch move operations.
- **`IMatchNotifier` / `SignalRMatchNotifier`:** Keep SignalR notification concerns in the Web layer.
- **Application:** Expose CQRS commands/queries and ports such as token service, repositories, and Unit of Work.
- **Infrastructure:** Implement guest token generation/hashing, EF Core persistence, repositories, and SQL Server access.
- **SQL Server:** Persist authoritative match and player state.

### 4.3 Application use cases shown in the architecture

**Commands:**

- `CreateMatch`
- `JoinMatch`
- `MakeMove`

**Queries:**

- `GetWaitingMatches`
- `GetMatchState`

**Application ports:**

- `IGuestTokenService`
- `IUnitOfWork` with `SaveChangesAsync`
- `IGameMatchRepository`
- `IMatchReadRepository`

## 5. Phased Implementation Plan (P00 through P09)

> Execute these phases sequentially. Each phase has a verification gate. Do not implement multiple phases in one batch. The phase status in the current repository must be audited before continuing.

## Phase P00 - Solution Hygiene and Architectural Scaffolding

**Goal:** A clean, compiling multi-project solution conforming to Clean Architecture and strict package ownership.

### Tasks

1. Fix `appsettings.json` syntax and extract connection-string configuration. Do not commit credentials.
2. Clean package references by removing unnecessary Identity/NuGet packages from `TicTacToe.Infrastructure` and `TicTacToe.Web`.
3. Create root `Directory.Build.props` with `net10.0`, nullable enabled, and implicit usings.
4. Scaffold missing test projects:
   - `tests/TicTacToe.Domain.Tests` (references Domain).
   - `tests/TicTacToe.Application.Tests` (references Application and Domain; packages: NUnit, Shouldly, `Autofac.Extras.Moq`).
   - `tests/TicTacToe.IntegrationTests` (references Web, Infrastructure, Application, Domain).
5. Add test projects to the solution and ensure `dotnet restore` and `dotnet build` succeed cleanly.
6. Add `src/TicTacToe.Application/AssemblyReference.cs` for mediator scanning.
7. Add `src/TicTacToe.Infrastructure/DependencyInjection.cs` setup method.
8. Configure `Program.cs` with Serilog, MVC Controllers with Views, SignalR, and `Cortex.Mediator` registration.

### Verification Gate

- `dotnet build` succeeds with zero errors and zero warnings.
- `GET /` serves a minimal view without crashes.

## Phase P01 - Domain Aggregate, Game Rules, and Invariants

**Goal:** Pure, fully encapsulated domain logic covering all game rules, win/draw detection, and invariants without external dependencies.

### Files to implement

- `src/TicTacToe.Domain/Enums/PlayerSymbol.cs` (`X`, `O`).
- `src/TicTacToe.Domain/Enums/MatchStatus.cs` (`Waiting`, `InProgress`, `Completed`).
- `src/TicTacToe.Domain/Enums/MatchOutcome.cs` (`None`, `XWins`, `OWins`, `Draw`).
- `src/TicTacToe.Domain/Enums/GameErrorCode.cs` (`InvalidName`, `NotYourTurn`, `CellOccupied`, `InvalidCell`, `MatchFull`, etc.).
- `src/TicTacToe.Domain/Exceptions/GameRuleException.cs`.
- `src/TicTacToe.Domain/Rules/WinningLines.cs` (the 8 combinations: 3 rows, 3 columns, 2 diagonals).
- `src/TicTacToe.Domain/Entities/MatchPlayer.cs` (`Id`, `MatchId`, `DisplayName`, `Symbol`, `TokenHash`).
- `src/TicTacToe.Domain/Entities/GameMatch.cs` (aggregate root: `Create`, `Join`, `MakeMove`, `GetPlayerByTokenHash`).

### Domain invariants enforced

- Exactly 9 cells are initialized to `.`.
- The creator selects a symbol; the joining player gets the opposite symbol.
- X always moves first, even if the joining player is X.
- Exactly 2 player slots are allowed; the creator cannot join again.
- Moves are valid only while status is `InProgress` and the cell index is between 0 and 8.
- `Revision` starts at 0 and increments by 1 on a successful join and every accepted move.
- Check for a win first. If all 9 cells are filled and there is no win, the outcome is a draw. The match transitions to `Completed` with `CompletedAtUtc` set.
- Rejected operations must not mutate aggregate state.

### Tests

- `GameMatchCreateTests`: initial board, status, creator symbol, and `Revision = 0`.
- `GameMatchJoinTests`: opposite symbol assignment, X always starts, rejection when already full.
- `GameMatchMoveTests`: turn switching, cell occupancy check, out-of-turn rejection.
- `GameMatchOutcomeTests`: all 8 winning combinations for both X and O, board draw detection, and move rejection on completed games.

### Verification Gate

`dotnet test tests/TicTacToe.Domain.Tests` passes 100%.

## Phase P02 - EF Core SQL Persistence, Concurrency, and Guest Security

**Goal:** Authoritative state persistence in SQL Server, shadow `RowVersion` optimistic locking, and cryptographic token issuance.

### Application contracts

- `IGameMatchRepository`: `GetByIdAsync(Guid)`, `AddAsync(GameMatch)`.
- `IMatchReadRepository`: `GetWaitingAsync()`, `GetAuthorizedStateAsync(Guid, string tokenHash)`.
- `IUnitOfWork`: `SaveChangesAsync(CancellationToken)`.
- `IGuestTokenService`: `GuestTokenIssue Issue()`, `string Hash(string rawToken)`.
- Exceptions: `MatchNotFoundException`, `MatchAccessDeniedException`, `MatchConflictException`.
- DTOs: `MatchStateDto`, `WaitingMatchDto`, `CreateMatchResult`, `JoinMatchResult`, `GuestTokenIssue`.
- `MatchStateMapper`: pure projection that does not expose secrets.

### Infrastructure implementation

- `GameDbContext`: entity configurations using Fluent API.
- `GameMatchConfiguration`: configure the table, 9-character `BoardState`, enum conversions, and shadow `.Property<byte[]>("RowVersion").IsRowVersion()`.
- `MatchPlayerConfiguration`: maximum length 20 for `DisplayName`; index on `TokenHash`.
- `GameMatchRepository`: load the tracked aggregate with `.Include(m => m.Players)`.
- `MatchReadRepository`: use `AsNoTracking()` queries returning read DTOs only.
- `EfUnitOfWork`: translate `DbUpdateConcurrencyException` into `MatchConflictException`.
- `GuestTokenService`: generate 32 cryptographically secure random bytes (base64url) and compute SHA-256 hashes (hex/base64).
- EF Core migration: generate and apply the initial migration to SQL Server.

### Tests

- Repository tracking and persistence tests.
- Concurrency tests using two `DbContext` instances loading the same aggregate; the second commit triggers `MatchConflictException`.
- Token hashing determinism and entropy verification.

### Verification Gate

Migration applied; unit and integration persistence tests pass.

## Phase P03 - FR-01/FR-02: Guest Identity, Match Creation, and Waiting Room

**Goal:** Player A enters a name, chooses a symbol, creates a match, stores a session token, and views an authorized waiting room.

### Application

- `CreateMatchCommand(DisplayName, CreatorSymbol)` and `CreateMatchCommandHandler`.
- `GetMatchStateQuery(MatchId, PlayerToken)` and `GetMatchStateQueryHandler`.
- Invariant: `SaveChanges` is called exactly once for valid creation and zero times for invalid creation.

### Web

- `CreateMatchInputModel` with data annotations and CSRF protection.
- `MatchesController.Create`: dispatch command and return JSON with a one-time raw token and match ID.
- `MatchesController.Room`: serve a generic Razor shell (`Room.cshtml`) without credentials.
- `GameHub.SubscribeMatch(matchId, token)`: hash the token, load state via query, add the connection to `match:{matchId}` group, and return a snapshot.
- Client JavaScript (`game.js`): persist `{ token, symbol }` to `sessionStorage`, start SignalR, call `SubscribeMatch`, and render “Waiting for opponent...”.

### Tests

- `CreateMatchCommandHandlerTests`: name trimming, symbol validation, and single save.
- `GetMatchStateQueryHandlerTests`: authorized lookup and invalid-token rejection.
- Integration tests for `POST /matches` and `SubscribeMatch`.

### Verification Gate

A single browser can create a game as X or O and view the authorized waiting room.

## Phase P04 - FR-03: Available Matches and Real-Time Lobby

**Goal:** Player B sees available waiting matches live without refreshing.

### Application

- `GetWaitingMatchesQuery()` and `GetWaitingMatchesQueryHandler` (read-only; zero saves).

### Web

- `IMatchNotifier` and `SignalRMatchNotifier` (Web layer only, using `IHubContext<GameHub>`).
- `GameHub.SubscribeLobby()`: add the connection to the `lobby` group and return an initial waiting-match snapshot.
- `MatchesController.Create`: after a successful SQL commit, call the notifier to broadcast `WaitingMatchesChanged` to `lobby`.
- `LobbyController.Index` and `Views/Lobby/Index.cshtml`: render a waiting-match list/table/cards with a Join button and an empty-state message.
- `wwwroot/js/lobby.js`: manage the SignalR connection to `/hubs/game`, listen for `WaitingMatchesChanged`, and update the DOM safely.

### Tests

- `GetWaitingMatchesQueryHandlerTests`.
- Integration test: Client B subscribed to the lobby receives `WaitingMatchesChanged` when Client A creates a match.

### Verification Gate

Creating a game in Browser A instantly updates the open lobby in Browser B.

## Phase P05 - FR-04: Join Match and Transition to Playing

**Goal:** Player B joins a waiting match from the lobby, receives the opposite symbol, and both browsers seamlessly transition to the game board.

### Application

- `JoinMatchCommand(MatchId, DisplayName, ExistingPlayerToken?)` and handler.
- Invariants: reject self-join (if the existing token matches the creator), enforce a maximum of 2 players, derive the opposite symbol, transition status to `InProgress`, and save once.

### Web

- `JoinMatchInputModel` and `POST /matches/{matchId}/join`.
- `SignalRMatchNotifier`: broadcast `WaitingMatchesChanged` to `lobby` and `MatchStateChanged` to `match:{matchId}`.
- `game.js`: listen for `MatchStateChanged`; when status becomes `InProgress`, transition the UI from waiting screen to the 3x3 game board.
- `lobby.js`: submit the join POST, store the token in `sessionStorage`, and navigate to `/matches/{matchId}`.

### Tests

- `JoinMatchCommandHandlerTests`: success path, opposite symbol, self-join rejection, and already-full rejection.
- SQL concurrency test: two simultaneous join calls for the same match; exactly one succeeds, one gets a 409 conflict, and no third player is saved.

### Verification Gate

Two browsers join together; both see an empty 3x3 board, player names, and the X turn indicator.

## Phase P06 - FR-05: Server-Authorized Moves and Live Board Synchronization

**Goal:** Interactive 3x3 board with server-verified turns and live synchronization between players.

### Application

- `MakeMoveCommand(MatchId, PlayerToken, CellIndex)` and handler.
- Validate caller token membership, turn order, and an unoccupied cell index from 0 to 8; apply the move, increment `Revision`, and save once.

### Web

- `GameHub.MakeMove(matchId, token, cellIndex)`: dispatch the command and broadcast updated `MatchStateChanged` with the new `Revision` after commit.
- `game.js`: render 9 clickable cells; disable clicks when it is not the player's turn or a cell is occupied. Ignore out-of-order events where `event.Revision < currentRevision`.
- `css/game.css`: clean, touch-friendly, responsive board layout.

### Tests

- `MakeMoveCommandHandlerTests`: alternating turns, occupied-cell rejection, unauthorized-player rejection, and save verification.
- Hub integration tests: moves broadcast to the opponent; rejected moves emit safe errors and zero broadcasts.

### Verification Gate

Browsers take turns placing symbols in real time; invalid clicks are rejected on the server.

## Phase P07 - FR-06: Outcomes and New-Match Flow

**Goal:** Accurate win and draw detection, a locked board on game over, and an option to return to the lobby/start a new match.

### Implementation

- The final move transitions the aggregate to `Completed` with an `Outcome` (`XWins`, `OWins`, or `Draw`) and `CompletedAtUtc`.
- `game.js`: display a banner with the winner's name/symbol or “It's a draw”; lock all board cells against further input.
- Navigation buttons: “Back to Lobby” and “Create New Match” (generates a fresh match ID and clean board).
- Domain/handler tests verify all 8 winning lines and draw scenarios.

### Verification Gate

Complete games end in a win or draw on both screens simultaneously, and no moves are accepted after completion.

## Phase P08 - FR-07/FR-08: Reconnection, Conflict Recovery, and User Feedback

**Goal:** Refreshing the browser or a brief disconnection restores the exact match state without losing data; errors provide clear user feedback.

### Implementation

- SignalR auto-reconnect: `withUrl(...).withAutomaticReconnect().build()`.
- Reconnection hook: on reconnect, the browser retrieves the match token from `sessionStorage` and re-invokes `SubscribeMatch(matchId, token)`.
- Disconnection indicator: “Connecting...”, “Connection lost. Reconnecting...”, and “Connected”.
- Centralized safe HTTP/Hub error mapping:
  - `400`: validation or game-rule error (e.g. square occupied).
  - `403` / `404`: unauthorized session or match not found.
  - `409`: concurrency conflict; prompts automatic state resynchronization.
  - `500`: generic internal error.
- Serilog configuration in Web for structured logging, with tokens sanitized/redacted.

### Verification Gate

Refreshing Browser A during a live match restores the exact board state and turn from SQL Server.

## Phase P09 - Integration Verification and Classroom Demo Gate

**Goal:** Full automated test suite passing and a complete two-browser demonstration verified against the PRD checklist.

### Automated verification

- Run `dotnet test TicTacToeLive.sln` across all three test projects (use the actual solution filename in the current workspace).
- Complete the integration test suite against a live SQL Server test database.

### Manual demo checklist

1. Open Browser A and Browser B.
2. Player A creates a match as X.
3. Browser B sees the match in the lobby in real time.
4. Player B joins as O; both screens transition to the live board.
5. Play valid moves; both boards update seamlessly.
6. Attempt an invalid move (out of turn / occupied cell); it is rejected.
7. Refresh Browser A; match, board, and turn are accurately restored.
8. Complete the match to a win or draw; the game-over banner is rendered.
9. Click “New Match”; a new game starts.

## 6. Verification Plan

### 6.1 Automated tests (PowerShell)

```powershell
# Restore and build
dotnet restore
dotnet build --no-restore

# Run Domain unit tests
dotnet test tests/TicTacToe.Domain.Tests/TicTacToe.Domain.Tests.csproj

# Run Application CQRS handler unit tests
dotnet test tests/TicTacToe.Application.Tests/TicTacToe.Application.Tests.csproj

# Run integration tests (SQL Server, MVC, SignalR)
dotnet test tests/TicTacToe.IntegrationTests/TicTacToe.IntegrationTests.csproj
```

### 6.2 Manual verification

1. Open two browser windows side by side (e.g. Chrome and Edge, or an Incognito window).
2. Create a match as Player A (`Alice`, X); verify the waiting screen.
3. Observe Browser B's lobby; verify `Alice (X)` appears in real time.
4. Join as Player B (`Bob`); verify both show the 3x3 grid, Alice as X, Bob as O, and Alice's turn.
5. Make moves alternately; verify both screens update without reload.
6. Refresh Browser A; verify board state and turn are preserved.
7. Finish the match; verify the win line is highlighted and the result is displayed.

---

## 7. Notes for Continuing Implementation

This file describes the intended plan and records the findings from the source PDF. It does **not** prove which phases are currently complete. A later AI agent must inspect the actual repository, Git history, build, tests, configuration, and implementation before deciding where to continue.

Recommended continuation rules:

1. Audit first; do not immediately generate code.
2. Classify each phase as `Not Started`, `Partial`, `Implemented - Unverified`, or `Verified`, and cite concrete repository evidence.
3. Continue only the earliest incomplete phase, unless an earlier phase contains a blocker.
4. Implement exactly one phase at a time.
5. Run appropriate automated tests and provide the user with a manual test checklist.
6. Stop and wait for the user's manual test result before starting the next phase.
7. Create one Git commit per phase only after the user confirms that phase's manual verification passed. Do not push unless explicitly requested.
8. Provide a short explanation of the architecture, key files, request/data flow, test coverage, and anything the user should understand before accepting the phase.

