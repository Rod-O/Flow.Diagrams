// Usage: node previews.cjs <catalog base url> [<preview dir>]
// Opens every example the gallery lists, fails it on console errors, page errors, failed requests or an empty
// diagram, and writes its preview (the live canvas) as <preview dir>/<id>.png when a directory is given.
// Exits non-zero when any example fails.
const { chromium } = require('playwright');
const path = require('path');

const [base, outDir] = process.argv.slice(2);
if (!base) {
  console.error('usage: node previews.cjs <catalog base url> [<preview dir>]');
  process.exit(2);
}

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1200, height: 860 }, deviceScaleFactor: 0.5 });
  let errors = [];
  page.on('console', m => { if (m.type() === 'error') errors.push('console: ' + m.text().slice(0, 200)); });
  page.on('pageerror', e => errors.push('pageerror: ' + String(e).slice(0, 200)));
  page.on('response', r => { if (r.status() >= 400 && !r.url().includes('/previews/')) errors.push(`${r.status()} ${r.url().replace(base, '')}`); });

  await page.goto(base, { waitUntil: 'networkidle' });
  await page.waitForSelector('a.card');
  const ids = await page.$$eval('a.card', cards => cards.map(c => c.getAttribute('href').split('/').pop()));
  console.log(`${ids.length} examples`);

  let failed = 0;
  for (const id of ids) {
    errors = [];
    await page.goto(`${base.replace(/\/$/, '')}/examples/${id}`, { waitUntil: 'networkidle' });
    await page.waitForSelector('.example-body .diagram-canvas', { timeout: 10000 }).catch(() => errors.push('no canvas'));
    await page.waitForTimeout(1200);
    const r = await page.evaluate(() => {
      const q = s => document.querySelectorAll(s).length;
      const paths = [...document.querySelectorAll('.diagram-link path')].map(p => p.getAttribute('d') || '');
      return {
        nodes: q('.diagram-node'), groups: q('.diagram-group'), links: q('.diagram-link'),
        nan: paths.filter(d => /NaN|Infinity/.test(d)).length,
        blazorError: getComputedStyle(document.getElementById('blazor-error-ui')).display !== 'none',
      };
    });
    if (r.nodes + r.groups === 0) errors.push('empty diagram');
    if (r.nan) errors.push(`${r.nan} link path(s) with NaN`);
    if (r.blazorError) errors.push('Blazor error bar shown');
    const ok = errors.length === 0;
    if (!ok) failed++;
    console.log(`${ok ? 'OK  ' : 'FAIL'} ${id.padEnd(28)} nodes=${r.nodes} groups=${r.groups} links=${r.links}`);
    for (const e of [...new Set(errors)].slice(0, 5)) console.log('       ' + e);
    if (outDir && ok) {
      const canvas = (await page.$('.example-canvas')) ?? (await page.$('.example-body .diagram-canvas'));
      await canvas.screenshot({ path: path.join(outDir, `${id}.png`) });
    }
  }
  await browser.close();
  process.exit(failed ? 1 : 0);
})();
