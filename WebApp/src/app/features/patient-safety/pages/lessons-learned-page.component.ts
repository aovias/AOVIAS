import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AiPageContextService } from '../../../core/services/ai-page-context.service';
import { IncidentApiService } from '../services/incident-api.service';
import type { LessonLearned } from '../models/incident.models';

@Component({
  selector: 'app-lessons-learned-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/patient-safety-workspace.layout.scss',
  template: `
    <div class="ps-page ps-page--medium">
      <header class="page-header"><h1>Lessons Learned</h1></header>
      <p class="subtitle">Searchable organizational knowledge from closed incidents</p>
      <input type="search" placeholder="Search lessons…" [(ngModel)]="search" (ngModelChange)="reload()" class="search" />
      <div class="cards">
        @for (l of lessons(); track l.id) {
          <article class="card">
            <span class="num">{{ l.lessonNumber }}</span>
            <h3>{{ l.summary }}</h3>
            <p>{{ l.recommendation }}</p>
            @if (l.trainingNeeded) { <small>Training: {{ l.trainingNeeded }}</small> }
            @if (l.sourceIncidentNumber) { <small>From incident {{ l.sourceIncidentNumber }}</small> }
          </article>
        } @empty {
          <p class="empty">No lessons learned published yet.</p>
        }
      </div>
    </div>
  `,
})
export class LessonsLearnedPageComponent implements OnInit {
  private readonly api = inject(IncidentApiService);
  private readonly aiContext = inject(AiPageContextService);
  readonly lessons = signal<LessonLearned[]>([]);
  search = '';

  ngOnInit(): void {
    this.aiContext.setPageContext({ pageContext: 'PatientSafety' });
    this.reload();
  }

  reload(): void {
    this.api.getLessons(this.search || undefined).subscribe((l) => this.lessons.set(l));
  }
}
