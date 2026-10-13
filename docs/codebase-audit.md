# My-Newsfeed codebase audit

Review scope: first-party public pages, admin CRUD and tracking controls, authentication and routing,
EF models and queries, ESPN and TVmaze clients, results synchronization, browser scripts, PWA,
and Docker deployment configuration. The Rust scraper and third-party vendored Bootstrap were
outside this review. This is a code review with tests and read-only live checks, not a penetration
or load test. No application fixes or deployment changes were made.

Local credentials and the shared integration-test database are accepted project choices and are
excluded from findings, following the owner's instructions.

## Validation

- Entire existing test suite: 362 passed, 0 failed, 0 skipped.
- Two existing xUnit2031 analyzer warnings in EventsTests; no build failures.
- Unauthenticated /admin, /admin/categories, /admin/sports-events and /admin/results return 302.
- /healthz and /movies return 200.
- The database confirms SportsEventSources foreign keys use NO_ACTION, with 943 games having source references.
- Current movie query matches 14 entries, and 55 video rows are stored. The pagination limits below
  are therefore latent for Movies and Videos, rather than currently hiding those records.
- Admin write actions and concurrency races were reviewed statically; they were not exercised on live data.

## Findings, in recommended repair order

### 1. Shared sports-event deletion fails without a recoverable message (high)

Components/Pages/Admin/SportsEventList.razor:319 directly deletes SportsEvents.
Data/WebScraperContext.Partial.cs configures its SportsEventSources relationship with NoAction;
the live database confirms that rule. Deleting any referenced event will fail with a foreign-key
exception. The handler has no catch, so an admin encounters an unhandled interactive-circuit error
instead of a useful explanation. This affects the existing shared Big 12 records, not just a hypothetical source.

Disable deletion for shared records, or introduce an explicitly designed transactional deletion
policy. Catch the expected rejection and show a message. Add a handler-level regression check.

### 2. Sports-event editing can break provider ownership (medium)

Components/Pages/Admin/SportsEventEdit.razor:103 loads all types, and line 123 accepts any selected type.
For example, assigning an MLB event to WNBA succeeds as a foreign key, but EventsQuery.Visible
requires the event and feed providers to match. The event then disappears from the public schedule.
Big 12 records have separate source attribution, so changing only SportsEventsTypeId can also leave
the admin assignment inconsistent with their actual enabled school sources.

Restrict manual assignment to compatible legacy/unlinked records; make provider-owned attribution
read-only or validate it against the provider contract on save.

### 3. Video editing permits mismatched channel/feed relationships (medium)

Components/Pages/Admin/YoutubeVideoEdit.razor:142 accepts any feed ID without checking its ChannelId
against the video's ChannelId. VideosQuery uses the video's ChannelId for grouping/filtering but the
linked feed's ChannelName for display. Assigning a video from channel A to feed B therefore gives it
B's label under A's group. Feed deletion also uses the link, so deleting B with its videos can delete
A's misassigned video.

Only offer and accept feeds with a matching channel ID. Validate at save time, not just in the dropdown.

### 4. Concurrent results syncs can collide on new games (medium; static race finding)

Scores/ScoresSync.cs:241 reads existing games, then creates missing rows and later saves them.
There is no shared lock or unique-conflict retry. The background service, admin Sync now and another
admin circuit can run separate ScoresSync instances concurrently. If two runs see the same game as
absent, both attempt an insert into the unique sport/league/event index. One fails; the admin handler
has no recovery for that database exception, and the background run can stop before its remaining follows.

Serialize syncs within one app process or implement a concurrency-safe upsert/retry. A process-local
lock alone is insufficient if more than one application instance runs. Verify with coordinated parallel runs.

### 5. Category forms do not prevent normal browser submission (medium; static UI finding)

Components/Pages/Admin/CategoryList.razor:14 and :60 bind asynchronous @onsubmit handlers without
@onsubmit:preventDefault. Unlike EditForm, these are ordinary forms. The browser's normal submission
can reload the page while the asynchronous add/rename handler is pending, interrupting the interaction
and losing the success/error message. TvSeriesAdd already uses the explicit preventDefault pattern.

