# TicTacToe Live — Phased Feature Implementation Plan

**Document:** `features.md`  
**Status:** Implementation-ready MVP plan  
**Product:** TicTacToe Live  
**Target:** .NET 10 (`net10.0`)  
**Architecture:** Clean Architecture, single deployable modular monolith, feature-based CQRS  
**Source of truth:** `TicTacToe_Live_PRD.md` (product requirements), `project-architecture (1).md` v2.0 (approved implementation baseline), `tech-stacks.md` v1.0 (technology choices)

> **Agent instruction:** Implement **one phase at a time in the order below**. Complete its checklist, tests, and exit gate before starting the next phase. Do not expand MVP scope or bypass a failing gate.

---

## 1. Non-negotiable implementation contract

### 1.1 Technology and architecture

| Concern | Required choice |
|---|---|
| Runtime | C# / .NET 10 (`net10.0`) |
| Server | ASP.NET Core MVC 10, Razor Views |
| UI | Bootstrap 5, minimal custom CSS, vanilla JavaScript |
| Real-time | ASP.NET Core SignalR, one `GameHub` |
| Database | SQL Server, EF Core 10, Code First migrations |
| CQRS | **Cortex.Mediator**, not MediatR |
| Writes | Focused repository + one Unit of Work; one commit per successful command |
| Reads | Separate query handlers and read repository; same SQL Server database |
| Production DI | Built-in `IServiceCollection` |
| Logging | Serilog configured in Web; `ILogger<T>` elsewhere as needed |
| Testing | NUnit, Shouldly, `Autofac.Extras.Moq` in **Application.Tests only**, real SQL Server integration tests |
| Deployment | One ASP.NET Core application; no separate API service or worker |

**Source precedence:** The approved architecture v2.0 takes precedence where `tech-stacks.md` is less specific. In particular, `Autofac.Extras.Moq` is **test-only**; it must never replace built-in production dependency injection. Keep NuGet versions pinned and compatible with .NET/EF Core 10; use package versions confirmed by restore/build, rather than assuming observed versions are the newest.

### 1.2 Layer boundaries

- **Domain:** `GameMatch`, `MatchPlayer`, enums, game invariants, win/draw rules. No EF Core, Cortex, HTTP, SignalR, repositories, or application DTOs.
- **Application:** Cortex commands/queries, handlers, DTOs, guest authorization, persistence/security abstractions. No `DbContext`, controllers, `IHubContext`, MVC, or concrete Serilog types.
- **Infrastructure:** SQL Server/EF mappings, repositories, Unit of Work, guest-token implementation, migrations. No game rules or SignalR.
- **Web:** MVC routes, Razor UI, client JavaScript, SignalR hub/groups, post-commit notifications, HTTP/Hub error handling, DI composition and Serilog setup. No direct EF/repository access or game-rule decisions.

**Project references:**

```text
TicTacToe.Domain              -> none
TicTacToe.Application         -> Domain
TicTacToe.Infrastructure      -> Application, Domain
TicTacToe.Web                 -> Application, Infrastructure
TicTacToe.Domain.Tests        -> Domain
TicTacToe.Application.Tests   -> Application, Domain
TicTacToe.IntegrationTests    -> Web, Infrastructure, Application, Domain
```

### 1.3 Strict MVP scope

Implement only FR-01 through FR-08: guest names, create, waiting lobby, join, live turns/moves, win/draw, refresh/reconnect, helpful feedback, and manually starting a **new** match through the lobby.

**Do not implement:** authentication/accounts, AI opponent, chat, spectators, tournaments, ranking/leaderboard, rematch, history, cancellation/expiry, share codes, Redis, message brokers, microservices, event sourcing, separate read database, React/Angular, or a generic CRUD framework.

### 1.4 Agent execution rules

1. Read the three source documents before coding; use the existing solution if already present rather than recreating it.
2. Create/modify **only** files required by the current phase and its tests; preserve the documented directory structure.
3. Implement Domain invariant first, then handler/repository as applicable, then Web integration, then feature tests. Do not place business logic in controller, hub, or JavaScript.
4. Use nullable reference types, async I/O, `CancellationToken`, constructor injection, and focused interfaces.
5. Every successful **Create**, **Join**, or **MakeMove** command calls `SaveChangesAsync` **exactly once**. Every rejected command calls it **zero times**. Queries never save.
6. Broadcast only **after** a successful SQL commit. Never send or log guest tokens in events, URLs, Razor HTML, or structured logs.
7. At each phase exit: run `dotnet restore` if dependencies changed, `dotnet build`, relevant `dotnet test` suites, inspect the diff, and document any blocked gate.
8. Never mark a checkbox complete based only on generated code; verify through automated tests and, where specified, two-browser behavior.
9. Keep SQL Server integration tests real. Do not replace SQL Server/`rowversion` behavior with EF Core InMemory or SQLite and claim equivalent coverage.
10. If an implementation detail is not specified by the sources, use the smallest compatible design. Document the choice in code/PR rather than adding a new product capability.

### 1.5 Shared definition of done (all phases)

- [ ] All implementation tasks in the phase are complete.
- [ ] Relevant Domain/Application unit tests pass; newly introduced handlers cover success, failure, authorization (where relevant), and save/no-save interactions.
- [ ] Required SQL Server/HTTP/SignalR integration tests for the phase pass when the feature requires them.
- [ ] `dotnet build` succeeds with no new warnings caused by the phase.
- [ ] `dotnet test` succeeds for configured test suites; unavailable SQL Server is recorded as **blocked**, not **passed**.
- [ ] No guest token, token hash, `DbContext`, `IQueryable`, or Domain entity leaks across prohibited boundaries.
- [ ] Changes can be explained and reviewed; do not proceed until the phase exit gate passes.

---

## 2. Feature-to-phase roadmap

| Phase | Goal | PRD coverage | Deliverable / dependency |
|---|---|---|---|
| **P00** | Solution/bootstrap | Foundation | Compiling four-layer solution, tests, DI, basic MVC host |
| **P01** | Domain model and rules | FR-01, FR-02, FR-04, FR-05, FR-06 rules | Fully tested `GameMatch` aggregate |
| **P02** | SQL persistence and guest security | Security/persistence for all FRs | EF migrations, repositories, rowversion, token service |
| **P03** | Create a match + waiting room | FR-01, FR-02; state read needed for FR-07 | Create HTTP flow, authorized match subscription, waiting screen |
| **P04** | Waiting lobby, live discovery | FR-03; FR-02 live visibility | Waiting query, lobby group, automatic create notifications |
| **P05** | Join a match | FR-01, FR-04; FR-03 removal | Join HTTP flow, start notification, automatic room transition |
| **P06** | Live game moves | FR-05 | Authorized Hub moves, persisted state, live board synchronization |
| **P07** | Outcomes and new-match flow | FR-06 | Shared win/draw UI, completed board disabled, back/new navigation |
| **P08** | Reconnection, conflict recovery, feedback | FR-07, FR-08 | Automatic re-subscribe, saved-state recovery, safe errors |
| **P09** | Final test/quality/demo gate | FR-01–FR-08 | Full SQL/HTTP/Hub verification and two-browser MVP demo |

