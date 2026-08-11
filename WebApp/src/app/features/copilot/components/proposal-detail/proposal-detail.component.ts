import {
  Component, Input, Output, EventEmitter, ChangeDetectionStrategy, inject, computed,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { CopilotStore } from '../../state/copilot.store';
import type { CopilotActionProposal } from '../../models/proposal.model';
import {
  ProposalStatus,
  getProposalStatusLabel,
  getProposalStatusColor,
  getProposalTypeLabel,
  getProposalTypeIcon,
} from '../../models/proposal.model';

/**
 * Sprint 13D – Proposal Detail panel
 * Shows full proposal information including payload, audit trail,
 * source recommendation/draft links, and lifecycle actions.
 */
@Component({
  selector: 'app-proposal-detail',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './proposal-detail.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProposalDetailComponent {
  @Input({ required: true }) proposal!: CopilotActionProposal;

  @Output() closed          = new EventEmitter<void>();
  @Output() approveClicked  = new EventEmitter<string>();
  @Output() rejectClicked   = new EventEmitter<string>();
  @Output() executeClicked  = new EventEmitter<string>();

  readonly store = inject(CopilotStore);

  readonly auditTrail = computed(() => this.store.proposalAuditTrail());

  // Helpers
  readonly ProposalStatus        = ProposalStatus;
  readonly getStatusLabel        = getProposalStatusLabel;
  readonly getStatusColor        = getProposalStatusColor;
  readonly getTypeLabel          = getProposalTypeLabel;
  readonly getTypeIcon           = getProposalTypeIcon;

  get parsedPayload(): Record<string, unknown> | null {
    try {
      return JSON.parse(this.proposal.payloadJson);
    } catch {
      return null;
    }
  }

  get payloadEntries(): Array<[string, string]> {
    const p = this.parsedPayload;
    if (!p) return [];
    return Object.entries(p)
      .filter(([, v]) => v !== null && v !== undefined && v !== '')
      .map(([k, v]) => [k, String(v)]);
  }

  close(): void  { this.closed.emit(); }
  onApprove(): void  { this.approveClicked.emit(this.proposal.id); }
  onReject(): void   { this.rejectClicked.emit(this.proposal.id); }
  onExecute(): void  { this.executeClicked.emit(this.proposal.id); }
}
