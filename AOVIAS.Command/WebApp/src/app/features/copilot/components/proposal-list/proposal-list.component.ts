import {
  Component, OnInit, ChangeDetectionStrategy, inject, signal, computed,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CopilotStore } from '../../state/copilot.store';
import { ApprovalCardComponent } from '../approval-card/approval-card.component';
import { ProposalDetailComponent } from '../proposal-detail/proposal-detail.component';
import type { CopilotActionProposal } from '../../models/proposal.model';
import { ProposalStatus, getProposalStatusLabel } from '../../models/proposal.model';

/**
 * Sprint 13D – Proposal List
 * Lists all action proposals with filtering by status, type, and search.
 * Delegates approve/reject/execute to the store.
 */
@Component({
  selector: 'app-proposal-list',
  standalone: true,
  imports: [CommonModule, FormsModule, ApprovalCardComponent, ProposalDetailComponent],
  templateUrl: './proposal-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProposalListComponent implements OnInit {
  readonly store = inject(CopilotStore);

  readonly statusFilter   = signal<ProposalStatus | 'all'>('all');
  readonly searchQuery    = signal('');
  readonly selectedProposal = signal<CopilotActionProposal | null>(null);
  readonly showRejectDialog = signal(false);
  readonly rejectReason     = signal('');
  readonly rejectingId      = signal<string | null>(null);

  // Re-export for template
  readonly ProposalStatus      = ProposalStatus;
  readonly getProposalStatusLabel = getProposalStatusLabel;

  readonly statusOptions = [
    { value: 'all' as const,              label: 'All Proposals' },
    { value: ProposalStatus.Proposed,     label: 'Pending Approval' },
    { value: ProposalStatus.Approved,     label: 'Approved' },
    { value: ProposalStatus.Rejected,     label: 'Rejected' },
    { value: ProposalStatus.Executed,     label: 'Executed' },
    { value: ProposalStatus.Expired,      label: 'Expired' },
  ];

  readonly filteredProposals = computed(() => {
    let items = this.store.proposals();

    const statusFilter = this.statusFilter();
    if (statusFilter !== 'all') {
      items = items.filter(p => p.statusEnum === statusFilter);
    }

    const query = this.searchQuery().toLowerCase().trim();
    if (query) {
      items = items.filter(p =>
        p.title.toLowerCase().includes(query) ||
        p.description.toLowerCase().includes(query) ||
        p.proposalType.toLowerCase().includes(query),
      );
    }

    return items.sort((a, b) =>
      new Date(b.proposedAtUtc).getTime() - new Date(a.proposedAtUtc).getTime(),
    );
  });

  readonly counts = this.store.approvalCounts;
  readonly loading = computed(() => this.store.loading().proposals);

  ngOnInit(): void {
    this.store.loadProposals();
  }

  setStatusFilter(value: ProposalStatus | 'all'): void {
    this.statusFilter.set(value);
  }

  openDetail(proposal: CopilotActionProposal): void {
    this.selectedProposal.set(proposal);
    this.store.loadProposalAuditTrail(proposal.id);
  }

  closeDetail(): void {
    this.selectedProposal.set(null);
  }

  onApprove(id: string): void {
    this.store.approveProposal(id);
  }

  onRejectPrompt(id: string): void {
    this.rejectingId.set(id);
    this.rejectReason.set('');
    this.showRejectDialog.set(true);
  }

  confirmReject(): void {
    const id = this.rejectingId();
    if (!id) return;
    this.store.rejectProposal(id, { reason: this.rejectReason() });
    this.showRejectDialog.set(false);
    this.rejectingId.set(null);
  }

  cancelReject(): void {
    this.showRejectDialog.set(false);
    this.rejectingId.set(null);
  }

  onExecute(id: string): void {
    this.store.executeProposal(id);
  }
}
