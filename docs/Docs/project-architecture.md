# TicTacToe Live — Project Architecture

**Version:** 2.0  
**Status:** Approved design baseline for MVP implementation  
**Target:** .NET 10 (`net10.0`) / ASP.NET Core MVC / Razor / SignalR / EF Core 10 / SQL Server  
**Architecture:** Clean Architecture · single deployable modular monolith · feature-based CQRS  
**Mediator:** `Cortex.Mediator` (**not** `MediatR`)  
**Testing:** NUnit · Shouldly · Autofac.Extras.Moq  

## 1. Solution structure

```text
TicTacToeLive.slnx
Directory.Build.props
src/
├── TicTacToe.Domain/
│   ├── Entities/
│   │   ├── GameMatch.cs
│   │   └── MatchPlayer.cs
│   ├── Enums/
│   │   ├── PlayerSymbol.cs             # X, O
│   │   ├── MatchStatus.cs              # Waiting, InProgress, Completed
│   │   ├── MatchOutcome.cs             # None, XWins, OWins, Draw
│   │   └── GameErrorCode.cs
│   ├── Rules/
│   │   └── WinningLines.cs             # Eight winning combinations
│   └── Exceptions/
│       └── GameRuleException.cs
│
├── TicTacToe.Application/
│   ├── Abstractions/
│   │   ├── Contracts/
│   │   │   ├── IGameMatchRepository.cs
│   │   │   ├── IMatchReadRepository.cs
│   │   │   └── IUnitOfWork.cs
│   │   └── Security/
│   │       └── IGuestTokenService.cs
│   ├── Common/
│   │   ├── Exceptions/
│   │   │   ├── MatchNotFoundException.cs
│   │   │   ├── MatchAccessDeniedException.cs
│   │   │   └── MatchConflictException.cs
│   │   └── Mapping/
│   │       └── MatchStateMapper.cs
│   ├── Features/
│   │   └── Matches/
│   │       ├── Commands/
│   │       │   ├── CreateMatch/
│   │       │   │   ├── CreateMatchCommand.cs
│   │       │   │   └── CreateMatchCommandHandler.cs
│   │       │   ├── JoinMatch/
│   │       │   │   ├── JoinMatchCommand.cs
│   │       │   │   └── JoinMatchCommandHandler.cs
│   │       │   └── MakeMove/
│   │       │       ├── MakeMoveCommand.cs
│   │       │       └── MakeMoveCommandHandler.cs
│   │       ├── Queries/
│   │       │   ├── GetWaitingMatches/
│   │       │   │   ├── GetWaitingMatchesQuery.cs
│   │       │   │   └── GetWaitingMatchesQueryHandler.cs
│   │       │   └── GetMatchState/
│   │       │       ├── GetMatchStateQuery.cs
│   │       │       └── GetMatchStateQueryHandler.cs
│   │       └── Dtos/
│   │           ├── MatchStateDto.cs
│   │           ├── WaitingMatchDto.cs
│   │           ├── CreateMatchResult.cs
│   │           ├── JoinMatchResult.cs
│   │           └── GuestTokenIssue.cs
│   └── AssemblyReference.cs             # Assembly marker for Cortex registration
│
├── TicTacToe.Infrastructure/
│   ├── Persistence/
│   │   ├── GameDbContext.cs
│   │   ├── Configurations/
│   │   │   ├── GameMatchConfiguration.cs
│   │   │   └── MatchPlayerConfiguration.cs
│   │   ├── Repositories/
│   │   │   ├── GameMatchRepository.cs
│   │   │   └── MatchReadRepository.cs
│   │   ├── EfUnitOfWork.cs
│   │   └── Migrations/
│   ├── Security/
│   │   └── GuestTokenService.cs
│   └── DependencyInjection.cs
│
└── TicTacToe.Web/
    ├── Controllers/
    │   ├── LobbyController.cs
    │   └── MatchesController.cs
    ├── Hubs/
    │   └── GameHub.cs
    ├── Notifications/
    │   ├── IMatchNotifier.cs
    │   └── SignalRMatchNotifier.cs
    ├── Models/
    │   ├── CreateMatchInputModel.cs
    │   └── JoinMatchInputModel.cs
    ├── Views/
    │   ├── Lobby/Index.cshtml
    │   ├── Matches/Room.cshtml           # Waiting and playing states
    │   └── Shared/
    ├── wwwroot/
    │   ├── js/lobby.js
    │   ├── js/game.js
    │   └── css/game.css
    ├── Program.cs
    └── appsettings.json

tests/
├── TicTacToe.Domain.Tests/
│   ├── Entities/
│   │   ├── GameMatchCreateTests.cs
│   │   ├── GameMatchJoinTests.cs
│   │   ├── GameMatchMoveTests.cs
│   │   └── GameMatchOutcomeTests.cs
│   └── Rules/WinningLinesTests.cs
│
├── TicTacToe.Application.Tests/
│   ├── Features/Matches/Commands/
│   │   ├── CreateMatchCommandHandlerTests.cs
│   │   ├── JoinMatchCommandHandlerTests.cs
│   │   └── MakeMoveCommandHandlerTests.cs
│   ├── Features/Matches/Queries/
│   │   ├── GetWaitingMatchesQueryHandlerTests.cs
│   │   └── GetMatchStateQueryHandlerTests.cs
│   └── TestData/MatchTestFactory.cs
│
└── TicTacToe.IntegrationTests/
    ├── Persistence/
    │   ├── GameMatchRepositoryTests.cs
    │   ├── MatchReadRepositoryTests.cs
    │   └── SqlServerConcurrencyTests.cs
    ├── Web/
    │   ├── MatchEndpointsTests.cs
    │   ├── LobbySignalRTests.cs
    │   ├── GameHubTests.cs
    │   └── ReconnectTests.cs
    └── Fixtures/
        └── TestAppFactory.cs
```

