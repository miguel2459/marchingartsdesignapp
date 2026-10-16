/**
 * MPR Lessons Results Hub (v2)
 * Container-bound Apps Script for the "MPR Lessons Results Hub" Google Sheet.
 *
 *  - Coach sign-up page (mada.mprstudios.com) POSTs a request -> Requests tab. You check "Approve".
 *  - Approval (or MPR > Add class…) creates the coach's class sheet, shares it, and emails the coach.
 *  - Coaches without a Google account get weekly emailed progress reports instead of a shared sheet.
 *  - Coaches type student names on their Roster tab; the hub assigns join codes + links (every 5 min).
 *  - The lesson joins with class code + join code (GET ?action=join) and POSTs finished practice checks.
 *
 * Hub tabs:   Requests, Classes, Students, Results, Log     (never share this file)
 * Coach file: Summary, Roster, Results                      (one file per class)
 */

const CONFIG = {
  ADMIN_EMAIL: 'contact@mprstudios.com',
  SENDER_NAME: 'MPR Studios',
  LESSON_ID: 'field-reading',
  LESSON_URL: 'https://mada.mprstudios.com/lessons/field-reading/',
  SIGNUP_URL: 'https://mada.mprstudios.com/coach/',
  GOOGLE_SIGNUP_URL: 'https://accounts.google.com/signup',
  SCHEMA_VERSION: 1,
  ACTIVE_LEVELS: 4,
  COACH_FOLDER: 'MPR Lessons - Coach Sheets',
  CODE_ALPHABET: 'ABCDEFGHJKMNPQRSTUVWXYZ23456789', // no 0/O, 1/I/L
  CODE_LENGTH: 5,
  JOIN_LIMIT_PER_CLASS_PER_10_MIN: 60,
  SIGNUP_LIMIT_PER_10_MIN: 20,
  RECENT_ATTEMPTS_FOR_WEAK_SKILLS: 5,
};

/** Section choices on the sign-up page -> class code suffix. Keep in sync with the page. */
const SECTIONS = {
  'Full band': 'BAND', 'Visual / marching': 'VIS', 'Winds': 'WIND', 'Percussion': 'PERC',
  'Front ensemble': 'FE', 'Color guard': 'CG', 'Leadership': 'LEAD', 'Other': 'GRP',
};

const TABS = { REQUESTS: 'Requests', CLASSES: 'Classes', STUDENTS: 'Students', RESULTS: 'Results', LOG: 'Log' };
const HUB_HEADERS = {
  Requests: ['Received', 'Coach name', 'Coach email', 'Group name', 'Section', 'Location', 'Est. students',
             'Access', 'Student names', 'Notes', 'Class code', 'Approve', 'Status', 'Coach sheet link'],
  Classes: ['Class code', 'Coach name', 'Coach email', 'Coach sheet ID', 'Coach sheet link', 'Active', 'Created', 'Access'],
  Students: ['Student ID', 'Class code', 'Name', 'Join code', 'Active', 'Updated'],
  Results: ['Received', 'Result ID', 'Schema', 'Lesson', 'Class code', 'Student ID', 'Name', 'Level',
            'Score', 'Out of', 'Passed', 'Attempt', 'Missed skills', 'Field', 'Client time', 'App version'],
  Log: ['Time', 'Type', 'Detail'],
};
const REQ = { CODE: 11, APPROVE: 12, STATUS: 13, LINK: 14 }; // 1-based columns on Requests
const ACCESS = { GOOGLE: 'Google sheet', EMAIL: 'Email reports' };

const COACH = {
  SUMMARY: 'Summary', ROSTER: 'Roster', RESULTS: 'Results',
  ROSTER_HEADERS: ['Student name', 'Join code', 'Join link', 'Active', 'Student ID'],
  RESULTS_HEADERS: ['Date', 'Student', 'Level', 'Score', 'Out of', 'Passed', 'Attempt', 'Missed skills', 'Field', 'Student ID'],
};
function summaryHeaders_() {
  const h = ['Student', 'Levels passed'];
  for (let L = 1; L <= CONFIG.ACTIVE_LEVELS; L++) h.push('Level ' + L + ' best');
  return h.concat(['Attempts', 'Weakest skills (recent)', 'Last active']);
}

/* ============================== Menu & setup ============================== */

/** Run once from the Apps Script editor (Run button) to approve every permission up front. */
function authorizeAll() {
  SpreadsheetApp.getActiveSpreadsheet();
  DriveApp.getRootFolder();
  Session.getEffectiveUser().getEmail();
  ScriptApp.getProjectTriggers();
  CacheService.getScriptCache();
  PropertiesService.getScriptProperties();
  MailApp.getRemainingDailyQuota();
  Logger.log('All permissions approved.');
}

