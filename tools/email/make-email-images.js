// Renders the hosted images the email templates use, at 2x, into docs/assets/email/.
// Run from the project root:  node tools/email/make-email-images.js
// Needs Playwright (npm i -D playwright, then npx playwright install chromium).
// After changing an image, save it under a new name (v2…) so inbox caches pick it up.

// Font: set BARLOW_WOFF2 to a local barlow-condensed-latin-700-normal.woff2 (from npm @fontsource/barlow-condensed)
// to embed it; otherwise Google Fonts is used.

const fs = require('fs');
const path = require('path');
const { chromium } = require('playwright');

const OUT = path.join(__dirname, '..', '..', 'docs', 'assets', 'email');
const FONT = process.env.BARLOW_WOFF2
  ? `<style>@font-face{font-family:'Barlow Condensed';font-weight:700;src:url(data:font/woff2;base64,${fs.readFileSync(process.env.BARLOW_WOFF2).toString('base64')}) format('woff2')}</style>`
  : "<link href='https://fonts.googleapis.com/css2?family=Barlow+Condensed:wght@700&display=swap' rel='stylesheet'>";
const xMark = (x, y, s, color, outline) => {
  const a = `M${x} ${y} L${x + s} ${y + s} M${x + s} ${y} L${x} ${y + s}`;
  return (outline ? `<path d="${a}" stroke="#132139" stroke-width="${s * 0.5}" stroke-linecap="round"/>` : '') +
    `<path d="${a}" stroke="${color}" stroke-width="${outline ? s * 0.25 : s * 0.3}" stroke-linecap="round"/>`;
};

const images = {
  // 598×210: the field strip at the top of announcement emails
  'field-hero-v1.png': { w: 598, h: 210, svg: `
    <svg xmlns="http://www.w3.org/2000/svg" width="598" height="210" viewBox="0 0 598 210">
      <defs><linearGradient id="g" x1="0" x2="1"><stop offset="0" stop-color="#6EC8FF"/><stop offset="1" stop-color="#C77DFF"/></linearGradient></defs>
      <rect width="598" height="170" fill="#2D6B40"/><rect y="170" width="598" height="40" fill="#1D3828"/><rect y="166" width="598" height="4" fill="#fff"/>
      <g stroke="#fff" stroke-opacity=".08">${[34, 68, 102, 136].map(y => `<line x1="0" y1="${y}" x2="598" y2="${y}"/>`).join('')}</g>
      <g stroke="#fff" stroke-width="3">${[59, 179, 299, 419, 539].map(x => `<line x1="${x}" y1="0" x2="${x}" y2="166"/>`).join('')}</g>
      <line x1="150" y1="132" x2="452" y2="52" stroke="url(#g)" stroke-width="5" stroke-linecap="round"/>
      ${xMark(143, 125, 14, '#6EC8FF')}${xMark(219, 106, 12, '#F2C230', true)}${xMark(294, 86, 12, '#F2C230', true)}${xMark(369, 66, 12, '#F2C230', true)}${xMark(445, 45, 14, '#C77DFF')}
      <g font-family="Barlow Condensed" font-weight="700" font-size="17">
        <text x="112" y="122" fill="#6EC8FF">Start</text><text x="236" y="100" fill="#fff">4</text><text x="311" y="81" fill="#fff">8</text>
        <text x="384" y="60" fill="#fff">12</text><text x="466" y="46" fill="#C77DFF">End</text></g>
      <g font-family="Barlow Condensed" font-weight="700" font-size="18" fill="#fff" text-anchor="middle">
        ${[['40', 59], ['45', 179], ['50', 299], ['45', 419], ['40', 539]].map(([t, x]) => `<text x="${x}" y="196">${t}</text>`).join('')}</g>
    </svg>` },
  // 150×26: start → checkpoints → end, used as the signature mark
  'path-mark-v1.png': { w: 150, h: 26, svg: `
    <svg xmlns="http://www.w3.org/2000/svg" width="150" height="26" viewBox="0 0 150 26">
      <defs><linearGradient id="g" x1="0" x2="1"><stop offset="0" stop-color="#6EC8FF"/><stop offset="1" stop-color="#C77DFF"/></linearGradient></defs>
      <line x1="9" y1="20" x2="141" y2="6" stroke="url(#g)" stroke-width="3.5" stroke-linecap="round"/>
      ${xMark(4, 15, 10, '#1A73B0')}${xMark(51, 11, 8, '#F2C230', true)}${xMark(94, 6, 8, '#F2C230', true)}${xMark(136, 1, 10, '#7A3FC4')}
    </svg>` },
  // 18×18: yellow checkpoint X used as a list bullet
  'checkpoint-v1.png': { w: 18, h: 18, svg: `
    <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 18 18">${xMark(3, 3, 12, '#F2C230', true)}</svg>` },
};

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage({ deviceScaleFactor: 2 });
  for (const [name, img] of Object.entries(images)) {
    await page.setViewportSize({ width: img.w, height: img.h });
    await page.setContent(`<html><head>${FONT}<style>html,body{margin:0;background:transparent}svg{display:block}</style></head><body>${img.svg}</body></html>`);
    await page.evaluate(() => document.fonts.ready);
    await page.screenshot({ path: path.join(OUT, name), omitBackground: true, clip: { x: 0, y: 0, width: img.w, height: img.h } });
    console.log('wrote', name);
  }
  await browser.close();
})();
