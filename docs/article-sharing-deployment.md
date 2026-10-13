# Article sharing

Deployed October 13, 2026 as mynewsfeed-web:article-sharing-20261013.
A Share article control beside Copy link opens a native dialog on News,
the homepage news list, saved articles and newly loaded cards.

Send by SMS uses a percent-encoded sms URI carrying the article title
and safe HTTP(S) link. iOS/iPadOS uses the body separator supported by
those clients; other platforms use the query separator. Actual SMS
composer behavior and prefilling depend on the installed messaging app
and require physical-device verification. No message is sent automatically.

Copy for Messenger copies title and URL for pasting into a conversation.
On secure contexts with navigator.share support, Share via apps invokes
the device share sheet. Messenger availability is determined by the
device, not by this site. Native sharing is omitted on the existing HTTP
address. No Facebook app credentials or tracking scripts are added.

Browser checks passed on News, homepage and Saved at phone/tablet/desktop
widths, SMS URI construction, Messenger copy fallback, modal dismissal
and focus restoration, and simulated native share success/cancellation.
Mocked APIs were used to avoid opening apps or sending messages.
Light/dark public regression checks and HTTP checks passed. JavaScript
syntax and whitespace checks passed; runtime environment and key volume
were preserved. Rollback: mynewsfeed-web:before-article-sharing.
Latest points to the new release.

## Share menu cleanup (2026-10-13)

Removed Copy for Messenger and its explanatory text from the Share
dialog; the existing Copy link button remains the single copy control.
SMS and supported native app sharing remain available. Deployed as
mynewsfeed-web:share-menu-20261013; rollback is
mynewsfeed-web:before-share-menu-cleanup. Browser checks passed on
News, Home and Saved for the simplified menu, original copy behavior,
SMS URI, dismissal/focus, and simulated native share/cancellation.
Production health returned HTTP 200 and runtime settings were preserved.

## Unified sharing and phone-only SMS (2026-10-13)

Share is now the single visible article sharing control. Copy link is
inside its dialog; the original hidden element supplies the article data
for progressive enhancement. SMS is shown only for a mobile phone user
agent (iPhone/iPod/Android Mobile or mobile User-Agent Client Hints).
Desktop and iPad do not offer SMS, even at a narrow viewport. Native
app sharing remains available where supported. The HTTP clipboard
fallback textarea is placed inside the open dialog so it can receive
selection despite the rest of the document being inert.

Deployed as mynewsfeed-web:unified-share-20261013. Browser checks passed
on desktop, iPhone, Android and iPad profiles for the single control,
Copy link contents, phone-only SMS and dismissal/focus behavior, plus
simulated desktop native sharing. Physical messaging app delivery was
not exercised. Health and public HTTP checks passed. Runtime environment
and key volume were preserved. Rollback: mynewsfeed-web:before-unified-share.