function onOpen() {
  SpreadsheetApp.getUi().createMenu('MPR')
    .addItem('1. Set up hub (safe to re-run)', 'setupHub')
    .addItem('2. Turn on automations (safe to re-run)', 'installAutomations')
    .addSeparator()
    .addItem('Add class…', 'addClass')
    .addItem('Sync rosters now', 'syncRostersNow')
    .addItem('Rebuild all summaries', 'rebuildAllSummaries')
    .addItem('Send email reports now', 'sendEmailReportsNow')
    .addSeparator()
    .addItem('Send a test result (MPR-TEST)', 'sendTestResult')
    .addToUi();
}

function setupHub() {
  const ss = SpreadsheetApp.getActiveSpreadsheet();
  PropertiesService.getScriptProperties().setProperty('HUB_ID', ss.getId());
  Object.keys(HUB_HEADERS).forEach(name => ensureSheet_(ss, name, HUB_HEADERS[name]));
  const blank = ss.getSheetByName('Sheet1');
  if (blank && blank.getLastRow() === 0 && ss.getSheets().length > 1) ss.deleteSheet(blank);
  SpreadsheetApp.getUi().alert('Hub is set up. Next: MPR > 2. Turn on automations.');
}

/** 5-minute roster sync, approval checkbox on Requests, weekly email reports (Mondays 7am). */
function installAutomations() {
  const names = ['syncRosters', 'onHubEdit', 'sendEmailReports'];
  ScriptApp.getProjectTriggers().filter(t => names.indexOf(t.getHandlerFunction()) >= 0)
    .forEach(t => ScriptApp.deleteTrigger(t));
  ScriptApp.newTrigger('syncRosters').timeBased().everyMinutes(5).create();
  ScriptApp.newTrigger('onHubEdit').forSpreadsheet(hub_()).onEdit().create();
  ScriptApp.newTrigger('sendEmailReports').timeBased().onWeekDay(ScriptApp.WeekDay.MONDAY).atHour(7).create();
  SpreadsheetApp.getUi().alert('Automations are on:\n• Roster sync every 5 minutes\n• Approve checkbox on Requests\n• Weekly email reports (Mondays, 7am)');
}

/* ============================== Classes ============================== */

function addClass() {
  const ui = SpreadsheetApp.getUi();
  const raw = ask_(ui, 'Add class', 'Class code (3–20 letters, numbers, or dashes), e.g. GVHS-VIS:');
  if (raw === null) return;
  const classCode = normCode_(raw);
  if (!/^[A-Z0-9-]{3,20}$/.test(classCode)) return ui.alert('Class codes must be 3–20 letters, numbers, or dashes.');
  if (findClass_(classCode)) return ui.alert('Class ' + classCode + ' already exists.');
  const coachName = ask_(ui, 'Add class', 'Coach name:');
  if (coachName === null) return;
  const coachEmail = ask_(ui, 'Add class', 'Coach email:');
  if (coachEmail === null) return;
  const g = ui.alert('Add class', 'Can this coach open Google Sheets with that email?\n\nYes = share their class sheet.\nNo = send weekly email reports instead.', ui.ButtonSet.YES_NO);
  const access = g === ui.Button.YES ? ACCESS.GOOGLE : ACCESS.EMAIL;

  const cls = createClass_(classCode, coachName, coachEmail, access, []);
  ui.alert('Class ' + classCode + ' is ready, and a welcome email went to ' + coachEmail + '.\n\nCoach sheet:\n' + cls.url +
    (access === ACCESS.GOOGLE ? '\n\nThe coach types student names on the Roster tab.' :
      '\n\nThis coach gets email reports. Type their students on the Roster tab yourself.'));
}

/** Creates the coach sheet, registers the class, adds any roster names, and emails the coach. */
function createClass_(classCode, coachName, coachEmail, access, studentNames) {
  const sheet = createCoachSheet_(classCode, coachEmail, access === ACCESS.GOOGLE);
  const cs = hub_().getSheetByName(TABS.CLASSES);
  const row = nextEmptyRow_(cs);
  cs.getRange(row, 1, 1, 8).setValues([[classCode, coachName, coachEmail, sheet.id, sheet.url, true, new Date(), access]]);
  cs.getRange(row, 6).insertCheckboxes();
  log_('class_added', classCode + ' -> ' + coachEmail + ' (' + access + ')' + (sheet.shareNote ? ' ' + sheet.shareNote : ''));

  if (studentNames && studentNames.length) {
    const roster = SpreadsheetApp.openById(sheet.id).getSheetByName(COACH.ROSTER);
    roster.getRange(2, 1, studentNames.length, 1).setValues(studentNames.map(n => [n]));
    syncRosters();
  }
  const cls = findClass_(classCode);
  sendWelcome_(cls, sheet.shareNote);
  return cls;
}

