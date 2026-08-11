import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';

const perfEnabled = typeof performance !== 'undefined';
if (perfEnabled) {
  performance.mark('app_bootstrap');
}

bootstrapApplication(App, appConfig)
  .then(() => {
    if (perfEnabled) {
      performance.mark('angular_init');
    }
  })
  .catch((err) => console.error(err));
