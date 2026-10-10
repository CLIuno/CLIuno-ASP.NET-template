# CLIuno ASP.NET template

ASP.NET Core 8 + EF Core (SQLite) REST API serving the CLIuno contract: JWT auth
(refresh, reset, email verification, OTP via Otp.NET), users, todos, posts+comments,
follows, roles — under `/api/v1`.

## Commands

```bash
dotnet run --project BackendASP.NET    # dev server (PORT env, default 3000)
dotnet build                           # compile
dotnet test                            # (no test project yet)
```

SQLite `BackendASP.NET/db.sqlite` auto-creates (`EnsureCreated`; delete for a reset —
EF does not migrate schema changes on an existing file). JWT secrets:
`JWT_SECRET_KEY` / `REFRESH_JWT_SECRET_KEY` env (dev defaults exist).

## Structure

- `Program.cs` — DI, snake_case `System.Text.Json` policy (dictionary keys are exempt —
  envelope keys like `isFollowing` stay literal), `IgnoreCycles`, and the middleware
  that turns `ApiException` into the shared error envelope.
- `Controllers/` — one controller per resource; handlers call
  `auth.Require(Request, db)` (Bearer → User or 401) and `AuthSupport.RequireAdmin`.
- `Data/Models.cs` — entities + `AppDb`; secrets and back-references are
  `[JsonIgnore]`; `ImageUrl` keeps its casing via `[JsonPropertyName]`.
- `Support/` — `Api` envelope builders, `JwtService`
  (System.IdentityModel.Tokens.Jwt), `TotpService` (Otp.NET), `AuthSupport`.

## Contract rules this codebase follows

- Responses: `{status, message, data}` with exact keys (`data.users/user/todos/todo/`
  `posts/post/followers/following/isFollowing`, login `data.token` + `data.refreshToken`).
- camelCase request keys; one-time tokens on the user row (`ResetToken`/`VerifyToken`
  columns), lookup by token; the `user` role is created on first registration.

## Conventions

Conventional commits; `.vs/` and `*.sqlite` are gitignored.

## AI agents (Claude Code and friends)

- **Check the CLIuno skills first**: `npx cliuno@latest skills status` — if any is
  `missing`/`outdated`, run `npx cliuno@latest skills install` (adds `.claude/skills/cliuno-*`:
  the generator workflow, the scaffold guide, and the shared REST contract).
- **Generate resources, don't hand-write them**:
  `npx cliuno@latest make:resource Product --fields name:string,price:number,inStock:boolean`
  creates `BackendASP.NET/Data/Product.cs`, `BackendASP.NET/Controllers/ProductsController.cs` and registers it in `BackendASP.NET/Data/Models.cs`, following this
  template's conventions and the `{status, message, data}` contract. `--dry-run` previews,
  re-running never duplicates registrations, `--force` overwrites.
- Single pieces: `make:model`, `make:controller`. `npx cliuno@latest make` lists them.
- Then edit fields/business logic only, and run the gates above.