function createCoachSheet_(classCode, coachEmail, shareWithCoach) {
  const ss = SpreadsheetApp.create(classCode + ' - Field Reading Results');
  const file = DriveApp.getFileById(ss.getId());
  file.moveTo(coachFolder_());

  const roster = ss.getSheets()[0].setName(COACH.ROSTER);
  writeHeaders_(roster, COACH.ROSTER_HEADERS);
  roster.getRange('A1').setNote('Type one student per row in this column (first name + last initial is enough). ' +
    'Join codes and links appear within about 5 minutes. Uncheck Active to turn a student off.');
  roster.setColumnWidth(1, 200).setColumnWidth(3, 380);
  roster.hideColumns(5);

  const results = ss.insertSheet(COACH.RESULTS);
  writeHeaders_(results, COACH.RESULTS_HEADERS);
  results.getRange('A2:A').setNumberFormat('mmm d, h:mm am/pm');
  results.hideColumns(COACH.RESULTS_HEADERS.length);

  const summary = ss.insertSheet(COACH.SUMMARY, 0);
  writeHeaders_(summary, summaryHeaders_());
  summary.getRange(2, summaryHeaders_().length, 999, 1).setNumberFormat('mmm d, h:mm am/pm');

  // Lock everything the coach shouldn't edit BEFORE sharing, so the coach is never an editor of these ranges.
  lockProtection_(results.protect().setDescription('Written by MPR Studios'));
  lockProtection_(summary.protect().setDescription('Written by MPR Studios'));
  lockProtection_(roster.getRange('B:C').protect().setDescription('Join codes are assigned automatically'));
  lockProtection_(roster.getRange('E:E').protect().setDescription('Student IDs are assigned automatically'));
  lockProtection_(roster.getRange('1:1').protect().setDescription('Header row'));

  let shareNote = '';
  const me = Session.getEffectiveUser().getEmail().toLowerCase();
  if (shareWithCoach && coachEmail && coachEmail.toLowerCase() !== me) {
    try { ss.addEditor(coachEmail); } catch (e) { shareNote = '(could not share: ' + e.message + ')'; }
  }
  file.setShareableByEditors(false);
  return { id: ss.getId(), url: ss.getUrl(), shareNote: shareNote };
}

/* ============================== Coach sign-up & approval ============================== */

function signup_(b) {
  if (b.website) return { ok: true }; // honeypot: bots fill hidden fields; pretend success
  const cache = CacheService.getScriptCache();
  const n = Number(cache.get('signups') || 0) + 1;
  cache.put('signups', String(n), 600);
  if (n > CONFIG.SIGNUP_LIMIT_PER_10_MIN) return { ok: false, error: 'try_later' };

  const s = (x, max) => String(x || '').replace(/[\u0000-\u001f]/g, ' ').trim().slice(0, max);
  const r = {
    coachName: s(b.coachName, 80), email: s(b.email, 120).toLowerCase(), groupName: s(b.groupName, 100),
    section: SECTIONS[b.section] ? b.section : 'Other', location: s(b.location, 100),
    estStudents: Math.max(0, Math.min(1000, parseInt(b.estStudents, 10) || 0)),
    access: b.access === 'email' ? ACCESS.EMAIL : ACCESS.GOOGLE,
    students: String(b.students || '').split(/\r?\n/).map(x => s(x, 60)).filter(Boolean).slice(0, 300),
    notes: s(b.notes, 1000),
  };
  if (!r.coachName || !r.groupName || !/^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(r.email)) return { ok: false, error: 'invalid' };

  const lock = LockService.getScriptLock();
  lock.waitLock(20000);
  let code;
  try {
    code = suggestCode_(r.groupName, r.section);
    const sh = hub_().getSheetByName(TABS.REQUESTS);
    const row = nextEmptyRow_(sh);
    sh.getRange(row, 1, 1, 13).setValues([[new Date(), r.coachName, r.email, r.groupName, r.section, r.location,
      r.estStudents, r.access, r.students.join('\n'), r.notes, code, false, 'Pending']]);
    sh.getRange(row, REQ.APPROVE).insertCheckboxes();
  } finally { lock.releaseLock(); }

  mail_(CONFIG.ADMIN_EMAIL, 'New coach request: ' + r.groupName + ' (' + r.section + ')',
    '<p><b>' + esc_(r.coachName) + '</b> (' + esc_(r.email) + ') requested access.</p><ul>' +
    '<li>Group: ' + esc_(r.groupName) + ' — ' + esc_(r.section) + '</li><li>Location: ' + esc_(r.location) + '</li>' +
    '<li>Students: about ' + r.estStudents + (r.students.length ? ' (' + r.students.length + ' names listed)' : '') + '</li>' +
    '<li>Access: ' + esc_(r.access) + '</li><li>Suggested class code: <b>' + code + '</b></li></ul>' +
    (r.notes ? '<p>Notes: ' + esc_(r.notes) + '</p>' : '') +
    '<p>To approve, open the hub\'s Requests tab, adjust the class code if you like, and check <b>Approve</b>.</p>');
  mail_(r.email, 'We got your request — MPR Studios Lessons',
    '<p>Hi ' + esc_(r.coachName.split(' ')[0]) + ',</p>' +
    '<p>Thanks for signing up <b>' + esc_(r.groupName) + '</b> for MPR Studios Lessons. We\'ll review your request and ' +
    'email you your class link, usually within a day or two.</p>' +
    (r.access === ACCESS.GOOGLE ? '<p>Your class sheet will be shared with this email address, so it needs to be a Google account ' +
      '(Gmail, or a school Google account). Not sure? You can make a free Google account with this same email, no Gmail needed: ' +
      '<a href="' + CONFIG.GOOGLE_SIGNUP_URL + '">accounts.google.com/signup</a> → "Use your existing email."</p>' :
      '<p>You chose email reports, so you\'ll get a progress summary for your students every Monday morning.</p>') +
    '<p>— MPR Studios</p>');
  return { ok: true };
}

