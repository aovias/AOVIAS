import {
  Component, Input, Output, EventEmitter, ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import type { CopilotActionProposal } from '../../models/proposal.model';
import {
  ProposalStatus,
  getProposalStatusLabel,
  getProposalStatusColor,
  getProposalTypeLabel,
  getProposalTypeIcon,
} from '../../models/proposal.model';

/**
 * Sprint 13D – Approval Card
 *
 * Displays an AI-generated action proposal and presents Approve / Reject / Execute
 * actions to an authenticated human reviewer.
 *
 * The component never directly calls services — it emits events so the parent
 * page/container handles backend communication.
 */
@Component({
  selector: 'app-approval-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './approval-card.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ApprovalCardComponent {
  @Input({ required: true }) proposal!: CopilotActionProposal;

  @Output() approveClicked  = new EventEmitter<string>();
  @Output() rejectClicked   = new EventEmitter<string>();
  @Output() executeClicked  = new EventEmitter<string>();
  @Output() viewSource      = new EventEmitter<string>();
  @Output() openDraft       = new EventEmitter<string>();

  // Re-export helpers for template use
  readonly ProposalStatus        = ProposalStatus;
  readonly getStatusLabel        = getProposalStatusLabel;
  readonly getStatusColor        = getProposalStatusColor;
  readonly getProposalTypeLabel  = getProposalTypeLabel;
  readonly getProposalTypeIcon   = getProposalTypeIcon;

  get statusLabel(): string {
    return getProposalStatusLabel(this.proposal.statusEnum);
  }

  get statusColor(): string {
    return getProposalStatusColor(this.proposal.statusEnum);
  }

  get typeLabel(): string {
    return getProposalTypeLabel(this.proposal.proposalTypeEnum);
  }

  get typeIcon(): string {
    return getProposalTypeIcon(this.proposal.proposalTypeEnum);
  }

  get expiresInDays(): number {
    const ms = new Date(this.proposal.expiresAtUtc).getTime() - Date.now();
    return Math.max(0, Math.ceil(ms / (1000 * 60 * 60 * 24)));
  }

  get isUrgent(): boolean {
    return this.expiresInDays <= 2 && !this.proposal.isExpired &&
           this.proposal.statusEnum === ProposalStatus.Proposed;
  }

  onApprove(): void   { this.approveClicked.emit(this.proposal.id); }
  onReject(): void    { this.rejectClicked.emit(this.proposal.id); }
  onExecute(): void   { this.executeClicked.emit(this.proposal.id); }
  onViewSource(): void { this.viewSource.emit(this.proposal.recommendationId); }
  onOpenDraft(): void {
    if (this.proposal.draftId) this.openDraft.emit(this.proposal.draftId);
  }
}
