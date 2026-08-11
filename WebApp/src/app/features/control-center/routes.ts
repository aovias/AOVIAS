import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/compliance-command-center.component').then(m => m.ComplianceCommandCenterComponent),
  },
  {
    path: 'compliance',
    loadComponent: () =>
      import('./pages/compliance-command-center.component').then(m => m.ComplianceCommandCenterComponent),
  },
  {
    path: 'tasks',
    loadComponent: () =>
      import('./pages/task-command-center.component').then(m => m.TaskCommandCenterComponent),
  },
  {
    path: 'knowledge-graph',
    loadComponent: () =>
      import('./pages/knowledge-graph.component').then(m => m.KnowledgeGraphComponent),
  },
];
