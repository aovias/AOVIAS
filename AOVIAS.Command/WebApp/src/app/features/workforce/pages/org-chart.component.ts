import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { EmploymentApiService, OrgChartNode } from '../services/employment-api.service';

@Component({
  selector: 'app-org-chart',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <section class="org-chart">
      <a routerLink="/app/workforce">← Directory</a>
      <h1>Organization Chart</h1>
      <label>Organization ID <input [(ngModel)]="organizationId" /></label>
      <button type="button" (click)="load()">Load</button>
      @if (root(); as node) {
        <div class="tree">{{ render(node) }}</div>
      }
    </section>
  `,
  styles: [`.org-chart { padding:1.5rem; } .tree { margin-top:1rem; font-family:monospace; white-space:pre; }`],
})
export class OrgChartComponent implements OnInit {
  private readonly api = inject(EmploymentApiService);
  organizationId = '';
  root = signal<OrgChartNode | null>(null);

  ngOnInit(): void { if (this.organizationId) this.load(); }

  load(): void {
    if (!this.organizationId) return;
    this.api.getOrgChart(this.organizationId).subscribe({ next: (n) => this.root.set(n) });
  }

  render(node: OrgChartNode, depth = 0): string {
    const indent = '  '.repeat(depth);
    let text = `${indent}${node.personSearchName} (${node.employmentNumber})${node.positionName ? ' — ' + node.positionName : ''}\n`;
    for (const child of node.reports) text += this.render(child, depth + 1);
    return text;
  }
}
