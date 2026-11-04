# .NET coding standard

The general standard for all of this owner's .NET projects. It is kept in the My-Newsfeed repository so it is
versioned and travels with the code; another project copies this file into its own `docs/` and keeps it in step.
Each project may add a short addendum for its own stack (here, [coding-standards.md](coding-standards.md)); where
the two disagree, the project addendum wins for that project.

Drafted November 4, 2026 (America/Chicago) from an audit of My-Newsfeed. Most rules write down what that codebase
already does in the large majority of files; the rest pick one side where it was split.

**How to read this:** **[Enforced]** means the build, an analyzer, ESLint or Prettier fails the change. **[Review]**
means a person or agent checks it. Existing code that breaks a rule is brought into line in dedicated clean-up
commits, one concern per commit, never mixed into an unrelated feature. Section 15 covers keeping it that way.

## 1. Tooling baseline

Every solution starts with these files, copied from My-Newsfeed and adjusted:

| File | Contents |
| --- | --- |
| `global.json` | Pinned SDK (`rollForward: latestFeature`) and the test runner. CI reads it (`global-json-file`). |
| `Directory.Build.props` | `TargetFramework`, `Nullable=enable`, `ImplicitUsings=enable`, `AnalysisLevel=latest-recommended`, `EnforceCodeStyleInBuild=true`, `TreatWarningsAsErrors=true` (NuGet advisories `NU1901-NU1904` excepted). |
| `.editorconfig` | LF, UTF-8, 4-space indent (2 for JSON/YAML/MSBuild), final newline, the C# rules below. |
| `eslint.config.js`, `.prettierrc.json`, `.prettierignore` | When the project has first-party JavaScript or CSS. Vendored libraries and generated or scratch output (`bin/`, `obj/`, `wwwroot/lib/`, `test-results/`, `TestResults/`) are ignored. |

- **[Enforced]** The build has zero warnings. Don't silence an analyzer with `#pragma` or `[SuppressMessage]`
  without a one-line reason next to it; downgrade a rule project-wide in `.editorconfig` only with a comment saying why.
- **[Enforced]** CI runs, on every push and PR: `dotnet build`, the full test suite, `dotnet format --verify-no-changes`,
  and when there is JavaScript or CSS, `npm run lint` and `npm run format:check`. A filtered "smoke" subset is
  not enough on its own.
- **[Review]** Secrets never appear in the repository: not in `appsettings*.json`, tests, scripts, docs or commit
  messages. Use user secrets locally and environment variables in containers. A secret that was ever committed is
  rotated, not just deleted.

## 2. Layout and naming

- **[Enforced]** File-scoped namespaces. `using` directives at the top of the file, outside the namespace, `System.*` first.
- **[Review]** Namespace = assembly name + folder path. A project's `RootNamespace` matches its assembly name.
  Don't reach across with fully qualified names in code; add a `using`.
- **[Review]** One primary type per file, and the file is named after it. Small records or enums returned only by
  that type may live in the same file, below it.
- **[Enforced]** PascalCase for types, methods, properties, events and constants. **[Review]** camelCase for
  locals, parameters and private fields, without a leading underscore.
- **[Review]** Every method that returns `Task`/`ValueTask` ends in `Async`. Test methods are the exception.
- **[Review]** Type names say their role:

| Role | Name | Shape |
| --- | --- | --- |
| Database reads | `XxxQuery` | `public static class` with `XxxAsync(TContext db, …, CancellationToken ct = default)` methods |
| Database writes / commands | `XxxStore` | `public static class`, same signature style |
| HTTP endpoints | `XxxEndpoints` | `public static class` with `MapXxx(this WebApplication app)` |
| Hosted background work | `XxxService` | subclass of `BackgroundService` |
| Typed outbound HTTP | `XxxClient` | registered with `AddHttpClient<XxxClient>()` |
| Options | `XxxOptions` | sealed class with `public const string Section = "Xxx";` |
| Tests | `XxxTests` | one class per feature or type under test |

  Don't introduce "Repository", "Manager", "Helper" or "Data" types; pick the role above, or a noun that says what it does.