/** Installable onEdit trigger: checking "Approve" on the Requests tab creates the class. */
function onHubEdit(e) {
  const r = e && e.range;
  if (!r || r.getNumRows() !== 1 || r.getNumColumns() !== 1) return;
  const sh = r.getSheet();
  if (sh.getName() !== TABS.REQUESTS || r.getColumn() !== REQ.APPROVE || r.getRow() < 2 || r.getValue() !== true) return;
  approveRequest_(sh, r.getRow());
}

function approveRequest_(sh, row) {
  const v = sh.getRange(row, 1, 1, HUB_HEADERS.Requests.length).getValues()[0];
  if (String(v[REQ.STATUS - 1]).indexOf('Approved') === 0) return;
  const status = msg => sh.getRange(row, REQ.STATUS).setValue(msg);
  const lock = LockService.getScriptLock();
  if (!lock.tryLock(30000)) { status('Busy — uncheck and check Approve again'); sh.getRange(row, REQ.APPROVE).setValue(false); return; }
  try {
    const code = normCode_(v[REQ.CODE - 1]);
    if (!/^[A-Z0-9-]{3,20}$/.test(code)) { status('Fix the class code (3–20 letters, numbers, dashes), then check Approve'); sh.getRange(row, REQ.APPROVE).setValue(false); return; }
    if (findClass_(code)) { status('Class code ' + code + ' is taken — change it, then check Approve'); sh.getRange(row, REQ.APPROVE).setValue(false); return; }
    status('Creating…');
    SpreadsheetApp.flush();
    const names = String(v[8]).split(/\r?\n/).map(x => x.trim()).filter(Boolean);
    lock.releaseLock();
    const cls = createClass_(code, String(v[1]), String(v[2]), String(v[7]) || ACCESS.GOOGLE, names);
    sh.getRange(row, REQ.CODE).setValue(code);
    sh.getRange(row, REQ.LINK).setValue(cls.url);
    status('Approved ' + Utilities.formatDate(new Date(), Session.getScriptTimeZone(), 'MMM d, h:mm a') + ' — welcome email sent');
  } catch (err) {
    status('Error: ' + err.message);
    sh.getRange(row, REQ.APPROVE).setValue(false);
    log_('approve_error', 'row ' + row + ': ' + err);
  } finally {
    try { lock.releaseLock(); } catch (e) { /* already released */ }
  }
}

/** e.g. "Golden Valley High School" + "Visual / marching" -> GVHS-VIS (adds -2, -3… if taken). */
function suggestCode_(groupName, section) {
  const stop = { OF: 1, THE: 1, AND: 1, AT: 1, FOR: 1, A: 1 };
  const words = String(groupName).toUpperCase().replace(/[^A-Z0-9 ]/g, ' ').split(/\s+/).filter(w => w && !stop[w]);
  let base = words.length > 1 ? words.map(w => (/^\d+$/.test(w) ? w : w[0])).join('') : (words[0] || 'GRP').slice(0, 6);
  base = base.slice(0, 10);
  const suffix = SECTIONS[section] || 'GRP';
  const taken = new Set(getClasses_().map(c => c.code));
  const rq = hub_().getSheetByName(TABS.REQUESTS);
  if (rq.getLastRow() > 1) rq.getRange(2, REQ.CODE, rq.getLastRow() - 1, 1).getValues().forEach(x => taken.add(normCode_(x[0])));
  let code = base + '-' + suffix, n = 2;
  while (taken.has(code)) code = base + '-' + suffix + (n++);
  return code;
}

/* ============================== Emails ============================== */