Use EditForm or add @onsubmit:preventDefault to both forms. Check add and rename in a browser,
including duplicate-name validation and Enter-key submission.

### 6. Removed schedule controls still apply invisible query filters (medium)

Components/Pages/Events.razor:131-150 still reads from/to/q and applies them. Its remaining forms
preserve those values, although the corresponding visible controls were removed. An old link such
as /events?to=2020-01-01 therefore shows no games; selecting another schedule keeps the invisible
bound. The empty-state clear-filter condition does not include from/to, so a date-only filtered link
can even lack the Clear filters recovery link.

Decide explicitly whether old date/search URLs remain supported. Either ignore retired parameters,
or visibly identify active hidden filters and provide a reset action, including date-only empty states.

### 7. Movies and videos have no route past their display caps (medium; latent)

Data/MoviesQuery.cs:46 takes 200 entries. The Movies page has neither filters nor paging now.
With more than 200 matching releases, later entries cannot be reached. Data/VideosQuery.cs:80
likewise caps videos at 300; selecting a channel can help but a channel with more than 300 stored
videos still has no older-page route. Both views report counts without offering continuation.

Add stable pagination or Load more. Keep the caps as batch sizes rather than making them unbounded.
Current data has 14 matching movies and 55 videos, so these caps are not currently reached.

### 8. A healthy empty sports feed is reported as a scraper problem (medium)

Data/ScraperHealth.cs:113 classifies items == 0 as Problem even when lastSeen is a recent successful
check. Provider-backed sports feeds use LastSuccessAt at line 227, so a newly configured league
with no upcoming fixtures is reported as Nothing collected yet despite a successful import.
That is a normal possible outcome for the upcoming-only providers.

For provider-backed feeds, distinguish never fetched/failed/stale from a successful empty schedule.
Also align Upcoming counts at line 167 with the public canceled/completed-status exclusions, so
health counts describe games users can actually see.

## Additional maintenance observations

- YoutubeFeedEdit uses the channelLocked value captured when the form loaded (line 114), rather than
  rechecking linked videos before changing ChannelId at line 156. A scraper insert between load and
  save can invalidate that assumption. Recheck inside the save transaction.
- Several edit components leave notFound true across parameter changes. Navigating from a missing
  ID to a valid ID of the same component can keep the old missing state. Reset request-specific state
  when parameters change.
- professional-schedule-deployment.md still describes From/Through and search controls that have
  been removed. Update the operational guide to describe the current interface.
- No common registry ties schedule sources to results follows. The WNBA/women's-basketball gap was
  fixed with seed 017, but future schedule additions can repeat it. A read-only admin coverage check
  would surface missing follows without silently re-enabling intentionally disabled follows.
- Test coverage is strongest for query helpers and rendered rows. Admin interactive form submission,
  provider/channel assignment rules, browser navigation and overlapping sync runs need coverage.

## Positive checks

Admin pages inherit Authorize, public queries generally use short-lived contexts and AsNoTracking,
source deletion helpers use transactions, public links restrict schemes to HTTP(S), and saved-card
text is rendered through textContent rather than parsing untrusted markup. Results and scraper
schedules retain separate ownership, and UTC display conversion handles SQL's unspecified DateTime kind.
These checks do not establish that every possible input or deployment configuration is safe.

## Repair progress

Findings 1–3 have been addressed in the working tree: provider-owned/shared sports
events have disabled delete controls and read-only attribution; delete/save handlers
recheck current ownership and handle expected rejection; legacy events remain editable.
Video assignment offers matching-channel feeds and validates the current channel at
save time. Regression coverage verifies protected records remain intact, compatible
legacy assignments/deletions work, and mismatched video feeds are rejected.
The full test suite passed with 364 tests. These repairs have not been deployed yet.

The personal-features release also addresses findings 4–8: syncs are serialized within
the application process, category forms prevent native submission, removed schedule
parameters are ignored, movie/video batches have page navigation, and successful empty
provider checks no longer count as failures. Regression and feature coverage now totals
376 passing tests, plus public/admin browser checks. The single-process sync gate does
not coordinate multiple application instances. The additional maintenance observations
remain separate follow-up work.

The personal-features release and the repairs described above are now deployed.
Live public/admin browser checks passed and the container is healthy.
