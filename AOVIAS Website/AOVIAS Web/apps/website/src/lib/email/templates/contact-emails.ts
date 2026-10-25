import { companyLegal } from '@/config/company-legal';
import type { ContactApiPayload } from '@/lib/contact/schema';

function escapeHtml(value: string): string {
  return value
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#39;');
}

function row(label: string, value: string): string {
  return `<tr>
    <td style="padding:6px 12px 6px 0;color:#5b6b7c;vertical-align:top;white-space:nowrap">${escapeHtml(label)}</td>
    <td style="padding:6px 0;vertical-align:top">${escapeHtml(value)}</td>
  </tr>`;
}

export type ContactMailMeta = {
  intent: string;
  timestamp: string;
  ip?: string;
  userAgent?: string;
};

export function buildInternalNotificationSubject(
  payload: ContactApiPayload,
): string {
  const company =
    payload.organization && payload.organization.length > 0
      ? payload.organization
      : 'Individual';
  return `New Contact Request — ${company} — ${payload.name}`;
}

/** Email 1 — internal AOVIAS inbox with complete contact details. */
export function buildInternalNotificationEmail(
  payload: ContactApiPayload,
  meta: ContactMailMeta,
): { subject: string; text: string; html: string } {
  const subject = buildInternalNotificationSubject(payload);
  const lines = [
    'New contact enquiry from the AOVIAS website.',
    '',
    `Intent: ${meta.intent}`,
    `Name: ${payload.name}`,
    `Company: ${payload.organization || '—'}`,
    `Email: ${payload.email}`,
    `Phone: ${payload.phone || '—'}`,
    `Subject: ${payload.subject}`,
    '',
    'Message:',
    payload.message,
    '',
    `Timestamp: ${meta.timestamp}`,
    `IP: ${meta.ip || '—'}`,
    `User-Agent: ${meta.userAgent || '—'}`,
    '',
    `Inbox: ${companyLegal.enquiryEmail}`,
  ];

  const text = lines.join('\n');
  const html = `
    <div style="font-family:IBM Plex Sans,Segoe UI,sans-serif;line-height:1.5;color:#0b1520">
      <h2 style="margin:0 0 12px">New contact enquiry</h2>
      <p style="margin:0 0 16px">Submitted via the AOVIAS website contact form.</p>
      <table style="border-collapse:collapse;width:100%;max-width:640px">
        <tbody>
          ${row('Intent', meta.intent)}
          ${row('Name', payload.name)}
          ${row('Company', payload.organization || '—')}
          ${row('Email', payload.email)}
          ${row('Phone', payload.phone || '—')}
          ${row('Subject', payload.subject)}
          ${row('Timestamp', meta.timestamp)}
          ${row('IP', meta.ip || '—')}
          ${row('User-Agent', meta.userAgent || '—')}
        </tbody>
      </table>
      <h3 style="margin:24px 0 8px">Message</h3>
      <pre style="white-space:pre-wrap;font-family:inherit;background:#f4f7fb;padding:12px;border-radius:8px">${escapeHtml(payload.message)}</pre>
    </div>
  `.trim();

  return { subject, text, html };
}

