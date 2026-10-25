import { Component, inject, signal } from '@angular/core';

import { CommonModule } from '@angular/common';

import { FormsModule } from '@angular/forms';

import { RouterLink } from '@angular/router';

import { AccessGovernanceApiService, AuthorizationDecision } from '../services/access-governance-api.service';



@Component({

  selector: 'app-authorization-simulator',

  standalone: true,

  imports: [CommonModule, FormsModule, RouterLink],

  template: `

    <section>

      <header><h1>Authorization Simulator</h1><a routerLink="/app/access-governance">Back</a></header>

      <div class="form">

        <label>Subject ID <input [(ngModel)]="subjectId" /></label>

        <label>Resource <input [(ngModel)]="resource" /></label>

        <label>Action <input [(ngModel)]="action" /></label>

        <button type="button" (click)="evaluate()">Evaluate</button>

      </div>

      @if (decision()) {

        <div class="result" [class.allowed]="decision()!.allowed" [class.denied]="!decision()!.allowed">

          <strong>{{ decision()!.allowed ? 'ALLOWED' : 'DENIED' }}</strong> — {{ decision()!.reason }}

        </div>

        @if (decision()!.evaluationTrace?.length) {

          <h3>Evaluation trace</h3>

          <ol>

            @for (step of decision()!.evaluationTrace!; track step.step + step.outcome) {

              <li><strong>{{ step.step }}</strong>: {{ step.outcome }} @if (step.detail) { ({{ step.detail }}) }</li>

            }

          </ol>

        }

      }

    </section>

  `,

  styles: [`.allowed { color: green; } .denied { color: crimson; }`],

})

export class AuthorizationSimulatorComponent {

  private api = inject(AccessGovernanceApiService);

  subjectId = '';

  resource = 'Compliance';

  action = 'Standard.View';

  decision = signal<AuthorizationDecision | null>(null);



  evaluate(): void {

    if (!this.subjectId) return;

    this.api.evaluate({ subjectId: this.subjectId, resource: this.resource, action: this.action }).subscribe((d) => this.decision.set(d));

  }

}

