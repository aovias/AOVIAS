import { HttpContextToken } from '@angular/common/http';

/**
 * When true, a 401 on this request must not trigger another token refresh (avoids refresh loops).
 * Set on the single retry after a successful refresh.
 */
export const SKIP_AUTH_REFRESH = new HttpContextToken<boolean>(() => false);
