import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { GovernanceApiService, ApprovalChainItem } from '../services/governance-api.service';

@Component({
  selector: 'app-governance-approval-matrix',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <section>
      <header><h1>Approval Matrix</h1><a routerLink="/app/governance">Back</a></header>
      <form (ngSubmit)="create()">
        <input [(ngModel)]="code" name="code" placeholder="Chain code" required />
        <input [(ngModel)]="name" name="name" placeholder="Name" required />
        <button type="submit">Create chain</button>
      </form>
      <form (ngSubmit)="evaluate()">
        <input [(ngModel)]="evaluateChainId" name="evaluateChainId" placeholder="Chain ID to evaluate" />
        <input [(ngModel)]="approvedSteps" name="approvedSteps" placeholder="Approved steps (e.g. 1)" />
        <button type="submit">Evaluate</button>
      </form>
      @if (evalResult()) { <p>{{ evalResult() }}</p> }
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
export class ApprovalMatrixComponent implements OnInit {
  private api = inject(GovernanceApiService);
  chains = signal<ApprovalChainItem[]>([]);
  code = '';
  name = '';
  evaluateChainId = '';
  approvedSteps = '';
  evalResult = signal('');

  ngOnInit(): void {
    this.api.listApprovalChains().subscribe((d) => this.chains.set(d));
  }

  create(): void {
    this.api.createApprovalChain({
      chainCode: this.code,
      chainName: this.name,
      approvalTypeValueId: crypto.randomUUID(),
      steps: [{ stepOrder: 1, mode: 'Sequential', assigneeType: 'Person', assigneeId: crypto.randomUUID() }],
    }).subscribe((c) => this.chains.update((list) => [...list, c]));
  }

  publish(id: string): void {
    this.api.publishApprovalChain(id).subscribe(() => this.ngOnInit());
  }

  evaluate(): void {
    this.api.evaluateApproval({ approvalChainId: this.evaluateChainId, approvedSteps: this.approvedSteps }).subscribe((r) =>
      this.evalResult.set(`${r.completed ? 'Complete' : 'Pending'} — ${r.reason}`));
  }
}
