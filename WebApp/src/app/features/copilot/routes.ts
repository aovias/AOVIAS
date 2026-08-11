import { Routes } from '@angular/router';

export const COPILOT_ROUTES: Routes = [
  {
    path: '',
    title: 'Compliance Copilot',
    data: {
      navSection: 'dashboard',
      description: 'Persona-driven compliance copilot workspace',
    },
    loadComponent: () =>
      import('./pages/copilot-workspace/copilot-workspace.component').then(
        (m) => m.CopilotWorkspaceComponent,
      ),
  },
];
