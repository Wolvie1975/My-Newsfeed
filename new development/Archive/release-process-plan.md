# Release process: tighter and faster

Plan, November 5, 2026 (America/Chicago). Not started. Review it first, then build on approval (AGENTS.md).

## Why

Each change goes through implement → test → deploy → commit. The steps are right, but three things make them slow
and easy to get wrong:

- **The live image isn't tied to a commit.** The image is built from the working tree before the commit exists. On
  November 4 the live web image (`e14fa19`) was built at 22:56 UTC and `974ddbe` was committed at 23:34 UTC, so
  nobody can say for sure what's running. That deploy also skipped the `before-…` / `<tag>` tagging. It happened
  again on November 5: web (`d0210fe`, 00:40 UTC) and admin (`204d065`, 00:56 UTC) were rebuilt from a working tree
  with uncommitted changes, with no rollback tags.
- **Deploys are a hand-run procedure.** About seven steps, copied from the handoff each time. Until November 5,
  admin was run with `docker run` (the "admin container swap") instead of Compose, probably a leftover from the
  web/admin split. Each swap copied its settings out and back in and left a stopped container holding secrets
  (14 by November 5). DEPLOY.md's rollback example uses the wrong image name (`mynewsfeed-web` instead of
  `mynewspaper-web`).
- **Verification tooling is thrown away.** The side-by-side HTML comparison that proved steps 3 and 4 lived in a
  session scratchpad and was lost; the handoff now spends 30 lines describing how to rebuild it. The `zz` clean-up
  SQL and the live-check list are also copied by hand.

## Target cadence

```text
implement → test (tools/verify.sh) → commit locally → deploy that commit (deployment/deploy.sh)
          → live checks (tools/live-check) → release-history entry
```

Commits stay local; pushing stays with the user (AGENTS.md). If live checks fail, roll back with one command and
fix forward with a new commit.

## 1. Commit first, deploy a commit

- [x] `deploy.sh` refuses to run on a dirty working tree (`git status --porcelain` not empty), unless it's given
  `--allow-dirty`. That makes the image's source the commit, by default.
- [x] Every image is tagged `<service>:<short-sha>` and labelled `org.opencontainers.image.revision=<full sha>`
  (`docker compose build` with `--build-arg`/labels from `git rev-parse HEAD`). `docker inspect` then answers "what's
  live?" directly. Human tags (`data-access-20261104`) stay optional.
- [x] A small `/version` endpoint on both sites (approved November 5) returns the commit and build time, so the
  live checks can confirm the running build is the one deployed. It returns nothing else (no environment or
  settings).
- [x] Update AGENTS.md step 4 ("implement, test, deploy … then commit") to the new order, and the release-history
  template to record the deployed commit.

## 2. One deploy script, one rollback script

- [x] **Admin back under Compose.** Done outside this plan: admin was recreated by `docker compose` at 00:56 UTC on
  November 5 and is healthy. Checked November 5 against the last swapped container
  (`mynewspaper-admin-before-data-access-20261104`), comparing value hashes only: same settings, port 8086, `/keys`
  volume and `dev-bridge` network. The one difference is `Results__SyncIntervalHours`, which the swap had carried
  over from the old single-container setup. Admin doesn't run the background sync, so it isn't needed.
- [x] Remove the admin container swap from DEPLOY.md, the handoffs and the separation runbook; Compose is the only
  deploy path for both services.
- [x] **`deployment/deploy.sh [web|admin|all]`:**
  1. Refuses a dirty tree; runs `tools/verify.sh --quick` (build and unit tests) unless told it already ran.
  2. Tags the running images `before-<short-sha>` (the commit being replaced).
  3. Builds, tags `<short-sha>`, recreates with `docker compose up -d --no-build`.
  4. Compares environment hashes before and after; waits for healthy; checks `/healthz` on both sites and that it
     reports the new commit.
  5. On any failure, rolls back automatically and exits non-zero.
  6. Prints a block to paste into release history: commit, images, rollback tag, health results.
- [x] **`deployment/rollback.sh <sha>`:** retags `before-<sha>` as `latest` and recreates without building, then
  the same health checks.
- [x] **Retention:** keep the last two `before-…` image tags per service (decided November 5); never keep stopped `…-before-…`
  containers (with Compose, the settings live in the env file, not the container). This also ends the habit that
  left the old `sa` password in 14 containers.
- [x] Rewrite DEPLOY.md's "Build and deploy" and "Rollback" sections around the two scripts, and fix the image name.

## 3. Keep verification tools in the repo

- [x] **`tools/verify.sh`:** the full pre-commit suite in one command: `dotnet build` (0 warnings), `dotnet test`
  against `WebScraper_Test`, `npm run lint`, `npm run format:check`. `--quick` runs only the build and unit tests.
  Prints one pass/fail summary.
