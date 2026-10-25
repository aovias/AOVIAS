import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-legacy-admin-redirect',
  standalone: true,
  template: '',
})
export class LegacyAdminRedirectComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  ngOnInit(): void {
    const legacyPrefix = this.route.snapshot.data['legacyPrefix'] as string;
    const targetPrefix = this.route.snapshot.data['targetPrefix'] as string;
    const current = this.router.url.split(/[?#]/)[0];
    const next = current.startsWith(legacyPrefix)
      ? targetPrefix + current.slice(legacyPrefix.length)
      : targetPrefix;
    void this.router.navigateByUrl(next, { replaceUrl: true });
  }
}
