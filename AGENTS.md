# Project development requirements

## Coding standards

User direction, November 4, 2026 (America/Chicago): all code follows the shared
[.NET coding standard](docs/dotnet-coding-standard.md) and this repository's
[coding standards addendum](docs/coding-standards.md). Read both before changing
code. Don't introduce a new way of doing something the standards already cover;
if a new pattern is needed, update the standard and its guard in the same change.
When a change touches one of the problems in the addendum's "Guardrails against
past drift" table, fix it and update that table's status.

## Mobile verification is mandatory

User direction, October 24, 2026 (America/Chicago): mobile is a necessary check
for anything we develop. Treat this as a standing acceptance requirement for all
features and fixes, including data/query changes that affect rendered content.

- Verify affected pages and user flows on mobile before calling work complete
  or ready to deploy. Desktop checks alone are insufficient.
- Use browser phone emulation at 320, 390 and 430px minimum, in light and dark
  mode. Exercise actual interactions and representative data; inspect screenshots
  as well as automated assertions. Include real-device checks when available.
- Check overflow, wrapping, card/section order, aligned date controls, readable
  filters, navigation, dialogs and touch targets of at least 44px. Verify refresh,
  saved preferences and no-JavaScript fallbacks where applicable to the change.
- Keep tablet/desktop regression checks appropriate to the affected layout.
- After deployment, repeat affected mobile checks against the live build.
- Record widths, themes, flows, outcomes and any unverified limits in the handoff
  and release evidence. Do not claim a mobile check that was not run. If mobile
  verification cannot run, report the gap and leave that validation outstanding.
- For documentation-only or other changes with no rendered/interactive impact,
  record why mobile verification is not applicable rather than claiming a pass.

See [the maintained mobile acceptance checklist](docs/interface.md#required-mobile-verification)
and [the deployment runbook](DEPLOY.md). Preserve existing local changes.

## Commits, not pushes

User direction, October 28, 2026 (America/Chicago): when asked to commit, commit
locally on `develop` and stop. Don't push, don't create a feature branch first,
and don't ask about or offer pushing; the user pushes to GitHub themselves.
Report the commit hashes and say they're local.

## Attached markdown files: review first, then build on approval

User direction, November 1, 2026 (America/Chicago): when the user attaches or
points to a markdown file (a plan, handoff, spec or notes), review it before
changing anything.

1. Read the whole file and check it against the current code, `git status` and
   `git log`.
2. Reply with a summary: what the file asks for, what is already done, the
   changes you would make (files and areas touched), open questions or
   conflicts with the code, and how you would test it.
3. Stop and wait for approval. Don't edit code, build or deploy yet.
4. Once the user approves, do all of it without further check-ins: implement,
   test (.NET tests plus browser checks, including the mobile checks above),
   deploy per [DEPLOY.md](DEPLOY.md) and verify the live build, then commit
   locally on `develop` (no push). Tick items off in the file as each is done.
5. Report what was built, test and live-check results, any gaps, and the
   local commit hashes.

## Questions yes, repeat approvals no

User direction, November 5, 2026 (America/Chicago): processes written down in
this repository are standing approvals. Carry them out without asking again,
in any session. Ask questions when you need an answer, not permission.

**Already approved; don't ask first:**

- Anything the user has approved in this session or in a plan, carried through
  to the end (see "Attached markdown files" above).
- Building, running the tests and browser checks, and running the apps locally
  against `WebScraper_Test`.
- Deploying to the live site and rolling back as [DEPLOY.md](DEPLOY.md)
  describes, including the live checks and their clean-up.
- Creating and deleting temporary `zz ` test data, per the clean-up rule below.
- Committing locally on `develop` (see "Commits, not pushes").
- Updating release history, handoffs, the guardrails table and other docs the
  process calls for.

A new chat doesn't reset these. Don't open by asking whether deploy and commit
are still allowed.

**Ask (one round, all questions together, before starting):**

- Requirements are unclear, or a choice is the user's: product, design, wording,
  or which of several reasonable approaches.
- The docs conflict with each other or with the code, or don't cover the step.
- Something unexpected changes the plan: uncommitted changes that aren't yours,
  a live state that doesn't match the handoff, a failure the runbook doesn't
  cover. Roll back first if the runbook says to, then ask.

**Always confirm first, even when a document describes it:**

- Anything that destroys data or can't be undone: dropping databases or tables,
  deleting or changing real household data, removing rollback images.
- Changes to credentials, SQL logins or permissions, and secret rotation.
- Anything outside this repository and its containers (other projects, the host,
  GitHub). Never push.

**Never put a password or connection string on a command line.** Not in
`sqlcmd -P`, a URL or an echoed variable: command lines land in permission
prompts, transcripts, shell history and the process list, and an "always allow"
answer saves them in agent settings files. Read credentials from `.env` inside a
script and pass them through the environment (`SQLCMDPASSWORD`). For routine
database checks, use the scripts in `tools/sql/`, not raw `sqlcmd`.

If the harness's own permission check stops an approved step, say which step and
why it's approved here, and continue once the user allows it. This file can't
switch that check off; the user's Claude Code settings can.

## Browser tests clean up their own data

User direction, November 4, 2026 (America/Chicago): every browser test run must
delete the data it creates when it finishes, against any database, live
included. On November 4, 383 leftover device rows and a stray
`zz Columns` profile had to be removed by hand.

- Run cleanup in teardown (`finally`, `after`/`afterAll`) so it also runs when a
  check fails or the run is interrupted. If cleanup can't run, report what was
  left behind and remove it before calling the work done.
- Temporary profiles get a `zz ` name prefix. Delete them through Admin's profile
  delete, which also removes their devices, follows, Saved list, topics and
  requests.
- Devices: any page visit from a browser without a device cookie creates a
  `MyDailyFeedProfileDevices` row with no profile (`ProfileGateMiddleware`), so
  every fresh browser context adds one. Reuse one context or saved storage state
  where you can. Record the UTC start time of the run, and at the end delete the
  unassigned devices (`ProfileId IS NULL`) created since then. Deleting a profile
  does not remove devices that never picked one.
- Put back anything the run changed: the household admin profile
  (`AdminProfileId`, currently Jason), household settings, picker mode, real
  profiles' settings and follows, request statuses, and Admin switches.
- Never delete or change real household profiles (currently only Jason) or data
  the run didn't create.
- After cleanup, check the database (no `zz ` profiles, no new unassigned
  devices, admin profile intact). Record the result in the handoff and release
  evidence. Don't claim a cleanup you didn't verify.
