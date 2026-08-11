import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  EventEmitter,
  Input,
  OnInit,
  Output,
  inject,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter, take } from 'rxjs/operators';
import { MatDialog } from '@angular/material/dialog';
import { AuthService } from '../auth/auth.service';
import { SessionService } from '../auth/session.service';
import { TokenService } from '../auth/token.service';
import { RoleRegistryService } from '../auth/roles/role-registry.service';
import { OrganizationService } from '../services/organization.service';
import { ComplianceContextService } from '../services/compliance-context.service';
import { AppContextService } from '../services/app-context.service';
import {
  HospitalSelectorDialogComponent,
  HospitalSelectorDialogData,
  HospitalFrameworkSelection,
  FrameworkSelectorItem,
} from '../../shared/ui/hospital-selector/hospital-selector-dialog.component';
import { sortByCpFrameworkCode } from '../../shared/utils/framework-code-sort';

@Component({
  selector: 'app-user-profile-menu',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="profile-menu" role="menu" aria-label="User account menu">
      <div class="profile-menu__identity">
        <span class="profile-menu__avatar" aria-hidden="true">{{ userInitial() }}</span>
        <div class="profile-menu__meta">
          <p class="profile-menu__name">{{ userName() || 'User' }}</p>
          <p class="profile-menu__role">{{ roleLabel() }}</p>
          @if (organizationName()) {
            <p class="profile-menu__org">{{ organizationName() }}</p>
          }
        </div>
      </div>

      <hr class="profile-menu__divider" />

      <a
        routerLink="/app/settings/account"
        class="profile-menu__item"
        role="menuitem"
        (click)="closed.emit()"
      >
        My Account
      </a>
      <a
        routerLink="/app/settings/account"
        class="profile-menu__item"
        role="menuitem"
        (click)="closed.emit()"
      >
        Account &amp; Security
      </a>
      <a
        routerLink="/app/settings/theme"
        class="profile-menu__item"
        role="menuitem"
        (click)="closed.emit()"
      >
        Theme Preferences
      </a>

      <hr class="profile-menu__divider" />

      <a
        routerLink="/app/settings"
        class="profile-menu__item"
        role="menuitem"
        (click)="closed.emit()"
      >
        Administration
      </a>

      <hr class="profile-menu__divider" />

      <button
        type="button"
        class="profile-menu__item profile-menu__item--button"
        role="menuitem"
        data-testid="workspace-activate"
        (click)="onSwitchOrganization()"
      >
        Switch Organization
      </button>

      <hr class="profile-menu__divider" />

      <button
        type="button"
        class="profile-menu__item profile-menu__item--button profile-menu__item--logout"
        role="menuitem"
        (click)="onLogout()"
      >
        Logout
      </button>
    </div>
  `,
  styles: `
    :host {
      display: block;
    }

    .profile-menu {
      min-width: 280px;
      max-width: min(320px, calc(100vw - 24px));
      padding: 8px 0;
      background: var(--color-bg-surface, var(--card-bg, #fff));
      border: 1px solid var(--color-border-subtle, var(--border-color, #e5e7eb));
      border-radius: 12px;
      box-shadow: var(--shadow-lg, 0 12px 28px rgba(0, 0, 0, 0.18));
      color: var(--color-text-primary, var(--text-primary, #111827));
      overflow: hidden;
    }

    .profile-menu__identity {
      display: flex;
      align-items: flex-start;
      gap: 12px;
      padding: 12px 16px 14px;
    }

    .profile-menu__avatar {
      width: 40px;
      height: 40px;
      border-radius: 50%;
      background: linear-gradient(
        135deg,
        var(--color-primary, #2f6bff),
        var(--color-primary-hover, #2557d6)
      );
      color: var(--color-text-inverse, #fff);
      display: inline-flex;
      align-items: center;
      justify-content: center;
      font-size: 16px;
      font-weight: 600;
      flex-shrink: 0;
      text-transform: uppercase;
    }

    .profile-menu__meta {
      min-width: 0;
    }

    .profile-menu__name {
      margin: 0;
      font-size: 15px;
      font-weight: 600;
      color: var(--color-text-primary, var(--text-primary, #111827));
      line-height: 1.3;
    }

    .profile-menu__role {
      margin: 4px 0 0;
      font-size: 12px;
      font-weight: 500;
      color: var(--color-text-secondary, var(--text-secondary, #6b7280));
    }

    .profile-menu__org {
      margin: 4px 0 0;
      font-size: 12px;
      color: var(--color-text-muted, var(--text-secondary, #9ca3af));
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .profile-menu__divider {
      margin: 4px 0;
      border: none;
      border-top: 1px solid var(--color-border-subtle, var(--border-color, #e5e7eb));
    }

    .profile-menu__item {
      display: block;
      width: 100%;
      box-sizing: border-box;
      padding: 10px 16px;
      text-align: left;
      font-size: 14px;
      font-weight: 500;
      line-height: 1.3;
      color: var(--color-text-primary, var(--text-primary, #111827));
      text-decoration: none;
      background: transparent;
      border: none;
      cursor: pointer;
      transition:
        background 0.15s ease,
        color 0.15s ease;
    }

    .profile-menu__item:hover {
      background: var(--color-bg-hover, var(--hover-bg, #f3f4f6));
      color: var(--color-primary, var(--primary-color, #2f6bff));
    }

    .profile-menu__item:focus-visible {
      outline: 2px solid var(--color-primary, #2f6bff);
      outline-offset: -2px;
    }

    .profile-menu__item--button {
      font-family: inherit;
    }

    .profile-menu__item--logout {
      color: var(--color-text-secondary, #6b7280);
    }

    .profile-menu__item--logout:hover {
      color: var(--color-status-danger, #ef4444);
    }
  `,
})
export class UserProfileMenuComponent implements OnInit {
  @Input() closeOnNavigate = true;
  @Output() closed = new EventEmitter<void>();

  private readonly authService = inject(AuthService);
  private readonly sessionService = inject(SessionService);
  private readonly tokenService = inject(TokenService);
  private readonly organizationService = inject(OrganizationService);
  private readonly complianceContext = inject(ComplianceContextService);
  private readonly appContext = inject(AppContextService);
  private readonly roleRegistry = inject(RoleRegistryService);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  readonly userName = signal('');
  readonly roleLabel = signal('—');
  readonly organizationName = signal('');

  ngOnInit(): void {
    this.resolveIdentity();
  }

  userInitial(): string {
    const name = this.userName();
    return name ? name.charAt(0).toUpperCase() : '?';
  }

  onSwitchOrganization(): void {
    this.closed.emit();

    const organization = this.organizationService.getOrganization();
    const hospitals = this.organizationService.getHospitals();
    const organizationName = organization?.organizationName ?? 'Organization';

    const hospitalJourneys: Record<string, FrameworkSelectorItem[]> = {};
    for (const hospital of hospitals) {
      hospitalJourneys[hospital.id] = sortByCpFrameworkCode(
        (hospital.journey ?? []).map((journey) => ({
          id: journey.journeyId,
          name: journey.frameworkName,
          code: journey.frameworkCode,
        })),
      );
    }

    const dialogData: HospitalSelectorDialogData = {
      hospitals: hospitals.map((hospital) => ({
        id: hospital.id,
        name: hospital.name,
        organizationName,
        isOnboarded: hospital.isOnboarded,
      })),
      hospitalJourneys,
      selectedHospitalId: this.complianceContext.getHospitalId(),
      selectedJourneyId: this.complianceContext.getJourneyId(),
    };

    this.dialog
      .open(HospitalSelectorDialogComponent, {
        data: dialogData,
        panelClass: 'hospital-selector-panel',
        backdropClass: 'hospital-selector-backdrop',
        width: '520px',
        maxWidth: '90vw',
        maxHeight: '80vh',
        autoFocus: false,
      })
      .afterClosed()
      .pipe(
        filter(
          (result): result is HospitalFrameworkSelection =>
            !!result?.hospitalId && !!result?.journeyId,
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        const journeyId = result.journeyId;
        this.appContext.switchContext(
          result.hospitalId,
          journeyId,
          this.organizationService.getFrameworkIdForJourney(result.hospitalId, journeyId),
        );
      });
  }

  onLogout(): void {
    this.closed.emit();
    this.authService.logout().subscribe({
      next: () => {
        this.authService.logoutLocal();
        window.location.href = '/login';
      },
      error: () => {
        this.authService.logoutLocal();
        window.location.href = '/login';
      },
    });
  }

  private resolveIdentity(): void {
    const userId = this.sessionService.getUserId();
    const displayName = this.organizationService.getMemberDisplayName(userId);
    if (displayName) {
      this.userName.set(displayName);
    }

    const org = this.organizationService.getOrganization();
    if (org?.organizationName) {
      this.organizationName.set(org.organizationName);
    }

    const member = userId ? this.organizationService.getMemberByUserId(userId) : null;
    if (member?.role) {
      this.roleLabel.set(this.roleRegistry.labelOf(member.role));
    } else {
      const token = this.tokenService.getAccessToken();
      const decoded = token ? this.tokenService.decodeJwt(token) : null;
      const role =
        decoded?.role ?? decoded?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      this.roleLabel.set(this.roleRegistry.labelOf(typeof role === 'string' ? role : ''));
    }

    if (!displayName || !org) {
      this.organizationService.organization$
        .pipe(
          filter((loaded) => loaded !== null),
          take(1),
          takeUntilDestroyed(this.destroyRef),
        )
        .subscribe(() => {
          const name = this.organizationService.getMemberDisplayName(userId);
          if (name) this.userName.set(name);
          const m = userId ? this.organizationService.getMemberByUserId(userId) : null;
          if (m?.role) this.roleLabel.set(this.roleRegistry.labelOf(m.role));
          const loadedOrg = this.organizationService.getOrganization();
          if (loadedOrg?.organizationName) {
            this.organizationName.set(loadedOrg.organizationName);
          }
        });
    }

    if (!this.userName()) {
      const token = this.tokenService.getAccessToken();
      const decoded = token ? this.tokenService.decodeJwt(token) : null;
      const email = decoded?.email ?? decoded?.sub;
      if (typeof email === 'string') this.userName.set(email);
    }
  }
}
