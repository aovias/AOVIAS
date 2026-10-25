import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { PersonApiService, PersonListItem } from '../services/person-api.service';

@Component({
  selector: 'app-person-directory',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <section class="person-directory">
      <header>
        <h1>Person Directory</h1>
        <a routerLink="merge" class="merge-link">Merge duplicates (preview)</a>
      </header>

      <div class="toolbar">
        <input
          type="search"
          placeholder="Search by name, email, phone, ID..."
          [(ngModel)]="searchTerm"
          (keyup.enter)="runSearch()"
        />
        <select [(ngModel)]="statusFilter" (change)="load()">
          <option value="">All statuses</option>
          <option value="Active">Active</option>
          <option value="Inactive">Inactive</option>
          <option value="Archived">Archived</option>
        </select>
        <button type="button" (click)="runSearch()">Search</button>
      </div>

      @if (loading()) {
        <p>Loading...</p>
      } @else if (error()) {
        <p class="error">{{ error() }}</p>
      } @else {
        <table>
          <thead>
            <tr>
              <th>Platform ID</th>
              <th>Name</th>
              <th>Email</th>
              <th>Phone</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            @for (person of persons(); track person.id) {
              <tr>
                <td>{{ person.platformId }}</td>
                <td>
                  <a [routerLink]="['/app/persons', person.id]">{{ person.displayName }}</a>
                </td>
                <td>{{ person.primaryEmail || '—' }}</td>
                <td>{{ person.primaryPhone || '—' }}</td>
                <td>{{ person.status }}</td>
              </tr>
            }
          </tbody>
        </table>
        <p class="meta">{{ totalCount() }} person(s)</p>
      }
    </section>
  `,
  styles: [
    `
      .person-directory { padding: 1.5rem; }
      header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; }
      .toolbar { display: flex; gap: 0.5rem; margin-bottom: 1rem; flex-wrap: wrap; }
      input, select { padding: 0.5rem; min-width: 12rem; }
      table { width: 100%; border-collapse: collapse; }
      th, td { text-align: left; padding: 0.5rem; border-bottom: 1px solid #e5e7eb; }
      .error { color: #b91c1c; }
      .meta { color: #6b7280; margin-top: 0.75rem; }
      .merge-link { font-size: 0.875rem; }
    `,
  ],
})
export class PersonDirectoryComponent implements OnInit {
  private readonly api = inject(PersonApiService);

  persons = signal<PersonListItem[]>([]);
  totalCount = signal(0);
  loading = signal(false);
  error = signal<string | null>(null);
  searchTerm = '';
  statusFilter = '';

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    const req = this.searchTerm.trim()
      ? this.api.search(this.searchTerm.trim())
      : this.api.list(1, 50, this.statusFilter || undefined);

    req.subscribe({
      next: (page) => {
        this.persons.set(page.items);
        this.totalCount.set(page.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Unable to load persons.');
        this.loading.set(false);
      },
    });
  }

  runSearch(): void {
    this.load();
  }
}
