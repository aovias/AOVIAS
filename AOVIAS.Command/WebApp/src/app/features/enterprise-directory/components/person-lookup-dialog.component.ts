import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { DirectorySearchBarComponent } from './directory-search-bar.component';
import { EnterpriseDirectoryApiService, DirectorySearchHit } from '../services/enterprise-directory-api.service';
import { SessionService } from '../../../core/auth/session.service';

@Component({
  selector: 'app-person-lookup-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatListModule, DirectorySearchBarComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>Find person</h2>
    <mat-dialog-content>
      <app-directory-search-bar (search)="runSearch($event)" />
      <mat-nav-list>
        @for (item of results(); track item.personId) {
          <button mat-list-item type="button" (click)="select(item)">
            <span matListItemTitle>{{ item.displayName }}</span>
            <span matListItemLine>{{ item.departmentName }}</span>
          </button>
        }
      </mat-nav-list>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
    </mat-dialog-actions>
  `,
})
export class PersonLookupDialogComponent {
  private api = inject(EnterpriseDirectoryApiService);
  private session = inject(SessionService);
  private dialogRef = inject(MatDialogRef<PersonLookupDialogComponent>);
  results = signal<DirectorySearchHit[]>([]);

  runSearch(q: string): void {
    const orgId = this.session.getOrganizationId();
    if (!orgId) return;
    this.api.search(orgId, q, 1, 10).subscribe((r) => this.results.set(r.items));
  }

  select(item: DirectorySearchHit): void {
    this.dialogRef.close(item);
  }
}
