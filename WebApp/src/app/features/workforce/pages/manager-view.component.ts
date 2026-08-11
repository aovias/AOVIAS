import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { EmploymentApiService, EmploymentListItem } from '../services/employment-api.service';

@Component({
  selector: 'app-manager-view',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <section class="manager-view">
      <a routerLink="/app/workforce">← Directory</a>
      <h1>Manager Team View</h1>
      <label>Manager employment ID <input [(ngModel)]="managerEmploymentId" /></label>
      <button type="button" (click)="load()">Load team</button>
      <ul>
        @for (m of team(); track m.id) {
          <li><a [routerLink]="['/app/workforce', m.id]">{{ m.personSearchName }}</a> ({{ m.employmentNumber }})</li>
        }
      </ul>
    </section>
  `,
  styles: [`.manager-view { padding:1.5rem; }`],
})
export class ManagerViewComponent {
  private readonly api = inject(EmploymentApiService);
  managerEmploymentId = '';
  team = signal<EmploymentListItem[]>([]);

  load(): void {
    if (!this.managerEmploymentId) return;
    this.api.getManagerTeam(this.managerEmploymentId).subscribe({ next: (t) => this.team.set(t) });
  }
}
