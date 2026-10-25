import type { AdapterMode } from './adapter';

/**
 * Optional diagnostics every domain client may expose.
 */
export type SdkClientInfo = {
  name: string;
  mode: AdapterMode;
  version: string;
};

export type WithSdkInfo = {
  getClientInfo?: () => SdkClientInfo;
};
