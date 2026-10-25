import { Component, inject, signal } from '@angular/core';

import { CommonModule } from '@angular/common';

import { FormsModule } from '@angular/forms';

import { RouterLink } from '@angular/router';

import { AccessGovernanceApiService, EffectivePermissionItem } from '../services/access-governance-api.service';



@Component({

  selector: 'app-effective-permissions',

  standalone: true,

  imports: [CommonModule, FormsModule, RouterLink],

  template: `

    <section>

      <header><h1>Effective Permissions</h1><a routerLink="/app/access-governance">Back</a></header>

      <label>User ID <input [(ngModel)]="userId" /></label>

      <button type="button" (click)="load()">Load</button>

      <table>

        <thead><tr><th>Permission</th><th>Sources</th></tr></thead>

        <tbody>

          @for (p of perms(); track p.code) {

            <tr><td>{{ p.code }}</td><td>{{ p.sources.join(', ') }}</td></tr>

          }

        </tbody>

      </table>

    </section>

  `,

})

export class EffectivePermissionsComponent {

  private api = inject(AccessGovernanceApiService);

  userId = '';

  perms = signal<EffectivePermissionItem[]>([]);

  load(): void {

    if (this.userId) this.api.effectivePermissions(this.userId).subscribe((d) => this.perms.set(d));

  }

}

