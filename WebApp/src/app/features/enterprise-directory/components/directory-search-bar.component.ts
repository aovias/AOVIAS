import { ChangeDetectionStrategy, Component, EventEmitter, inject, input, Output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { debounceTime, distinctUntilChanged, Subject, switchMap } from 'rxjs';
import { EnterpriseDirectoryApiService, DirectorySuggestion } from '../services/enterprise-directory-api.service';
import { SessionService } from '../../../core/auth/session.service';

@Component({
  selector: 'app-directory-search-bar',
  standalone: true,
  imports: [CommonModule, FormsModule, MatFormFieldModule, MatInputModule, MatAutocompleteModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <mat-form-field appearance="outline" class="directory-search-bar w-full">
      <mat-label>Search people, skills, credentials…</mat-label>
      <input matInput [ngModel]="value()" (ngModelChange)="onInput($event)" [matAutocomplete]="auto" (keyup.enter)="emitSearch()" />
      <mat-autocomplete #auto="matAutocomplete" (optionSelected)="selectSuggestion($event.option.value)">
        @for (s of suggestions(); track s.text) {
          <mat-option [value]="s.text">
            <span>{{ s.text }}</span>
            @if (s.subtitle) { <small> — {{ s.subtitle }}</small> }
          </mat-option>
        }
      </mat-autocomplete>
    </mat-form-field>
  `,
  styles: [`.directory-search-bar { width: 100%; max-width: 640px; }`],
})
export class DirectorySearchBarComponent {
  private api = inject(EnterpriseDirectoryApiService);
  private session = inject(SessionService);
  private input$ = new Subject<string>();

  placeholder = input('Search…');
  @Output() search = new EventEmitter<string>();

  value = signal('');
  suggestions = signal<DirectorySuggestion[]>([]);

  constructor() {
    this.input$.pipe(
      debounceTime(250),
      distinctUntilChanged(),
      switchMap((prefix) => {
        const orgId = this.session.getOrganizationId();
        if (!orgId || prefix.length < 2) return [];
        return this.api.suggestions(prefix, orgId);
      }),
    ).subscribe((items) => this.suggestions.set(items));
  }

  onInput(v: string): void {
    this.value.set(v);
    this.input$.next(v);
  }

  selectSuggestion(text: string): void {
    this.value.set(text);
    this.emitSearch();
  }

  emitSearch(): void {
    const q = this.value().trim();
    if (q) this.search.emit(q);
  }
}
