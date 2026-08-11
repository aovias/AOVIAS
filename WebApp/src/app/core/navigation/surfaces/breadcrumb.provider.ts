import { Injectable, inject } from '@angular/core';
import type { PageBreadcrumbItem } from '../../../shared/ui/page-breadcrumb.component';
import { NavigationQueryService } from '../engine/navigation-query.service';

@Injectable({ providedIn: 'root' })
export class BreadcrumbProvider {
  private readonly query = inject(NavigationQueryService);

  forUrl(url: string): PageBreadcrumbItem[] {
    return this.query.breadcrumbForUrl(url).map((c) => ({
      label: c.title,
      route: c.route ?? null,
    }));
  }
}