**Dependency rule:** P00 → P01 → P02 → P03 → P04 → P05 → P06 → P07 → P08 → P09. Domain win/draw logic is implemented in P01 but becomes visible to users in P07. Basic error handling is added with each endpoint and finalized in P08; it must not be deferred if needed for an earlier feature to work safely.

---

## 3. Stable contracts that all phases must follow

### 3.1 Domain aggregate (source-defined)

```text
GameMatch
  Id: Guid
  Status: MatchStatus                  # Waiting | InProgress | Completed
  BoardState: string                   # nine chars: '.' | 'X' | 'O'
  CurrentTurn: PlayerSymbol            # X starts
  Outcome: MatchOutcome                # None | XWins | OWins | Draw
  Revision: int                        # 0 at creation; +1 per successful join/move
  CreatedAtUtc: DateTimeOffset
  CompletedAtUtc: DateTimeOffset?
  Players: IReadOnlyCollection<MatchPlayer>

MatchPlayer
  Id: Guid
  MatchId: Guid
  DisplayName: string                  # trimmed, 1–20 characters
  Symbol: PlayerSymbol                 # X | O
  TokenHash: string                    # never delivered to browser
```

Only these aggregate methods may mutate game state:

```text
GameMatch.Create(matchId, creatorPlayerId, name, symbol, creatorTokenHash)
GameMatch.Join(joinerPlayerId, name, joinerTokenHash)
GameMatch.MakeMove(playerId, cellIndex)
GameMatch.GetPlayerByTokenHash(tokenHash)
```

Invariants: at most two players, distinct symbols, first turn always X, board indexes 0–8, valid unoccupied cell, authenticated match membership, correct turn, no moves before start/after completion, exactly eight winning lines, draw on full board, no mutation on rejected moves. Throw `GameRuleException(GameErrorCode)` for rule failures.

**Win lines** (zero-based indices): `(0,1,2)`, `(3,4,5)`, `(6,7,8)`, `(0,3,6)`, `(1,4,7)`, `(2,5,8)`, `(0,4,8)`, `(2,4,6)`.

**Concurrency:** `Revision` is an application-visible ordering number, **not** the SQL concurrency token. EF Core's private shadow `RowVersion` is the database concurrency token; Domain must not contain a SQL-specific `byte[]` property.

### 3.2 CQRS requests (exact architectural names)

| Request | Interface | Input | Output |
|---|---|---|---|
| `CreateMatchCommand` | `ICommand<CreateMatchResult>` | `(DisplayName, CreatorSymbol)` | Created match ID, raw guest token, initial state |
| `JoinMatchCommand` | `ICommand<JoinMatchResult>` | `(MatchId, DisplayName, ExistingPlayerToken?)` | Match ID, raw guest token, started state |
| `MakeMoveCommand` | `ICommand<MatchStateDto>` | `(MatchId, PlayerToken, CellIndex)` | Committed state |
| `GetWaitingMatchesQuery` | `IQuery<IReadOnlyList<WaitingMatchDto>>` | `()` | Public waiting match summaries |
| `GetMatchStateQuery` | `IQuery<MatchStateDto>` | `(MatchId, PlayerToken)` | Authorized persisted state |

Use `Cortex.Mediator.Commands` and `Cortex.Mediator.Queries` contracts. Register handlers via `AddCortexMediator` scanning `typeof(TicTacToe.Application.AssemblyReference)` from Web. Do not use MediatR or dispatch concrete handler classes from another handler.

### 3.3 Persistence interfaces (exact signatures)

```csharp
public interface IGameMatchRepository
{
    Task<GameMatch?> GetByIdAsync(Guid matchId, CancellationToken ct);
    Task AddAsync(GameMatch match, CancellationToken ct);
}

public interface IMatchReadRepository
{
    Task<IReadOnlyList<WaitingMatchDto>> GetWaitingAsync(CancellationToken ct);
    Task<MatchStateDto?> GetAuthorizedStateAsync(
        Guid matchId, string playerTokenHash, CancellationToken ct);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
}

public sealed record GuestTokenIssue(string RawToken, string TokenHash);

public interface IGuestTokenService
{
    GuestTokenIssue Issue();
    string Hash(string rawToken);
}
```

A write repository loads tracked `GameMatch` **with players**. Read queries use `AsNoTracking()` and project DTOs without token hashes. Repository methods never commit; `EfUnitOfWork` shares the scoped `GameDbContext` with both repositories.

### 3.4 HTTP, Hub, groups and events (exact architecture)

| Entry point | Handler | Behavior |
|---|---|---|
| `GET /` | `LobbyController.Index` | Razor lobby |
| `GET /matches/{matchId}` | `MatchesController.Room` | Razor waiting/game page; never include token in URL |
| `POST /matches` | `MatchesController.Create` | Create command → JSON result → notify lobby after save |
| `POST /matches/{matchId}/join` | `MatchesController.Join` | Join command → JSON result → notify lobby and match after save |
| `/hubs/game` | `GameHub` | Only SignalR hub |

| Client → Hub method | Operation |
|---|---|
| `SubscribeLobby()` | Join `lobby` group and return latest waiting matches |
| `SubscribeMatch(matchId, token)` | Authorize with `GetMatchStateQuery`, join `match:{matchId}` group, return saved match state |
| `MakeMove(matchId, token, cellIndex)` | Execute `MakeMoveCommand`, then broadcast the committed state |

| Server → client event | Payload |
|---|---|
| `WaitingMatchesChanged` | Latest `IReadOnlyList<WaitingMatchDto>` |
| `MatchStateChanged` | Latest `MatchStateDto` including `Revision` |

**Notification implementation:** `IMatchNotifier` and `SignalRMatchNotifier` live only in Web. `SignalRMatchNotifier` uses `IHubContext<GameHub>`; controllers or Hub call it **after** their command returns successfully. Application cannot publish SignalR notifications.

### 3.5 DTO data requirements and browser identity

The source documents establish DTO **names**, results and behavior, but do **not** fix their full property lists or JSON casing. To make implementation consistent, the agent should define the **minimum safe data** below in Application; the final property spelling/JSON policy must be made uniform in MVC, SignalR, and JavaScript.

| DTO | Minimum safe fields |
|---|---|
| `MatchStateDto` | Match ID, status, nine-cell board, current turn, outcome, revision, public player names/symbols; completion time if needed for display |
| `WaitingMatchDto` | Match ID, creator display name, creator symbol; optionally creation time |
| `CreateMatchResult` | Match ID, **one-time** raw guest token, initial `MatchStateDto`; caller's assigned symbol if required by the client |
| `JoinMatchResult` | Match ID, **one-time** raw guest token, started `MatchStateDto`; caller's assigned symbol if required by the client |
| `GuestTokenIssue` | Raw token and its hash; **internal only**, not a public match-state DTO |

**Important:** `MatchStateDto` and `WaitingMatchDto` are **not personalized** and never include a token or token hash, because their snapshots are shared with match/lobby clients. The browser stores **its own symbol together with its match token** in `sessionStorage` on successful create/join, keyed by match ID (e.g., `tictactoe:match:<id>`). This lets it render “Your turn” after refresh without trusting the browser to authorize a move; **every actual move still resolves the player from the server-side token hash**. Duplicate display names must remain valid.

