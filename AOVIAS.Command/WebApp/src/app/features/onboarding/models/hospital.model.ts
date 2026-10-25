export interface Hospital {
  name: string;
  code: string;
  address: string;
  nabhLevel: string;
  countryCode: string;
  stateCode: string;
  cityName: string;
  /** Selected framework `code` values from onboarding wizard (`FrameworkLookup.code`). */
  frameworkCodes?: string[];
}
