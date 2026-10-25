import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { GovernanceApiService, LookupValue } from '../services/governance-api.service';

@Component({
  selector: 'app-committee-membership',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <section>
      <header><h1>Committee Membership</h1><a [routerLink]="['/app/governance/committees']">Back</a></header>
      <form (ngSubmit)="addMember()" class="form-grid">
        <input [(ngModel)]="memberId" name="memberId" placeholder="Member assignee ID (person/employment)" required />
        <label>
          Committee role
          <select [(ngModel)]="roleId" name="roleId" required>
            <option value="" disabled>Select role</option>
            @for (r of roles(); track r.id) {
              <option [value]="r.id">{{ r.name }}</option>
            }
          </select>
        </label>
        <button type="submit">Add member</button>
      </form>
      @if (error()) { <p class="err">{{ error() }}</p> }
      @if (ok()) { <p class="ok">Member added.</p> }
      <p>Committee: {{ committeeId() }}</p>
    </section>
  `,
  styles: [`.form-grid { display: grid; gap: 0.5rem; max-width: 28rem; } .err { color: #b42318; } .ok { color: #067647; }`],
})
export class CommitteeMembershipComponent implements OnInit {
  private api = inject(GovernanceApiService);
  private route = inject(ActivatedRoute);
  committeeId = signal('');
  roles = signal<LookupValue[]>([]);
  error = signal('');
  ok = signal(false);
  memberId = '';
  roleId = '';

  ngOnInit(): void {
    this.route.paramMap.subscribe((p) => this.committeeId.set(p.get('id') ?? ''));
    this.api.getLookups('CommitteeRole').subscribe({ next: (d) => this.roles.set(d) });
  }

  addMember(): void {
    const id = this.committeeId();
    if (!id) return;
    this.error.set('');
    this.ok.set(false);
    this.api.addCommitteeMember(id, {
      memberAssigneeId: this.memberId,
      committeeRoleValueId: this.roleId,
      termStart: new Date().toISOString(),
      votingRights: true,
    }).subscribe({
      next: () => this.ok.set(true),
      error: () => this.error.set('Add member failed'),
    });
  }
}
