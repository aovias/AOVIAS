import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ExecutionChapterItemVm } from '../../models/journey-execution-vm.model';

@Component({
  selector: 'app-chapter-item',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './chapter-item.component.html',
  styleUrl: './chapter-item.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChapterItemComponent {
  readonly chapter = input.required<ExecutionChapterItemVm>();
  readonly active = input(false);
  readonly selectChapter = output<string>();

  onSelect(): void {
    this.selectChapter.emit(this.chapter().id);
  }
}
