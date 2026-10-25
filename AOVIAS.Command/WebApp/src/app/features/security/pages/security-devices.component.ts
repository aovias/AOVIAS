import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { SecurityApiService, TrustedDeviceItem } from '../services/security-api.service';
import { NotificationService } from '../../../core/ui/services/notification.service';

@Component({
  selector: 'app-security-devices',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section class="page">
      <h1><a routerLink="..">Security</a> / Trusted devices</h1>
      @for (d of devices(); track d.id) {
        <div class="row">
          <div>
            <strong>{{ d.platform || 'Unknown' }}</strong> — {{ d.browser || d.deviceId }}
            <div class="meta">Risk: {{ d.riskLevel }} · Trusted until {{ d.trustedUntil | date:'medium' }}</div>
          </div>
          <button type="button" (click)="revoke(d.id)">Revoke</button>
        </div>
      } @empty {
        <p>No trusted devices.</p>
      }
    </section>
  `,
  styles: [`.page { padding: 1.5rem; } .row { display: flex; justify-content: space-between; padding: 0.75rem 0; border-bottom: 1px solid #eee; } .meta { font-size: 0.85rem; color: #666; }`],
})
export class SecurityDevicesComponent implements OnInit {
  private api = inject(SecurityApiService);
  private notify = inject(NotificationService);
  devices = signal<TrustedDeviceItem[]>([]);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.api.getDevices().subscribe({ next: (items) => this.devices.set(items), error: () => this.notify.showError('Unable to load devices') });
  }

  revoke(id: string): void {
    this.api.revokeDevice(id).subscribe({
      next: () => { this.notify.showSuccess('Device revoked'); this.load(); },
      error: () => this.notify.showError('Revoke failed'),
    });
  }
}