**Do not add** an unauthenticated match-state endpoint or send an access token as a query string. A match's Razor route may render a generic shell; it must not expose private match state before `SubscribeMatch` authorizes the client.

### 3.6 HTTP/Hub error policy

| Failure | HTTP status | Client handling |
|---|---|---|
| Invalid guest name, invalid symbol, invalid/out-of-range cell, other validation/rule error | `400` | Show clear safe message; preserve current board |
| Missing/incorrect guest credential | `403` (uniform `404` is acceptable for private lookups) | Disable play and show access error |
| Match does not exist | `404` | Show not-found state; offer lobby |
| Match full, no longer waiting, SQL stale-write conflict | `409` | Show conflict; resynchronize authorized current state |
| Unexpected exception | `500` | Safe generic message; log diagnostic detail without secrets |

Map Domain/Application exceptions centrally in Web. SignalR uses equivalent **safe** `HubException` messages/codes. Never expose stack traces, SQL details, raw tokens, hashes, or repository internals.

---

## 4. P00 — Initialize solution and architecture foundation

**Depends on:** None.  
**Goal:** A compiling, correctly referenced solution and minimal running MVC host, without game behavior.

### Implementation tasks

- [ ] Create or verify `TicTacToeLive.slnx` and `Directory.Build.props` (nullable enabled, consistent .NET 10 settings).
- [ ] Create four production projects with the exact names: `TicTacToe.Domain`, `TicTacToe.Application`, `TicTacToe.Infrastructure`, `TicTacToe.Web`.
- [ ] Create `TicTacToe.Domain.Tests`, `TicTacToe.Application.Tests`, `TicTacToe.IntegrationTests` as NUnit projects.
- [ ] Set project references exactly as in §1.2; ensure Domain remains dependency-free.
- [ ] Add only necessary packages: Cortex.Mediator in Application/Web, EF Core SQL Server in Infrastructure, EF Core design-time tooling, Serilog in Web, and NUnit/Shouldly/test packages in tests.
- [ ] Add `Autofac.Extras.Moq`/Moq to `TicTacToe.Application.Tests` only; use built-in DI in production.
- [ ] Pin compatible package versions using `Directory.Packages.props` if adopting central package management.
- [ ] Add `AssemblyReference.cs` to Application for mediator scanning.
- [ ] Implement `Infrastructure/DependencyInjection.cs` registration entry point without business-specific behavior yet.
- [ ] Configure `Program.cs` with MVC, Razor, SignalR, Cortex, Infrastructure service registration, Serilog, middleware, HTTPS redirection and a `/hubs/game` mapping once the Hub stub exists.
- [ ] Add minimal `LobbyController.Index`, `Views/Lobby/Index.cshtml`, shared Razor layout, Bootstrap 5, and application CSS/JS placeholders.
- [ ] Set up `appsettings.json` without credentials; document development configuration via environment variables/user secrets.
- [ ] Prepare solution-level build/test commands and a test database connection strategy that **cannot target production data**.

### Required files (initial set)

```text
TicTacToeLive.slnx
Directory.Build.props
src/TicTacToe.Application/AssemblyReference.cs
src/TicTacToe.Infrastructure/DependencyInjection.cs
src/TicTacToe.Web/Program.cs
src/TicTacToe.Web/Controllers/LobbyController.cs
src/TicTacToe.Web/Views/Lobby/Index.cshtml
src/TicTacToe.Web/Views/Shared/_Layout.cshtml
src/TicTacToe.Web/appsettings.json
tests/{TicTacToe.Domain.Tests,TicTacToe.Application.Tests,TicTacToe.IntegrationTests}/
```

### Tests and exit gate

- [ ] `dotnet restore` succeeds.
- [ ] `dotnet build TicTacToeLive.slnx` succeeds.
- [ ] `dotnet test TicTacToeLive.slnx` discovers test projects; no intentionally failing placeholder tests.
- [ ] `dotnet run --project src/TicTacToe.Web` renders `GET /`.
- [ ] Production Web host uses `IServiceCollection`; no Autofac provider factory.

**P00 complete when:** The solution compiles, the lobby shell loads, and dependency direction is verified.

---

## 5. P01 — Domain aggregate, game rules and state transitions

**Depends on:** P00.  
**Goal:** All game behavior is correct and independently testable without SQL, Web, or mocks.  
**PRD:** FR-01, FR-02, FR-04, FR-05, FR-06 (Domain portions).

### Implementation tasks

- [ ] Create `PlayerSymbol { X, O }`, `MatchStatus { Waiting, InProgress, Completed }`, `MatchOutcome { None, XWins, OWins, Draw }`.
- [ ] Define explicit `GameErrorCode` values for invalid names/symbols, unavailable/full match, invalid player, wrong turn, invalid/occupied cell, not started/completed match.
- [ ] Implement `GameRuleException` carrying `GameErrorCode`; use clear exceptions, not boolean success flags that silently fail.
- [ ] Implement `WinningLines` with exactly eight unique lines; make win checks deterministic.
- [ ] Implement `MatchPlayer` with its immutable identity, match association, trimmed 1–20 character name, symbol, and hash; avoid public setters for protected state.
- [ ] Implement `GameMatch` as aggregate root; expose read-only player collection and guard state mutations in Domain methods only.
- [ ] Implement `GameMatch.Create(...)`: validate IDs, name, symbol, token hash; set nine `'.'` cells, `Waiting`, `None`, `CurrentTurn=X`, `Revision=0`, one creator, UTC creation time, no completion time.
- [ ] Implement `GameMatch.Join(...)`: require `Waiting` and one current player, assign opposite symbol, set `InProgress`, preserve `X` as first turn, increment revision once.
- [ ] Implement `GetPlayerByTokenHash(...)` as a membership lookup **without** cryptographic operations in Domain.
- [ ] Implement `MakeMove(...)`: verify `InProgress`, player membership, cell 0–8, empty cell, correct player's turn; write exactly one symbol and increment revision once.
- [ ] After each accepted move: check win first; otherwise check draw; otherwise switch turn. On completion set `Completed`, corresponding outcome, and `CompletedAtUtc`. Completed matches must never accept another move.
- [ ] Ensure rejected create/join/move operations do not leave a partially mutated aggregate.
- [ ] Use the same board encoding everywhere (`'.'`, `X`, `O`); no browser-maintained authoritative board.

### Required files

```text
src/TicTacToe.Domain/Entities/GameMatch.cs
src/TicTacToe.Domain/Entities/MatchPlayer.cs
src/TicTacToe.Domain/Enums/PlayerSymbol.cs
src/TicTacToe.Domain/Enums/MatchStatus.cs
src/TicTacToe.Domain/Enums/MatchOutcome.cs
src/TicTacToe.Domain/Enums/GameErrorCode.cs
src/TicTacToe.Domain/Rules/WinningLines.cs
src/TicTacToe.Domain/Exceptions/GameRuleException.cs
tests/TicTacToe.Domain.Tests/Entities/GameMatchCreateTests.cs
tests/TicTacToe.Domain.Tests/Entities/GameMatchJoinTests.cs
tests/TicTacToe.Domain.Tests/Entities/GameMatchMoveTests.cs
tests/TicTacToe.Domain.Tests/Entities/GameMatchOutcomeTests.cs
tests/TicTacToe.Domain.Tests/Rules/WinningLinesTests.cs
```

