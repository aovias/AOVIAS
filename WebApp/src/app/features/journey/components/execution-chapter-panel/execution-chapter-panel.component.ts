import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  Output,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideSearch } from '@ng-icons/lucide';
import { ScrollingModule } from '@angular/cdk/scrolling';
import { ExecutionChapterNavItemVm, ExecutionChapterPanelVm } from '../../models/execution-screen-vm.model';
import { JourneyOperationalModeUiPolicy } from '../../models/journey-operational-mode-ui.model';
import { environment } from '../../../../../environments/environment';

@Component({
  selector: 'app-execution-chapter-panel',
  standalone: true,
  imports: [CommonModule, NgIcon, ScrollingModule],
  viewProviders: [provideIcons({ lucideSearch })],
  templateUrl: './execution-chapter-panel.component.html',
  styleUrl: './execution-chapter-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExecutionChapterPanelComponent {
  @Input({ required: true }) vm!: ExecutionChapterPanelVm;
  /** Operational mode policy — chapter badges / indicators (Phase 48). */
  @Input() modeUi: JourneyOperationalModeUiPolicy | null = null;
  @Input() searchQuery = '';
  @Input() sortByRiskDesc = false;

  @Output() readonly chapterSelect = new EventEmitter<string>();
  @Output() readonly searchQueryChange = new EventEmitter<string>();
  /** `'risk'` = high-first; `'code'` = alphanumeric code order. */
  @Output() readonly sortMode = new EventEmitter<'risk' | 'code'>();
  readonly virtualScrollEnabled = environment.journeyVirtualScrollChaptersEnabled;

  /** Virtual scroll needs a fixed viewport height; use plain list for typical chapter counts. */
  get useVirtualScroll(): boolean {
    return this.virtualScrollEnabled && (this.vm?.items?.length ?? 0) > 40;
  }

  readonly virtualRowSize = 124;

  onChapterSelect(chapterId: string): void {
    this.chapterSelect.emit(chapterId);
  }

  onSearchInput(event: Event): void {
    const v = (event.target as HTMLInputElement).value;
    this.searchQueryChange.emit(v);
  }

  onSortRisk(): void {
    this.sortMode.emit('risk');
  }

  onSortCode(): void {
    this.sortMode.emit('code');
  }

  vmTrackByChapterId(_index: number, chapter: ExecutionChapterNavItemVm): string {
    return chapter.id;
  }
}