## 3. C# style

- **[Enforced]** Nullable reference types on; no `!` (null-forgiving) except `= default!` on `[EditorRequired]`
  Blazor parameters.
- **[Review]** DTOs and query results are `public sealed record` with positional properties. Collections returned
  from methods or held in records are `IReadOnlyList<T>`, not `List<T>` or arrays.
- **[Review]** Mark classes `sealed` unless they are designed for inheritance. EF entity classes included.
- **[Review]** Prefer `var`, primary constructors, collection expressions (`[]`), target-typed `new()`, switch
  expressions and pattern matching. Use `is null` / `is not null`, never `== null`, except inside expression trees
  (EF LINQ predicates), where an `is` pattern doesn't compile (CS8122).
- **[Review]** Expression-bodied members for one-liners; block bodies once a method needs more than one statement.
- **[Review]** Regular expressions are `[GeneratedRegex]` partial methods, not `new Regex` or `Regex.IsMatch(…, pattern)`.
- **[Review]** Parse and format with `CultureInfo.InvariantCulture` for anything that isn't shown to a reader.
- **[Review]** Comments: a one-line `/// <summary>` on every public type and on members whose purpose isn't obvious
  from the name. Inside methods, comment *why*, not *what*. No commented-out code.

## 4. Async, cancellation and time