### Mandatory NUnit/Shouldly cases

- [ ] Create with X and with O → one correct player, `Waiting`, nine empty cells, `Revision=0`, `CurrentTurn=X`.
- [ ] Valid 1-character and 20-character trimmed names accepted; null/blank/whitespace/>20 rejected; duplicate names allowed.
- [ ] Join creator X → joiner O; join creator O → joiner X and **X plays first**.
- [ ] Join when already full/started/completed rejected; no third player; revision unchanged on failure.
- [ ] Valid move changes one cell, switches turn (unless completed), increments revision once.
- [ ] Wrong turn, occupied cell, negative index, index 9+, unauthorized player, waiting match, completed match → rejected with no board/revision change.
- [ ] All **8 winning lines** recognized for X and O where legal; no premature winner.
- [ ] Full board without winner → `Draw`, `Completed`, `CompletedAtUtc` set.
- [ ] Win takes precedence over a full-board draw; subsequent move rejected.
- [ ] `GameMatch.GetPlayerByTokenHash` finds correct member and does not create/modify state.

**P01 complete when:** Domain tests cover valid and invalid transitions, and Domain has no Infrastructure/Application/Web dependency.

---

## 6. P02 — EF Core SQL Server persistence, concurrency and guest-token security

**Depends on:** P01.  
**Goal:** Authoritative state survives process/browser changes; writes are concurrency-safe; guest tokens remain secret.

### Implementation tasks — Application contracts

- [ ] Add `IGameMatchRepository`, `IMatchReadRepository`, and `IUnitOfWork` with signatures from §3.3.
- [ ] Add `IGuestTokenService` and `GuestTokenIssue` from §3.3.
- [ ] Add `MatchNotFoundException`, `MatchAccessDeniedException`, `MatchConflictException`.
- [ ] Define safe DTO shells under `Features/Matches/Dtos/` (only fields required by §3.5); keep them free from EF types.
- [ ] Implement reusable `MatchStateMapper` for aggregate → safe snapshot, without exposing raw token or hash.

### Implementation tasks — Infrastructure

- [ ] Create `GameDbContext` with `GameMatch` and `MatchPlayer` mappings.
- [ ] Configure keys, required fields, foreign key between match and players, maximum display-name length 20, fixed nine-character board encoding, enum conversions, UTC timestamps, and suitable token-hash storage.
- [ ] In `GameMatchConfiguration`, add **shadow** SQL `rowversion`: `.Property<byte[]>("RowVersion").IsRowVersion()`.
- [ ] Keep `RowVersion` out of Domain classes and public DTOs.
- [ ] Enforce player slot/symbol correctness at Domain level; add compatible relational constraints/indexes where useful, without bypassing aggregate invariants.
- [ ] Create initial EF Core migration in `Infrastructure/Persistence/Migrations/`; verify schema on a **real test SQL Server**.
- [ ] Implement `GameMatchRepository.GetByIdAsync(...)` with tracked aggregate and included players; `AddAsync` only stages insertion.
- [ ] Implement `MatchReadRepository.GetWaitingAsync(...)` with `AsNoTracking()` and projection of **only Waiting** matches.
- [ ] Implement `MatchReadRepository.GetAuthorizedStateAsync(...)` with `AsNoTracking()` and a filter verifying **match ID + hashed player token**, returning null for unauthorized/missing matches.
- [ ] Implement `EfUnitOfWork.SaveChangesAsync(...)` as the **only** commit path; translate `DbUpdateConcurrencyException` into `MatchConflictException`, without blind retry.
- [ ] Implement stateless `GuestTokenService.Issue()` with cryptographically secure random bytes and an opaque URL-safe token; store only a one-way cryptographic hash in SQL.
- [ ] Implement `GuestTokenService.Hash(rawToken)` consistently for create/join/move/state authorization. Do not log incoming tokens/hashes.
- [ ] Register DbContext/repositories/UoW as **scoped**, guest-token service as **singleton**, through `Infrastructure.DependencyInjection`.
- [ ] Keep SQL connection strings and other secrets outside committed source; use secrets/environment configuration.

### Required files

```text
src/TicTacToe.Application/Abstractions/Persistence/IGameMatchRepository.cs
src/TicTacToe.Application/Abstractions/Persistence/IMatchReadRepository.cs
src/TicTacToe.Application/Abstractions/Persistence/IUnitOfWork.cs
src/TicTacToe.Application/Abstractions/Security/IGuestTokenService.cs
src/TicTacToe.Application/Common/Exceptions/*.cs
src/TicTacToe.Application/Common/Mapping/MatchStateMapper.cs
src/TicTacToe.Application/Features/Matches/Dtos/*.cs
src/TicTacToe.Infrastructure/Persistence/GameDbContext.cs
src/TicTacToe.Infrastructure/Persistence/Configurations/GameMatchConfiguration.cs
src/TicTacToe.Infrastructure/Persistence/Configurations/MatchPlayerConfiguration.cs
src/TicTacToe.Infrastructure/Persistence/Repositories/GameMatchRepository.cs
src/TicTacToe.Infrastructure/Persistence/Repositories/MatchReadRepository.cs
src/TicTacToe.Infrastructure/Persistence/EfUnitOfWork.cs
src/TicTacToe.Infrastructure/Persistence/Migrations/*
src/TicTacToe.Infrastructure/Security/GuestTokenService.cs
src/TicTacToe.Infrastructure/DependencyInjection.cs
tests/TicTacToe.IntegrationTests/Persistence/*
```

### Mandatory tests

- [ ] Migration applies cleanly to fresh SQL Server test database; persisted aggregate loads with both players and board/revision intact.
- [ ] Repository loads tracked aggregate with players and saves a Domain mutation without `Update()`.
- [ ] Waiting projection excludes in-progress and completed matches and never contains secrets.
- [ ] Authorized state query returns only to a match participant with the correct hashed token.
- [ ] Guest tokens are random per issue; stored hash does not equal raw token; hashing is deterministic for the same token.
- [ ] With **two separate DbContexts** that loaded the same revision, competing joins cannot both commit.
- [ ] With **two separate DbContexts** that loaded the same revision, stale/competing moves cannot both commit; the loser receives `MatchConflictException`.
- [ ] Unit of Work maps SQL concurrency exceptions without retrying or allowing illegal persisted states.

**P02 complete when:** SQL migrations and real-SQL concurrency tests pass, tokens are hashed at rest, and repositories/UoW respect their boundaries.

---

## 7. P03 — FR-01/FR-02: Guest identity, match creation and waiting room

**Depends on:** P02.  
**Goal:** Browser A creates a match, receives its one-time guest credential, and sees an authorized Waiting room.

### Backend implementation

