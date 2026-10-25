import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CommunicationApiService, ReminderItem } from '../services/communication-api.service';

@Component({
  selector: 'app-reminder-manager',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section>
      <header><h1>Reminder Manager</h1><a routerLink="..">← Back</a></header>
      <ul>
        @for (r of reminders(); track r.id) {
          <li>{{ r.name }} · {{ r.cronExpression }} · {{ r.templateCode }} · {{ r.isActive ? 'Active' : 'Inactive' }}</li>
        }
      </ul>
    </section>
  `,
})
export class ReminderManagerComponent implements OnInit {
  private api = inject(CommunicationApiService);
  reminders = signal<ReminderItem[]>([]);

  ngOnInit(): void {
    this.api.listReminders().subscribe({ next: (d) => this.reminders.set(d ?? []), error: () => {} });
  }
}