function sendWelcome_(cls, shareNote) {
  const first = String(cls.coachName).split(' ')[0] || 'Coach';
  const steps = cls.access === ACCESS.GOOGLE ?
    '<p>Your class sheet is ready:<br><a href="' + cls.url + '"><b>' + esc_(cls.code) + ' — Field Reading Results</b></a></p>' +
    '<ol><li>Open the sheet while signed in to Google as <b>' + esc_(cls.coachEmail) + '</b>.</li>' +
    '<li>On the <b>Roster</b> tab, type one student per row (first name + last initial is enough).</li>' +
    '<li>Within about 5 minutes, each student gets a <b>join code</b> and a personal <b>join link</b>. Send each student their link.</li>' +
    '<li>Students open their link once and practice. Results show up on your <b>Summary</b> and <b>Results</b> tabs automatically.</li></ol>' +
    '<p>Can\'t open the sheet? It needs a Google account for this email address. You can make one free with this same email, ' +
    'no Gmail needed: <a href="' + CONFIG.GOOGLE_SIGNUP_URL + '">accounts.google.com/signup</a> → "Use your existing email." ' +
    'Or reply to this email and we\'ll switch you to weekly email reports.</p>' :
    '<p>You\'ll get a progress report for your students every Monday morning.</p>' + rosterLinksHtml_(cls) +
    '<p>To add or remove students, just reply to this email with the changes.</p>';
  mail_(cls.coachEmail, 'Your MPR Studios Lessons class is ready (' + cls.code + ')',
    '<p>Hi ' + esc_(first) + ',</p><p>Welcome to MPR Studios Lessons! Your class code is <b>' + esc_(cls.code) + '</b>.</p>' + steps +
    '<p>The lesson: <a href="' + CONFIG.LESSON_URL + '">Field Reading &amp; Step Size</a></p><p>— MPR Studios</p>');
  if (shareNote) mail_(CONFIG.ADMIN_EMAIL, 'Sharing problem for ' + cls.code, '<p>' + esc_(shareNote) + '</p><p>Sheet: ' + cls.url + '</p>');
}

function rosterLinksHtml_(cls) {
  const roster = readRoster_(SpreadsheetApp.openById(cls.sheetId).getSheetByName(COACH.ROSTER)).filter(r => r.name && r.code && r.active);
  if (!roster.length) return '';
  return '<p>Send each student their personal join link:</p><table cellpadding="6" style="border-collapse:collapse">' +
    roster.map(r => '<tr><td style="border-bottom:1px solid #ddd">' + esc_(r.name) + '</td><td style="border-bottom:1px solid #ddd">' +
      '<a href="' + r.link + '">' + r.link + '</a></td></tr>').join('') + '</table>';
}

function sendEmailReportsNow() {
  const n = sendEmailReports();
  SpreadsheetApp.getUi().alert(n + ' email report(s) sent.');
}

/** Weekly (Mondays): progress summary to every active "Email reports" coach. */
function sendEmailReports() {
  let sent = 0;
  getClasses_().filter(c => c.active && c.access === ACCESS.EMAIL && c.sheetId).forEach(cls => {
    try {
      const ss = SpreadsheetApp.openById(cls.sheetId);
      rebuildSummary_(cls, ss);
      const vals = ss.getSheetByName(COACH.SUMMARY).getDataRange().getDisplayValues();
      const table = '<table cellpadding="6" style="border-collapse:collapse;font-size:14px">' + vals.map((r, i) =>
        '<tr>' + r.map(c => (i === 0 ? '<th style="text-align:left;border-bottom:2px solid #333">' : '<td style="border-bottom:1px solid #ddd">') +
          esc_(c) + (i === 0 ? '</th>' : '</td>')).join('') + '</tr>').join('') + '</table>';
      mail_(cls.coachEmail, 'Weekly progress: ' + cls.code,
        '<p>Hi ' + esc_(String(cls.coachName).split(' ')[0]) + ', here\'s where your students stand this week.</p>' +
        (vals.length > 1 ? table : '<p>No students on the roster yet.</p>') + rosterLinksHtml_(cls) + '<p>— MPR Studios</p>');
      sent++;
    } catch (e) { log_('report_error', cls.code + ': ' + e); }
  });
  return sent;
}

function mail_(to, subject, html) {
  MailApp.sendEmail({ to: to, subject: subject, htmlBody: html, name: CONFIG.SENDER_NAME, replyTo: CONFIG.ADMIN_EMAIL });
}

/* ============================== Rosters ============================== */

function syncRostersNow() {
  const n = syncRosters();
  SpreadsheetApp.getUi().alert('Rosters synced. ' + n + ' new join code(s) assigned.');
}

