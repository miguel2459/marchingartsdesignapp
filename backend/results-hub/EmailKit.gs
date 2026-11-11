/**
 * EmailKit.gs — branded, email-safe HTML for MADA Lessons emails.
 *
 * One look for every email: light paper, white card, the field path's blue→purple gradient
 * as the only accent, yellow checkpoint marks. Matches the lesson's light theme.
 *
 * Email-safe rules this file follows:
 *  - Table layout, inline styles, 600px card, role="presentation" on layout tables.
 *  - Gradients always sit on top of a solid fallback color (Outlook shows the solid).
 *  - Web fonts load where supported (Apple Mail, iOS); elsewhere Arial Narrow / Arial.
 *  - Pictures the HTML can't draw are hosted PNGs under docs/assets/email/ (2x, with alt text).
 *  - Every value from a sheet or form goes through EK.esc().
 *
 * Each template returns { subject, html, text }. Send with:
 *   const m = EK.welcome({...});
 *   MailApp.sendEmail({ to, subject: m.subject, body: m.text, htmlBody: m.html, name: CONFIG.SENDER_NAME, replyTo: CONFIG.ADMIN_EMAIL });
 *
 * Source copy lives in backend/results-hub/EmailKit.gs. Paste it into the Apps Script project as a
 * second file named EmailKit; it shares the global scope with Code.gs.
 */

