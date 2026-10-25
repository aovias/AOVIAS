import { Component, ChangeDetectionStrategy, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatSortModule } from '@angular/material/sort';

@Component({
  selector: 'app-table',
  standalone: true,
  imports: [CommonModule, MatTableModule, MatPaginatorModule, MatSortModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="table-container">
      <table mat-table [dataSource]="dataSource" class="mat-elevation-z2">
        <ng-content></ng-content>
      </table>
      <mat-paginator [pageSizeOptions]="pageSizeOptions" [pageSize]="pageSize" showFirstLastButtons>
      </mat-paginator>
    </div>
  `,
  styles: [
    `
      .table-container {
        width: 100%;
      }
    `,
  ],
})
export class TableComponent {
  @Input() dataSource: any[] = [];
  @Input() pageSize = 10;
  @Input() pageSizeOptions = [5, 10, 25, 100];
}
