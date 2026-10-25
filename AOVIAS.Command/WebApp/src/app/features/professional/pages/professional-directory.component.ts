import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ProfessionalApiService, ProfessionalListItem } from '../services/professional-api.service';

@Component({
  selector: 'app-professional-directory',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <section class="professional-directory">
      <header>
        <h1>Professional Directory</h1>
        <a routerLink="renewals">Renewal Dashboard</a>
      </header>
      <div class="toolbar">
        <input type="search" placeholder="Search PRO number, registration, name..." [(ngModel)]="searchTerm" (keyup.enter)="load()" />
        <button type="button" (click)="load()">Search</button>
      </div>
      @if (loading()) { <p>Loading...</p> }
      @else {
        <table>
          <thead><tr><th>PRO #</th><th>Name</th><th>Employment</th></tr></thead>
          <tbody>
            @for (p of items(); track p.id) {
              <tr>
                <td>{{ p.professionalNumber }}</td>
                <td><a [routerLink]="['/app/professional', p.id]">{{ p.personSearchName }}</a></td>
                <td><a [routerLink]="['/app/workforce', p.employmentId]">View employment</a></td>
              </tr>
            }
          </tbody>
        </table>
        <p>{{ total() }} professional profile(s)</p>
      }
    </section>
  `,
  styles: [`.professional-directory { padding: 1.5rem; } header { display:flex; justify-content:space-between; } table { width:100%; border-collapse:collapse; } th,td { padding:.5rem; border-bottom:1px solid #e5e7eb; }`],
})
export class ProfessionalDirectoryComponent implements OnInit {
  private readonly api = inject(ProfessionalApiService);
  items = signal<ProfessionalListItem[]>([]);
  total = signal(0);
  loading = signal(false);
  searchTerm = '';

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    const req = this.searchTerm.trim() ? this.api.search(this.searchTerm.trim()) : this.api.list();
    req.subscribe({
      next: (page) => { this.items.set(page.items); this.total.set(page.totalCount); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }
}
