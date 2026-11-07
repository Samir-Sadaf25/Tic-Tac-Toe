# TicTacToe Live — Technology Stack

**Version:** 1.0  
**Status:** Architecture proposal / agreed technology choices  
**Platform:** .NET 10  
**Project type:** Small educational, real-time multiplayer web application  
**Companion documents:** `TicTacToe_Live_PRD.md` and `project-architecture.md`

> **Purpose:** Record *which technologies we will use*, why they fit the PRD, and what we will deliberately avoid. This document is not the feature-by-feature implementation plan.

## 1. Project constraints and decisions

We are building a **single ASP.NET Core application** for two guest players, not a distributed gaming platform. Two browsers can create/join a match, make moves live, and recover the persisted board after refresh. A classroom demonstration and understanding the code are more important than unnecessary scale.

The following choices are **requested and accepted**:

- .NET 10, ASP.NET Core MVC and Razor Views.
- ASP.NET Core SignalR for real-time lobby and match messages.
- SQL Server and Entity Framework Core 10 for persistence.
- Serilog for structured application logging.
- Built-in Microsoft Dependency Injection through `IServiceCollection`.
- Clean Architecture with Domain, Application, Infrastructure, and Web projects.
- CQRS with **Cortex.Mediator**, **not** the `MediatR` package.
- Repository and Unit of Work patterns.
- SOLID principles and clear separation of concerns.

**Important distinction:** CQRS here means separate command and query *code paths*. Both use the **same SQL Server database**. We do not need separate read databases, event sourcing, or message brokers.

## 2. Stack at a glance

| Concern | Chosen technology | Notes |
|---|---|---|
| Language and runtime | **C# / .NET 10 (`net10.0`)** | One target framework across app and tests. |
| Application host | **ASP.NET Core 10 MVC** | Controllers and server-rendered Razor Views; a separate Web API is unnecessary. |
| Views and styling | **Razor Views, Bootstrap 5, small custom CSS** | One lobby, waiting screen, and 3×3 game board. |
| Browser code | **Vanilla JavaScript** | Keeps a small game easy to understand; no React/Angular dependency. |
| Real-time transport | **ASP.NET Core SignalR** + JavaScript client | Named lobby and match groups; live notifications. |
| Database | **Microsoft SQL Server** | Local SQL Server / Developer / Express / LocalDB, as supported by the machine. |
| Data access | **EF Core 10 + SQL Server provider** | Code First, Fluent API configurations, migrations. |
| Mediator and CQRS | **Cortex.Mediator 3.1.2** | Use `ICommand<T>`, `IQuery<T>`, handlers, and the Cortex DI registration. |
| Dependency injection | **Microsoft.Extensions.DependencyInjection / `IServiceCollection`** | Scoped DbContext, UoW, and repositories. No Autofac. |
| Data access boundaries | **Focused repositories + one Unit of Work** | Interfaces in Application; implementations in Infrastructure. |
| Logging | **Serilog + Serilog.AspNetCore** | Structured logs through `ILogger<T>`; console and rolling file sinks for development. |
| Testing | **NUnit + Shouldly** | Consistent with the existing practice repository; unit and integration tests. |
| Source control | **Git + GitHub** | Small task branches/commits, review AI-produced changes. |
| Local tooling | **.NET SDK, Visual Studio/Rider/VS Code, SQL Server, `dotnet ef`** | Pick an editor already available. |

### Package guidance

- Pin the exact NuGet versions in the solution, preferably via `Directory.Packages.props`, **after running a restore/build**. Use a consistent supported **10.0.x** version for Microsoft EF Core packages; do not mix EF Core major versions.
- The reviewed practice repository currently references **Cortex.Mediator 3.1.2**, **Serilog.AspNetCore 10.0.0**, and EF Core SQL Server **10.0.11**. These are **observations from that repository**, not a promise that every dependency will remain the newest release.
- Add `Microsoft.EntityFrameworkCore.SqlServer` to **Infrastructure**, and `Microsoft.EntityFrameworkCore.Design` to the appropriate startup/migrations setup as a development-only dependency.
- Add `Cortex.Mediator` to **Application** (for command/query abstractions) and the **Web** composition root (for registration and dispatch).
- Add `Serilog.AspNetCore` to **Web** and configure console/rolling-file sinks as needed. Other layers use `ILogger<T>` or no logging, not Serilog static calls.
- SignalR server support comes with ASP.NET Core's shared framework; the **browser SignalR JavaScript client** must be loaded separately (from a locally managed script or a version-pinned package/CDN).
- For tests, use `NUnit`, `NUnit3TestAdapter`, `Microsoft.NET.Test.Sdk`, `Shouldly`, and an optional mocking library when needed. Avoid introducing Autofac just for tests.



## 4. Project-to-package ownership

| Project | Primary dependencies | Must not depend on |
|---|---|---|
| `TicTacToe.Domain` | .NET base library only | Application, EF Core, MVC, SignalR, SQL Server, mediator, Serilog |
| `TicTacToe.Application` | Domain, Cortex.Mediator; optional logging/validation abstractions | Web, Infrastructure, EF Core, SignalR, Serilog implementation |
| `TicTacToe.Infrastructure` | Application, Domain, EF Core SQL Server, DI abstractions | Web / Razor / SignalR |
| `TicTacToe.Web` | Application, Infrastructure, ASP.NET Core MVC, SignalR, Cortex.Mediator, Serilog | Direct database queries or game rules inside controllers/hubs |
| `TicTacToe.Domain.Tests` | Domain, NUnit, Shouldly | Database or web server |
| `TicTacToe.Application.Tests` | Application, Domain, test doubles, NUnit, Shouldly | Live database for isolated unit tests |
| `TicTacToe.IntegrationTests` (later) | Web/Infrastructure test harness, SQL Server test instance | In-memory EF tests as a substitute for SQL Server concurrency coverage |

**Reference direction:** Application → Domain; Infrastructure → Application (+ Domain transitively); Web → Application + Infrastructure. Domain references no other project.

## 5. Security and correctness essentials

- **Guest identity is not a display name.** Generate a random per-player, per-match access token on create/join, store only its hash on the server, and retain the raw token in the same browser's `sessionStorage` to support refresh. Transmit it only over HTTPS when deployed.
- The server resolves the caller's authorized player/symbol from the token. The client submits **match ID + token + cell index**, not a board or winner.
- Use standard MVC anti-forgery protection for state-changing HTTP forms and keep XSS protections enabled; malicious JavaScript can steal `sessionStorage` tokens.
- The creator can be blocked from joining again using the *same credential/session*. **Without authentication, the server cannot reliably prove that two different browsers belong to the same human**; document this limitation rather than claiming otherwise.
- Store committed match state in SQL Server. Never use a static dictionary or SignalR connection ID as the permanent source of truth.
- Detect SQL concurrency conflicts on join/move and return a controlled conflict result; the client resynchronizes from persisted state.



## 7. Development standards

- Enable nullable reference types, async/await, and cancellation tokens for I/O.
- Treat every external input (HTTP or SignalR) as untrusted.
- Use feature-first names in Application: `Features/Matches/Commands/...` and `Queries/...`.
- Run `dotnet build` and `dotnet test` after each meaningful AI-assisted change; inspect diffs before merging.
- Commit EF Core migrations to Git. Put credentials in environment variables or user secrets, never in source control.
- Design for SOLID, but **no architecture document can guarantee zero violations**. Enforce boundaries through code review, tests, and incremental refactoring.

