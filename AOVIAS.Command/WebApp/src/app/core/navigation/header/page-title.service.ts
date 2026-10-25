import { Injectable, computed, inject } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map, startWith } from 'rxjs';
import { NavigationQueryService } from '../engine/navigation-query.service';
import { normalizeNavPath } from '../registry/route-match.util';

@Injectable({ providedIn: 'root' })
export class PageTitleService {
  private readonly router = inject(Router);
  private readonly query = inject(NavigationQueryService);

  private readonly url = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map(() => this.router.url),
      startWith(this.router.url),
    ),
    { initialValue: this.router.url },
  );

  readonly title = computed(() => this.resolveTitle(this.url() ?? this.router.url));

  resolveTitle(url: string): string {
    const path = normalizeNavPath(url);
    let node = this.router.routerState.snapshot.root;
    let dataTitle: string | undefined;
    while (node) {
      const t = node.data?.['title'];
      if (typeof t === 'string' && t.trim()) dataTitle = t;
      if (!node.firstChild) break;
      node = node.firstChild;
    }
    if (dataTitle) return dataTitle;

    const fromRegistry = this.query.titleForUrl(path);
    if (fromRegistry) return fromRegistry;

    const last = path.split('/').filter(Boolean).pop() ?? 'AOVIAS';
    return last.replace(/[-_]/g, ' ').replace(/\b\w/g, (c) => c.toUpperCase());
  }
}
