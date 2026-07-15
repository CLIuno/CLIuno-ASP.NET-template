# CLIuno ASP.NET template

ASP.NET Core 8 + EF Core (SQLite) REST API serving the CLIuno demo app — auth (JWT with
refresh, reset, email verification, OTP), users, todos, posts + comments, follows and
roles — against the shared CLIuno contract under `/api/v1`. Registering the first user
creates the `user` role automatically, so a fresh clone works against an empty database.

## Run

```bash
dotnet run --project BackendASP.NET     # serves http://localhost:3000/api/v1
dotnet build                            # compile
```

The server port comes from `PORT` (default **3000**); data lives in a local SQLite
`BackendASP.NET/db.sqlite` (EF Core `EnsureCreated` — delete the file to reset the schema).

## The API contract

Login sends `{usernameOrEmail, password}` and returns `data.token` (+ `data.refreshToken`).
Every response is `{status, message, data}` with the exact keys CLIuno frontends
destructure (`data.users/user/todos/todo/posts/post/followers/following/isFollowing`).
Request keys are camelCase; resource fields are snake_case. Any CLIuno frontend template
(each runs on its own dev port and calls this API at `:3000`) works against this backend.

## License

GNU Affero General Public License v3.0 — see [LICENSE](LICENSE).
