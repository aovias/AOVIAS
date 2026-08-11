import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideNativeDateAdapter } from '@angular/material/core';

import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { apiResponseInterceptor } from './core/interceptors/api-response.interceptor';
import { journeyLongReadInterceptor } from './core/interceptors/journey-long-read.interceptor';
import { loadingInterceptor } from './core/interceptors/loading.interceptor';
import { httpTimingInterceptor } from './core/interceptors/http-timing.interceptor';
import { DEFAULT_GEO_CONFIG, GEO_CONFIG } from './core/config/geo.config';
import { provideNavigationFramework } from './core/navigation/engine/provide-navigation';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideNativeDateAdapter(),
    provideRouter(routes),
    provideNavigationFramework(),
    {
      provide: GEO_CONFIG,
      useValue: DEFAULT_GEO_CONFIG,
    },
    // HTTP: timing → loading → auth → journeyLongRead → apiResponse unwrap.
    provideHttpClient(
      withInterceptors([
        httpTimingInterceptor,
        loadingInterceptor,
        authInterceptor,
        journeyLongReadInterceptor,
        apiResponseInterceptor,
      ]),
    ),
  ],
};