const EK = (() => {
  /* ---------- settings ---------- */
  const S = {
    ASSETS: 'https://mada.mprstudios.com/assets/email/',
    SITE: 'https://mada.mprstudios.com',
    LESSON_URL: 'https://mada.mprstudios.com/lessons/field-reading/',
    REPLY_EMAIL: 'contact@mprstudios.com',
    ADDRESS: '[Mailing address]',   // TODO: a mailing address or PO box (required in commercial email)
    CITY: 'Bakersfield, CA',
    FROM_NAME: 'Miguel Rodriquez',
    FROM_TITLE: 'MPR Studios · Visual caption head, 15+ years in the marching arts',
    IMG: { hero: 'field-hero-v1.png', mark: 'path-mark-v1.png', bullet: 'checkpoint-v1.png' },
  };

  /* ---------- tokens (the lesson's light theme) ---------- */
  const C = {
    paper: '#EEF2F5', card: '#FFFFFF', ink: '#132139', muted: '#566275', line: '#D2D9E1',
    start: '#6EC8FF', end: '#C77DFF', startInk: '#1A73B0', endInk: '#7A3FC4',
    signal: '#F2C230', turf: '#2D6B40',
    good: '#1D7448', goodBg: '#E2F2E9', bad: '#B3372B', badBg: '#F8E5E2',
  };
  const H = "'Barlow Condensed','Arial Narrow',Arial,Helvetica,sans-serif";
  const B = "'Atkinson Hyperlegible','Segoe UI',Arial,Helvetica,sans-serif";
  const GRAD = 'linear-gradient(90deg,' + C.start + ',' + C.end + ')';
  const GRAD_INK = 'linear-gradient(90deg,' + C.startInk + ',' + C.endInk + ')';

  /* ---------- helpers ---------- */
  const esc = (s) => String(s == null ? '' : s)
    .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&#39;');
  const img = (name) => S.ASSETS + name;
  const firstName = (full) => String(full || '').trim().split(/\s+/)[0] || 'Coach';
  const tbl = (inner, extra) => '<table role="presentation" cellpadding="0" cellspacing="0" border="0"' + (extra || '') + '>' + inner + '</table>';
  /** One padded content row inside the card. */
  const row = (inner, pad, cls) => '<tr><td class="ek-pad' + (cls ? ' ' + cls : '') + '" style="padding:' + (pad || '0 36px 14px') + '">' + inner + '</td></tr>';

  const p = (text, opts) => {
    const o = opts || {};
    return '<p style="margin:0 0 ' + (o.mb == null ? 14 : o.mb) + 'px;font-family:' + B + ';font-size:' + (o.size || 16) +
      'px;line-height:' + (o.lh || 1.65) + ';color:' + (o.color || C.ink) + '">' + (o.html ? text : esc(text)) + '</p>';
  };
  const h1 = (text) => '<h1 class="ek-h1" style="margin:0 0 12px;font-family:' + H + ';font-weight:700;font-size:40px;line-height:1.05;color:' +
    C.ink + '">' + esc(text) + '</h1>';
  const h2 = (text) => '<h2 style="margin:0 0 8px;font-family:' + H + ';font-weight:600;font-size:24px;line-height:1.1;color:' + C.ink + '">' + esc(text) + '</h2>';
  const label = (text, color) => '<span style="font-family:' + H + ';font-weight:600;font-size:13px;letter-spacing:0.14em;text-transform:uppercase;color:' +
    (color || C.muted) + '">' + esc(text) + '</span>';
  const link = (text, url) => '<a href="' + esc(url) + '" style="color:' + C.startInk + ';text-decoration:underline">' + esc(text) + '</a>';

  /** Gradient button (one per email) or outline button. */
  const button = (text, url, kind) => {
    const outline = kind === 'outline';
    const td = outline
      ? 'bgcolor="' + C.card + '" style="border-radius:8px;border:2px solid ' + C.ink + ';background:' + C.card + ';mso-padding-alt:11px 20px"'
      : 'bgcolor="' + C.startInk + '" style="border-radius:8px;background:' + C.startInk + ';background-image:' + GRAD_INK + ';mso-padding-alt:13px 22px"';
    const a = '<a href="' + esc(url) + '" style="display:inline-block;padding:' + (outline ? '11px 20px' : '13px 22px') + ';font-family:' + H +
      ';font-weight:700;font-size:19px;line-height:22px;letter-spacing:0.02em;color:' + (outline ? C.ink : '#FFFFFF') +
      ';text-decoration:none;border-radius:8px">' + esc(text) + '</a>';
    return tbl('<tr><td align="center" ' + td + '>' + a + '</td></tr>', ' style="border-collapse:separate"');
  };
  const buttons = (list) => tbl('<tr>' + list.map((b, i) =>
    (i ? '<td width="12" style="width:12px;font-size:0;line-height:0">&nbsp;</td>' : '') + '<td>' + button(b.text, b.url, b.kind) + '</td>').join('') + '</tr>');

  /** Yellow-edged tip, like the lesson's notes. */
  const note = (text) => tbl('<tr><td style="border-left:3px solid ' + C.signal + ';padding:4px 0 4px 14px;font-family:' + B +
    ';font-size:15px;line-height:1.55;color:' + C.muted + '">' + esc(text) + '</td></tr>', ' width="100%"');

  /** List with checkpoint-X bullets. items: [{ bold, text }] */
  const bullets = (items) => tbl(items.map((it) => '<tr><td width="30" valign="top" style="width:30px;padding:3px 0 12px">' +
    '<img src="' + img(S.IMG.bullet) + '" width="18" height="18" alt="" style="display:block;border:0"></td>' +
    '<td valign="top" style="padding:0 0 12px;font-family:' + B + ';font-size:16px;line-height:1.55;color:' + C.ink + '">' +
    (it.bold ? '<b>' + esc(it.bold) + '</b> ' : '') + esc(it.text || '') + '</td></tr>').join(''), ' width="100%"');

  /** Numbered step: big turf-green number, subhead, body, optional extra HTML. */
  const step = (n, title, body, extraHtml) => tbl('<tr><td width="40" valign="top" style="width:40px;font-family:' + H +
    ';font-weight:700;font-size:34px;line-height:1;color:' + C.turf + '">' + n + '</td><td valign="top">' + h2(title) +
    p(body, { size: 15, lh: 1.55, color: C.muted, mb: extraHtml ? 12 : 0 }) + (extraHtml || '') + '</td></tr>', ' width="100%"');

  /** Simple data table. cols: [{ label, align, width }], rows: arrays of cell HTML (already escaped). */
  const dataTable = (cols, rows) => {
    const th = cols.map((c) => '<th class="ek-cell" align="' + (c.align || 'left') + '"' + (c.width ? ' width="' + c.width + '"' : '') +
      ' style="padding:9px 10px;background:' + C.paper + ';font-family:' + H + ';font-weight:600;font-size:13px;letter-spacing:0.1em;' +
      'text-transform:uppercase;color:' + C.muted + ';text-align:' + (c.align || 'left') + '">' + esc(c.label) + '</th>').join('');
    const tr = rows.map((r) => '<tr>' + r.map((cell, i) => '<td class="ek-cell" align="' + (cols[i].align || 'left') + '" style="padding:9px 10px;border-top:1px solid ' +
      C.line + ';font-family:' + B + ';font-size:14px;line-height:1.4;color:' + C.ink + '">' + cell + '</td>').join('') + '</tr>').join('');
    return tbl('<tr>' + th + '</tr>' + tr, ' width="100%" style="border:1px solid ' + C.line + ';border-radius:8px;border-collapse:separate;overflow:hidden"');
  };

  /** Score pill: pass true = green, false = red, null = not attempted. */
  const score = (text, pass) => {
    if (pass == null || text === '' || text == null) return '<span style="color:' + C.muted + '">–</span>';
    return '<span class="ek-pill" style="display:inline-block;min-width:40px;padding:3px 0;border-radius:4px;font-weight:700;text-align:center;background:' +
      (pass ? C.goodBg : C.badBg) + ';color:' + (pass ? C.good : C.bad) + '">' + esc(text) + '</span>';
  };

  /** Row of stat tiles. stats: [{ value, label, accent }] */
  const stats = (list) => tbl('<tr>' + list.map((s, i) => '<td class="ek-stat ek-s' + (i % 2) + '" width="' + Math.floor(100 / list.length) + '%" valign="top" style="padding:' +
    (i ? '0 0 10px 10px' : '0 0 10px 0') + '">' + tbl('<tr><td align="center" style="border:1px solid ' + C.line + ';border-radius:8px;padding:14px 6px;' +
      (s.accent ? 'background:#F6F2FC;' : '') + '"><div style="font-family:' + H + ';font-weight:700;font-size:34px;line-height:1;color:' +
      (s.accent ? C.endInk : C.ink) + '">' + esc(s.value) + '</div><div style="margin-top:6px;font-family:' + B +
      ';font-size:11px;line-height:1.3;letter-spacing:0.1em;text-transform:uppercase;color:' + C.muted + '">' + esc(s.label) + '</div></td></tr>',
    ' width="100%" style="border-collapse:separate"') + '</td>').join('') + '</tr>', ' width="100%"');

  /** Horizontal bars for weakest skills. items: [{ label, count }] */
  const bars = (items, unit) => {
    const max = Math.max.apply(null, items.map((i) => i.count).concat([1]));
    return tbl(items.map((it) => {
      const pct = Math.max(4, Math.round((it.count / max) * 100));
      const bar = tbl('<tr><td width="' + pct + '%" height="10" bgcolor="' + C.start + '" style="height:10px;font-size:0;line-height:0;border-radius:5px;background:' +
        C.start + ';background-image:' + GRAD + '">&nbsp;</td>' + (pct < 100 ? '<td height="10" style="height:10px;font-size:0;line-height:0">&nbsp;</td>' : '') + '</tr>',
      ' width="100%" bgcolor="' + C.paper + '" style="background:' + C.paper + ';border-radius:5px;border-collapse:separate"');
      return '<tr><td width="40%" style="padding:5px 12px 5px 0;font-family:' + B + ';font-size:14px;line-height:1.3;color:' + C.ink + '">' + esc(it.label) +
        '</td><td style="padding:5px 0">' + bar + '</td><td width="70" align="right" style="width:70px;padding:5px 0 5px 12px;font-family:' + B +
        ';font-size:14px;color:' + C.muted + ';white-space:nowrap">' + esc(it.count) + ' ' + esc(unit || 'misses') + '</td></tr>';
    }).join(''), ' width="100%"');
  };

  /** Miguel's signature with the path mark. */
  const signature = () => '<img src="' + img(S.IMG.mark) + '" width="150" height="26" alt="" style="display:block;border:0;margin:0 0 8px">' +
    '<div style="font-family:' + H + ';font-weight:700;font-size:22px;line-height:1.1;color:' + C.ink + '">' + esc(S.FROM_NAME) + '</div>' +
    '<div style="font-family:' + B + ';font-size:14px;line-height:1.5;color:' + C.muted + '">' + esc(S.FROM_TITLE) + '</div>' +
    '<div style="font-family:' + B + ';font-size:14px;line-height:1.5">' + link('mada.mprstudios.com', S.SITE) + ' · ' +
    link(S.REPLY_EMAIL, 'mailto:' + S.REPLY_EMAIL) + '</div>';
  const signoff = () => p('<b>Miguel</b> <span style="color:' + C.muted + '">· MPR Studios</span>', { html: true, mb: 0 });

  /** Footer under the card. kind: outreach | product | report */
  const footer = (kind, opts) => {
    const o = opts || {};
    const addr = 'MPR Studios · ' + esc(S.ADDRESS) + ' · ' + esc(S.CITY);
    const stop = (subject, text) => '<a href="mailto:' + S.REPLY_EMAIL + '?subject=' + encodeURIComponent(subject) + '" style="color:' + C.muted + ';text-decoration:underline;white-space:nowrap">' + text + '</a>';
    let lead;
    if (kind === 'outreach') lead = 'Not the right person, or not interested? Reply "no thanks" and I won\'t follow up.';
    else if (kind === 'report') lead = 'Sent every Monday at 7am to the coach of ' + esc(o.code) + '. Reply to reach Miguel.';
    else lead = esc(o.reason || 'You\'re getting this because you have a MADA Lessons class.');
    const tail = kind === 'report' ? ' · ' + stop('Stop weekly reports for ' + (o.code || ''), 'Stop weekly reports')
      : kind === 'product' && o.unsubscribe ? ' · ' + stop('Unsubscribe', 'Unsubscribe') : '';
    return '<tr><td class="ek-pad" align="center" style="padding:16px 24px 0;font-family:' + B + ';font-size:12px;line-height:1.55;color:' + C.muted + '">' +
      lead + '<br>' + addr + tail + '</td></tr>';
  };

  /** The page shell: preheader, paper background, white card with the path stripe and wordmark header. */
  const shell = (o) => {
    const header = '<tr><td class="ek-pad" style="padding:22px 36px ' + (o.headerPadBottom == null ? 18 : o.headerPadBottom) + 'px">' +
      tbl('<tr><td class="ek-brand" valign="bottom" style="font-family:' + H + ';font-weight:700;font-size:22px;line-height:1;letter-spacing:0.02em;color:' + C.ink + '">MADA ' +
        '<span style="font-weight:600;font-size:15px;letter-spacing:0.16em;text-transform:uppercase;color:' + C.muted + '">Lessons</span></td>' +
        (o.label ? '<td class="ek-stack" align="right" valign="bottom" style="text-align:right">' + o.label + '</td>' : '') + '</tr>', ' width="100%"') + '</td></tr>';
    return '<!DOCTYPE html>\n<html lang="en" xmlns="http://www.w3.org/1999/xhtml" xmlns:v="urn:schemas-microsoft-com:vml" xmlns:o="urn:schemas-microsoft-com:office:office">\n<head>\n' +
      '<meta charset="utf-8">\n<meta name="viewport" content="width=device-width,initial-scale=1">\n<meta name="x-apple-disable-message-reformatting">\n' +
      '<meta name="format-detection" content="telephone=no,address=no,email=no,date=no">\n' +
      '<meta name="color-scheme" content="light">\n<meta name="supported-color-schemes" content="light">\n<title>' + esc(o.subject) + '</title>\n' +
      '<!--[if mso]><noscript><xml><o:OfficeDocumentSettings><o:PixelsPerInch>96</o:PixelsPerInch></o:OfficeDocumentSettings></xml></noscript><![endif]-->\n' +
      '<link href="https://fonts.googleapis.com/css2?family=Atkinson+Hyperlegible:wght@400;700&amp;family=Barlow+Condensed:wght@600;700&amp;display=swap" rel="stylesheet">\n' +
      '<style>\n' +
      ':root{color-scheme:light;supported-color-schemes:light}\n' +
      'body{margin:0;padding:0;width:100%!important;-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%}\n' +
      'table{border-collapse:collapse;mso-table-lspace:0pt;mso-table-rspace:0pt}\n' +
      'img{border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic}\n' +
      'a{color:' + C.startInk + '}\n' +
      '@media (max-width:620px){\n' +
      '  .ek-card{width:100%!important}\n' +
      '  .ek-pad{padding-left:20px!important;padding-right:20px!important}\n' +
      '  .ek-h1{font-size:32px!important}\n' +
      '  .ek-hero{width:100%!important;height:auto!important}\n' +
      '  .ek-stat{display:inline-block!important;width:50%!important;box-sizing:border-box}\n' +
      '  .ek-s0{padding:0 5px 10px 0!important}\n' +
      '  .ek-s1{padding:0 0 10px 5px!important}\n' +
      '  .ek-brand{display:block!important;width:100%!important}\n' +
      '  .ek-stack{display:block!important;width:100%!important;text-align:left!important;padding-top:10px!important}\n' +
      '  .ek-outer{padding:12px 8px 24px!important}\n' +
      '  .ek-tight{padding-left:12px!important;padding-right:12px!important}\n' +
      '  .ek-cell{padding:8px 4px!important;font-size:13px!important}\n' +
      '  .ek-pill{min-width:0!important;padding:3px 5px!important}\n' +
      '}\n</style>\n</head>\n' +
      '<body style="margin:0;padding:0;background:' + C.paper + '">\n' +
      '<div style="display:none;font-size:1px;line-height:1px;max-height:0;max-width:0;opacity:0;overflow:hidden;mso-hide:all">' + esc(o.preheader || '') +
      '&#8199;&#65279;&#847;'.repeat(40) + '</div>\n' +
      '<table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" bgcolor="' + C.paper + '" style="background:' + C.paper + '"><tr><td class="ek-outer" align="center" style="padding:24px 12px 32px">\n' +
      '<!--[if mso]><table role="presentation" width="600" cellpadding="0" cellspacing="0" border="0"><tr><td><![endif]-->\n' +
      '<table role="presentation" class="ek-card" width="600" cellpadding="0" cellspacing="0" border="0" bgcolor="' + C.card + '" style="width:600px;max-width:600px;background:' + C.card +
      ';border:1px solid ' + C.line + ';border-radius:10px;border-collapse:separate">\n' +
      '<tr><td height="4" bgcolor="' + C.start + '" style="height:4px;font-size:0;line-height:0;background:' + C.start + ';background-image:' + GRAD + ';border-radius:10px 10px 0 0">&nbsp;</td></tr>\n' +
      header + '\n' + o.body + '\n</table>\n' +
      '<table role="presentation" class="ek-card" width="600" cellpadding="0" cellspacing="0" border="0" style="width:600px;max-width:600px">' + o.footer + '</table>\n' +
      '<!--[if mso]></td></tr></table><![endif]-->\n</td></tr></table>\n</body>\n</html>\n';
  };

  /** Plain-text version (sent alongside the HTML; helps deliverability). */
  const text = (lines) => lines.filter((l) => l != null).join('\n\n').replace(/\n{3,}/g, '\n\n') + '\n';
  const textFooter = (kind, o) => (kind === 'outreach' ? 'Not the right person, or not interested? Reply "no thanks" and I won\'t follow up.\n' : '') +
    'MPR Studios · ' + S.ADDRESS + ' · ' + S.CITY;

  /* =================================================================== */
  /* Templates                                                            */
  /* =================================================================== */

  /**
   * 1 · Personal letter. A note from Miguel, light branding, one button.
   * o: { subject, preheader, paragraphs: [..], button: {text,url}, after: [..], footer: 'outreach'|'product', footerReason }
   */
  function letter(o) {
    const body = row(o.paragraphs.map((t) => p(t)).join('') +
        (o.button ? '<div style="padding:6px 0 18px">' + button(o.button.text, o.button.url) + '</div>' : '') +
        (o.after || []).map((t) => p(t)).join(''), '6px 36px 4px') +
      row(signature(), '6px 36px 28px');
    const kind = o.footer || 'outreach';
    return {
      subject: o.subject,
      html: shell({ subject: o.subject, preheader: o.preheader, body: body, footer: footer(kind, { reason: o.footerReason }), headerPadBottom: 6 }),
      text: text(o.paragraphs.concat(o.button ? [o.button.text + ': ' + o.button.url] : [], o.after || [],
        [S.FROM_NAME + '\n' + S.FROM_TITLE + '\n' + S.SITE + ' · ' + S.REPLY_EMAIL, textFooter(kind)])),
    };
  }

  /**
   * 2 · Announcement. Field hero, headline, checkpoint bullets, one button, tip.
   * o: { subject, preheader, label, hero: true, headline, intro, bullets: [{bold,text}], button, note, closing, footerReason, unsubscribe }
   */
  function announcement(o) {
    const hero = o.hero === false ? '' : '<tr><td style="padding:0;font-size:0;line-height:0"><img class="ek-hero" src="' + img(S.IMG.hero) +
      '" width="598" height="210" alt="A marcher\'s path on the field from Start to End, with checkpoints on counts 4, 8 and 12" style="display:block;width:598px;height:210px;border:0"></td></tr>';
    const body = hero +
      row(h1(o.headline) + p(o.intro) + (o.bullets ? '<div style="padding:4px 0 2px">' + bullets(o.bullets) + '</div>' : '') +
        (o.button ? '<div style="padding:4px 0 18px">' + button(o.button.text, o.button.url) + '</div>' : '') +
        (o.note ? note(o.note) : ''), '28px 36px 6px') +
      row(p(o.closing || 'Questions? Just reply; it comes straight to me.', { mb: 8 }) + signoff(), '14px 36px 28px');
    return {
      subject: o.subject,
      html: shell({ subject: o.subject, preheader: o.preheader, label: o.label ? label(o.label) : '', body: body,
        footer: footer('product', { reason: o.footerReason, unsubscribe: o.unsubscribe !== false }) }),
      text: text([o.headline.toUpperCase(), o.intro].concat((o.bullets || []).map((b) => '× ' + (b.bold ? b.bold + ' ' : '') + (b.text || '')),
        [o.button ? o.button.text + ': ' + o.button.url : null, o.note, o.closing || 'Questions? Just reply; it comes straight to me.', 'Miguel · MPR Studios', textFooter('product')])),
    };
  }

  /**
   * 3 · Welcome / class setup.
   * o: { coachName, code, program, section, access: 'email'|'google', sheetUrl, coachEmail,
   *      students: [{ name, code, link }], dashboardUrl, previewUrl, googleSignupUrl }
   */
  function welcome(o) {
    const subject = 'Your MADA Lessons class is ready: ' + o.code;
    const google = o.access === 'google';
    const codeChip = '<span style="font-family:' + H + ';font-weight:700;font-size:15px;letter-spacing:0.08em;color:' + C.ink + ';background:' + C.paper +
      ';border:1px solid ' + C.line + ';border-radius:6px;padding:4px 10px">' + esc(o.code) + '</span>';
    const roster = (o.students || []).filter((s) => s.name && s.link);
    const rosterTable = roster.length ? dataTable([{ label: 'Student' }, { label: 'Join code' }, { label: 'Link' }],
      roster.map((s) => [esc(s.name), '<span style="font-family:' + H + ';font-weight:700;letter-spacing:0.06em">' + esc(s.code) + '</span>', '<span style="white-space:nowrap">' + link('Open link', s.link) + '</span>'])) : '';
    const step1 = google
      ? step(1, 'Add your students in the class sheet', 'Open the sheet signed in to Google as ' + o.coachEmail + '. On the Roster tab, type one student per row (first name and last initial). Within about 5 minutes each one gets a join code and link to send them.',
        '<div style="padding:0 0 4px">' + button('Open class sheet', o.sheetUrl, 'outline') + '</div>' +
        p('Can\'t open it? It needs a Google account for this email. You can make one free with this same email, no Gmail needed: ' +
          link('accounts.google.com/signup', o.googleSignupUrl || 'https://accounts.google.com/signup') + '. Or reply and I\'ll switch you to weekly email reports.', { html: true, size: 13, lh: 1.5, color: C.muted, mb: 0 }))
      : step(1, 'Send each student their join link', 'They open it once on their phone. No account, no email, no name box.',
        rosterTable || p('No students yet. Reply with their names (first name and last initial) and I\'ll send you their links.', { size: 15, mb: 0 }));
    const body =
      row(h1('Your class is ready') + p('Hi ' + firstName(o.coachName) + ', I approved your class' + (o.program ? ' for ' + o.program + (o.section ? ' · ' + o.section : '') : '') +
        '. Three steps and your students can start.', { mb: 6 }), '22px 36px 8px') +
      row(step1, '14px 36px 12px') +
      row(step(2, 'Bookmark your dashboard', 'Scores, weakest skills and recent activity for every student, refreshed every two minutes.',
        button('Open your dashboard', o.dashboardUrl)), '14px 36px 12px') +
      row(step(3, 'Try the lesson as a coach', 'Every level is open in preview, and nothing you do is recorded.',
        button('Preview the lesson', o.previewUrl, 'outline')), '14px 36px 16px') +
      row(note('Keep your dashboard and preview links to yourself. They open your class\'s results. Students only ever need their own join link.'), '10px 36px 6px') +
      row(p(google ? 'Questions? Just reply; it comes straight to me.' : 'Need to add or remove students? Reply with the names and I\'ll update your roster.', { mb: 8 }) + signoff(), '14px 36px 28px');
    return {
      subject: subject,
      html: shell({ subject: subject, preheader: 'Three steps and your students can start: join links, your dashboard, and a coach preview.',
        label: codeChip, body: body, headerPadBottom: 0, footer: footer('product', { reason: 'You\'re getting this because you signed up for a MADA Lessons class.' }) }),
      text: text(['Hi ' + firstName(o.coachName) + ',', 'Your MADA Lessons class ' + o.code + ' is ready. Three steps and your students can start.',
        google ? '1. Add your students in the class sheet: ' + o.sheetUrl + '\nOn the Roster tab, type one student per row. Each gets a join code and link within about 5 minutes.'
          : '1. Send each student their join link:\n' + (roster.length ? roster.map((s) => s.name + ' (' + s.code + '): ' + s.link).join('\n') : 'No students yet. Reply with their names.'),
        '2. Bookmark your dashboard: ' + o.dashboardUrl, '3. Try the lesson as a coach: ' + o.previewUrl,
        'Keep your dashboard and preview links to yourself. Students only ever need their own join link.', 'Miguel · MPR Studios', textFooter('product')]),
    };
  }

  /**
   * 4 · Weekly coach report.
   * o: { code, coachName, lessonTitle, dateLabel, stats: { students, active, checks, passedAll }, levels: 4,
   *      students: [{ name, scores: [{ text, pass }], last, notStarted }], weakSkills: [{ label, count }], tip,
   *      dashboardUrl, attachments: true }
   */
  function report(o) {
    const st = o.stats;
    const subject = o.code + ' weekly report: ' + st.active + ' of ' + st.students + ' students active';
    const levels = o.levels || 4;
    const cols = [{ label: 'Student' }].concat(Array.from({ length: levels }, (_, i) => ({ label: 'L' + (i + 1), align: 'center', width: '12%' })), [{ label: 'Last active' }]);
    const rows = (o.students || []).map((s) => [esc(s.name)].concat(
      Array.from({ length: levels }, (_, i) => { const c = (s.scores || [])[i] || {}; return score(c.text, c.pass); }),
      [s.notStarted ? '<span style="color:' + C.bad + ';font-weight:700">Not started</span>' : '<span style="color:' + C.muted + '">' + esc(s.last || '') + '</span>']));
    const body =
      row(h1(o.code + ' this week') + p((o.lessonTitle || 'Field Reading & Step Size') + ' · Coach: ' + o.coachName, { size: 15, lh: 1.55, color: C.muted, mb: 4 }), '22px 36px 8px') +
      row(stats([{ value: st.students, label: 'Students' }, { value: st.active, label: 'Active' },
        { value: st.checks, label: 'Checks done' }, { value: st.passedAll, label: 'Passed Level ' + levels, accent: true }]), '10px 36px 8px') +
      row(h2('Students') + (rows.length ? dataTable(cols, rows) : p('No students on the roster yet.', { mb: 0 })) +
        p('Green = passing score · red = below passing · – = not attempted yet.' + (o.attachments ? ' Full roster in the attached PDF and CSV.' : ''),
          { size: 12, lh: 1.5, color: C.muted, mb: 0 }).replace('margin:0 0 0px', 'margin:8px 0 0'), '16px 36px 8px', 'ek-tight') +
      (o.weakSkills && o.weakSkills.length ? row(h2('Weakest skills this week') + bars(o.weakSkills) + (o.tip ? '<div style="padding-top:12px">' + note(o.tip) + '</div>' : ''), '18px 36px 8px') : '') +
      row(buttons([{ text: 'Open your dashboard', url: o.dashboardUrl }].concat(o.lessonUrl ? [{ text: 'Open the lesson', url: o.lessonUrl, kind: 'outline' }] : [])), '18px 36px 30px');
    const sample = o.sample ? '<span style="font-family:' + B + ';font-size:11px;font-weight:700;letter-spacing:0.1em;text-transform:uppercase;color:' + C.endInk +
      ';border:1px solid ' + C.end + ';border-radius:4px;padding:2px 6px">Sample data</span>&nbsp;&nbsp;' : '';
    return {
      subject: subject,
      html: shell({ subject: subject, preheader: st.active + ' of ' + st.students + ' active · ' + st.checks + ' checks done · ' + st.passedAll + ' passed Level ' + levels,
        label: sample + label('Weekly report · ' + o.dateLabel), body: body, headerPadBottom: 0, footer: footer('report', { code: o.code }) }),
      text: text(['Hi ' + firstName(o.coachName) + ', here\'s where ' + o.code + ' stands this week (' + o.dateLabel + ').',
        'Students: ' + st.students + ' · Active this week: ' + st.active + ' · Checks done: ' + st.checks + ' · Passed Level ' + levels + ': ' + st.passedAll,
        (o.students || []).map((s) => s.name + ': ' + (s.notStarted ? 'not started' : (s.scores || []).map((c, i) => 'L' + (i + 1) + ' ' + (c.text || '–')).join(', '))).join('\n'),
        o.weakSkills && o.weakSkills.length ? 'Weakest skills: ' + o.weakSkills.map((w) => w.label + ' (' + w.count + ')').join(', ') : null,
        o.tip, 'Dashboard: ' + o.dashboardUrl, 'Sent every Monday at 7am. Reply to reach Miguel.\n' + textFooter('report')]),
    };
  }

  /* ---------- ready-made content ---------- */

  /** Pilot invitation. variant: 'known' (a director you know) or 'cold'. agreement attached separately. */
  function pilotInvite(o) {
    const name = o.firstName || 'there';
    const tryUrl = o.lessonUrl || S.LESSON_URL;
    if (o.variant === 'cold') {
      return letter({
        subject: 'Where\'s your marchers\' dot on count 12? (free pilot)',
        preheader: 'Free January–March pilot of MADA Lessons for a small group of programs.',
        paragraphs: ['Hi ' + name + ',',
          'I\'m Miguel Rodriquez, a visual caption head in the Central Valley with 15+ years in the marching arts, including touring drum corps. I\'m inviting a small group of programs to pilot a new tool for their marchers, free from January through March.',
          'MADA Lessons teaches students to read the field, calculate their own step sizes, and hit checkpoints on any count, through short interactive lessons on their phones. No student accounts or emails. You get a private dashboard showing each student\'s progress and their weakest skills, so rehearsal time goes to fixing drill instead of explaining it.',
          'The pilot is free through March 31. In return I ask for two 15-minute feedback calls and permission to quote you if it helps your program. The one-page agreement is attached.'],
        button: { text: 'Try Level 1 (about 5 minutes)', url: tryUrl },
        after: ['If it looks useful, reply "I\'m in" and I\'ll set up your class before winter break. Happy to answer any questions first.', 'Thanks,'],
      });
    }
    return letter({
      subject: 'Free pilot for your marchers: MADA Lessons (Jan–Mar)',
      preheader: 'Short interactive lessons for your marchers, plus a private coach dashboard. Free January–March.',
      paragraphs: ['Hi ' + name + ',',
        'I\'ve been building something on the side that I think your students would get a lot out of, and I\'d love for your program to be one of the first to try it.',
        'MADA Lessons is a set of short interactive lessons that teach marchers to read the field, work out their own step sizes, and know where their dot is on any count. Students open a link on their phone; no account or email needed. You get a private dashboard showing who\'s done what and where each student is struggling.',
        'I\'m running a free pilot from January 4 to March 31 with a small group of programs. All I ask is that your students use it, plus two 15-minute feedback calls and permission to quote you if you like it. The one-page agreement is attached.'],
      button: { text: 'Try Level 1 (about 5 minutes)', url: tryUrl },
      after: ['If you\'re in, just reply "I\'m in" and I\'ll set up your class before winter break.', 'Thanks,'],
    });
  }

  /** Pilot launch announcement (Jan 4). */
  function pilotLaunch(o) {
    return announcement({
      subject: 'The MADA Lessons pilot starts today',
      preheader: 'Your class is open: Levels 1–4, your coach dashboard and a report every Monday.',
      label: 'Pilot · Jan 4, 2027',
      headline: 'The pilot starts today',
      intro: 'Hi ' + firstName(o.coachName) + ', your class is open. Here\'s what\'s ready for your students this season.',
      bullets: [
        { bold: 'Levels 1–4 of Field Reading & Step Size.', text: 'Reading the field, step size, diagonals and checkpoints.' },
        { bold: 'Your coach dashboard.', text: 'Every student\'s scores and weakest skills, refreshed as they work.' },
        { bold: 'A report every Monday.', text: 'A one-page summary in your inbox, with a PDF and CSV.' }],
      button: { text: 'Open your dashboard', url: o.dashboardUrl },
      note: 'Our first feedback call is in late January. I\'ll email to find a time that works around rehearsal.',
      footerReason: 'You\'re getting this because your program is in the MADA Lessons pilot.',
    });
  }

  /** Sample data for every template (used by previews and test sends). */
  function samples() {
    const DASH = S.SITE + '/coach/dashboard/#class=GVHS-VIS&key=SAMPLE';
    const PREVIEW = S.LESSON_URL + '#coachclass=GVHS-VIS&coachkey=SAMPLE';
    const join = (c) => S.LESSON_URL + '?class=GVHS-VIS&code=' + c;
    return {
      '1-letter-invite-known': pilotInvite({ firstName: '[Name]', variant: 'known' }),
      '1-letter-invite-cold': pilotInvite({ firstName: '[Name]', variant: 'cold' }),
      '2-announcement-pilot-launch': pilotLaunch({ coachName: '[Coach]', dashboardUrl: DASH }),
      '3-welcome-email-reports': welcome({
        coachName: '[Coach]', code: 'GVHS-VIS', program: '[Program]', section: '[Section]', access: 'email',
        students: [{ name: 'Student A.', code: 'K7M2P', link: join('K7M2P') }, { name: 'Student B.', code: 'R4T9W', link: join('R4T9W') },
          { name: 'Student C.', code: 'H3N8Q', link: join('H3N8Q') }],
        dashboardUrl: DASH, previewUrl: PREVIEW,
      }),
      '3-welcome-google-sheet': welcome({
        coachName: '[Coach]', code: 'GVHS-VIS', program: '[Program]', section: '[Section]', access: 'google',
        coachEmail: 'coach@example.com', sheetUrl: 'https://docs.google.com/spreadsheets/d/SAMPLE', dashboardUrl: DASH, previewUrl: PREVIEW,
      }),
      '4-report-weekly': report({
        sample: true, code: 'GVHS-VIS', coachName: '[Coach]', dateLabel: 'Nov 16', levels: 4, attachments: true, dashboardUrl: DASH,
        stats: { students: 18, active: 14, checks: 63, passedAll: 5 },
        students: [
          { name: 'Student A.', last: 'Sun', scores: [{ text: '8/8', pass: true }, { text: '10/10', pass: true }, { text: '9/10', pass: true }, { text: '7/8', pass: true }] },
          { name: 'Student B.', last: 'Sat', scores: [{ text: '8/8', pass: true }, { text: '9/10', pass: true }, { text: '6/10', pass: false }, {}] },
          { name: 'Student C.', last: 'Wed', scores: [{ text: '7/8', pass: true }, { text: '7/10', pass: false }, {}, {}] },
          { name: 'Student D.', notStarted: true, scores: [] },
        ],
        weakSkills: [{ label: 'Odd-count checkpoint', count: 9 }, { label: 'Diagonal checkpoint', count: 6 }, { label: 'Crossing a yard line', count: 4 }],
        tip: 'Worth five minutes at rehearsal: odd-count checkpoints, where the answer lands between yard markers.',
      }),
    };
  }

  return { S: S, C: C, esc: esc, letter: letter, announcement: announcement, welcome: welcome, report: report,
    pilotInvite: pilotInvite, pilotLaunch: pilotLaunch, samples: samples };
})();

/**
 * Run from the Apps Script editor: sends every template, filled with sample data, to the testing inbox.
 * Push docs/assets/email/ to the live site first so the images load.
 */
function sendEmailTemplateTests() {
  const to = 'contact.mprstudios@gmail.com';
  const all = EK.samples();
  Object.keys(all).forEach((name) => {
    const m = all[name];
    MailApp.sendEmail({ to: to, subject: '[TEST ' + name + '] ' + m.subject, body: m.text, htmlBody: m.html, name: 'MPR Studios', replyTo: EK.S.REPLY_EMAIL });
  });
  Logger.log('Sent ' + Object.keys(all).length + ' test emails to ' + to);
}
