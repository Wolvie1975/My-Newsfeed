# Project development requirements

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
