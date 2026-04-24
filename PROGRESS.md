# KooxiChess — Progress & Todo

## Phase Status

| Phase | Description | Status |
|---|---|---|
| Phase 1 | Foundation — Chess.Shared domain layer | ✅ Complete |
| Phase 2 | Server — ASP.NET Core Web API + SignalR hub | 🔲 Not started |
| Phase 3 | Client — Blazor WebAssembly board UI | 🔲 Not started |
| Phase 4 | Polish — Clocks, lobby, persistence, scale-out | 🔲 Not started |

---

## Phase 1 — Foundation ✅ Complete

### Chess.Shared

**Enums**
- [x] `PieceType` — Pawn, Knight, Bishop, Rook, Queen, King
- [x] `PieceColor` — White, Black
- [x] `GameStatus` — 9 states (WaitingForOpponent → Checkmate/TimedOut/Abandoned)
- [x] `MoveType` — Normal, Capture, CastleKingSide, CastleQueenSide, EnPassant, PawnPromotion

**Models**
- [x] `Square` — algebraic notation parsing (`"e4"` ↔ file/rank)
- [x] `Piece` — FEN character export (`ToFenChar()`)
- [x] `Board` — 8×8 grid, FEN serialization (both directions), `Clone()` for move simulation
- [x] `Move` — full metadata (notation, timestamps, move number, captured piece)
- [x] `GameState` — root aggregate (board, active color, castling rights, en passant, clocks, move history, FEN export)
- [x] `Player` — Azure AD identity (UserId, DisplayName, Email, ConnectionId, Color)

**DTOs**
- [x] `MoveDto` — slim client→server payload (GameId, From, To, optional PromotionPiece)
- [x] `MoveResultDto` — server→client broadcast (notation, new FEN, check/checkmate flags)
- [x] `GameDto` — full state sync (FEN, status, players, clocks, move history)
- [x] `PlayerDto` — lightweight player info
- [x] `LobbyGameDto` / `CreateGameDto` — lobby listing and creation

**Constants**
- [x] `HubMethods` — all SignalR method names as typed constants (no magic strings)
- [x] `ApiRoutes` — all REST endpoint paths as constants

**Project Scaffolding**
- [x] `Chess.Server.csproj` — NuGet packages + Chess.Shared reference configured
- [x] `Chess.Client.csproj` — NuGet packages + Chess.Shared reference configured

---

## Phase 2 — Server + SignalR Hub 🔲 Not Started

> Goal: full move lifecycle testable via SignalR test client — no UI needed.

**Startup**
- [ ] `Program.cs` — DI registration, middleware pipeline, SignalR, auth, EF Core
- [ ] `appsettings.json` — Azure AD (TenantId, ClientId, Audience), connection string template

**SignalR Hub**
- [ ] `Hubs/GameHub.cs` — `JoinGame`, `MakeMove`, `Resign`, `OfferDraw`, `AcceptDraw`, `DeclineDraw`

**Controllers**
- [ ] `Controllers/GameController.cs` — get game, game history
- [ ] `Controllers/LobbyController.cs` — list games, create game, join game
- [ ] `Controllers/AuthController.cs` — user profile endpoint

**Services**
- [ ] `Services/GameEngineService.cs` — move generation and full chess rules
- [ ] `Services/MoveValidatorService.cs` — validates `{from, to}` against legal moves
- [ ] `Services/MatchmakerService.cs` — in-memory game store and player matching

**Data**
- [ ] `Data/ChessDbContext.cs` — EF Core DbContext
- [ ] `Data/Repositories/` — game and player repositories

**Auth**
- [ ] `Auth/` — Azure AD JWT configuration and policy setup

**Background Services**
- [ ] `BackgroundServices/ClockWorker.cs` — per-game clock ticker and timeout enforcement

---

## Phase 3 — Client + Board UI 🔲 Not Started

> Goal: playable game in the browser with MSAL login.

**Startup**
- [ ] `Program.cs` — Blazor startup, MSAL configuration, SignalR client DI
- [ ] `App.razor` — root component and router
- [ ] `_Imports.razor` — global using directives

**Components**
- [ ] `Components/BoardComponent.razor` — 8×8 grid rendering
- [ ] `Components/PieceComponent.razor` — piece rendering with drag-and-drop
- [ ] `Components/LobbyComponent.razor` — game list and create/join UI
- [ ] `Components/ClockComponent.razor` — countdown display (interpolated from server)

**Pages**
- [ ] `Pages/GamePage.razor` — active game view (board + clocks + move history)
- [ ] `Pages/LobbyPage.razor` — lobby/waiting room
- [ ] `Pages/ProfilePage.razor` — user profile and game history

**Services**
- [ ] `Services/ChessHubService.cs` — SignalR client wrapper (connect, send moves, receive broadcasts)
- [ ] `Services/AuthService.cs` — MSAL login/logout wrapper

**Static Assets**
- [ ] `wwwroot/index.html` — Blazor WASM host page
- [ ] `wwwroot/css/app.css` — base styles and board theme
- [ ] `wwwroot/images/` — piece image assets

---

## Phase 4 — Polish 🔲 Not Started

- [ ] Matchmaking lobby — waiting room UI + server-side player matching queue
- [ ] Move clocks — `ClockWorker` timeout enforcement + client-side interpolation
- [ ] Game history persistence — EF Core migrations + Azure SQL storage
- [ ] Azure SignalR Service scale-out — `AddAzureSignalR()` in `Program.cs`
- [ ] Spectator mode — read-only SignalR group join, no move input