/** Runs every 5 minutes. Assigns IDs/codes to new roster names and refreshes the hub Students tab. */
function syncRosters() {
  const lock = LockService.getScriptLock();
  if (!lock.tryLock(30000)) return 0;
  let added = 0;
  try {
    const all = [];
    getClasses_().filter(c => c.active && c.sheetId).forEach(cls => {
      let ss;
      try { ss = SpreadsheetApp.openById(cls.sheetId); } catch (e) { log_('sync_error', cls.code + ': ' + e); return; }
      const sh = ss.getSheetByName(COACH.ROSTER);
      const rows = readRoster_(sh);
      const taken = new Set(rows.map(r => r.code).filter(Boolean));
      rows.forEach(r => {
        if (!r.name) return;
        if (!r.id) {
          r.id = 'S' + Utilities.getUuid().replace(/-/g, '').slice(0, 12);
          r.code = newCode_(taken); taken.add(r.code);
          r.active = true; r.dirty = true; added++;
        }
        const link = CONFIG.LESSON_URL + '?class=' + encodeURIComponent(cls.code) + '&code=' + r.code;
        if (r.link !== link) { r.link = link; r.dirty = true; }
        if (r.dirty) {
          sh.getRange(r.row, 2, 1, 4).setValues([[r.code, r.link, r.active, r.id]]);
          sh.getRange(r.row, 4).insertCheckboxes();
        }
        all.push([r.id, cls.code, r.name, r.code, r.active, new Date()]);
      });
    });
    const st = hub_().getSheetByName(TABS.STUDENTS);
    if (st.getLastRow() > 1) st.getRange(2, 1, st.getLastRow() - 1, HUB_HEADERS.Students.length).clearContent();
    if (all.length) st.getRange(2, 1, all.length, HUB_HEADERS.Students.length).setValues(all);
    CacheService.getScriptCache().remove('students');
  } finally { lock.releaseLock(); }
  return added;
}

function readRoster_(sh) {
  const n = sh.getLastRow() - 1;
  if (n < 1) return [];
  return sh.getRange(2, 1, n, 5).getValues().map((v, i) => ({
    row: i + 2, name: String(v[0]).trim(), code: String(v[1]).trim(), link: String(v[2]).trim(),
    active: v[3] === true, id: String(v[4]).trim(),
  }));
}

function newCode_(taken) {
  const a = CONFIG.CODE_ALPHABET;
  for (;;) {
    let c = '';
    for (let i = 0; i < CONFIG.CODE_LENGTH; i++) c += a[Math.floor(Math.random() * a.length)];
    if (!taken.has(c)) return c;
  }
}

/* ============================== Web app ============================== */

function doGet(e) {
  const p = (e && e.parameter) || {};
  try {
    if (p.action === 'join') return json_(join_(p));
    if (p.action === 'ping') return json_({ ok: true });
    return json_({ ok: false, error: 'unknown_action' });
  } catch (err) {
    log_('error', 'doGet: ' + err);
    return json_({ ok: false, error: 'server_error' });
  }
}

/** Student joins once: class code + personal join code -> student ID + coach-entered name. */
function join_(p) {
  const classCode = normCode_(p.class);
  const code = String(p.code || '').replace(/\s+/g, '').toUpperCase();
  if (!/^[A-Z0-9-]{3,20}$/.test(classCode) || code.length !== CONFIG.CODE_LENGTH) return { ok: false, error: 'not_found' };

  const cache = CacheService.getScriptCache(), key = 'j:' + classCode;
  const tries = Number(cache.get(key) || 0) + 1;
  cache.put(key, String(tries), 600);
  if (tries > CONFIG.JOIN_LIMIT_PER_CLASS_PER_10_MIN) { log_('join_limited', classCode); return { ok: false, error: 'try_later' }; }

  const cls = findClass_(classCode);
  if (!cls || !cls.active) return { ok: false, error: 'not_found' };
  const st = getStudents_().find(s => s.classCode === classCode && s.code === code && s.active);
  if (!st) { log_('join_failed', classCode + ' / ' + code); return { ok: false, error: 'not_found' }; }
  return { ok: true, studentId: st.id, name: st.name, classCode: classCode };
}

/** JSON body sent as Content-Type: text/plain. type = 'result' (lesson) or 'signup' (coach page). */
function doPost(e) {
  let body;
  try { body = JSON.parse(e.postData.contents); } catch (err) { return json_({ ok: false, error: 'bad_json' }); }
  try {
    if (body && body.type === 'signup') return json_(signup_(body));
    return json_(recordResult_(body));
  } catch (err) {
    log_('error', 'doPost: ' + err);
    return json_({ ok: false, error: 'server_error', retry: true });
  }
}

function recordResult_(b) {
  const v = validate_(b);
  if (!v.ok) { log_('rejected', v.error + ' ' + JSON.stringify(b).slice(0, 300)); return v; }
  const r = v.result;

  const cls = findClass_(r.classCode);
  if (!cls || !cls.active) return { ok: false, error: 'unknown_class' };
  const st = getStudents_().find(s => s.id === r.studentId && s.classCode === r.classCode);
  if (!st || !st.active) return { ok: false, error: 'unknown_student' };

  const lock = LockService.getScriptLock();
  lock.waitLock(20000);
  try {
    if (isDuplicate_(r.resultId)) return { ok: true, duplicate: true };
    const now = new Date();
    const missed = r.missed.join(', ');
    hub_().getSheetByName(TABS.RESULTS).appendRow([now, r.resultId, CONFIG.SCHEMA_VERSION, r.lesson, r.classCode, st.id, st.name,
      r.level, r.score, r.total, r.passed ? 'Yes' : 'No', r.attempt, missed, r.field, r.clientTime, r.appVersion]);
    CacheService.getScriptCache().put('r:' + r.resultId, '1', 21600);

    const coach = SpreadsheetApp.openById(cls.sheetId);
    coach.getSheetByName(COACH.RESULTS).appendRow([now, st.name, r.level, r.score, r.total, r.passed ? 'Yes' : 'No',
      r.attempt, missed, r.field === 'hs' ? 'High school' : 'College', st.id]);
    rebuildSummary_(cls, coach);
  } finally { lock.releaseLock(); }
  return { ok: true };
}