- **[Review]** Every async method that does I/O takes `CancellationToken ct = default` as its **last** parameter and
  passes it to everything it awaits, including `CreateDbContextAsync(ct)`, EF calls and `HttpClient` calls.
  Endpoint handlers and components get their token from the framework (`HttpContext.RequestAborted`, the
  handler's `CancellationToken` parameter) and pass it down.
- **[Review]** No sync-over-async (`.Result`, `.Wait()`, `GetAwaiter().GetResult()`). No `ConfigureAwait(false)` in
  application code (there is no synchronization context in ASP.NET Core).
- **[Review]** Read the clock once, at the edge: inject `TimeProvider` into endpoints, components, services and
  hosted services, call `GetUtcNow()`, and pass the value down as a `DateTime nowUtc` parameter. Query and helper
  code never calls `DateTime.Now`, `DateTime.UtcNow` or `DateTimeOffset.UtcNow`.
- **[Review]** Store and compare times in UTC (`datetime2`, names ending in `At`). Convert to the display time zone
  only for presentation, through one configured zone object; never hard-code a zone ID in feature code. An unknown
  zone ID is a startup error, not a silent UTC fallback.

## 5. Configuration and dependency injection

- **[Review]** Configuration is read through options classes (`services.Configure<XxxOptions>(config.GetSection(XxxOptions.Section))`
  and `IOptions<XxxOptions>`), not `IConfiguration["..."]` in feature code. `Program.cs` may read a value directly
  only to decide what to register.
- **[Review]** Registrations shared by more than one host live in one `AddXxx(this IServiceCollection)` extension
  method in the shared project; each host's `Program.cs` calls it and adds only what is specific to that host.
- **[Review]** Lifetimes: stateless services and services that only hold an `IDbContextFactory`/`HttpClient`
  factory are **singletons**; anything holding per-request state is **scoped**; **transient** only for cheap,
  stateful-per-use objects. `HttpClient`s come from `IHttpClientFactory`; prefer typed clients (`XxxClient`) over
  named ones, and if a named client is unavoidable its name is a `const`.
- **[Review]** Inject `IMemoryCache`; don't construct a private `MemoryCache`.

## 6. Data access (EF Core)

- **[Review]** Register `AddDbContextFactory`, never `AddDbContext`. Each operation creates its own context:
  `await using var db = await factory.CreateDbContextAsync(ct);`. Never share a context across concurrent work or
  hold one in a singleton or a long-lived component.
- **[Review]** UI code (Razor components, endpoint lambdas) doesn't write LINQ against the context. It creates the
  context and calls an `XxxQuery`/`XxxStore` method, so every query is in one testable place.
- **[Review]** Reads use `AsNoTracking()` and project with `Select` into records; entities don't leave the query
  class. Every list read is bounded (`Take`, a page size constant, or a keyset cursor). Prefer keyset paging over
  `Skip/Take` for feeds.
- **[Review]** Raw SQL uses the interpolated, parameterised APIs only: `FromSql`, `SqlQuery<T>`, `ExecuteSqlAsync`.
  No `*Raw` variants with concatenated strings. Hand-written `DbCommand` code is allowed only where EF can't express
  the statement, with a comment saying why, and always with `DbParameter`s.
- **[Review]** Read-modify-write of a shared row (JSON settings columns, lists) takes a row lock (`UPDLOCK`) inside a
  transaction, or uses `ExecuteUpdateAsync` with a concurrency condition.
- **[Review]** Bulk updates and deletes use `ExecuteUpdateAsync`/`ExecuteDeleteAsync`.

## 7. Errors and logging

- **[Review]** Expected outcomes (not found, already exists, locked, invalid choice) are return values: an enum or a
  small result record. Exceptions are for broken invariants and infrastructure failure.
- **[Review]** Catch narrowly with an exception filter: `catch (Exception ex) when (IsReadFailure(ex))`. Each app has
  one shared filter for "the database or a remote source is unavailable"; use it everywhere instead of ad-hoc
  `catch (DbException)`. Never `catch (Exception)` without a filter except at a top-level loop boundary (a hosted
  service iteration), and there, log and continue.
- **[Review]** Never swallow an exception silently. A degrade-gracefully catch logs at `Warning` and the UI shows an
  "unavailable" state, so a failure is distinguishable from "no data".
- **[Review]** When a page is assembled from several independent reads, a failure in one section degrades only that
  section.
- **[Review]** Custom exceptions are top-level `public sealed class XxxException : Exception` in the feature's
  namespace, not nested types.
- **[Review]** Logging uses message templates with named placeholders (`log.LogWarning("Sync failed for {League}", league)`),
  never string interpolation. The logger field or parameter is named `log`. Messages are sentence case without a
  trailing period. Don't log secrets, PINs, tokens or full connection strings.

## 8. HTTP endpoints

- **[Review]** Minimal APIs grouped by feature in `Endpoints/XxxEndpoints.cs`, mapped with `MapGroup`. Routes are
  lowercase kebab-case with typed constraints (`{id:int}`).
- **[Review]** Handlers return `Results.*`. Error bodies are `{ error = "…" }`. Invalid input → 400, missing → 404,
  conflict → 409; never 500 for a user mistake.
- **[Review]** Every state-changing endpoint (POST/PUT/PATCH/DELETE) is protected against CSRF: `[FromForm]` binding
  (automatic antiforgery), the shared JSON API filter, or `.RequireAntiforgery()` for endpoints that bind no form.
  A POST that binds nothing is not automatically protected.
- **[Review]** Handlers longer than a few lines are private static `XxxAsync` methods in the same class, not inline lambdas.
- **[Review]** Redirect targets from user input go through a local-URL check (`LocalRedirect` or a shared
  `SafeReturnUrl`).

## 9. Security baseline

- **[Review]** Cookies: `HttpOnly`, `SameSite=Lax` or stricter, `SecurePolicy = Always` for anything that grants
  access. Admin and sign-in surfaces are served only over HTTPS and rate-limited.
- **[Review]** Responses carry `X-Content-Type-Options: nosniff`, a `Referrer-Policy`, frame protection
  (`frame-ancestors`/`X-Frame-Options`) and, where practical, a Content-Security-Policy.
- **[Review]** Outbound requests to user-supplied URLs go through an SSRF guard that checks resolved addresses at
  connect time and re-checks every redirect.
- **[Review]** Never build HTML from untrusted text in JavaScript with `innerHTML`/template strings; use
  `textContent`, `createElement` or a `<template>`. In Razor, `MarkupString` only for content you produced and
  encoded yourself.
- **[Review]** Apps connect to SQL Server with their own least-privilege logins, never `sa`.

## 10. Blazor

- **[Review]** Static server rendering by default; `@rendermode InteractiveServer` only on the component that needs
  it, declared at the smallest scope that works.
- **[Review]** Components keep their code in an `@code { }` block at the bottom of the `.razor` file. Add a
  `.razor.cs` partial only when the code block exceeds roughly 150 lines.
- **[Review]** Dependencies through `@inject`, with standard names (`Factory` for the DbContext factory, `Log` for
  the logger, `Clock` for `TimeProvider`). Short type names, with namespaces imported in `_Imports.razor`.
- **[Review]** Parameters: `[Parameter] public T Name { get; set; }` on one line; required ones are
  `[Parameter, EditorRequired]`. Query-string values use `[SupplyParameterFromQuery(Name = "x")]` on a **private**
  property.
- **[Review]** Load data in `OnParametersSetAsync` when parameters or query strings drive the content, otherwise
  `OnInitializedAsync`. Reset per-request state (errors, not-found flags) at the start of each load.
- **[Review]** Every data-driven component renders three states: content, empty, and unavailable (read failure).
- **[Review]** Routable pages live in `Pages/`; reusable components live in feature folders, not in `Pages/`.
- **[Review]** Keep markup lines under about 160 characters. Break attributes onto separate lines rather than
  writing one long line.

## 11. Front-end assets (JavaScript and CSS)

- **[Enforced]** ESLint (`no-var`, `prefer-const`, `eqeqeq`) and Prettier (120 columns, single quotes, 4 spaces).
- **[Review]** Each feature script is one classic script with an IIFE and `'use strict'`, loaded with `defer`, or an
  ES module; pick one per project and stick to it. No globals other than one deliberate namespace object.
- **[Review]** Scripts find markup through `data-<feature>-*` attributes, not styling classes. Use event delegation
  for lists. Pages work without JavaScript (forms, links, `<details>`); scripts enhance them.
- **[Review]** `localStorage` keys are `<app>-<feature>-v<N>`; bump `N` when the stored shape changes.
- **[Review]** CSS colors, spacing and sizes come from custom properties (tokens) defined in one place per app;
  literal colors only inside token definitions. Theme switching is attribute-driven (`data-theme`) from one early
  script.
- **[Review]** Class names: one short, unique prefix per feature, BEM-style (`prefix-block__element--modifier`),
  `is-`/`has-` for state. A prefix must not collide with a CSS framework the app loads.
- **[Review]** Use the project's documented breakpoints only. `!important` only to beat a third-party rule, with a comment.
- **[Review]** Accessibility: links navigate, buttons act, and every `<button>` has an explicit `type`. Touch targets
  are at least 44×44 px (use a token). Never remove a focus outline without a `:focus-visible` replacement. One
  shared visually-hidden utility class.

## 12. Tests

- **[Review]** xUnit. Test classes are `XxxTests`; methods are sentences with underscores
  (`Saved_list_rejects_the_501st_article`), no `Async` suffix. Plain `Assert`; mocks only when a real
  dependency is impractical.
- **[Review]** Tests use fixed dates and a fake `TimeProvider`, not the real clock.
- **[Review]** Integration tests get their connection string from one shared helper (user secrets or an environment
  variable) and run against a **dedicated test database**, never a live one. Each test cleans up: an uncommitted
  transaction, `#temp` tables, or explicit deletion in `finally`. Shared setup (creating a context, seeding a row)
  lives in that helper, not copied into each file.
- **[Review]** Browser tests share one helper module for the base URL, viewports, themes, screenshots and cleanup.
  The default base URL is always local, never production. Every run deletes the data it created, in teardown, even
  when it fails.
- **[Review]** A bug fix comes with a test that fails before the fix.

## 13. SQL migrations

- **[Review]** Numbered `NNN_verb_object.sql` files in `db/`, applied in order, each listed in the deployment
  runbook in the same commit that adds it.
- **[Review]** Header: purpose; prerequisites ("Run after NNN"); "Repeatable" or "Run once"; backup requirement.
- **[Review]** Body: `SET XACT_ABORT ON; BEGIN TRANSACTION; … COMMIT TRANSACTION;` with no `GO`. `THROW` if a
  prerequisite is missing. Guard every change (`OBJECT_ID`, `COL_LENGTH`, `sys.*` lookups) so the script is safely
  re-runnable; if a data update would overwrite later manual changes, guard it too, or label the script "Run once".
  Use `EXEC(N'…')` for statements that must compile after an earlier change in the same script.
- **[Review]** Naming: app-owned tables share a prefix and are plural; keys are `Id`; constraints are
  `PK_/FK_/UQ_/CK_/DF_/IX_<Table>_<Column>`; time columns are `datetime2` UTC named `…At` with `SYSUTCDATETIME()`
  defaults; JSON columns are `nvarchar(max)` with an `ISJSON` check constraint.

## 14. Git and documentation

- **[Review]** Commit subjects: imperative, capitalized, no trailing period, at most 72 characters ("Add …",
  "Fix …"). Add a body when the reason isn't obvious from the diff. AI-assisted commits carry the
  `Co-Authored-By` trailer.
- **[Review]** Formatting-only and refactoring-only changes are separate commits from behavior changes.
- **[Review]** Dates in docs are written "Month D, YYYY (America/Chicago)". Each project keeps one changelog
  (newest first) and one deployment runbook; handoffs live in `docs/`, with unique names, and are not duplicated
  elsewhere. Remove or update links when files move.

## 15. Keeping the codebase consistent

Drift happens when a rule exists only in prose. The November 2026 audit of My-Newsfeed found exactly that: four ways
to query the database, three ways to read the clock, 35 breakpoint widths, a hard-coded password in ten test files,
and two `Program.cs` files that had quietly diverged. Most of those had a convention; nothing checked it. So:

- **Prefer a guard to a sentence.** When a rule can be checked by a tool, it is: an `.editorconfig` severity, an
  analyzer, the banned-API list, an ESLint rule, or a small check script that CI runs. Add the rule and its guard in
  the same commit. A rule that stays **[Review]** says why it can't be automated.
- **Guards available for the common cases:**

| Problem | Guard |
| --- | --- |
| `== null`, missing `is null` pattern | `.editorconfig`: `dotnet_style_prefer_is_null_check_over_reference_equality_method` and `IDE0041` at `warning` |
| Namespace doesn't match the folder | `IDE0130` at `warning` (with `dotnet_style_namespace_match_folder = true`) |
| Token not passed on | `CA2016` at `warning` |
| Missing `Async` suffix, `_field` names | `.editorconfig` naming rules at `warning` |
| Reading the clock or raw SQL strings in app code | `Microsoft.CodeAnalysis.BannedApiAnalyzers` with a `BannedSymbols.txt` listing `DateTime.Now`, `DateTime.UtcNow`, `DateTimeOffset.UtcNow`, `FromSqlRaw`, `SqlQueryRaw`, `ExecuteSqlRaw(Async)` (test projects exempt where needed) |
| Secrets in the repo | CI check that fails on `Password=`/`Pwd=` in tracked files outside `*.example`, plus secret scanning on the host |
| Duplicated host setup | One shared `AddXxx()` registration method; reviewers reject new registrations copied between hosts |
| Formatting and lint drift | `dotnet format --verify-no-changes`, `npm run lint`, `npm run format:check` in CI |
| CSS breakpoints, prefixes, hidden-text helpers | A project check script that fails on unregistered `@media` widths, unregistered class prefixes, or copies of the visually-hidden clip pattern |
| Browser checks without the shared helper | The same check script fails on any `*.browser.cjs` that doesn't load the shared helper |

- **New patterns go through the standard first.** Before introducing a new way to do something that already has a
  rule (a new data-access style, a new breakpoint, a new prefix, a new storage key scheme), update the standard and
  its guard in the same change, or don't introduce it.
- **Review checklist for every change:** it builds with zero warnings; CI checks pass; new types follow §2 naming;
  async I/O takes and passes `ct`; no clock reads below the edge; UI calls query/store classes; failures degrade and
  log; new CSS uses registered breakpoints, tokens and prefixes; tests clean up after themselves; docs and the
  migration table are updated.
- **Audit periodically.** Re-run a consistency audit after any large feature or every couple of months, record
  the result, and turn any new repeated deviation into either a rule plus guard or an explicit exception.
