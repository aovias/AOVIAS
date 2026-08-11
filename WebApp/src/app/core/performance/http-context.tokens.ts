import { HttpContextToken } from '@angular/common/http';

/** When true, the global loading overlay is not shown for this request. */
export const SKIP_LOADING_OVERLAY = new HttpContextToken<boolean>(() => false);

/** When true, request timing is recorded but not classified as blocking first paint. */
export const BACKGROUND_REQUEST = new HttpContextToken<boolean>(() => false);

/** Classifies dashboard HTTP for performance reports. */
export type DashboardHttpCategory =
  | 'snapshot'
  | 'hospital-dashboard'
  | 'control-center'
  | 'organization'
  | 'experience'
  | 'other';

export const DASHBOARD_HTTP_CATEGORY = new HttpContextToken<DashboardHttpCategory>(() => 'other');