/** Email 2 — visitor confirmation (professional HTML). */
export function buildVisitorConfirmationEmail(
  payload: ContactApiPayload,
  meta: Pick<ContactMailMeta, 'intent' | 'timestamp'>,
): { subject: string; text: string; html: string } {
  const subject = `We received your enquiry — ${companyLegal.brandName}`;
  const phoneLine = companyLegal.phoneDisplay
    ? `Phone: ${companyLegal.phoneDisplay}`
    : '';

  const text = [
    `Hello ${payload.name},`,
    '',
    `Thank you for contacting ${companyLegal.brandName}. We have received your enquiry and our team will review it shortly.`,
    '',
    `Subject: ${payload.subject}`,
    `Submitted: ${meta.timestamp}`,
    '',
    'What happens next:',
    '1. Our team reviews your enquiry',
    '2. We schedule a discovery discussion when needed',
    '3. We share a demonstration or next steps',
    '',
    `Typical response: within one business day (Mon–Fri, 09:30–18:30 IST).`,
    '',
    `If you need to add details, reply to this email or write to ${companyLegal.enquiryEmail}.`,
    phoneLine,
    '',
    `— ${companyLegal.brandName}`,
    companyLegal.website,
  ]
    .filter(Boolean)
    .join('\n');

  const html = `
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>${escapeHtml(subject)}</title>
</head>
<body style="margin:0;padding:0;background:#0b1520;font-family:IBM Plex Sans,Segoe UI,Helvetica,Arial,sans-serif;color:#0b1520">
  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#0b1520;padding:32px 16px">
    <tr>
      <td align="center">
        <table role="presentation" width="100%" style="max-width:600px;background:#ffffff;border-radius:12px;overflow:hidden">
          <tr>
            <td style="background:#0f2744;padding:24px 28px">
              <div style="font-size:13px;letter-spacing:0.08em;text-transform:uppercase;color:#8eb6ff;font-weight:600">${escapeHtml(companyLegal.brandName)}</div>
              <div style="margin-top:8px;font-size:22px;line-height:1.3;color:#ffffff;font-weight:600">We received your enquiry</div>
            </td>
          </tr>
          <tr>
            <td style="padding:28px">
              <p style="margin:0 0 16px;font-size:16px;line-height:1.55">Hello ${escapeHtml(payload.name)},</p>
              <p style="margin:0 0 16px;font-size:15px;line-height:1.55;color:#334155">
                Thank you for contacting ${escapeHtml(companyLegal.brandName)}. Our institutional team has received your message and will review it carefully.
              </p>
              <table role="presentation" width="100%" style="border-collapse:collapse;margin:20px 0;background:#f4f7fb;border-radius:8px">
                <tr>
                  <td style="padding:14px 16px">
                    <div style="font-size:12px;color:#64748b;text-transform:uppercase;letter-spacing:0.06em;margin-bottom:4px">Subject</div>
                    <div style="font-size:15px;font-weight:600;color:#0b1520">${escapeHtml(payload.subject)}</div>
                  </td>
                </tr>
              </table>
              <p style="margin:0 0 8px;font-size:14px;font-weight:600;color:#0b1520">What happens next</p>
              <ol style="margin:0 0 20px;padding-left:20px;color:#334155;font-size:14px;line-height:1.6">
                <li>Team review of your enquiry</li>
                <li>Discovery discussion when needed</li>
                <li>Product demonstration or next steps</li>
              </ol>
              <p style="margin:0 0 20px;font-size:14px;line-height:1.55;color:#334155">
                Typical response: <strong>within one business day</strong> (Monday–Friday, 09:30–18:30 IST).
              </p>
              <p style="margin:0;font-size:13px;line-height:1.55;color:#64748b">
                Need to add context? Reply to this email or write to
                <a href="mailto:${escapeHtml(companyLegal.enquiryEmail)}" style="color:#2563eb;text-decoration:none">${escapeHtml(companyLegal.enquiryEmail)}</a>${
                  companyLegal.phoneDisplay
                    ? ` · <a href="tel:${escapeHtml(companyLegal.phoneTel)}" style="color:#2563eb;text-decoration:none">${escapeHtml(companyLegal.phoneDisplay)}</a>`
                    : ''
                }.
              </p>
            </td>
          </tr>
          <tr>
            <td style="padding:16px 28px 24px;border-top:1px solid #e2e8f0;font-size:12px;color:#94a3b8;line-height:1.5">
              ${escapeHtml(companyLegal.legalName)} · ${escapeHtml(companyLegal.website.replace(/^https?:\/\//, ''))}
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>
  `.trim();

  return { subject, text, html };
}
