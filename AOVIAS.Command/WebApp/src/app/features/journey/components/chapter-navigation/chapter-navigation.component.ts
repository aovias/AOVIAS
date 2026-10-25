import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ExecutionChapterItemVm, ExecutionSortMode } from '../../models/journey-execution-vm.model';
import { ChapterItemComponent } from '../chapter-item/chapter-item.component';

@Component({
  selector: 'app-chapter-navigation',
  standalone: true,
  imports: [CommonModule, ChapterItemComponent],
  templateUrl: './chapter-navigation.component.html',
  styleUrl: './chapter-navigation.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChapterNavigationComponent {
  readonly chapters = input.required<ExecutionChapterItemVm[]>();
  readonly selectedChapterId = input<string | null>(null);
  readonly searchTerm = input('');
  readonly sortMode = input<ExecutionSortMode>('code');

  readonly searchChange = output<string>();
  readonly sortModeChange = output<ExecutionSortMode>();
  readonly chapterSelected = output<string>();

  onSearch(term: string): void {
    this.searchChange.emit(term);
  }

  toggleSort(): void {
    this.sortModeChange.emit(this.sortMode() === 'risk' ? 'code' : 'risk');
  }
}
