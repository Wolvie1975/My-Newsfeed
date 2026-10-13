# Admin layout deployment

Deployed October 13, 2026 as mynewsfeed-web:admin-layout-20261013.
The release derives from the existing production image and adds only the
shared admin CSS and navigation enhancement to its existing static assets.
Unrelated pending source changes were not included. Runtime environment,
port mapping, shared network, and persistent key volume are preserved.
The latest tag points to this release.

The admin workspace now has grouped navigation, an administration header
with a public newsfeed link, spaced table rows and headers, clearer form
labels, larger controls, and keyboard focus outlines. At widths up to
900 pixels, the sidebar becomes a collapsible menu; tables scroll inside
their own containers. The CSS is scoped to MainLayout's page wrapper.
Navigation headings and the header link are enhanced by public.js after
initial rendering and Blazor navigation.

Authenticated Playwright checks passed on scraper status, Sources, Add
source, Categories, Pages, YouTube feeds and Sports event types at 390,
768, 1024 and 1440 pixels. Checks covered page overflow, the full-width
collapsed sidebar on smaller screens, navigation groups, tablet menu
selection/closing, and the public newsfeed link. Desktop source-list and
tablet form screenshots were visually inspected. No production records
were changed by these checks.

Production /healthz, /news, /saved and /results returned HTTP 200.
New CSS and JavaScript fingerprints and identity/gzip/Brotli responses
were verified. JavaScript syntax and git diff whitespace checks passed.
Rollback image: mynewsfeed-web:before-admin-layout.

## Admin Home destination

Home and the sidebar brand now point to /admin. View newsfeed remains
the separate public-site link. NavMenu source uses the admin destination;
the deployed JavaScript enhancement applies it to the existing production
markup and updates Home’s active state after navigation.
Deployed October 13 as mynewsfeed-web:admin-home-20261013, retaining
mynewsfeed-web:before-admin-home for rollback. Authenticated browser
checks verified both links return to /admin, the Home active state, and
the separate public link. All seven-page responsive checks passed again;
/healthz returned HTTP 200 and all JavaScript encodings were verified.

## Public theme isolation fix (2026-10-13)

PublicLayout also uses a page class on its main element. The original
admin .page selectors accidentally overrode public theme colors and
layout, causing dark headlines against dark article cards. Admin rules
now explicitly match div.page, the MainLayout wrapper, leaving the
public main.page untouched. Deployed as mynewsfeed-web:dark-scope-20261013
with regenerated stylesheet metadata and compressed assets. Rollback:
mynewsfeed-web:before-dark-scope.

Browser checks passed in light/dark mode across News, NFL standings,
Results, Events and Rankings at 390/768/1280 pixels. Headline and
standings text contrast, theme inheritance, page overflow and theme
persistence through navigation were checked. The authenticated
seven-page admin responsive/navigation checks also passed. Production
health and all stylesheet encodings were verified; runtime environment
was preserved.