- [ ] Create `CreateMatchCommand : ICommand<CreateMatchResult>` and `CreateMatchCommandHandler`.
- [ ] Handler validates/normalizes guest name and symbol, issues one token, creates the `GameMatch` through Domain, adds it to repository, saves **once**, maps initial state, returns match ID + raw token + state.
- [ ] Do not let an invalid create request call `AddAsync` or `SaveChangesAsync`.
- [ ] Create `GetMatchStateQuery : IQuery<MatchStateDto>` and `GetMatchStateQueryHandler` using `IGuestTokenService.Hash` + `IMatchReadRepository.GetAuthorizedStateAsync`.
- [ ] Query distinguishes missing/invalid credentials through safe Web handling without leaking private match details; **never saves**.
- [ ] Implement `CreateMatchInputModel` with validated name/symbol; use a matching antiforgery-protected `POST /matches` action.
- [ ] Implement `MatchesController.Create`: dispatch command through Cortex; return JSON credential **once**, not as a redirect query string or HTML fragment.
- [ ] Implement `MatchesController.Room` at `GET /matches/{matchId}` with a generic Razor shell; game state is loaded **after** Hub authorization.
- [ ] Implement `GameHub.SubscribeMatch(matchId, token)`: dispatch `GetMatchStateQuery`, reject unauthorized callers, add authorized connection to `match:{matchId}`, and return the latest persisted state.
- [ ] Account for a join/move between authorization and group registration: after group subscription, retrieve a fresh authorized snapshot (or otherwise establish equivalent gap-free initial state).
- [ ] Register hub and routing in `Program.cs`; use `/hubs/game`, not one hub per match.
- [ ] Start Web error mapping for input validation, access denied, match missing and unexpected errors.

### Browser implementation

- [ ] Create lobby form with display name, X/O choice, and **Create Match**; show client-side guidance but rely on server validation.
- [ ] Submit POST with MVC antiforgery token; prevent accidental duplicate submission and show validation feedback.
- [ ] After success, save `{ token, symbol }` in `sessionStorage` under the returned match ID; then navigate to `/matches/{matchId}`.
- [ ] `Views/Matches/Room.cshtml` renders a generic waiting/game container, connection text, and stable UI placeholders. It does **not** embed raw credentials.
- [ ] Implement initial `game.js`: read match ID from the page/route and token from `sessionStorage`; start SignalR connection; call `SubscribeMatch`; render authorized `Waiting` state.
- [ ] If browser lacks a credential for the match, show safe access feedback and a link to the lobby; **do not** present controls as an authorized player.
- [ ] Display creator name, chosen symbol, and **“Waiting for another player…”**. No playable board during `Waiting`.

### Required files

```text
src/TicTacToe.Application/Features/Matches/Commands/CreateMatch/CreateMatchCommand.cs
src/TicTacToe.Application/Features/Matches/Commands/CreateMatch/CreateMatchCommandHandler.cs
src/TicTacToe.Application/Features/Matches/Queries/GetMatchState/GetMatchStateQuery.cs
src/TicTacToe.Application/Features/Matches/Queries/GetMatchState/GetMatchStateQueryHandler.cs
src/TicTacToe.Web/Models/CreateMatchInputModel.cs
src/TicTacToe.Web/Controllers/MatchesController.cs
src/TicTacToe.Web/Hubs/GameHub.cs
src/TicTacToe.Web/Views/Lobby/Index.cshtml
src/TicTacToe.Web/Views/Matches/Room.cshtml
src/TicTacToe.Web/wwwroot/js/lobby.js
src/TicTacToe.Web/wwwroot/js/game.js
tests/TicTacToe.Application.Tests/Features/Matches/Commands/CreateMatchCommandHandlerTests.cs
tests/TicTacToe.Application.Tests/Features/Matches/Queries/GetMatchStateQueryHandlerTests.cs
tests/TicTacToe.IntegrationTests/Web/MatchEndpointsTests.cs
tests/TicTacToe.IntegrationTests/Web/GameHubTests.cs
```

### Required tests / acceptance

- [ ] Create handler: valid name/symbol, trims name, correct waiting state, correct guest token, `AddAsync` once, save exactly once.
- [ ] Invalid name/symbol: no Add, no Save; error is safe and actionable.
- [ ] State query: valid token retrieves own match; bad/missing token denied; missing match handled; no Save.
- [ ] Real HTTP POST persists match and returns one-time token; missing/invalid antiforgery request rejected.
- [ ] Guest token is **absent** from URL, match HTML, logs, `MatchStateDto`, and Hub event payloads.
- [ ] Unauthorized Hub subscription is rejected and is **not** placed into the match group.
- [ ] Manual check: create as X **and** as O; each reaches a Waiting room with correct creator details.

**P03 complete when:** A single browser can create and restore its authorized Waiting state from SQL using its session credential.

---

## 8. P04 — FR-03: Available matches and real-time lobby

**Depends on:** P03.  
**Goal:** Browser B sees newly created waiting matches without refreshing.

### Implementation tasks

- [ ] Create `GetWaitingMatchesQuery : IQuery<IReadOnlyList<WaitingMatchDto>>` and handler using `IMatchReadRepository` only.
- [ ] Return only `Waiting` matches with creator name, creator symbol, and match ID. Keep guest tokens/hashes completely out of the result.
- [ ] Implement `GameHub.SubscribeLobby()` to add connection to `lobby` group and return a fresh waiting list (subscribe before snapshot to reduce missed updates).
- [ ] Create Web-only `IMatchNotifier` and `SignalRMatchNotifier` using `IHubContext<GameHub>`.
- [ ] After successful `CreateMatchCommand` commit, `MatchesController.Create` asks notifier to refresh waiting list and publish `WaitingMatchesChanged` to the `lobby` group.
- [ ] Treat notifications as post-commit delivery; a notification failure must **not undo** a committed match. Log a safe error; a later subscription retrieves the SQL snapshot.
- [ ] Update `Lobby/Index.cshtml`: waiting-match cards/rows, creator display name/symbol, Join affordance (enabled after P05), and friendly empty state.
- [ ] Implement `lobby.js` to connect to SignalR, call `SubscribeLobby`, replace the displayed list on `WaitingMatchesChanged`, and show connection status.
- [ ] Do not trust the browser's current lobby list to determine whether a match is joinable; server join validation is authoritative (implemented P05).
- [ ] Escape/encode guest names in Razor and DOM rendering; never concatenate untrusted names into HTML.

### Required tests / acceptance

- [ ] `GetWaitingMatchesQueryHandlerTests`: empty list, populated list, dependency failure handling, read-only interaction, **zero Save**.
- [ ] SQL read repository returns only Waiting matches and no secret properties.
- [ ] SignalR integration: Browser B subscribes first; Browser A creates; B receives `WaitingMatchesChanged` without a page refresh.
- [ ] Newly opened Browser C sees existing waiting matches immediately from the subscription snapshot.
- [ ] No matches → friendly empty message; player names/symbols render safely.

**P04 complete when:** A match created in Browser A appears live in an already-open lobby in Browser B.

---

## 9. P05 — FR-04: Join match and transition from waiting to playing

**Depends on:** P04.  
**Goal:** Exactly one guest joins a waiting match, receives the opposite symbol, and both browsers enter the game together.

### Application implementation

