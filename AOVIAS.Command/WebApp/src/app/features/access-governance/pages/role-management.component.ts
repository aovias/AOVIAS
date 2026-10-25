import { Component, OnInit, inject, signal } from '@angular/core';

import { CommonModule } from '@angular/common';

import { FormsModule } from '@angular/forms';

import { RouterLink } from '@angular/router';

import { AccessGovernanceApiService, RoleAssignmentItem, RoleListItem } from '../services/access-governance-api.service';



@Component({

  selector: 'app-role-management',

  standalone: true,

  imports: [CommonModule, FormsModule, RouterLink],

  template: `

    <section>

      <header><h1>Role Management</h1>

        <nav>

          <a routerLink="permissions">Permissions</a> |

          <a routerLink="policies">Policies</a> |

          <a routerLink="delegations">Delegations</a> |

          <a routerLink="/app/governance/responsibilities">Responsibilities</a> |

          <a routerLink="simulator">Simulator</a>

        </nav>

      </header>

      @if (loading()) { <p>Loading roles...</p> }

      @else {

        <table><thead><tr><th>Code</th><th>Name</th></tr></thead>

          <tbody>@for (r of roles(); track r.id) { <tr><td>{{ r.roleCode }}</td><td>{{ r.roleName }}</td></tr> }</tbody>

        </table>

      }

      <h2>Role Assignments</h2>

      <div class="form">

        <label>User ID <input [(ngModel)]="filterUserId" /></label>

        <button type="button" (click)="loadAssignments()">Filter</button>

      </div>

      <table>

        <thead><tr><th>Role</th><th>User</th><th>Scope</th><th>Effective</th><th></th></tr></thead>

        <tbody>

          @for (a of assignments(); track a.id) {

            <tr>

              <td>{{ a.roleCode }}</td>

              <td>{{ a.userId }}</td>

              <td>{{ a.organizationId || 'platform' }}</td>

              <td>{{ a.effectiveFrom || '—' }} → {{ a.effectiveTo || 'open' }}</td>

              <td>@if (!a.isRevoked) { <button type="button" (click)="revoke(a.id)">Revoke</button> }</td>

            </tr>

          }

        </tbody>

      </table>

      <h3>Assign role</h3>

      <div class="form">

        <label>Role ID <input [(ngModel)]="assignRoleId" /></label>

        <label>User ID <input [(ngModel)]="assignUserId" /></label>

        <label>Org ID <input [(ngModel)]="assignOrgId" /></label>

        <button type="button" (click)="assign()">Assign</button>

      </div>

    </section>

  `,

})

export class RoleManagementComponent implements OnInit {

  private api = inject(AccessGovernanceApiService);

  roles = signal<RoleListItem[]>([]);

  assignments = signal<RoleAssignmentItem[]>([]);

  loading = signal(true);

  filterUserId = '';

  assignRoleId = '';

  assignUserId = '';

  assignOrgId = '';



  ngOnInit(): void {

    this.api.listRoles().subscribe({

      next: (p) => { this.roles.set(p.items); this.loading.set(false); },

      error: () => this.loading.set(false),

    });

    this.loadAssignments();

  }



  loadAssignments(): void {

    this.api.listRoleAssignments(this.filterUserId || undefined).subscribe((items) => this.assignments.set(items));

  }



  assign(): void {

    if (!this.assignRoleId || !this.assignUserId) return;

    this.api.assignRole({

      roleId: this.assignRoleId,

      userId: this.assignUserId,

      organizationId: this.assignOrgId || null,

    }).subscribe(() => this.loadAssignments());

  }



  revoke(id: string): void {

    this.api.revokeAssignment(id).subscribe(() => this.loadAssignments());

  }

}

