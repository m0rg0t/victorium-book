import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { readFile } from 'node:fs/promises';
import { test } from 'node:test';
import vm from 'node:vm';

const html = await readFile(new URL('../index.html', import.meta.url), 'utf8');
const app = await readFile(new URL('../assets/js/app.js', import.meta.url), 'utf8');

test('loads jQuery before one copy of jQuery Mobile', () => {
  const sources = [...html.matchAll(/<script[^>]*src="([^"]+)"/g)].map(match => match[1]);
  assert.equal(sources.filter(src => src === 'assets/js/jquery.mobile.min.js').length, 1);
  assert.ok(sources.indexOf('assets/js/jquery-1.6.2.min.js') < sources.indexOf('assets/js/jquery.mobile.min.js'));
});

test('book content is unchanged apart from the corrected life-points anchor', () => {
  const body = html.slice(html.indexOf('<body>')).replace('href="#lifepoins_page"', 'href="#lifepoints_page"');
  assert.equal(createHash('sha256').update(body).digest('hex'), '6287c392f068866c403dd2a6728b93ce205f15814c6823fd726b8a6556b3c669');
});

test('every internal book section link resolves', () => {
  const uncommented = html.replace(/<!--[\s\S]*?-->/g, '');
  const ids = new Set([...uncommented.matchAll(/\bid="([^"]+)"/g)].map(match => match[1]));
  for (const [, target] of uncommented.matchAll(/href="#([^"]+)"/g)) {
    assert.ok(ids.has(target), `Missing section #${target}`);
  }
});

function start(navigator, extra = {}) {
  const registered = [];
  vm.runInNewContext(app, {
    navigator, run: callback => callback(),
    when: (id, callback) => registered.push({ id, callback }),
    ...extra,
  });
  return registered;
}

test('initialization without legacy PhoneGap networking is safe', () => {
  assert.equal(start({}).length, 4);
  assert.equal(start({ network: {} }).length, 4);
});

test('preserves available PhoneGap networking with synthetic responses', () => {
  const hosts = [];
  const navigator = { network: { isReachable(host, callback) {
    hosts.push(host);
    callback({ internetConnectionStatus: 1 });
  } } };
  assert.equal(start(navigator, { NetworkStatus: { NOT_REACHABLE: 0 } }).length, 4);
  assert.deepEqual(hosts, ['google.com']);
});

test('a partial native API without NetworkStatus does not prevent startup', () => {
  assert.equal(start({ network: { isReachable(_host, callback) { callback(0); } } }).length, 4);
});
