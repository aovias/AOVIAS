import type { Page } from '@playwright/test';
import { APP_BASE_URL } from '../environment/service-registry.js';

export interface SessionSnapshot {
  readonly hasAccessToken: boolean;
  readonly hasRefreshToken: boolean;
  readonly accessTokenExpiresInSeconds: number | null;
  readonly url: string;
}

export interface Credentials {
  readonly email: string;
  readonly password: string;
}

/**
 * Everything a hospital user does to get into the product, expressed as operations rather than
 * clicks. Scenarios never touch selectors; when the login page changes, only this file changes.
 *
 * Selectors are data-testid only — see TESTID-CONTRACT.md. RC2 produced a false defect report
 * ("login submit is broken") that was actually stale click coordinates against a re-rendered form,
 * so this operator uses Playwright locators that re-resolve on every action rather than positions.
 */
export class AuthOperator {
  constructor(private readonly page: Page) {}

  async gotoLogin(): Promise<void> {
    await this.page.goto(`${APP_BASE_URL}/login`, { waitUntil: 'domcontentloaded' });
    await this.page.getByTestId('login-email').waitFor({ state: 'visible' });
  }

  /**
   * Signs in and waits for the application to leave the login route.
   *
   * `fill()` is used deliberately: RC2 established that synthetic keystrokes did not reach these
   * inputs, leaving the Angular form pristine and the submit button disabled while appearing typed.
   */
  async login(credentials: Credentials): Promise<void> {
    await this.gotoLogin();
    await this.page.getByTestId('login-email').fill(credentials.email);
    await this.page.getByTestId('login-password').fill(credentials.password);

    const submit = this.page.getByTestId('login-submit');
    await submit.waitFor({ state: 'visible' });
    await submit.click();

    await this.page.waitForURL(url => !url.pathname.startsWith('/login'), { timeout: 45_000 });
  }

  async logout(): Promise<void> {
    await this.page.evaluate(() => {
      localStorage.removeItem('access_token');
      localStorage.removeItem('refresh_token');
    });
    await this.page.goto(`${APP_BASE_URL}/login`, { waitUntil: 'domcontentloaded' });
  }

  /**
   * Reads session state from the application's own storage.
   *
   * Read-only by design. RC2 destroyed the evidence for an authentication defect by redeeming a
   * refresh token with a diagnostic fetch; this operator observes tokens and never spends them.
   */
  async session(): Promise<SessionSnapshot> {
    return this.page.evaluate(() => {
      const access = localStorage.getItem('access_token');
      const refresh = localStorage.getItem('refresh_token');

      let expiresIn: number | null = null;
      if (access) {
        try {
          const payload = JSON.parse(atob(access.split('.')[1] ?? ''));
          if (typeof payload.exp === 'number') {
            expiresIn = payload.exp - Math.floor(Date.now() / 1000);
          }
        } catch {
          expiresIn = null;
        }
      }

      return {
        hasAccessToken: !!access,
        hasRefreshToken: !!refresh,
        accessTokenExpiresInSeconds: expiresIn,
        url: location.href,
      };
    });
  }

  async isLoggedIn(): Promise<boolean> {
    const s = await this.session();
    return s.hasAccessToken && !s.url.includes('/login');
  }

  /**
   * Watches for a token renewal while `action` runs.
   *
   * Exists to close RC2's longest-standing "not demonstrated": DEF-RC2S1-001's fix was unit-tested
   * but a successful runtime renewal was never observed.
   */
  async detectRefresh<T>(action: () => Promise<T>): Promise<{ result: T; refreshed: boolean; status?: number }> {
    let refreshed = false;
    let status: number | undefined;

    const listener = (response: { url: () => string; status: () => number }) => {
      if (response.url().includes('/identity/refresh')) {
        refreshed = true;
        status = response.status();
      }
    };

    this.page.on('response', listener);
    try {
      const result = await action();
      return status === undefined ? { result, refreshed } : { result, refreshed, status };
    } finally {
      this.page.off('response', listener);
    }
  }
}
