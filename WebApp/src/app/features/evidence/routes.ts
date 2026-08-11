import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/evidence.component').then((m) => m.EvidenceComponent),
  },
  {
    path: 'review-queue',
    loadComponent: () =>
      import('./pages/evidence-review-queue.component').then((m) => m.EvidenceReviewQueueComponent),
  },
  {
    path: 'binder',
    loadComponent: () => import('./binder/evidence-binder-page.component').then((m) => m.EvidenceBinderPageComponent),
  },
];