## 2. Project references

```text
TicTacToe.Domain                   -> (no project references)
TicTacToe.Application              -> TicTacToe.Domain
TicTacToe.Infrastructure           -> TicTacToe.Application, TicTacToe.Domain
TicTacToe.Web                      -> TicTacToe.Application, TicTacToe.Infrastructure

TicTacToe.Domain.Tests             -> TicTacToe.Domain
TicTacToe.Application.Tests        -> TicTacToe.Application, TicTacToe.Domain
TicTacToe.IntegrationTests         -> TicTacToe.Web, TicTacToe.Infrastructure,
                                     TicTacToe.Application, TicTacToe.Domain
```

**Dependency direction:** Web → Application → Domain; Infrastructure → Application/Domain. The Web project references Infrastructure **only to register implementations in `Program.cs`**.

## 3. Layer ownership

| Layer | Owns | Must not contain |
|---|---|---|
| **Domain** | `GameMatch` aggregate, participants, rules, board state, turn/order, win/draw, domain errors | EF Core, CQRS, Cortex, SQL, SignalR, HTTP, repositories, UoW |
| **Application** | Commands, queries, handlers, DTOs, validation, guest authorization, repository/UoW/security interfaces | `DbContext`, SQL implementation, Hub, controller, Razor, Serilog concrete API |
| **Infrastructure** | EF Core mappings, SQL repositories, `EfUnitOfWork`, token generation/hashing, migrations | Game rules, MVC, SignalR notifications |
| **Web** | MVC endpoints, Razor/UI, SignalR group management and broadcast, HTTP error mapping, Serilog setup, DI composition | Game rules, direct SQL/EF queries, transactions |

## 4. Domain contract

**Aggregate root:** `GameMatch`. `MatchPlayer` exists only inside its match aggregate.

```text
GameMatch
  Id: Guid
  Status: MatchStatus
  BoardState: string               # Nine cells; '.' = empty, 'X'/'O' = occupied
  CurrentTurn: PlayerSymbol        # X starts
  Outcome: MatchOutcome
  Revision: int                    # Increases on accepted state changes
  CreatedAtUtc: DateTimeOffset
  CompletedAtUtc: DateTimeOffset?
  Players: IReadOnlyCollection<MatchPlayer>

MatchPlayer
  Id: Guid
  MatchId: Guid
  DisplayName: string              # Trimmed; 1–20 characters
  Symbol: PlayerSymbol
  TokenHash: string                # Opaque stored value; never serialized to clients
```

**Aggregate behavior:**

