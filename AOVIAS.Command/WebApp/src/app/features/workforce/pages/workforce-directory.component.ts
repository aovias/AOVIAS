import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { EmploymentApiService, EmploymentListItem } from '../services/employment-api.service';

@Component({
  selector: 'app-workforce-directory',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <section class="workforce-directory">
      <header>
        <h1>Workforce Directory</h1>
        <nav>
          <a routerLink="org-chart">Org Chart</a>
          <a routerLink="manager">Manager View</a>
        </nav>
      </header>
      <div class="toolbar">
        <input type="search" placeholder="Search employment number or name..." [(ngModel)]="searchTerm" (keyup.enter)="load()" />
        <button type="button" (click)="load()">Search</button>
      </div>
      @if (loading()) { <p>Loading...</p> }
      @else {
        <table>
          <thead><tr><th>EMP #</th><th>Name</th><th>Position</th><th>Joined</th></tr></thead>
          <tbody>
            @for (e of items(); track e.id) {
              <tr>
                <td>{{ e.employmentNumber }}</td>
                <td><a [routerLink]="['/app/workforce', e.id]">{{ e.personSearchName }}</a></td>
                <td>{{ e.primaryPositionName || '—' }}</td>
                <td>{{ e.joiningDate | date:'mediumDate' }}</td>
              </tr>
            }
          </tbody>
        </table>
        <p class="meta">{{ total() }} employment record(s)</p>
      }
    </section>
  `,
  styles: [`.workforce-directory { padding: 1.5rem; } header { display:flex; justify-content:space-between; } table { width:100%; border-collapse:collapse; } th,td { padding:.5rem; border-bottom:1px solid #e5e7eb; }`],
})
export class WorkforceDirectoryComponent implements OnInit {
  private readonly api = inject(EmploymentApiService);
  items = signal<EmploymentListItem[]>([]);
  total = signal(0);
  loading = signal(false);
  searchTerm = '';

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    const req = this.searchTerm.trim() ? this.api.search(this.searchTerm.trim()) : this.api.list();
    req.subscribe({
      next: (p) => { this.items.set(p.items); this.total.set(p.totalCount); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }
}