- [ ] Create `JoinMatchCommand : ICommand<JoinMatchResult>` and `JoinMatchCommandHandler`.
- [ ] Validate match ID, trimmed 1–20 character display name and `ExistingPlayerToken?`.
- [ ] Load tracked `GameMatch` including players; missing match → `MatchNotFoundException`; not waiting/full → conflict.
- [ ] If `ExistingPlayerToken` identifies the creator already in this match, reject self-join; the UI must also hide/disable Join for a locally owned match.
- [ ] Document limitation: anonymous sessions cannot reliably identify the same human across unrelated browsers; no registration/fingerprinting workaround.
- [ ] Generate a **new** guest token for the second player, derive opposite symbol through `GameMatch.Join`, transition to `InProgress`, retain X as first turn, increment revision once, save exactly once.
- [ ] Handle `DbUpdateConcurrencyException` through `MatchConflictException`; never retry stale join against a different state automatically.
- [ ] Return match ID, new player's raw token once, and committed started state.

### Web and real-time implementation

- [ ] Add `JoinMatchInputModel`, MVC antiforgery protection, and `POST /matches/{matchId}/join`.
- [ ] Join action dispatches Cortex command and returns JSON success or safe `400/403/404/409` errors.
- [ ] Following successful commit, notifier publishes refreshed `WaitingMatchesChanged` to `lobby` and `MatchStateChanged` to `match:{matchId}`.
- [ ] Update lobby Join action: guest supplies a display name; include `ExistingPlayerToken` if a match credential already exists locally; block accidental duplicate clicks.
- [ ] On success save joining player's `{ token, symbol }` for that match in `sessionStorage`, then navigate to `/matches/{matchId}`.
- [ ] Update waiting room to switch **automatically** to the game state when `MatchStateChanged` reports `InProgress`; otherwise initial `SubscribeMatch` snapshot already handles a join that occurred during navigation.
- [ ] Render both names and symbols (including duplicate names), an empty 3×3 board, and a correct textual turn indicator.
- [ ] For creator O, joining X must be displayed as first player to move; never assume the creator starts.

### Required files

```text
src/TicTacToe.Application/Features/Matches/Commands/JoinMatch/JoinMatchCommand.cs
src/TicTacToe.Application/Features/Matches/Commands/JoinMatch/JoinMatchCommandHandler.cs
src/TicTacToe.Web/Models/JoinMatchInputModel.cs
src/TicTacToe.Web/Controllers/MatchesController.cs
src/TicTacToe.Web/Notifications/IMatchNotifier.cs
src/TicTacToe.Web/Notifications/SignalRMatchNotifier.cs
src/TicTacToe.Web/wwwroot/js/lobby.js
src/TicTacToe.Web/wwwroot/js/game.js
tests/TicTacToe.Application.Tests/Features/Matches/Commands/JoinMatchCommandHandlerTests.cs
tests/TicTacToe.IntegrationTests/Persistence/SqlServerConcurrencyTests.cs
tests/TicTacToe.IntegrationTests/Web/LobbySignalRTests.cs
tests/TicTacToe.IntegrationTests/Web/MatchEndpointsTests.cs
```

### Required tests / acceptance

- [ ] Join handler: success, opposite symbol, `InProgress`, revision increase, exactly one Save.
- [ ] Invalid/blank/>20 name, missing match, already full/not waiting, creator's existing credential → rejected with zero Save.
- [ ] **Two concurrent SQL joins to the final slot:** exactly one succeeds, the other conflicts, persisted match has only two players.
- [ ] Unauthorized third party cannot subscribe to the private match group.
- [ ] Two browsers show the same two names, two symbols, empty board and turn after join.
- [ ] Waiting match disappears from already-open lobby **without refresh**.
- [ ] Creator X and creator O paths both tested; **joining X moves first** in creator-O case.

**P05 complete when:** Two guests can reliably form a match and both see one consistent started state; no third slot can be committed.

---

## 10. P06 — FR-05: Server-authorized moves and live board updates

**Depends on:** P05.  
**Goal:** All moves are validated and persisted on the server before both browsers display them.

### Application implementation

- [ ] Create `MakeMoveCommand : ICommand<MatchStateDto>` and `MakeMoveCommandHandler`.
- [ ] Validate match ID, token presence/format, and cell index before mutation.
- [ ] Hash caller's token with `IGuestTokenService`, load tracked `GameMatch` including players, resolve membership using `GetPlayerByTokenHash`.
- [ ] Never accept a client-supplied player ID, symbol, full board, next turn, or winner as an authorization/game-rule input.
- [ ] Call `GameMatch.MakeMove(player.Id, cellIndex)`, then `IUnitOfWork.SaveChangesAsync(ct)` exactly once, then return mapped committed `MatchStateDto`.
- [ ] Propagate appropriate Domain/Application errors; on failed validation/rule/authorization do not Save.
- [ ] Translate competing SQL writes to controlled conflict; do not accept a stale move by automatic retry.

### SignalR and browser implementation

- [ ] Add `GameHub.MakeMove(matchId, token, cellIndex)`; dispatch Cortex command and notify `match:{matchId}` **only after the save succeeds**.
- [ ] Broadcast `MatchStateChanged` containing the full authoritative board, status, outcome, turn, players, and revision (never the credential).
- [ ] `game.js` renders exactly nine accessible/tappable cells indexed 0–8, showing `'.'` as empty.
- [ ] Enable local board input only when match is `InProgress`, socket is usable, player has a token, it is **their** turn, and cell is empty. This is UX only: server validation always remains authoritative.
- [ ] Send **only** `(matchId, token, cellIndex)` on click/tap; do not update the authoritative board optimistically.
- [ ] Prevent duplicate client submissions while a move is pending; re-enable appropriately after result/error/resync.
- [ ] Process any state snapshot only if its `Revision` is **not older** than the last rendered revision; never roll the board back because events arrive out of order.
- [ ] On `409`/Hub conflict, recover state by calling authorized `SubscribeMatch` again; show a helpful message rather than applying an unconfirmed move.
- [ ] Use a textual indicator: “Your turn” or “Opponent's turn”; make symbols visually distinct without relying on color alone.

### Required files

```text
src/TicTacToe.Application/Features/Matches/Commands/MakeMove/MakeMoveCommand.cs
src/TicTacToe.Application/Features/Matches/Commands/MakeMove/MakeMoveCommandHandler.cs
src/TicTacToe.Web/Hubs/GameHub.cs
src/TicTacToe.Web/Notifications/SignalRMatchNotifier.cs
src/TicTacToe.Web/wwwroot/js/game.js
src/TicTacToe.Web/wwwroot/css/game.css
tests/TicTacToe.Application.Tests/Features/Matches/Commands/MakeMoveCommandHandlerTests.cs
tests/TicTacToe.IntegrationTests/Web/GameHubTests.cs
tests/TicTacToe.IntegrationTests/Persistence/SqlServerConcurrencyTests.cs
```

### Required tests / acceptance

- [ ] Valid X then O moves update correct cells and turn and commit once each.
- [ ] Wrong-turn, occupied, negative/>8 cell, missing/invalid token, waiting match, completed match: no mutation and zero Save.
- [ ] Player with token from **another match** cannot make a move in this one.
- [ ] Two clients receive the same committed state after a move without refreshing.
- [ ] Two near-simultaneous writes from stale versions: only one valid SQL state transition commits; loser receives safe conflict and resynchronizes.
- [ ] Tests confirm **no** `MatchStateChanged` event is published for a rejected/uncommitted move.
- [ ] Older revision events never overwrite a newer board in the browser.

