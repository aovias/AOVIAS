import hostingerSettingsJson from '@/config/email/hostinger.settings.json';

export type HostingerEmailSettings = typeof hostingerSettingsJson;

/**
 * Non-secret Hostinger Mail defaults (host, ports, retry policy).
 * Credentials always come from process.env — never from this JSON.
 */
export function getHostingerEmailSettings(): HostingerEmailSettings {
  return hostingerSettingsJson;
}
