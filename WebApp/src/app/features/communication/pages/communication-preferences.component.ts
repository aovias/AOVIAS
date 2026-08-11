import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CommunicationApiService, PreferenceItem } from '../services/communication-api.service';

@Component({
  selector: 'app-communication-preferences',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section>
      <header><h1>Communication Preferences</h1><a routerLink="..">← Back</a></header>
      @if (prefs().length === 0) {
        <p>No preferences configured — defaults apply.</p>
      } @else {
        <ul>
          @for (p of prefs(); track p.id) {
            <li>{{ p.moduleCode ?? 'Global' }} · {{ p.channel }} · {{ p.frequency }} · {{ p.enabled ? 'Enabled' : 'Disabled' }}</li>
          }
        </ul>
      }
    </section>
  `,
})
export class CommunicationPreferencesComponent implements OnInit {
  private api = inject(CommunicationApiService);
  prefs = signal<PreferenceItem[]>([]);

  ngOnInit(): void {
    this.api.listPreferences().subscribe({ next: (d) => this.prefs.set(d ?? []), error: () => {} });
  }
}
