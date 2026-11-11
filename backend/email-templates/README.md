# MADA Lessons email templates

Branded, email-safe HTML for every MADA Lessons email. The look matches the lesson's light theme: light paper, a white card, the field path's blue→purple gradient as the only accent, and yellow checkpoint marks. The design source is the "MADA Lessons Email Templates" canvas on Claude.

## Files

| Path | What it is |
| --- | --- |
| `backend/results-hub/EmailKit.gs` | The kit: shell, blocks and the four templates. Paste into the Apps Script project as a second file named **EmailKit**. |
| `docs/assets/email/*.png` | Hosted images (2x): field hero, path-mark signature, checkpoint bullet. Served at `mada.mprstudios.com/assets/email/`. |
| `tools/email/make-email-images.js` | Regenerates those PNGs. |
| `backend/email-templates/render-previews.js` | Renders every template with sample data into `previews/`. |
| `backend/email-templates/previews/` | Preview `.html` (open in a browser) and `.txt` (the plain-text version). |

## The templates

| Template | Function | Use for |
| --- | --- | --- |
| 1 · Personal letter | `EK.letter()`, preset `EK.pilotInvite({ firstName, variant: 'known' \| 'cold' })` | Pilot invites, follow-ups, check-ins. Reads like a note from Miguel. |
| 2 · Announcement | `EK.announcement()`, preset `EK.pilotLaunch({ coachName, dashboardUrl })` | Pilot launch, new levels, event invites. |
| 3 · Welcome / setup | `EK.welcome({ coachName, code, program, section, access, students, sheetUrl, coachEmail, dashboardUrl, previewUrl })` | Class approved. `access: 'email'` lists join links; `'google'` points to the class sheet. |
| 4 · Weekly report | `EK.report({ code, coachName, dateLabel, stats, levels, students, weakSkills, tip, dashboardUrl, attachments })` | Monday coach report. |

Every function returns `{ subject, html, text }`. Send both versions:

```js
const m = EK.welcome({ ... });
MailApp.sendEmail({ to, subject: m.subject, body: m.text, htmlBody: m.html, name: CONFIG.SENDER_NAME, replyTo: CONFIG.ADMIN_EMAIL });
```

The function signatures above list each template's data. `EK.samples()` has a filled-in example of every one.

## Before the first real send

1. **Mailing address.** Set `ADDRESS` at the top of `EmailKit.gs`. Commercial email, including pilot invites, needs a physical address (a PO box works) and a way to opt out.
2. **Push the images.** Merge and push so `docs/assets/email/` is live, then check `https://mada.mprstudios.com/assets/email/field-hero-v1.png` loads.
3. **Test in real inboxes.** In the Apps Script editor, run `sendEmailTemplateTests`. It sends all six samples to contact.mprstudios@gmail.com. Check them in Gmail (web and phone), iPhone Mail and Outlook if you can.

## Rules the kit follows (keep them when editing)

- Table layout, inline styles, a 600px card, `role="presentation"` on layout tables. No flexbox or grid in emails.
- Every gradient sits on a solid fallback (`bgcolor` plus `background`). Outlook shows the solid blue.
- Fonts: Barlow Condensed and Atkinson Hyperlegible load in Apple Mail and iOS. Gmail and Outlook show Arial Narrow and Arial.
- Only the field and path pictures are images. Text, buttons, tables and bars are live HTML, so they still read with images off.
- Every value from a sheet or form goes through `EK.esc()`.
- One gradient button per email; any others are outline buttons.
- Each email stays well under Gmail's 102 KB clipping limit (the largest is about 22 KB).

## Changing things

- **Copy:** edit the preset (`pilotInvite`, `pilotLaunch`) or the template text in `EmailKit.gs`, then run `node backend/email-templates/render-previews.js` and open the previews.
- **An image:** edit `tools/email/make-email-images.js`, bump the file name (`-v2.png`) so inbox caches refresh, update `S.IMG` in the kit, then run `node tools/email/make-email-images.js`.
- **Phase 3 (next):** switch `sendWelcome_` and `sendEmailReports` in `Code.gs` to the kit, and add an Outreach tab to send pilot invites from the hub.