function validate_(b) {
  const bad = e => ({ ok: false, error: e });
  if (!b || b.type !== 'result') return bad('invalid');
  const int = x => Number.isInteger(Number(x)) ? Number(x) : NaN;
  const r = {
    resultId: String(b.resultId || ''),
    lesson: String(b.lesson || CONFIG.LESSON_ID).slice(0, 40),
    classCode: normCode_(b.classCode),
    studentId: String(b.studentId || '').trim(),
    level: int(b.level), score: int(b.score), total: int(b.total), attempt: int(b.attempt || 1),
    passed: b.passed === true,
    missed: (Array.isArray(b.missed) ? b.missed : String(b.missed || '').split(','))
      .map(s => String(s).trim().replace(/[^\w &'()-]/g, '').slice(0, 40)).filter(Boolean).slice(0, 20),
    field: b.field === 'hs' ? 'hs' : 'college',
    clientTime: String(b.clientTime || '').slice(0, 40),
    appVersion: String(b.appVersion || '').slice(0, 20),
  };
  if (!/^[A-Za-z0-9-]{8,64}$/.test(r.resultId)) return bad('invalid');
  if (!/^[A-Z0-9-]{3,20}$/.test(r.classCode) || !/^S[a-f0-9]{12}$/.test(r.studentId)) return bad('invalid');
  if (!(r.level >= 1 && r.level <= 6)) return bad('invalid');
  if (!(r.total >= 1 && r.total <= 20) || !(r.score >= 0 && r.score <= r.total)) return bad('invalid');
  if (!(r.attempt >= 1 && r.attempt <= 9999)) return bad('invalid');
  return { ok: true, result: r };
}

function isDuplicate_(resultId) {
  if (CacheService.getScriptCache().get('r:' + resultId)) return true;
  const sh = hub_().getSheetByName(TABS.RESULTS);
  const last = sh.getLastRow();
  if (last < 2) return false;
  const start = Math.max(2, last - 1999);
  return sh.getRange(start, 2, last - start + 1, 1).getValues().some(v => v[0] === resultId);
}

/* ============================== Summaries ============================== */

function rebuildAllSummaries() {
  getClasses_().filter(c => c.active && c.sheetId).forEach(c => rebuildSummary_(c, SpreadsheetApp.openById(c.sheetId)));
  SpreadsheetApp.getUi().alert('All summaries rebuilt.');
}

function rebuildSummary_(cls, coach) {
  const roster = readRoster_(coach.getSheetByName(COACH.ROSTER)).filter(r => r.id && r.name);
  const rs = coach.getSheetByName(COACH.RESULTS);
  const n = rs.getLastRow() - 1;
  const rows = n > 0 ? rs.getRange(2, 1, n, COACH.RESULTS_HEADERS.length).getValues() : [];
  const byStudent = {};
  rows.forEach(r => { (byStudent[r[9]] = byStudent[r[9]] || []).push(r); });

  const out = roster.map(st => {
    const list = (byStudent[st.id] || []).sort((a, b) => new Date(a[0]) - new Date(b[0]));
    const passed = [...new Set(list.filter(r => r[5] === 'Yes').map(r => Number(r[2])))].sort((a, b) => a - b);
    const best = [];
    for (let L = 1; L <= CONFIG.ACTIVE_LEVELS; L++) {
      const lv = list.filter(r => Number(r[2]) === L);
      if (!lv.length) { best.push('–'); continue; }
      const top = lv.reduce((m, r) => (r[3] / r[4] > m[3] / m[4] ? r : m));
      best.push(top[3] + ' of ' + top[4]);
    }
    const counts = {};
    list.slice(-CONFIG.RECENT_ATTEMPTS_FOR_WEAK_SKILLS).forEach(r =>
      String(r[7]).split(',').map(s => s.trim()).filter(Boolean).forEach(s => { counts[s] = (counts[s] || 0) + 1; }));
    const weak = Object.keys(counts).sort((a, b) => counts[b] - counts[a]).slice(0, 2)
      .map(s => s + ' (' + counts[s] + ')').join(', ');
    return [st.name, passed.length ? passed.join(', ') : 'None yet', ...best, list.length,
      weak || (list.length ? 'No misses' : ''), list.length ? list[list.length - 1][0] : 'Not started'];
  });

  const sh = coach.getSheetByName(COACH.SUMMARY);
  const width = summaryHeaders_().length;
  if (sh.getLastRow() > 1) sh.getRange(2, 1, sh.getLastRow() - 1, width).clearContent();
  if (out.length) sh.getRange(2, 1, out.length, width).setValues(out);
}

/* ============================== Test ============================== */

/** Sends a fake result for the first student on the MPR-TEST roster, through the real pipeline. */
function sendTestResult() {
  const ui = SpreadsheetApp.getUi();
  const st = getStudents_().find(s => s.classCode === 'MPR-TEST' && s.active);
  if (!st) return ui.alert('Add a name to the MPR-TEST Roster tab and run MPR > Sync rosters now first.');
  const res = recordResult_({
    type: 'result', resultId: 'test-' + Utilities.getUuid(), lesson: CONFIG.LESSON_ID,
    classCode: 'MPR-TEST', studentId: st.id, level: 2, score: 8, total: 10, passed: true, attempt: 1,
    missed: ['Crossing the 50', 'Counts needed'], field: 'college', clientTime: new Date().toISOString(), appVersion: 'test',
  });
  ui.alert('Result: ' + JSON.stringify(res) + '\n\nCheck the hub Results tab and the MPR-TEST coach sheet.');
}

/* ============================== Helpers ============================== */

function hub_() {
  const id = PropertiesService.getScriptProperties().getProperty('HUB_ID');
  return id ? SpreadsheetApp.openById(id) : SpreadsheetApp.getActiveSpreadsheet();
}

function getClasses_() {
  const sh = hub_().getSheetByName(TABS.CLASSES);
  const n = sh.getLastRow() - 1;
  if (n < 1) return [];
  return sh.getRange(2, 1, n, HUB_HEADERS.Classes.length).getValues()
    .filter(v => String(v[0]).trim())
    .map(v => ({
      code: String(v[0]).trim().toUpperCase(), coachName: v[1], coachEmail: v[2], sheetId: String(v[3]).trim(),
      url: v[4], active: v[5] === true, access: String(v[7]).trim() || ACCESS.GOOGLE,
    }));
}
function findClass_(code) { return getClasses_().find(c => c.code === code) || null; }

function getStudents_() {
  const cache = CacheService.getScriptCache();
  const hit = cache.get('students');
  if (hit) return JSON.parse(hit);
  const sh = hub_().getSheetByName(TABS.STUDENTS);
  const n = sh.getLastRow() - 1;
  const list = n < 1 ? [] : sh.getRange(2, 1, n, 5).getValues().map(v => ({
    id: String(v[0]).trim(), classCode: String(v[1]).trim().toUpperCase(), name: String(v[2]).trim(),
    code: String(v[3]).trim().toUpperCase(), active: v[4] === true,
  }));
  try { cache.put('students', JSON.stringify(list), 300); } catch (e) { /* too large to cache; fine */ }
  return list;
}

/** First row after the last non-empty cell in column A (ignores checkboxes further down). */
function nextEmptyRow_(sh) {
  const n = Math.max(sh.getLastRow(), 1);
  const col = sh.getRange(1, 1, n, 1).getValues();
  for (let i = col.length - 1; i >= 0; i--) if (String(col[i][0]).trim()) return i + 2;
  return 2;
}

function normCode_(x) { return String(x || '').trim().toUpperCase().replace(/[_\s]+/g, '-'); }

function coachFolder_() {
  const it = DriveApp.getFoldersByName(CONFIG.COACH_FOLDER);
  return it.hasNext() ? it.next() : DriveApp.createFolder(CONFIG.COACH_FOLDER);
}

function ensureSheet_(ss, name, headers) {
  const sh = ss.getSheetByName(name) || ss.insertSheet(name);
  writeHeaders_(sh, headers);
  return sh;
}
function writeHeaders_(sh, headers) {
  sh.getRange(1, 1, 1, headers.length).setValues([headers]).setFontWeight('bold');
  sh.setFrozenRows(1);
}

function lockProtection_(p) {
  const me = Session.getEffectiveUser();
  p.addEditor(me);
  p.removeEditors(p.getEditors().filter(u => u.getEmail() !== me.getEmail()));
  if (p.canDomainEdit()) p.setDomainEdit(false);
}

function ask_(ui, title, msg) {
  const r = ui.prompt(title, msg, ui.ButtonSet.OK_CANCEL);
  return r.getSelectedButton() === ui.Button.OK ? r.getResponseText().trim() : null;
}

function esc_(s) {
  return String(s == null ? '' : s).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
}

function log_(type, detail) {
  try { hub_().getSheetByName(TABS.LOG).appendRow([new Date(), type, String(detail).slice(0, 500)]); } catch (e) { /* ignore */ }
}

function json_(obj) {
  return ContentService.createTextOutput(JSON.stringify(obj)).setMimeType(ContentService.MimeType.JSON);
}
