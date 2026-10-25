import { inject, Injectable } from '@angular/core';
import { City, Country, State } from 'country-state-city';
import { GEO_CONFIG } from '../../core/config/geo.config';
import { GeoCityOption, GeoCountryOption, GeoStateOption } from '../models/geo.models';

@Injectable({
  providedIn: 'root',
})
export class GeoSelectorService {
  private config = inject(GEO_CONFIG);

  private statesCache = new Map<string, GeoStateOption[]>();
  private citiesCache = new Map<string, GeoCityOption[]>();

  getActiveCountries(): GeoCountryOption[] {
    const enabled = new Set(this.config.enabledCountryCodes.map((c) => c.toUpperCase()));
    return Country.getAllCountries()
      .filter((c) => enabled.has(c.isoCode.toUpperCase()))
      .map((c) => ({ code: c.isoCode, name: c.name }))
      .sort((a, b) => a.name.localeCompare(b.name));
  }

  getStates(countryCode: string = this.config.defaultCountryCode): GeoStateOption[] {
    const cc = countryCode.toUpperCase();
    if (!this.isCountryEnabled(cc)) {
      return [];
    }

    const cached = this.statesCache.get(cc);
    if (cached) {
      return cached;
    }

    const states = State.getStatesOfCountry(cc)
      .map((s) => ({ code: s.isoCode, name: s.name }))
      .sort((a, b) => a.name.localeCompare(b.name));

    this.statesCache.set(cc, states);
    return states;
  }

  getCities(stateCode: string, countryCode: string = this.config.defaultCountryCode): GeoCityOption[] {
    const cc = countryCode.toUpperCase();
    if (!this.isCountryEnabled(cc)) {
      return [];
    }

    const sc = (stateCode || '').trim();
    if (!sc) {
      return [];
    }

    const cacheKey = `${cc}:${sc}`;
    const cached = this.citiesCache.get(cacheKey);
    if (cached) {
      return cached;
    }

    // `country-state-city` expects:
    // - countryCode: ISO2 (e.g. IN)
    // - stateCode: ISO2 state code within country (e.g. MH, KA)
    const cities = City.getCitiesOfState(cc, sc)
      .map((c) => ({ name: c.name }))
      .sort((a, b) => a.name.localeCompare(b.name));

    this.citiesCache.set(cacheKey, cities);
    return cities;
  }

  clearCache(): void {
    this.statesCache.clear();
    this.citiesCache.clear();
  }

  private isCountryEnabled(countryCode: string): boolean {
    return this.config.enabledCountryCodes.some((c) => c.toUpperCase() === countryCode.toUpperCase());
  }
}