**P06 complete when:** Two browsers alternate valid moves on one persisted board and every invalid attempt is rejected without changing state.

---

## 11. P07 — FR-06: Win/draw completion and start-new-match navigation

**Depends on:** P06.  
**Goal:** The winning or drawn result appears consistently in both browsers and no completed match can be changed.

### Implementation tasks

- [ ] Reuse P01 Domain logic; **do not** implement duplicate win or draw calculations in JavaScript, MVC, or Application.
- [ ] Ensure the final accepted `MakeMove` returns and broadcasts `Status=Completed`, correct `Outcome`, incremented revision and completion timestamp.
- [ ] For X/O win, resolve the winning player's **display name and symbol** from public match players; show them to both clients.
- [ ] For a draw, show **“It's a draw”** on both clients.
- [ ] Hide/disable interactive cells for `Completed`; still reject any direct malicious Hub call after completion.
- [ ] Render meaningful outcome text independently of color; show the same final board to both players.
- [ ] Provide **Back to Lobby / New Match** navigation. “New Match” returns to the create flow and creates a **different** match ID with a blank board; it is **not** a rematch on the existing aggregate.
- [ ] Keep finished match persisted as `Completed`; ensure it never reappears in the waiting list.

### Required tests / acceptance

- [ ] Domain tests cover all eight winning lines for X and O, and a no-winner full-board draw.
- [ ] Handler returns completed status/outcome on finishing move and saves once.
- [ ] Both connected clients receive identical winner/draw and final board.
- [ ] Further move after completion rejected with zero Save and no broadcast.
- [ ] Completed match not present in waiting lobby.
- [ ] New Match creates a new, independent Waiting aggregate; previous match remains completed and unchanged.

**P07 complete when:** Win, draw, no-more-moves and new-match flows work in both browsers without a manual refresh.

---

## 12. P08 — FR-07/FR-08: Refresh, reconnect, errors and recovery

**Depends on:** P07.  
**Goal:** Temporary connection loss or refresh cannot lose committed match state, and failures show helpful feedback.

### Reconnection and persistence behavior

- [ ] Enable SignalR JavaScript automatic reconnect; display `Connecting…`, `Connection lost. Reconnecting…`, and restored connection status.
- [ ] On initial page load and after any successful reconnect, read match-specific token from `sessionStorage` and call `SubscribeMatch(matchId, token)` again.
- [ ] Do **not** treat an old SignalR connection ID or former group membership as authorization on a new connection.
- [ ] Use the new authorized SQL snapshot as the source of truth; accept it based on revision ordering.
- [ ] Confirm `sessionStorage` preserves token and caller symbol across a page refresh in the **same browser session**; cross-device identity recovery is explicitly out of scope.
- [ ] Maintain authoritative match data in SQL Server, not JavaScript memory, a static dictionary, or `IHubContext` group state.
- [ ] Disconnection **does not** forfeit a player, end a match, remove a player, or erase waiting/move history.
- [ ] If token is missing or invalid, show an access message and lobby link, not a new identity silently substituted into the existing match.
- [ ] On lost event or conflicting action, fetch the fresh persisted match state; a reconnection must not replay stale user moves.
- [ ] Ensure waiting-lobby clients also re-subscribe via `SubscribeLobby()` after reconnect and receive the latest list.

### Feedback, security and UX implementation

- [ ] Add consistent safe HTTP exception mapping and equivalent Hub errors for validation `400`, access `403`, not found `404`, conflict `409` and unexpected `500`.
- [ ] Show user-friendly messages for waiting, turn ownership, occupied cell, invalid turn, match full, match completed, missing access and connection loss.
- [ ] On transient connection failure, disable move input and restore it only when authorized state/connection are current.
- [ ] Use antiforgery on both POST endpoints, HTTPS in deployed environments, and safe DOM rendering of guest names.
- [ ] Validate all HTTP/SignalR inputs server-side; include no bearer-like guest credential in query strings, Razor markup, events or logs.
- [ ] Configure Serilog structured console/rolling file logs in Web for useful operation/error metadata **without token values**.
- [ ] Ensure post-commit notification exceptions are recorded without converting committed success into an unsafe client retry loop.
- [ ] Ensure touch-friendly, keyboard-accessible board cells, readable labels and responsive Bootstrap layout.

### Required files / test coverage

```text
src/TicTacToe.Web/wwwroot/js/game.js
src/TicTacToe.Web/wwwroot/js/lobby.js
src/TicTacToe.Web/wwwroot/css/game.css
src/TicTacToe.Web/Program.cs
src/TicTacToe.Web/Controllers/MatchesController.cs
src/TicTacToe.Web/Hubs/GameHub.cs
tests/TicTacToe.IntegrationTests/Web/ReconnectTests.cs
tests/TicTacToe.IntegrationTests/Web/GameHubTests.cs
tests/TicTacToe.IntegrationTests/Web/MatchEndpointsTests.cs
```

### Required tests / acceptance

- [ ] Play several moves, refresh Browser A; restored board, turn, player names, status and revision equal Browser B.
- [ ] Briefly disconnect Browser A; Browser B continues; A reconnects and sees latest committed state without replaying missed events.
- [ ] Lobby disconnect/reconnect restores up-to-date waiting list.
- [ ] Invalid/missing session token cannot join private SignalR match group or make moves.
- [ ] Simulated conflict shows error and triggers authorized resynchronization; current state is not overwritten.
- [ ] Safe HTTP and Hub error payloads contain no exception trace, token, hash or database details.
- [ ] Antiforgery requests are enforced; raw guest tokens are not in URLs, public DTOs, page HTML or logs.
- [ ] Basic keyboard/touch and narrow-screen manual checks pass.

**P08 complete when:** The same-browser guest can refresh/reconnect, preserve exact persisted state, and understand recoverable errors.

---

## 13. P09 — Final integration, code review and MVP acceptance

**Depends on:** P08.  
**Goal:** Demonstrate FR-01–FR-08 together, backed by automated tests and a repeatable manual flow.

### Integration environment

- [ ] Implement/finish `tests/TicTacToe.IntegrationTests/Fixtures/TestAppFactory.cs` using an ASP.NET Core test host (`WebApplicationFactory` or a running test server), isolated real SQL Server test database and actual SignalR clients for Hub tests.
- [ ] Set test database configuration separately from production; apply checked-in migrations to a clean test DB.
- [ ] Verify app startup, scoped DI, Cortex scanning, hub registration, MVC routes and connection handling end to end.
- [ ] Ensure tests are independent, predictable and do not depend on shared persisted state left by earlier tests.
- [ ] Run complete solution restore/build/test with a configured SQL Server; capture failing tests rather than removing assertions.

### Required test inventory

