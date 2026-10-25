import { Directive, Input, TemplateRef, ViewContainerRef, inject, OnInit, OnDestroy } from '@angular/core';
import { AuthorizationService } from './authorization.service';
import { Subscription } from 'rxjs';

@Directive({ selector: '[hasPermission]', standalone: true })
export class HasPermissionDirective implements OnInit, OnDestroy {
  private templateRef = inject(TemplateRef<unknown>);
  private vcr = inject(ViewContainerRef);
  private auth = inject(AuthorizationService);
  private sub?: Subscription;

  @Input({ required: true }) hasPermission!: string;

  ngOnInit(): void {
    this.sub = this.auth.can(this.hasPermission).subscribe((allowed) => {
      this.vcr.clear();
      if (allowed) this.vcr.createEmbeddedView(this.templateRef);
    });
  }

  ngOnDestroy(): void { this.sub?.unsubscribe(); }
}
