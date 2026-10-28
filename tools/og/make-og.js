// Builds docs/assets/og/field-reading-v3.png (1200×630 link-preview image) from the lesson's own field drawing.
// Usage: node tools/og/make-og.js [--chrome <path to chrome>] [--page <dir>]
//   --page <dir>  only write the scratch page into <dir> (for previewing), no screenshot.
// Needs Google Chrome (or Chromium/Edge); no npm packages.
const fs = require('fs'), os = require('os'), path = require('path'), { execFileSync } = require('child_process');

const ROOT = path.resolve(__dirname, '..', '..');
const LESSON = path.join(ROOT, 'docs', 'lessons', 'field-reading', 'index.html');
const OUT = path.join(ROOT, 'docs', 'assets', 'og', 'field-reading-v3.png');
// FIG_TOP_PAD sets the crop: 8.4 makes it 72 steps wide, so both edges fall between yard numbers.
const W = 1200, H = 630, FIG_MINW = 150, FIG_TOP_PAD = 8.4;

const arg = k => { const i = process.argv.indexOf(k); return i > 0 ? process.argv[i + 1] : null; };
const CHROME = arg('--chrome') || process.env.CHROME || [
  'C:/Program Files/Google/Chrome/Application/chrome.exe',
  'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',
  '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome',
  '/usr/bin/google-chrome', '/usr/bin/chromium',
].find(p => fs.existsSync(p));

const html = fs.readFileSync(LESSON, 'utf8');
const fonts = html.match(/<link href="https:\/\/fonts\.googleapis\.com[^>]+>/)[0];
const style = html.slice(html.indexOf('<style>') + 7, html.indexOf('</style>'));
let script = html.slice(html.lastIndexOf('<script>') + 8, html.lastIndexOf('</script>'));
script = script.replace(/\nload\(\);classLoad\(\)[\s\S]*$/, '\n'); // definitions only, no app boot

// A diagonal move across the 50 with checkpoints on 4, 8 and 12 of 16 counts, like the Level 4 player.
// Kept near the front sideline so the yard numbers fit at a large scale; minW matches the panel's shape.
const draw = `
const a={x:66,y:5},b={x:94,y:13};
const cps=[4,8,12].map(c=>({x:a.x+(b.x-a.x)*c/16,y:a.y+(b.y-a.y)*c/16,cls:'cp',r:0.6,label:String(c)}));
document.getElementById('fig').innerHTML=fieldSVG({id:'og',minW:${FIG_MINW},
  pts:[{...a,label:'Start'},{...b,cls:'end',label:'End'},...cps],paths:[{pts:[a,b]}],place:true,
  aria:'A marcher\\'s move from Start to End with checkpoints on counts 4, 8 and 12'});
// Crop to the panel's shape: from the yard-number band up to just above the move, centred on the path.
const svg=document.querySelector('#fig svg'),fb=document.getElementById('fig').getBoundingClientRect();
const vTop=b.y+${FIG_TOP_PAD},ht=vTop+5,wd=ht*fb.width/fb.height;
svg.setAttribute('viewBox',(a.x+b.x)/2-wd/2+' '+(-vTop)+' '+wd+' '+ht);
document.fonts.ready.then(()=>document.body.dataset.ready='1');`;

const page = `<!doctype html>
<html lang="en" data-theme="dark"><head><meta charset="utf-8">
<meta name="viewport" content="width=${W}">
<title>OG image</title>
${fonts}
<style>${style}
html,body{width:${W}px;height:${H}px;margin:0;padding:0;overflow:hidden}
body{background:var(--paper);display:flex;flex-direction:column;align-items:center}
.og-head{text-align:center;padding-top:44px}
.og-head h1{font-size:64px;letter-spacing:.005em;color:var(--ink)}
.og-head p{font-family:var(--display);font-weight:600;font-size:34px;color:var(--signal);margin:6px 0 0;letter-spacing:.04em}
#fig{margin-top:30px;width:1060px;height:388px;border-radius:18px;overflow:hidden;border:1px solid var(--line)}
#fig svg{display:block;width:100%;height:100%}
</style></head>
<body>
<div class="og-head"><h1>Field Reading &amp; Step Size</h1><p>MADA Lessons</p></div>
<div id="fig"></div>
<script>${script}
${draw}</script>
</body></html>`;

const pageDir = arg('--page');
if (pageDir) {
  fs.mkdirSync(pageDir, { recursive: true });
  fs.writeFileSync(path.join(pageDir, 'og.html'), page);
  console.log('📝 Scratch page: ' + path.join(pageDir, 'og.html'));
  process.exit(0);
}

if (!CHROME) { console.error('❌ Chrome not found. Pass --chrome <path> or set CHROME.'); process.exit(1); }
const tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'og-'));
const scratch = path.join(tmp, 'og.html');
try {
  fs.writeFileSync(scratch, page);
  fs.mkdirSync(path.dirname(OUT), { recursive: true });
  execFileSync(CHROME, ['--headless=new', '--disable-gpu', '--hide-scrollbars', '--force-device-scale-factor=1',
    `--window-size=${W},${H}`, '--virtual-time-budget=8000', `--user-data-dir=${path.join(tmp, 'profile')}`,
    `--screenshot=${OUT}`, 'file:///' + scratch.replace(/\\/g, '/')], { stdio: 'ignore' });
} finally {
  fs.rmSync(tmp, { recursive: true, force: true }); // scratch page is never kept
}
const kb = fs.statSync(OUT).size / 1024;
console.log(`✅ ${path.relative(ROOT, OUT)} (${kb.toFixed(0)} KB)`);
if (kb > 300) { console.error('⚠️ Over 300 KB'); process.exit(1); }