- [x] **`tools/compare-builds/`:** the no-behaviour-change proof from the evening handoff, as a script:
  `compare-builds <old-ref> [<new-ref>]`.
  - It makes a worktree for the old ref, publishes Web and Admin from both trees, runs the four servers against
    `WebScraper_Test` (admin over HTTPS with a throwaway cert), fetches a fixed page list, normalises (antiforgery
    tokens, Blazor comments and state, `_bl_` and `b-` attributes, ports, countdown attributes), and diffs.
  - Node only (the container's Python lacks `difflib`). Cleans up servers and the worktree in `finally`.
- [x] **`tools/live-check/`:** the standard live checks: `/healthz`, the ~19 public pages at 320/390/430/1440px in
  light and dark (overflow, script errors, error banner), every admin route at 390 and 1440px, admin HTTPS sign-in.
  - It creates its `zz` profile and records the UTC start; teardown deletes the profile through Admin and the
    unassigned devices since the start, then verifies the household (AGENTS.md clean-up rule).
  - Writes a summary and screenshots to `test-results/live/`, ready for release history.
- [x] **`tools/sql/cleanup-zz.sql`:** the `zz` clean-up from the midday handoff, parameterised by start time.
  Superseded by `tools/sql/cleanup-zz.sh` (part 5).
- [x] Shrink the handoffs: replace the rebuild descriptions with the commands.
  Done November 5 (evening and midday handoffs).

**Part 3 result, November 5:** `40ae0ec` verify.sh (full run: build, 1,112 .NET tests, JS unit, lint, format all
pass; `7f4c367` formatted the four files that failed `format:check`), `1c7fdd8` compare-builds.cjs (single files under
`tools/` so Prettier and ESLint cover them; `c025a8d`→`7370fbf` identical over 300 public + 25 admin pages; the College
Sports commit flagged; asset fingerprints normalised and listed separately), `832ec75` live-check.cjs (live:
176/176 public, 55/55 admin, clean-up through Admin and `cleanup-zz.sh`, household 1 profile / 5 devices / 0
unassigned; browser timezone America/Chicago), `c74c4d7` docs and permissions. live-check doesn't cover the feature
flows (Where to watch search, TV search, admin request); those stay with the feature's own checks.

**Parts 1–2 result, November 5:** `154fb78` (`/version` via SourceRevisionId + BuildTime assembly metadata; image
label; 8 new or extended tests), `7745e2a` (`deploy.sh`, `rollback.sh`, permissions), `d1159dc` (DEPLOY.md, standards,
AGENTS.md). Differences from the plan: the rollback tag is `before-<sha of the new deploy>` (what ran before that
deploy), so `rollback.sh <sha>` takes the deploy to undo; the deploy checks `/version` from inside each container;
retention only touches `before-<12 hex>` and `<12 hex>` tags, never the ~65 older hand-named `before-*` tags (the
dry run showed it would otherwise have removed them; that's the user's call). 1,120 tests pass.
**Deployed November 5 (user allowed it):** `d1159dc05981` via `deploy.sh`, healthy, environment unchanged,
`/version` correct; live-check 176/176 public, 55/55 admin; `rollback.sh d1159dc05981` and redeploy both worked;
release history `91a275a`. **The plan is complete** apart from: Codex rules unchecked (no Codex in the container),
the admin password rotation (user), and the user's decision on the ~65 old hand-named `before-*` image tags.

## 4. Coding standards documentation

The rules go in the standards in the same change as the scripts that enforce them (AGENTS.md: "update the standard
and its guard in the same change"). Until then they would describe tools that don't exist, so they land with parts
1–3, not before.

- [x] **General standard, [dotnet-coding-standard.md](../docs/dotnet-coding-standard.md) §14:** rename it "Git,
  releases and documentation" and add the project-neutral rules:
  - **[Enforced]** Deploy only committed code: the deploy script refuses a dirty working tree.
  - **[Enforced]** Every image is tagged with its short commit and labelled `org.opencontainers.image.revision`;
    each site exposes `/version` (commit and build time only).
  - **[Review]** One deploy script and one rollback script per project; the runbook documents those, not
    hand-run steps. Services run under Compose, with settings in a private env file outside the repo, not only in
    a running container.
  - **[Enforced]** Rollback keeps a fixed number of previous images per service and no stopped containers (their
    saved environment holds secrets).
  - **[Review]** Verification tools (build comparison, live checks, clean-up SQL) live in the repo, not in a
    session scratchpad; handoffs give the command, not instructions for rebuilding it.
- [x] **General standard §15:** add rows to the guards table: "Deploying uncommitted code" → deploy script refuses
  a dirty tree; "Live build not traceable" → commit tag, revision label, `/version`; "Old containers holding
  secrets" → deploy script retention.
- [x] **Repo addendum, [coding-standards.md](../docs/coding-standards.md):**
  - Change the intro: AGENTS.md keeps the cadence (implement → verify → commit → deploy → live check → release
    history); release mechanics are in the new section below.
  - New **Releases** section: `tools/verify.sh` before every commit; `deployment/deploy.sh` and `rollback.sh` are
    the only deploy path, for both web and admin; two rollback images per service; `tools/compare-builds` is the
    proof for a no-behaviour-change step; `tools/live-check` is the post-deploy check and the live mobile
    verification; release-history entries record the deployed commit and the rollback tag.
  - Add the review checklist item (general §15) "deployed from a commit; release history names it".
- [x] **Guardrails table** in the addendum, one row per problem in "Why" above, each with rule, guard and status:
  - "Live images built from uncommitted trees, untagged (`e14fa19` November 4; `d0210fe`, `204d065` November 5)"
  - "Hand-run deploy and admin container swap; 14 stopped containers with the old `sa` password"
  - "Verification tooling lost with the session scratchpad"
  - Status is "Planned" until each guard lands, then "Done <date>" with the evidence, like the existing rows.
- [x] **AGENTS.md** step 4 order (commit before deploy, part 1) and a link to the new Releases section.

## 5. The same rules and permissions for every agent

The user works with three agents: Claude Code, Codex and Antigravity. AGENTS.md ("Questions yes, repeat approvals
no", added November 5) decides what an agent asks. Each tool's own permission prompts are separate, and AGENTS.md
can't switch them off.

**Instructions.** Keep one source, AGENTS.md:

- Claude Code: `CLAUDE.md` imports AGENTS.md. Nothing to do.
- Codex: reads AGENTS.md natively. Nothing to do.
- [x] Antigravity: reads AGENTS.md. Checked November 5: 19 of its session transcripts include AGENTS.md's headings.
  No `GEMINI.md` needed.

**Clean up what's already saved (found November 5).** Antigravity's allow list had 138 entries built from
"always allow" answers: about 30 `sqlcmd -U sa -P <password>` commands with the `sa` password in full, a `curl`
admin sign-in with an admin password in it, and broad grants (bare `sqlcmd`, `docker stop`, `git checkout`, `rm -f`,
`env $(cat .env …)`, `read_file(/root/.bash_history)`).

- [x] With the user's OK and Antigravity closed: back up `config.json` (root-only, outside the repo), remove every
  entry containing a password and the broad grants above, then confirm a search for `-P ` and `password=` finds
  nothing. Delete the backup once Antigravity starts cleanly.
  Done November 5 (25 password entries and 8 broad grants removed, verified after the window reload).
- [ ] Rotate the admin password, which is now in that file and in Antigravity's transcripts. The `sa` password is
  handled by the SQL credentials plan (phase 4).
- [ ] Antigravity's transcripts (`/root/.gemini/antigravity/brain/*/.system_generated/logs/`) also contain both
  passwords. Delete old sessions once the passwords are rotated, or leave them: they're dead after rotation.

**Permissions.** One list in the repo, generated into each tool's format so they can't drift:

- [x] **`tools/agent-permissions/commands.json`:** the source of truth, with two lists.
  - **allow:** `dotnet build`/`test`/`format`, `npm run` (lint, format:check, test:unit, test:browser), `npm ci`,
    `node tests/browser/…`, `docker compose build|up|ps|logs`, `docker ps`/`inspect`/`images`/`image tag`,
    `git status`/`diff`/`log`/`show`/`add`/`commit`/`worktree`, and, once they exist, `tools/verify.sh`,
    `deployment/deploy.sh`, `deployment/rollback.sh`, `tools/compare-builds`, `tools/live-check`.
  - **deny:** `git push`, `git reset --hard`, `git clean`, `docker rm`, `docker rmi`, `docker volume rm`,
    `docker system prune`, `rm -rf`. (These still ask or are refused; they're never pre-approved.)
  - Raw `sqlcmd` isn't on either list: a SQL command's risk is in the SQL, not the command name, so it keeps asking.
    Routine database work goes through scripts that are on the allow list (below).
  Built as `tools/agent-permissions.json` with three decisions (allow, ask, deny): destructive commands ask rather than being refused, so the user can still approve them; only `git push` and volume/system prune are refused.
- [x] **`tools/sql/` scripts for routine database work.** Each reads the connection from `.env` inside the script
  and passes the password as `SQLCMDPASSWORD` (`docker exec -e SQLCMDPASSWORD sql2025 …/sqlcmd`), never `-P` on a
  command line. On November 5 an agent's permission prompt showed `sqlcmd -U sa -P '<password>'` in full, offering
  to save it as an "always allow" rule.
  - `household-check.sh [--db WebScraper|WebScraper_Test]`: read-only. Profiles, admin profile, devices,
    unassigned devices, leftover `zz ` profiles. Allowed.
  - `cleanup-zz.sh <start-utc>`: deletes only `zz ` profiles and unassigned devices created since the start
    (AGENTS.md clean-up rule), then runs `household-check.sh`. Allowed: it's a documented process.
  - `query.sh <file.sql>`: read-only ad hoc queries, run as a read-only login once the SQL credentials work lands.
    Until then, not on the allow list.
  - After the SQL credentials work, the scripts use the least-privilege logins instead of `sa`.
  Done November 5: `household-check.sh` and `cleanup-zz.sh` (dry run unless `--apply`) over a shared `lib.sh`; `query.sh` waits for the read-only login. Tested on `WebScraper_Test` with seeded rows (`119f748`).
- [x] **`tools/agent-permissions/generate.cjs`** writes:
  - `.claude/settings.json`: `permissions.allow` / `permissions.deny` as `Bash(<prefix>:*)` entries.
  - `.codex/rules/project.rules`: one `prefix_rule(pattern=[…], decision="allow"|"forbidden", justification=…)`
    per entry, with `match`/`not_match` examples so Codex checks them on load.
  - Antigravity: its grants are in `/root/.gemini/config/config.json`, `userSettings.globalPermissionGrants.allow`
    (`command(<prefix>)` strings). That file is global to the container, not per project, and the app rewrites it.
    The generator merges only its own entries (it keeps a list of what it added, removes those it no longer
    lists, leaves the rest), backs the file up first, and runs only while Antigravity is closed.
  Built as `tools/agent-permissions.cjs` (Prettier covers `tools/*.cjs`). Antigravity gets allow entries only: its config has no known ask/deny keys, so the generator warns about existing grants that cover an ask/deny command instead (`fa322ff`).
- [x] A check (`RepositoryHygieneTests` or the step 8 check script) fails if a generated file differs from what
  `generate.cjs` would write, so nobody edits one by hand.
  Done: `tests/browser/agent-permissions.test.cjs` (run by `npm run test:unit`) instead of `RepositoryHygieneTests`.
- [x] Testing:
  - Claude: start a session and run an allowed command and a denied one.
  - Codex: `codex execpolicy check --pretty --rules .codex/rules/project.rules -- git push`, which should be
    forbidden, and the same for an allowed command. Then mark the project as trusted and restart Codex.
  - Antigravity: regenerate with the app closed, reopen it, and run one allowed and one denied command.
  Done November 5: generated files checked by the unit test; Antigravity merge run twice (idempotent, no conflicts after narrowing `docker compose`/`docker image`). **Gap:** Codex isn't installed in the dev container, so `codex execpolicy check` hasn't run; Claude's rules apply only when Claude Code's project folder is My-Newsfeed (the VS Code workspace is `/workspaces/DotNet_Projects`).
- [x] Document it in the repo addendum's Releases section (part 4): "add a command to `commands.json` and
  regenerate; never edit the generated files".
  Done November 5 in the addendum's new "Agents and database tools" section (the Releases section comes with parts 1–3), plus AGENTS.md and a general-standard §15 row (`fa322ff`).

## Order and size

1. **Part 5 permissions, with the current commands:** small, no production change, and it removes most prompts
   from everything after it. Add the new scripts to `commands.json` as each one lands.
2. **Part 3** (`verify.sh`, `cleanup-zz.sql`, `compare-builds`, then `live-check`), with its part 4 rows. No
   production change, and it pays off straight away in clean-up steps 5 and 6.
3. **Parts 1 and 2, the scripts and `/version`,** with the rest of part 4: the first real use is the next deploy;
   that deploy is the test. (Admin is already under Compose.)

Fits naturally before clean-up step 5. Step 7 (shared `tests/browser/lib.cjs`) should build on `live-check`'s
helpers rather than make a second copy.

## Testing

- `verify.sh`: run it on a clean `develop` (expect pass) and with a deliberate warning (expect fail).
- `compare-builds`: run it on `7370fbf~8` vs `7370fbf` (step 4, known identical) and against a commit with a known
  markup change (expect a diff).
- `deploy.sh`: dry-run mode first (prints actions, changes nothing); then a real deploy of the current commit; then
  `rollback.sh` and back again, recording health and environment hashes each time.
- `live-check`: run against live; verify the household afterwards (1 profile, Jason as admin, no new unassigned
  devices).
- Mobile verification: the scripts don't change rendered pages, so it's not applicable to them. `live-check` itself
  is the mobile check for later releases.

## Decisions (November 5, 2026)

1. **Admin and Compose:** the user didn't know admin was outside Compose; it probably happened in the web/admin
   split. It's back under Compose now (see part 2).
2. **`/version`:** yes, on both sites.
3. **Rollback images:** keep the last two per service.
