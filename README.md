Victorium book
---

A starting point for PhoneGap apps.

To get started: fork this repo, modify the config.xml to match your details, and get building!

## Offline maintenance checks

Use Node 24 and npm. Run `npm ci --ignore-scripts --no-audit`, `npm test`, `npx playwright install chromium`, and `npm run test:browser`.

The browser fixtures intercept every request and serve only checked-in local files. Native networking is stubbed, geolocation throws if requested, and external requests fail the checks. Coverage includes each book section, back/reload navigation, browser-only startup, synthetic `deviceready`, and the original QUnit assertions with the correct touch event.

The book body is unchanged except for correcting the life-points link to its existing section ID. A SHA-256 guard enforces that narrow exception. The PhoneGap/native package configuration, vendored jQuery 1.6.2, jQuery Mobile 1.0b2, XUI and Lawnchair are intentionally retained: upgrading this discontinued native/UI stack requires a separate compatibility migration and actual target-device validation. Current Playwright is development-only. No build, publishing or live location/network access is part of these checks.
