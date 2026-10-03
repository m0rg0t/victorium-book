import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { extname, resolve, sep } from 'node:path';
import { chromium } from 'playwright';

const root = resolve(import.meta.dirname, '..');
const mime = { '.html': 'text/html', '.js': 'text/javascript', '.css': 'text/css', '.png': 'image/png', '.jpg': 'image/jpeg' };
const browser = await chromium.launch();
try {
  for (const native of [false, true]) {
    const context = await browser.newContext({ viewport: { width: 390, height: 844 } });
    const externalRequests = [];
    await context.route('**/*', async route => {
      const url = new URL(route.request().url());
      if (url.origin !== 'http://victorium.test') {
        externalRequests.push(url.href);
        return route.abort();
      }
      if (url.pathname === '/favicon.ico') return route.fulfill({ status: 204, body: '' });
      if (url.pathname === '/phonegap.js') {
        return route.fulfill({ contentType: 'text/javascript', body: '' });
      }
      const path = resolve(root, '.' + (url.pathname === '/' ? '/index.html' : decodeURIComponent(url.pathname)));
      assert.ok(path.startsWith(root + sep));
      return route.fulfill({ contentType: mime[extname(path)] ?? 'application/octet-stream', body: await readFile(path) });
    });
    await context.addInitScript(enabled => {
      window.nativeCalls = [];
      Object.defineProperty(navigator, 'geolocation', { configurable: true, value: {
        getCurrentPosition() { throw new Error('Geolocation must not be requested when reading the book'); },
      } });
      if (enabled) {
        window.NetworkStatus = { NOT_REACHABLE: 0 };
        Object.defineProperty(navigator, 'network', { configurable: true, value: {
          isReachable(host, callback) { window.nativeCalls.push(host); callback({ code: 0 }); },
        } });
      }
    }, native);
    const page = await context.newPage();
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    await page.goto('http://victorium.test/');
    await page.evaluate(() => document.dispatchEvent(new Event('deviceready')));
    await page.waitForSelector('#main_page.ui-page-active');
    const links = await page.locator('#main_page a[href^="#"]').evaluateAll(elements =>
      [...new Set(elements.map(element => element.getAttribute('href')))].filter(href => href !== '#main_page'));
    assert.ok(links.length >= 18);
    for (const href of links) {
      await page.locator(`#main_page a[href="${href}"]`).first().click();
      await page.waitForSelector(`${href}.ui-page-active`);
      await page.goBack();
      await page.waitForSelector('#main_page.ui-page-active');
    }
    await page.locator('#main_page a[href="#lifepoins_page"]').click();
    await page.waitForSelector('#lifepoins_page.ui-page-active');
    await page.reload();
    await page.waitForSelector('#lifepoins_page.ui-page-active');
    assert.deepEqual(await page.evaluate(() => window.nativeCalls), []);
    // The full reload creates a fresh document, so invoke the native readiness event again.
    await page.evaluate(() => document.dispatchEvent(new Event('deviceready')));
    assert.deepEqual(await page.evaluate(() => window.nativeCalls), native ? ['google.com'] : []);
    assert.deepEqual(errors, []);
    assert.deepEqual(externalRequests, []);
    await context.close();
    console.log(`Offline navigation/back/reload passed (${native ? 'stubbed native API' : 'browser-only'})`);
  }

  const context = await browser.newContext();
  await context.route('**/*', async route => {
    const url = new URL(route.request().url());
    assert.equal(url.origin, 'http://victorium.test');
    if (url.pathname === '/favicon.ico') return route.fulfill({ status: 204, body: '' });
    const path = resolve(root, '.' + decodeURIComponent(url.pathname));
    assert.ok(path.startsWith(root + sep));
    await route.fulfill({ contentType: mime[extname(path)] ?? 'application/octet-stream', body: await readFile(path) });
  });
  const page = await context.newPage();
  await page.goto('http://victorium.test/test/runner.html');
  await page.evaluate(() => {
    QUnit.done = (failed, total) => { window.qunitResult = { failed, total }; };
    document.dispatchEvent(new Event('deviceready'));
  });
  await page.waitForFunction(() => window.qunitResult?.total >= 7);
  const result = await page.evaluate(() => window.qunitResult);
  assert.equal(result.failed, 0);
  console.log(`Original QUnit suite passed (${result.total} assertions)`);
  await context.close();
} finally {
  await browser.close();
}