| Suite/file | Must demonstrate |
|---|---|
| `Domain.Tests/GameMatchCreateTests` | Valid initialization, names, creator symbol, revision |
| `Domain.Tests/GameMatchJoinTests` | Opposite symbol, max two players, X starts, invalid joins |
| `Domain.Tests/GameMatchMoveTests` | Correct turn, allowed cell, unauthorized/invalid moves |
| `Domain.Tests/GameMatchOutcomeTests` | X/O wins on all lines, draw, completed rejection |
| `Domain.Tests/WinningLinesTests` | Exactly eight lines |
| `Application.Tests/CreateMatchCommandHandlerTests` | Success/failure; Add once + Save once vs no Save |
| `Application.Tests/JoinMatchCommandHandlerTests` | Membership/self-join, full/missing match, Save once vs zero |
| `Application.Tests/MakeMoveCommandHandlerTests` | Token authorization, valid/invalid moves, Save once vs zero |
| `Application.Tests/GetWaitingMatchesQueryHandlerTests` | Filtered read model, no Save |
| `Application.Tests/GetMatchStateQueryHandlerTests` | Authorized persisted snapshot, invalid token/missing match, no Save |
| `IntegrationTests/GameMatchRepositoryTests` | Tracked aggregate and SQL persistence |
| `IntegrationTests/MatchReadRepositoryTests` | No-tracking public/authorized read filtering |
| `IntegrationTests/SqlServerConcurrencyTests` | Simultaneous joins, competing moves, rowversion conflicts |
| `IntegrationTests/MatchEndpointsTests` | Create/join MVC input, antiforgery, HTTP errors, persistence |
| `IntegrationTests/LobbySignalRTests` | Live appearance/removal, subscriber snapshots |
| `IntegrationTests/GameHubTests` | Authorized subscribe/moves, broadcasts after commit, safe errors |
| `IntegrationTests/ReconnectTests` | Refresh/reconnect resynchronization against SQL state |

### Mandatory end-to-end scenarios

- [ ] **Scenario A — Create/join:** A enters name, chooses X, creates; B sees match automatically, joins as O; both see correct initial board, names and X turn.
- [ ] **Scenario B — Creator O:** A creates as O, B joins as X; B moves first.
- [ ] **Scenario C — Turn correctness:** A and B alternate; attempt to move out of turn and into an occupied square; rejected actions do not modify SQL state.
- [ ] **Scenario D — Win:** Play a legal horizontal, vertical and diagonal win; correct winning name/symbol appears on both clients; finished board cannot be changed.
- [ ] **Scenario E — Draw:** Play a legal full-board draw; both clients show “It's a draw”.
- [ ] **Scenario F — Concurrent join:** Two different guests attempt the one available slot at the same time; one succeeds, one receives conflict; only two players are persisted.
- [ ] **Scenario G — Concurrent moves:** Stale simultaneous updates cannot both commit; loser resynchronizes.
- [ ] **Scenario H — Refresh:** Refresh either player after multiple moves; both browsers display identical persisted state/revision.
- [ ] **Scenario I — Disconnect:** Temporary SignalR loss does not forfeit/erase anything; reconnect resubscribes and reloads the saved board.
- [ ] **Scenario J — Errors/security:** Invalid guest name, full match, wrong-turn/occupied/out-of-range cell, invalid token and missing match produce safe useful feedback.
- [ ] **Scenario K — New game:** After win/draw, Back to Lobby/New Match works, creating a new ID and empty Waiting board without changing the previous match.
- [ ] **Scenario L — Demo readiness:** Mobile-width layout, keyboard/touch interaction, no token in URLs/events/logs, and demonstrable automated test output.

### Architecture review gate

- [ ] Domain has no public writeable game-state setters or technical infrastructure dependencies.
- [ ] Every Cortex handler is unit-tested using NUnit + Shouldly and `Autofac.Extras.Moq`; production does **not** use Autofac.
- [ ] Application services do not depend on `DbContext`, Hub, MVC or Serilog concrete APIs.
- [ ] Web controllers/hub do not use repositories, Unit of Work or direct SQL; commands/queries are dispatched through Cortex.
- [ ] Both repositories and UoW use the same scoped DbContext; only command handlers save.
- [ ] All SQL mutations have a rowversion concurrency guard and do not silently retry invalid decisions.
- [ ] Only participants can obtain private snapshots or play; guest identity never depends on display name.
- [ ] Notifications occur after commit; missed messages are repaired through persisted state subscriptions.
- [ ] No unnecessary framework, distributed service, database or feature was introduced.

**P09 complete when:** All applicable automated tests pass against a real SQL Server, the two-browser demo passes every relevant PRD acceptance step, and the source boundaries remain intact.

---

## 14. Phase-by-phase test execution

Run these from the repository root; apply migrations using the project's configured startup/migrations setup and a non-production test connection.

```bash
# Run whenever a phase changes dependencies or code
dotnet restore TicTacToeLive.slnx
dotnet build TicTacToeLive.slnx

# Domain behavior (P01 onward)
dotnet test tests/TicTacToe.Domain.Tests/TicTacToe.Domain.Tests.csproj

# CQRS handlers (P03 onward)
dotnet test tests/TicTacToe.Application.Tests/TicTacToe.Application.Tests.csproj

# SQL Server + MVC + SignalR integration (P02 onward, requires test SQL Server)
dotnet test tests/TicTacToe.IntegrationTests/TicTacToe.IntegrationTests.csproj

# Final acceptance
dotnet test TicTacToeLive.slnx
```

**Environment caveat:** The documents do not select a particular developer machine's SQL Server edition, database name, credentials or deployment host. Use a supported local/test SQL Server, configure secrets privately, and record any unavailable infrastructure as a blocked test gate. Do not silently choose a production database or fake SQL concurrency coverage.

---

## 15. PRD requirements traceability

| PRD | Owning phase(s) | Acceptance evidence |
|---|---|---|
| **FR-01** Guest display names | P01, P03, P05 | Domain validation; create/join HTTP/handler tests; duplicate names permitted |
| **FR-02** Create match | P01, P03, P04 | `CreateMatchCommandHandlerTests`, stored waiting match, live lobby appearance |
| **FR-03** Waiting lobby | P02, P04, P05 | Read-query tests, live appear/remove SignalR integration |
| **FR-04** Join | P01, P05 | Handler tests, creator-O/X paths, SQL concurrent-join test |
| **FR-05** Live moves | P01, P06 | Move handler and Hub tests, SQL competing moves, two-browser board |
| **FR-06** Win/draw | P01, P07 | All eight lines, draw, completed rejection, both-browser result |
| **FR-07** Refresh/reconnect | P02, P03, P08 | Authorized SQL state query, `ReconnectTests`, browser refresh |
| **FR-08** Helpful feedback | P03–P08 | Safe HTTP/Hub error tests, conflict recovery, connection-status UX |

---

## 16. Agent phase handoff template

After **each** phase, record the following in the implementation PR/task notes (do **not** create extra application features or classes just for reporting):

```markdown
### Phase PXX — <phase title>
- Status: COMPLETE | BLOCKED
- PRD requirements addressed:
- Files added/changed:
- Domain/Application/Web decisions made:
- Unit tests executed and results:
- SQL/HTTP/SignalR integration tests executed and results:
- Manual acceptance performed:
- Architecture/security checks:
- Known issues or blocking dependencies:
- Next allowed phase:
```

**Final rule:** Do not begin optional PRD enhancements until P09 is complete. At the MVP finish line, the application must remain a small, understandable, fully tested two-person Tic-Tac-Toe experience—not a generalized gaming platform.
