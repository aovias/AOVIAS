import { InjectionToken } from '@angular/core';

export interface GeoConfig {
  defaultCountryCode: string;
  enabledCountryCodes: string[];
  allowCountrySelection: boolean;
  cityAutocompleteMinChars: number;
  maxCityOptions: number;
}

export const DEFAULT_GEO_CONFIG: GeoConfig = {
  defaultCountryCode: 'IN',
  enabledCountryCodes: ['IN'],
  allowCountrySelection: false,
  cityAutocompleteMinChars: 2,
  maxCityOptions: 200,
};

export const GEO_CONFIG = new InjectionToken<GeoConfig>('GEO_CONFIG');
