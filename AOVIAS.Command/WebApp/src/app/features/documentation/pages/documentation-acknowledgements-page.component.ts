import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { catchError, finalize, of } from 'rxjs';
import { DocumentationApiService } from '../services/documentation-api.service';
import type { DocumentAcknowledgementListItem } from '../models/documentation.model';

@Component({
  selector: 'app-documentation-acknowledgements-page',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/documentation-workspace.layout.scss',
  template: `
    <div class="qdms-workspace">
      <header class="page-header">
        <div>
          <h1>Read &amp; Acknowledge</h1>
          <p class="subtitle">Track read, acknowledged, and pending completion by department, role, and user</p>
        </div>
      </header>

      @if (error()) { <div class="banner error">{{ error() }}</div> }
      @if (actionMessage()) { <div class="banner success">{{ actionMessage() }}</div> }
      @if (loading()) { <p class="muted">Loading acknowledgements…</p> }
      @else {
        <section class="panel">
          <table class="data-table">
            <thead>
              <tr>
                <th>Document</th>
                <th>Title</th>
                <th>Assignee</th>
                <th>Type</th>
                <th>Read</th>
                <th>Acknowledged</th>
                <th>Reminders</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (a of items(); track a.id) {
                <tr>
                  <td><strong>{{ a.documentNumber }}</strong></td>
                  <td>{{ a.title }}</td>
                  <td>{{ a.assigneeName }}</td>
                  <td>{{ a.assigneeType }}</td>
                  <td>{{ a.readAt ? (a.readAt | date: 'short') : 'Pending' }}</td>
                  <td>{{ a.acknowledgedAt ? (a.acknowledgedAt | date: 'short') : 'Pending' }}</td>
                  <td>{{ a.reminderCount }}</td>
                  <td>
                    @if (!a.acknowledgedAt) {
                      <button
                        type="button"
                        class="btn-primary"
                        [disabled]="ackingId() === a.id"
                        (click)="acknowledge(a)">
                        {{ ackingId() === a.id ? 'Saving…' : 'Ack' }}
                      </button>
                    }
                  </td>
                </tr>
              } @empty {
                <tr><td colspan="8" class="muted">No acknowledgement assignments yet.</td></tr>
              }
            </tbody>
          </table>
        </section>
      }
    </div>
  `,
})
export class DocumentationAcknowledgementsPageComponent implements OnInit {
  private readonly api = inject(DocumentationApiService);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly actionMessage = signal<string | null>(null);
  readonly items = signal<DocumentAcknowledgementListItem[]>([]);
  readonly ackingId = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  acknowledge(item: DocumentAcknowledgementListItem): void {
    this.ackingId.set(item.id);
    this.actionMessage.set(null);
    this.error.set(null);
    this.api.acknowledge({ acknowledgementId: item.id }).pipe(
      catchError(() => {
        this.error.set('Unable to acknowledge document.');
        return of(null);
      }),
      finalize(() => this.ackingId.set(null)),
    ).subscribe((updated) => {
      if (!updated) return;
      this.items.update((list) => list.map((a) => (a.id === updated.id ? updated : a)));
      this.actionMessage.set(`Acknowledged ${updated.documentNumber}.`);
    });
  }

  private load(): void {
    this.loading.set(true);
    this.api.getAcknowledgements().pipe(
      catchError(() => { this.error.set('Unable to load acknowledgements.'); return of([]); }),
      finalize(() => this.loading.set(false)),
    ).subscribe((list) => this.items.set(list));
  }
}
