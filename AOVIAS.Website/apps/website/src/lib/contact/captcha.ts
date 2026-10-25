/**
 * CAPTCHA verification seam — no-op until a provider is configured.
 * Keeps contact API free of provider coupling.
 */

export type CaptchaVerificationInput = {
  token?: string;
  remoteIp?: string;
};

export type CaptchaVerificationResult = {
  ok: boolean;
  skipped?: boolean;
  error?: string;
};

export async function verifyCaptcha(
  input: CaptchaVerificationInput,
): Promise<CaptchaVerificationResult> {
  const secret = process.env['CAPTCHA_SECRET_KEY'];
  if (!secret) {
    return { ok: true, skipped: true };
  }

  if (!input.token) {
    return { ok: false, error: 'CAPTCHA token missing' };
  }

  // Provider wiring (Turnstile / hCaptcha / reCAPTCHA) lands here later.
  void input.remoteIp;
  void secret;
  return { ok: true, skipped: true };
}
