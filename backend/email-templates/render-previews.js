// Renders every email template with sample data into backend/email-templates/previews/.
// Run from the project root:  node backend/email-templates/render-previews.js
// Uses the real EmailKit.gs (and its EK.samples()), so what you preview is what the hub sends.
// Images point at the local docs/assets/email/ copies so previews work before you push.
// Add --live to point images at mada.mprstudios.com instead.

const fs = require('fs');
const path = require('path');
const vm = require('vm');

const KIT = path.join(__dirname, '..', 'results-hub', 'EmailKit.gs');
const OUT = path.join(__dirname, 'previews');
const ctx = {};
vm.createContext(ctx);
vm.runInContext(fs.readFileSync(KIT, 'utf8') + '\nthis.EK = EK;', ctx);
const EK = ctx.EK;
if (!process.argv.includes('--live')) EK.S.ASSETS = '../../../docs/assets/email/';

fs.mkdirSync(OUT, { recursive: true });
for (const [name, m] of Object.entries(EK.samples())) {
  fs.writeFileSync(path.join(OUT, name + '.html'), m.html);
  fs.writeFileSync(path.join(OUT, name + '.txt'), 'Subject: ' + m.subject + '\n\n' + m.text);
  console.log(name.padEnd(30), (Buffer.byteLength(m.html) / 1024).toFixed(1) + ' KB', '·', m.subject);
}
