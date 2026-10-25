import {
  Component, Input, Output, EventEmitter, ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import type { ProposalAuditEvent } from '../../models/proposal.model';

/**
 * Sprint 13D – Proposal History
 * Renders a chronological audit trail for a single proposal.
 * Used inside ProposalDetailComponent and standalone audit pages.
 */
@Component({
  selector: 'app-proposal-history',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './proposal-history.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProposalHistoryComponent {
  @Input({ required: true }) events: ProposalAuditEvent[] = [];
  @Input() proposalId?: string;

  @Output() eventSelected = new EventEmitter<ProposalAuditEvent>();

  getEventIcon(eventType: string): string {
    switch (eventType.toLowerCase()) {
      case 'created':
      case 'generated':  return 'add_circle_outline';
      case 'viewed':     return 'visibility';
      case 'approved':   return 'thumb_up';
      case 'rejected':   return 'thumb_down';
      case 'executed':   return 'play_circle';
      case 'expired':    return 'timer_off';
      default:           return 'radio_button_unchecked';
    }
  }

  getEventColor(eventType: string): string {
    switch (eventType.toLowerCase()) {
      case 'approved':   return 'success';
      case 'rejected':   return 'danger';
      case 'executed':   return 'primary';
      case 'expired':    return 'secondary';
      case 'created':
      case 'generated':  return 'info';
      default:           return 'muted';
    }
  }

  select(event: ProposalAuditEvent): void {
    this.eventSelected.emit(event);
  }
}
