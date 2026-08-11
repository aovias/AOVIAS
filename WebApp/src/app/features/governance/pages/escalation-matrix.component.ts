import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { GovernanceApiService, ApprovalChainItem } from '../services/governance-api.service';

@Component({
  selector: 'app-escalation-matrix',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <section>
      <header><h1>Escalation Matrix</h1><a routerLink="/app/governance">Back</a></header>
      <form (ngSubmit)="create()">
        <input [(ngModel)]="code" name="code" placeholder="Chain code" required />
        <input [(ngModel)]="name" name="name" placeholder="Name" required />
        <button type="submit">Create chain</button>
      </form>
      <form (ngSubmit)="resolve()">
        <input [(ngModel)]="resolveChainId" name="resolveChainId" placeholder="Chain ID" />
        <button type="submit">Resolve (level 0)</button>
      </form>
      @if (resolveResult()) { <p>{{ resolveResult() }}</p> }
      <table><thead><tr><th>Code</th><th>Name</th><th>Status</th><th></th></tr></thead>
        <tbody>@for (c of chains(); track c.id) {
          <tr>
            <td>{{ c.chainCode }}</td><td>{{ c.chainName }}</td><td>{{ c.status }}</td>
            <td><button type="button" (click)="publish(c.id)">Publish</button></td>
          </tr>
        }</tbody>
      </table>
    </section>
  `,
})
export class EscalationMatrixComponent implements OnInit {
  private api = inject(GovernanceApiService);
  chains = signal<ApprovalChainItem[]>([]);
  code = '';
  name = '';
  resolveChainId = '';
  resolveResult = signal('');

  ngOnInit(): void {
    this.api.listEscalationChains().subscribe((d) => this.chains.set(d));
  }

  create(): void {
    this.api.createEscalationChain({
      chainCode: this.code,
      chainName: this.name,
      escalationTypeValueId: crypto.randomUUID(),
      levels: [{ levelOrder: 1, targetAssigneeType: 'User', targetAssigneeId: crypto.randomUUID(), delayHours: 0 }],
    }).subscribe((c) => this.chains.update((list) => [...list, c]));
  }

  publish(id: string): void {
    this.api.publishEscalationChain(id).subscribe(() => this.ngOnInit());
  }

  resolve(): void {
    this.api.resolveEscalation({ escalationChainId: this.resolveChainId, currentLevel: 0, hoursSinceOpen: 0 }).subscribe((r: { reason?: string }) =>
      this.resolveResult.set(r.reason ?? 'Done'));
  }
}
