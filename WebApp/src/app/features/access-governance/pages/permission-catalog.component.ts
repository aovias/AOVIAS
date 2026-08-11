import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AccessGovernanceApiService, PermissionItem } from '../services/access-governance-api.service';

@Component({
  selector: 'app-permission-catalog',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section>
      <header><h1>Permission Catalog</h1><a routerLink="/app/access-governance">Back</a></header>
      <table><thead><tr><th>Code</th><th>Module</th><th>Feature</th><th>Action</th></tr></thead>
        <tbody>@for (p of items(); track p.id) {
          <tr><td>{{ p.code }}</td><td>{{ p.module }}</td><td>{{ p.feature }}</td><td>{{ p.action }}</td></tr>
        }</tbody>
      </table>
    </section>
  `,
})
export class PermissionCatalogComponent implements OnInit {
  private api = inject(AccessGovernanceApiService);
  items = signal<PermissionItem[]>([]);
  ngOnInit(): void { this.api.listPermissions().subscribe((d) => this.items.set(d)); }
}
