import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CommunicationApiService, TemplateItem } from '../services/communication-api.service';

@Component({
  selector: 'app-template-designer',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section>
      <header><h1>Template Designer</h1><a routerLink="..">← Back</a></header>
      <ul>
        @for (t of templates(); track t.id) {
          <li>{{ t.code }} — {{ t.name }} (v{{ t.currentVersion }}) · {{ t.channel }} · {{ t.status }}</li>
        }
      </ul>
    </section>
  `,
})
export class TemplateDesignerComponent implements OnInit {
  private api = inject(CommunicationApiService);
  templates = signal<TemplateItem[]>([]);

  ngOnInit(): void {
    this.api.listTemplates().subscribe({ next: (d) => this.templates.set(d ?? []), error: () => {} });
  }
}
