import { Routes } from '@angular/router';

export const ONBOARDING_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/ai-setup-assistant.component').then((m) => m.AiSetupAssistantComponent),
  },
];