```text
GameMatch.Create(matchId, creatorPlayerId, name, symbol, creatorTokenHash)
GameMatch.Join(joinerPlayerId, name, joinerTokenHash)
GameMatch.MakeMove(playerId, cellIndex)
GameMatch.GetPlayerByTokenHash(tokenHash)  # Membership lookup; no cryptography
```

**Enforced invariants:** two player slots maximum; X/O must differ; X moves first; cells 0–8 only; one mark per cell; player must belong to the match; correct turn; exactly eight winning lines; draw on full board; no moves before start or after completion. Domain methods are the only way to change game state. Domain errors use `GameRuleException(GameErrorCode)`.

`Revision` starts at `0` on creation and increments on successful join or accepted move. The browser ignores older revisions. SQL Server write concurrency is protected separately with an EF Core **shadow `rowversion`**, not a database-specific property on Domain.

## 5. Application: CQRS + Cortex.Mediator

**Cortex namespaces and contracts:**

```csharp
using Cortex.Mediator.Commands;  // ICommand<TResult>, ICommandHandler<TCommand, TResult>
using Cortex.Mediator.Queries;   // IQuery<TResult>, IQueryHandler<TQuery, TResult>
```

| Use case | Cortex request | Result | Dependencies | Save |
|---|---|---|---|---|
| Create | `CreateMatchCommand` : `ICommand<CreateMatchResult>` | ID, guest token, initial match state | `IGameMatchRepository`, `IUnitOfWork`, `IGuestTokenService` | Once |
| Join | `JoinMatchCommand` : `ICommand<JoinMatchResult>` | Guest token, started match state | `IGameMatchRepository`, `IUnitOfWork`, `IGuestTokenService` | Once |
| Move | `MakeMoveCommand` : `ICommand<MatchStateDto>` | Committed match state | `IGameMatchRepository`, `IUnitOfWork`, `IGuestTokenService` | Once |
| Waiting list | `GetWaitingMatchesQuery` : `IQuery<IReadOnlyList<WaitingMatchDto>>` | Public waiting summaries | `IMatchReadRepository` | Never |
| Restore/state | `GetMatchStateQuery` : `IQuery<MatchStateDto>` | Authorized current state | `IMatchReadRepository`, `IGuestTokenService` | Never |

**Request fields:**

```text
CreateMatchCommand(DisplayName, CreatorSymbol)
JoinMatchCommand(MatchId, DisplayName, ExistingPlayerToken?)
MakeMoveCommand(MatchId, PlayerToken, CellIndex)
GetWaitingMatchesQuery()
GetMatchStateQuery(MatchId, PlayerToken)
```

**Policies:**

- Input names are trimmed and validated (1–20 characters). Guest tokens are never emitted in `MatchStateDto` or `WaitingMatchDto`.
- `IGuestTokenService` generates cryptographically random tokens and hashes incoming tokens. `MatchPlayer.TokenHash` is compared only after hashing.
- Join rejects an existing creator token from the same browser/session. Without sign-in, a person using an unrelated anonymous browser cannot be reliably identified as the creator.
- A handler loads the aggregate, checks membership where necessary, invokes Domain behavior, commits once, then returns a DTO. Do not return Domain entities to Web.
- Query handlers use read-only projections. Queries never modify state or call `SaveChangesAsync`.
- No handler references `IHubContext`, MVC types, EF types, or another handler's concrete class.

## 6. Repository and Unit of Work contracts

**Location:** `TicTacToe.Application/Abstractions/Persistence/`

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
```

**Implementation rules:**

1. `GameMatchRepository` loads a **tracked aggregate**, including players. A Domain change needs no generic `Update()` method.
2. `MatchReadRepository` uses `AsNoTracking()` and returns Application DTOs; it must never expose token hashes.
3. `EfUnitOfWork` and repositories share **one scoped `GameDbContext`**. Only the command handler invokes `SaveChangesAsync`.
4. `SaveChangesAsync` uses EF Core's atomic save and translates `DbUpdateConcurrencyException` into `MatchConflictException`.
5. Configure `GameMatch` with `.Property<byte[]>("RowVersion").IsRowVersion()` in Infrastructure. Two concurrent joins or stale moves cannot both commit.
6. No `IQueryable`, `DbSet`, generic CRUD base repository, automatic retries of rejected moves, or repository properties on the Unit of Work.

**Guest token port:**

```csharp
public sealed record GuestTokenIssue(string RawToken, string TokenHash);

