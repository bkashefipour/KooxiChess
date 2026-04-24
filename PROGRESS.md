# KooxiChess — Progress & Todo

## Phase Status

| Phase | Description | Status |
|---|---|---|
| Phase 1 | Foundation — Chess.Shared domain layer | ✅ Complete |
| Phase 2 | Server — ASP.NET Core Web API + SignalR hub | ✅ Complete |
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
- [x] All projects upgraded to net10.0 to match installed SDK and workloads

---

## Phase 2 — Server + SignalR Hub ✅ Complete

**Startup**
- [x] `Program.cs` — DI registration, middleware pipeline, SignalR, CORS, OpenAPI
- [x] `appsettings.json` — Azure AD placeholders, Cosmos DB connection string (emulator)
- [x] Azure AD auth — conditional, activates when TenantId is configured; dev fallback is anonymous
- [x] Scalar UI — interactive API explorer at `/scalar/v1`

**SignalR Hub**
- [x] `Hubs/GameHub.cs` — `JoinGame`, `MakeMove`, `Resign`, `OfferDraw`, `AcceptDraw`, `DeclineDraw`
- [x] Connection lifecycle — `OnDisconnectedAsync` marks player offline, reconnect detection on `JoinGame`
- [x] Clock tick on each move — deducts elapsed time, adds increment, resets `ClockStartedAt`

**Controllers**
- [x] `Controllers/LobbyController.cs` — list open games, create game, join game, get game state
- [x] `Controllers/GameController.cs` — get game, get move history, list all games
- [x] `Controllers/AuthController.cs` — `/api/auth/me` (JWT claims), `/api/auth/ping` (health check)

**Services**
- [x] `Services/GameEngineService.cs` — full chess rules engine
  - Pseudo-legal move generation for all piece types
  - Legal move filtering (no self-check)
  - Castling with king/rook path and attack validation
  - En passant capture and target tracking
  - Pawn promotion (all four promotion pieces)
  - Check, checkmate, stalemate detection
  - Fifty-move rule and insufficient material draw detection
  - SAN notation generation with disambiguation
- [x] `Services/MoveValidatorService.cs` — validates `MoveDto` against legal moves
- [x] `Services/MatchmakerService.cs` — in-memory cache + Cosmos-backed persistence, create/join/reconnect/disconnect

**Data — Azure Cosmos DB**
- [x] `Data/GameDocument.cs` — serializable Cosmos document (flattens Board 2D array to piece list, Newtonsoft.Json `[JsonProperty("id")]`)
- [x] `Data/GameRepository.cs` — upsert, get by ID, query by status; auto-creates `KooxiChessDb/Games` container on startup
- [x] Emulator configured — `AccountEndpoint=https://localhost:8081/`, SSL bypass for self-signed cert, Gateway mode
- [x] Partition key `/GameId`, persistence on every state change (create, join, move, resign, draw)

**Background Services**
- [x] `BackgroundServices/ClockWorker.cs` — ticks every second, broadcasts `ClockUpdate`, triggers `GameOver` on timeout

---

## Phase 3 — Client + Board UI 🔲 Not Started

> Goal: playable game in the browser with MSAL login.

**Startup**
- [x] `Program.cs` — Blazor WASM startup, HttpClient DI (scaffolded)
- [x] `App.razor` — root component and router (scaffolded)
- [x] `_Imports.razor` — global using directives (scaffolded)
- [x] `wwwroot/index.html` — Blazor WASM host page (scaffolded)
- [x] `wwwroot/css/app.css` — base styles (scaffolded)

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

---

## Phase 4 — Polish 🔲 Not Started

- [ ] Matchmaking lobby — waiting room UI + server-side player matching queue
- [ ] Move clocks — client-side interpolation between server `ClockUpdate` broadcasts
- [ ] Azure SignalR Service scale-out — swap `AddSignalR()` for `AddAzureSignalR()` in `Program.cs`
- [ ] Spectator mode — read-only SignalR group join, no move input
- [ ] Threefold repetition draw detection
- [ ] Insufficient material — same-color bishop edge case