public interface IGuestTokenService
{
    GuestTokenIssue Issue();
    string Hash(string rawToken);
}
```

## 7. Web contract and real-time flow

**Routes / responsibilities:**

| Entry point | Operation |
|---|---|
| `GET /` | `LobbyController.Index` → Razor lobby |
| `GET /matches/{matchId}` | `MatchesController.Room` → Razor waiting/game screen; no token in URL |
| `POST /matches` | `MatchesController.Create` → `CreateMatchCommand` → JSON token + match ID → notify lobby |
| `POST /matches/{matchId}/join` | `MatchesController.Join` → `JoinMatchCommand` → JSON token + match ID → notify lobby/match |
| `/hubs/game` | One SignalR `GameHub` |

**SignalR groups:** `lobby`, `match:{matchId}`.

| Client → Hub | Responsibility |
|---|---|
| `SubscribeLobby()` | Add connection to lobby, return current waiting list |
| `SubscribeMatch(matchId, token)` | Validate guest through `GetMatchStateQuery`, add to authorized match group, return current persisted state |
| `MakeMove(matchId, token, cellIndex)` | Dispatch `MakeMoveCommand`; broadcast **only after commit** |

| Server → client | Payload |
|---|---|
| `WaitingMatchesChanged` | Latest waiting-match summaries |
| `MatchStateChanged` | Latest `MatchStateDto` with `Revision` |

`SignalRMatchNotifier` (Web-only) uses `IHubContext<GameHub>` and is called by the HTTP controllers or Hub **after** the corresponding command succeeds. Neither Application nor Domain knows about SignalR.

**Browser session flow:** create/join POST returns the raw guest token **once**; JavaScript keeps it in `sessionStorage` keyed by match ID, then navigates to the match page. The match page calls `SubscribeMatch`; on reconnect, it subscribes again and reloads the authoritative state. Never place tokens in query strings, HTML, event DTOs, or logs. Enable MVC antiforgery on both POST operations; use HTTPS when deployed.

**Consistency rules:** successful commit precedes a broadcast; losing a concurrency race returns a conflict and causes client resynchronization; `Revision` prevents out-of-order events from rolling back the visible board. Notification loss does not lose stored game state.

## 8. Dependency injection, logging and errors

**Production DI:** `IServiceCollection` only. Autofac is **test-only** through `Autofac.Extras.Moq`; no Autofac service-provider factory in the Web application.

| Registration | Lifetime |
|---|---|
| `GameDbContext` | Scoped |
| `IGameMatchRepository` → `GameMatchRepository` | Scoped |
| `IMatchReadRepository` → `MatchReadRepository` | Scoped |
| `IUnitOfWork` → `EfUnitOfWork` | Scoped |
| `IGuestTokenService` → `GuestTokenService` | Singleton (stateless implementation) |
| `IMatchNotifier` → `SignalRMatchNotifier` | Scoped |
| Cortex handlers | Resolved via `AddCortexMediator` using **Application** assembly marker |

`Program.cs` registers MVC, SignalR, Cortex.Mediator (scan `typeof(TicTacToe.Application.AssemblyReference)`), Infrastructure services, and Serilog; maps routes and `/hubs/game`. Application/Infrastructure use `ILogger<T>` when necessary; only Web configures Serilog. Do not log commands that contain guest tokens.

**Error mapping in Web:** validation/domain error → `400`; unauthorized guest → `403` (or a uniform `404` for private match lookups); missing match → `404`; stale/full match → `409`; unexpected error → `500` with a safe message. SignalR returns equivalent safe Hub errors.

## 9. NUnit test architecture

**Unit test packages:** `NUnit`, `NUnit3TestAdapter`, `Microsoft.NET.Test.Sdk`, `Shouldly`, `Autofac.Extras.Moq` (uses Moq). **Only the test project references `Autofac.Extras.Moq`/Autofac.**

| Suite | Test subject | Strategy |
|---|---|---|
| `Domain.Tests` | Rules and aggregate transitions | Real Domain objects; **no mocks** |
| `Application.Tests` | **Every Cortex command and query handler** | NUnit + Shouldly; `AutoMock.GetLoose()` / configured mocks; no DB, SignalR, or Web host |
| `IntegrationTests` | EF Core SQL Server, HTTP endpoints, SignalR, reconnection | NUnit + real SQL Server test database + ASP.NET Core test host / actual SignalR client |

**Required Application test rule:** Every handler must have success, relevant validation/failure/authorization, and persistence-interaction tests. Every successful state-changing handler saves **exactly once**; rejected actions save **zero** times. Queries save **never**. Verify parameters and cancellation tokens where meaningful.

### Feature-by-feature test coverage (mandatory)

| PRD feature | Unit tests | Integration tests |
|---|---|---|
| FR-01 Guest names | Create/Join handler invalid/trim/max-length tests; Domain name invariants | Create/Join endpoint validation |
| FR-02 Create match | `CreateMatchCommandHandlerTests`: creator symbol, waiting state, token, Add, one Save | Stored match and lobby notification |
| FR-03 Waiting lobby | `GetWaitingMatchesQueryHandlerTests`: empty/populated/query failures; no Save | Live appearance/removal in two clients |
| FR-04 Join match | `JoinMatchCommandHandlerTests`: opposite symbol, start, self-token rejection, full/missing/invalid match, one Save | Two simultaneous join attempts; only one succeeds |
| FR-05 Make move | `MakeMoveCommandHandlerTests`: authorized valid move, wrong turn, occupied/out-of-range cell, no Save on failure | Two clients receive the same board; competing moves |
| FR-06 Win/draw | `GameMatchOutcomeTests`: all 8 win lines, draw, completed-board rejection; move handler result | Both clients receive completion state |
| FR-07 Refresh/reconnect | `GetMatchStateQueryHandlerTests`: authorized state, invalid token, missing match, no Save | Disconnect/reconnect and persisted state after refresh |
| FR-08 Error feedback | Handler exceptions, invalid credentials, no state mutation | Safe HTTP/Hub errors and state resynchronization |

**Example standard for an Application handler test:**

```csharp
[Test]
public async Task Handle_ValidCreateMatch_SavesExactlyOnce()
{
    using var mock = AutoMock.GetLoose();
    mock.Mock<IGuestTokenService>()
        .Setup(x => x.Issue())
        .Returns(new GuestTokenIssue("test-raw-token", "test-hash"));
    mock.Mock<IGameMatchRepository>()
        .Setup(x => x.AddAsync(It.IsAny<GameMatch>(), It.IsAny<CancellationToken>()))
        .Returns(Task.CompletedTask);
    mock.Mock<IUnitOfWork>()
        .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(1);

    var handler = mock.Create<CreateMatchCommandHandler>();
    var result = await handler.Handle(
        new CreateMatchCommand("Alex", PlayerSymbol.X), CancellationToken.None);

    result.AccessToken.ShouldBe("test-raw-token");
    mock.Mock<IGameMatchRepository>().Verify(
        x => x.AddAsync(It.IsAny<GameMatch>(), It.IsAny<CancellationToken>()),
        Times.Once);
    mock.Mock<IUnitOfWork>().Verify(
        x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
}
```

_Imports omitted for brevity; use `Autofac.Extras.Moq`, `Moq`, `NUnit.Framework`, and `Shouldly`. The handler and DTO signatures above are the contracts this test targets._

**Integration gate:** real SQL Server (not EF Core InMemory) validates foreign keys, migration, `rowversion`, duplicate joins, and concurrent moves. `WebApplicationFactory` / a running test server validates MVC + SignalR transport. Do not claim end-to-end coverage from mocked Application tests alone.

## 10. Engineering constraints

- All handlers and persistence methods support `CancellationToken`.
- No Domain public setters that let a handler rewrite board, symbol, status, or winner.
- No cross-layer DTO leakage into Domain; no HTTP or SignalR APIs in Application.
- No Unit of Work save in repositories; no accidental double-commit from mediator behaviors.
- No direct use of `DbContext` in Web; no repositories injected into a Hub/controller.
- Apply SOLID via single-purpose handlers, focused contracts, invariant-rich aggregate, constructor injection, and substitutable repository test doubles.
- Commit migrations and tests with each feature; run `dotnet build` and `dotnet test` after every feature.
- **MVP only:** no leaderboard, rematch, authentication system, Redis, microservices, generic CRUD framework, or separate read database.

**Acceptance gate:** Every FR-01–FR-08 has passing applicable unit and integration tests, then a successful two-browser demo.
