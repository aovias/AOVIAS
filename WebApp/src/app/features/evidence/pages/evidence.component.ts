import {
  Component,
  ChangeDetectionStrategy,
  signal,
  computed,
  inject,
  OnInit,
  OnDestroy,
  ViewChild,
  ElementRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import {
  Subject,
  debounceTime,
  distinctUntilChanged,
  of,
  catchError,
  finalize,
  takeUntil,
  forkJoin,
  switchMap,
  EMPTY,
  combineLatest,
} from 'rxjs';
import {
  EvidenceApiService,
  EvidenceDashboard,
  EvidenceListItem,
  EvidenceTimelineItem,
  LibraryEvidenceDetail,
} from '../services/evidence-api.service';
import { JourneyDataService } from '../../journey/services/journey-data.service';
import {
  buildSingleEvidenceFormData,
} from '../../journey/utils/oe-evidence-upload.payload';
import {
  OE_EVIDENCE_DEFAULT_EVIDENCE_TYPE,
  validateEvidenceFilesForMode,
} from '../../journey/utils/oe-evidence-upload.constants';
import { ComplianceContextService } from '../../../core/services/compliance-context.service';
import { OrganizationService } from '../../../core/services/organization.service';
import { MembershipScopeService } from '../../../core/services/membership-scope.service';
import { CommonLookupService } from '../../../shared/services/common-lookup.service';
import { DepartmentLookup } from '../../../shared/models/lookup.models';
import { EvidenceStatusBadgeComponent } from '../../../shared/ui/evidence-status-badge/evidence-status-badge.component';
import {
  canReviewEvidenceInQueue,
} from '../../../shared/models/evidence-status.model';
import {
  EVIDENCE_REVIEW_QUEUE_PATH,
  evidenceReviewQueueQuery,
} from '../../../shared/utils/evidence-review-navigation.util';

interface SummaryCard {
  label: string;
  value: number;
  color: 'green' | 'yellow' | 'red' | 'orange' | 'blue';
  icon: string;
}

interface EvidenceRecord {
  id: string;
  evidenceId: string;
  objectiveElementId: string;
  fileName: string;
  fileType: string;
  fileSize: string;
  objectiveElementName: string;
  objectiveElementCode: string;
  chapter: string;
  chapterCode: string;
  department: string;
  uploadedBy: string;
  uploadDate: string;
  status: import('../services/evidence-api.service').EvidenceVerificationStatus;
  linkedTask: string;
  description: string;
  version: number;
  isApproved?: boolean | null;
  rowVersion?: string | null;
  reviewerName?: string;
  reviewDate?: string;
  reviewComment?: string;
}

interface ChartData {
  label: string;
  value: number;
  color: string;
}

interface ActivityEvent {
  id: string;
  type: 'upload' | 'approve' | 'reject' | 'review';
  title: string;
  user: string;
  timestamp: string;
  icon: string;
}

type TimelineStatus = 'approved' | 'rejected' | 'pendingReview';

type OeBadgeVariant = 'acc' | 'cop' | 'pcc' | 'moc' | 'qms' | 'default';

interface TimelineRow {
  evidenceId: string;
  fileName: string;
  fileType: string;
  uploadedByName: string;
  uploadedAt: string;
  uploadedAtIso: string;
  updatedByName: string;
  updatedAt: string;
  updatedAtIso: string;
  hasUpdate: boolean;
  status: TimelineStatus;
  reviewerName: string | null;
  verifiedAt: string | null;
  verifiedAtIso: string | null;
}

interface TimelineGroup {
  objectiveElementId: string;
  code: string;
  name: string;
  badgeVariant: OeBadgeVariant;
  rows: TimelineRow[];
}

interface MissingEvidence {
  objectiveElementId: string;
  objectiveElementCode: string;
  objectiveElementName: string;
  chapterLabel: string;
  priority: 'high' | 'medium' | 'low';
}

type PreviewDisplayMode = 'idle' | 'loading' | 'pdf' | 'image' | 'text' | 'unsupported' | 'error';

interface HistoryTimelineEntry {
  id: string;
  kind: 'upload' | 'version' | 'approved' | 'rejected' | 'pending';
  title: string;
  subtitle: string;
}

@Component({
  selector: 'app-evidence',
  standalone: true,
  imports: [CommonModule, FormsModule, MatSnackBarModule, RouterLink, RouterLinkActive, EvidenceStatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="evidence-page">
      <div class="page-header">
        <nav class="module-tabs">
          <a routerLink="/app/evidence" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">Repository</a>
          <a href="#coverage-timeline">Coverage</a>
          <a routerLink="/app/evidence/review-queue">Review Queue</a>
        </nav>
        <div class="header-row">
        <div class="header-content">
          <h1 class="page-title">Evidence Library</h1>
          <p class="page-description">
            Manage and review compliance evidence linked to objective elements, tasks, and audit findings.
          </p>
        </div>
        <div class="page-header-actions">
          <button type="button" class="btn-header-secondary" (click)="openUploadModal()">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor">
              <path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4M17 8l-5-5-5 5M12 3v12" stroke-width="2" stroke-linecap="round"/>
            </svg>
            Upload Evidence
          </button>
          <button type="button" class="upload-button" (click)="goToEvidenceBinder()">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor">
              <path d="M4 19.5A2.5 2.5 0 016.5 17H20" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              <path d="M6.5 2H20v20H6.5A2.5 2.5 0 014 19.5v-15A2.5 2.5 0 016.5 2z" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
              <path d="M8 7h8M8 11h6" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
            </svg>
            Evidence Binder
          </button>
        </div>
        </div>
      </div>

      @if (pageError()) {
        <div class="page-banner error">{{ pageError() }}</div>
      }

      <div class="summary-section">
        <div class="summary-cards">
          @for (card of summaryCards(); track card.label) {
            <div class="summary-card" [attr.data-color]="card.color">
              <div class="card-icon" [innerHTML]="card.icon"></div>
              <div class="card-content">
                <div class="card-value">{{ card.value }}</div>
                <div class="card-label">{{ card.label }}</div>
              </div>
            </div>
          }
        </div>
      </div>

      <div class="filters-section">
        <div class="filter-controls">
          <select class="filter-dropdown" [(ngModel)]="filterChapter" (ngModelChange)="onChapterFilterChange($event)">
            <option value="">All Chapters</option>
            @for (opt of chapterFilterOptions(); track opt.id) {
              <option [value]="opt.id">{{ opt.label }}</option>
            }
          </select>

          <select class="filter-dropdown" [(ngModel)]="filterStandard" (ngModelChange)="onFilterChange()">
            <option value="">All Standards</option>
            @for (opt of standardFilterOptionsForUi(); track opt.id) {
              <option [value]="opt.id">{{ opt.label }}</option>
            }
          </select>

          <select class="filter-dropdown" [(ngModel)]="filterStatus" (ngModelChange)="onFilterChange()">
            <option value="">All Status</option>
            <option value="approved">Approved</option>
            <option value="pendingReview">Pending Review</option>
            <option value="rejected">Rejected</option>
          </select>

          <select class="filter-dropdown" [(ngModel)]="filterDepartment" (ngModelChange)="onFilterChange()">
            <option value="">All Departments</option>
            @for (opt of departmentFilterOptions(); track opt.id) {
              <option [value]="opt.id">{{ opt.label }}</option>
            }
          </select>

          <select class="filter-dropdown" [(ngModel)]="filterFileType" (ngModelChange)="onFilterChange()">
            <option value="">All File Types</option>
            <option value="pdf">PDF</option>
            <option value="docx">Word</option>
            <option value="xlsx">Excel</option>
            <option value="jpg">Image</option>
          </select>
        </div>

        <div class="search-box">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <circle cx="11" cy="11" r="8" stroke-width="2"/>
            <path d="M21 21l-4.35-4.35" stroke-width="2" stroke-linecap="round"/>
          </svg>
          <input
            type="text"
            placeholder="Search evidence by file name, objective element, chapter, or department..."
            [(ngModel)]="searchQuery"
            (ngModelChange)="onSearchChange($event)"
          />
        </div>
      </div>

      <div class="content-layout">
        <div class="evidence-table-section">
          <div class="table-container">
            <table class="evidence-table">
              <thead>
                <tr>
                  <th>File</th>
                  <th>Objective Element</th>
                  <th>Chapter</th>
                  <th>Department</th>
                  <th>Uploaded By</th>
                  <th>Upload Date</th>
                  <th>Status</th>
                  <th>Linked Task</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                @for (record of pagedRows(); track record.id) {
                  <tr
                    class="evidence-row"
                    [class.selected]="selectedRecord()?.id === record.id"
                    (click)="selectRecord(record)"
                  >
                    <td class="file-cell">
                      <div class="file-info">
                        <div class="file-icon" [attr.data-type]="fileIconCategory(record.fileType)">
                          @switch (fileIconCategory(record.fileType)) {
                            @case ('pdf') {
                              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                                <path d="M13 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V9z" stroke-width="2"/>
                                <path d="M13 2v7h7" stroke-width="2"/>
                              </svg>
                            }
                            @case ('docx') {
                              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                                <path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z" stroke-width="2"/>
                                <path d="M14 2v6h6M16 13H8m8 4H8m2-8H8" stroke-width="2"/>
                              </svg>
                            }
                            @case ('xlsx') {
                              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                                <path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z" stroke-width="2"/>
                                <path d="M14 2v6h6M8 13h8M8 17h8M8 9h2" stroke-width="2"/>
                              </svg>
                            }
                            @case ('image') {
                              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                                <rect x="3" y="3" width="18" height="18" rx="2" stroke-width="2"/>
                                <circle cx="8.5" cy="8.5" r="1.5" fill="currentColor"/>
                                <path d="M21 15l-5-5L5 21" stroke-width="2"/>
                              </svg>
                            }
                            @case ('text') {
                              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                                <path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z" stroke-width="2"/>
                                <path d="M10 9H8m2 4H8m2 4H8m5-12h2a2 2 0 012 2v10" stroke-width="2"/>
                              </svg>
                            }
                            @case ('csv') {
                              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                                <path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z" stroke-width="2"/>
                                <path d="M8 13h8M8 17h5M8 9h8" stroke-width="2"/>
                              </svg>
                            }
                            @default {
                              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                                <path d="M13 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V9z" stroke-width="2"/>
                                <path d="M13 2v7h7" stroke-width="2"/>
                                <path d="M8 13h8M8 17h5" stroke-width="2" stroke-linecap="round"/>
                              </svg>
                            }
                          }
                        </div>
                        <div class="file-details">
                          <div class="file-name">{{ record.fileName }}</div>
                          <div class="file-meta">{{ record.fileSize }} • {{ fileTypeLabel(record.fileType) }}</div>
                        </div>
                      </div>
                    </td>
                    <td>
                      <div class="clause-info">
                        <span class="clause-code">{{ record.objectiveElementCode }}</span>
                        <span class="clause-text">{{ record.objectiveElementName }}</span>
                      </div>
                    </td>
                    <td>
                      <span class="chapter-badge">{{ record.chapterCode }}</span>
                    </td>
                    <td>{{ record.department || '—' }}</td>
                    <td>{{ record.uploadedBy }}</td>
                    <td>{{ formatDate(record.uploadDate) }}</td>
                    <td>
                      <app-evidence-status-badge [status]="record.status" />
                    </td>
                    <td>{{ record.linkedTask }}</td>
                    <td>
                      <div class="action-buttons">
                        <button class="icon-button" (click)="previewEvidence($event, record)" title="Preview">
                          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                            <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" stroke-width="2"/>
                            <circle cx="12" cy="12" r="3" stroke-width="2"/>
                          </svg>
                        </button>
                        <button class="icon-button" (click)="downloadEvidence($event, record)" title="Download">
                          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                            <path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4M7 10l5 5 5-5M12 15V3" stroke-width="2" stroke-linecap="round"/>
                          </svg>
                        </button>
                        <button class="icon-button" (click)="viewHistory($event, record)" title="View History">
                          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                            <circle cx="12" cy="12" r="10" stroke-width="2"/>
                            <path d="M12 6v6l4 2" stroke-width="2" stroke-linecap="round"/>
                          </svg>
                        </button>
                      </div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          @if (listError()) {
            <div class="inline-error">{{ listError() }}</div>
          }
          @if (totalListCount() > 0) {
            <div class="table-toolbar">
              <label class="page-size-label">
                Rows per page
                <select
                  class="filter-dropdown page-size-select"
                  [ngModel]="pageSize()"
                  (ngModelChange)="onPageSizeChange($event)"
                >
                  <option [ngValue]="10">10</option>
                  <option [ngValue]="20">20</option>
                  <option [ngValue]="50">50</option>
                  <option [ngValue]="100">100</option>
                </select>
              </label>
              <div class="table-pagination">
                <button type="button" class="page-nav" [disabled]="currentPage() <= 1" (click)="goToPage(currentPage() - 1)">
                  Previous
                </button>
                <span class="page-info">Page {{ currentPage() }} of {{ totalPages() }}</span>
                <button
                  type="button"
                  class="page-nav"
                  [disabled]="currentPage() >= totalPages()"
                  (click)="goToPage(currentPage() + 1)"
                >
                  Next
                </button>
              </div>
            </div>
          }
        </div>

        @if (selectedRecord()) {
          <div class="detail-panel">
            <div class="panel-header">
              <h2 class="panel-title">Evidence Details</h2>
              <button class="close-button" (click)="closeDetailPanel()">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                  <path d="M18 6L6 18M6 6l12 12" stroke-width="2" stroke-linecap="round"/>
                </svg>
              </button>
            </div>

            <div class="panel-tabs">
              @for (tab of detailTabs(); track tab.id) {
                <button
                  class="panel-tab"
                  [class.active]="activeTab() === tab.id"
                  (click)="setActiveTab(tab.id)"
                >
                  {{ tab.label }}
                </button>
              }
            </div>

            <div class="panel-content">
              @if (activeTab() === 'preview') {
                <div class="preview-section">
                  <div class="preview-container">
                    @if (libraryDetailLoading()) {
                      <div class="preview-state">Loading preview…</div>
                    } @else if (libraryDetailError()) {
                      <div class="preview-state error">{{ libraryDetailError() }}</div>
                    } @else if (previewMode() === 'error') {
                      <div class="preview-state error">Unable to load preview.</div>
                    } @else {
                      <div
                        #previewStage
                        class="preview-stage"
                        [class.preview-stage--zoom]="previewZoom() !== 1"
                        [style.transform]="'scale(' + previewZoom() + ')'"
                      >
                        @switch (previewMode()) {
                          @case ('pdf') {
                            @if (previewSafeResourceUrl()) {
                              <iframe
                                class="preview-iframe"
                                title="Document preview"
                                [src]="previewSafeResourceUrl()"
                                sandbox="allow-same-origin allow-scripts allow-popups"
                              ></iframe>
                            }
                          }
                          @case ('image') {
                            @if (previewSafeResourceUrl()) {
                              <img class="preview-img" [src]="previewSafeResourceUrl()" alt="" />
                            }
                          }
                          @case ('text') {
                            <pre class="preview-text">{{ previewTextContent() }}</pre>
                          }
                          @case ('unsupported') {
                            <div class="preview-unsupported">
                              <svg width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                                <path d="M13 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V9z" stroke-width="2"/>
                                <path d="M13 2v7h7" stroke-width="2"/>
                              </svg>
                              <p>{{ selectedRecord()?.fileName }}</p>
                              <p class="preview-unsupported-hint">Preview is not available for this file type. Use Download to open it.</p>
                            </div>
                          }
                          @default {
                            <div class="preview-placeholder">
                              <svg width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                                <path d="M13 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V9z" stroke-width="2"/>
                                <path d="M13 2v7h7" stroke-width="2"/>
                              </svg>
                              <p>{{ selectedRecord()?.fileName }}</p>
                            </div>
                          }
                        }
                      </div>
                    }
                    <div class="preview-controls">
                      <button type="button" class="control-button" (click)="zoomInPreview()" aria-label="Zoom in">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                          <circle cx="11" cy="11" r="8" stroke-width="2"/>
                          <path d="M21 21l-4.35-4.35M11 8v6" stroke-width="2" stroke-linecap="round"/>
                        </svg>
                        Zoom In
                      </button>
                      <button type="button" class="control-button" (click)="resetZoomPreview()" aria-label="Reset zoom">Reset</button>
                      <button type="button" class="control-button" (click)="togglePreviewFullscreen()" aria-label="Full screen">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                          <path d="M8 3H5a2 2 0 00-2 2v3m18 0V5a2 2 0 00-2-2h-3m0 18h3a2 2 0 002-2v-3M3 16v3a2 2 0 002 2h3" stroke-width="2" stroke-linecap="round"/>
                        </svg>
                        Full Screen
                      </button>
                      <button
                        type="button"
                        class="control-button"
                        (click)="downloadSelectedEvidence()"
                        [disabled]="previewDownloadInFlight()"
                        aria-label="Download file"
                      >
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                          <path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4M7 10l5 5 5-5M12 15V3" stroke-width="2" stroke-linecap="round"/>
                        </svg>
                        {{ previewDownloadInFlight() ? 'Downloading…' : 'Download' }}
                      </button>
                    </div>
                  </div>
                </div>
              }

              @if (activeTab() === 'metadata') {
                <div class="metadata-section">
                  <div class="metadata-grid">
                    <div class="metadata-item">
                      <label>File Name</label>
                      <div class="metadata-value">{{ selectedRecord()?.fileName }}</div>
                    </div>
                    <div class="metadata-item">
                      <label>File Type</label>
                      <div class="metadata-value">{{ selectedRecord()?.fileType?.toUpperCase() }}</div>
                    </div>
                    <div class="metadata-item">
                      <label>File Size</label>
                      <div class="metadata-value">{{ selectedRecord()?.fileSize }}</div>
                    </div>
                    <div class="metadata-item">
                      <label>Upload Date</label>
                      <div class="metadata-value">{{ formatDate(selectedRecord()?.uploadDate) }}</div>
                    </div>
                    <div class="metadata-item">
                      <label>Uploaded By</label>
                      <div class="metadata-value">{{ selectedRecord()?.uploadedBy }}</div>
                    </div>
                    <div class="metadata-item">
                      <label>Department</label>
                      <div class="metadata-value">{{ selectedRecord()?.department }}</div>
                    </div>
                    <div class="metadata-item full-width">
                      <label>Description</label>
                      <div class="metadata-value">{{ selectedRecord()?.description }}</div>
                    </div>
                  </div>
                </div>
              }

              @if (activeTab() === 'context') {
                <div class="context-section">
                  <div class="context-card">
                    <h3 class="context-title">Linked Compliance Context</h3>
                    <div class="context-grid">
                      <div class="context-item">
                        <label>Linked Chapter</label>
                        <div class="context-value">
                          <span class="chapter-badge">{{ selectedRecord()?.chapterCode }}</span>
                          {{ selectedRecord()?.chapter }}
                        </div>
                      </div>
                      <div class="context-item">
                        <label>Linked Objective Element</label>
                        <div class="context-value">
                          <span class="clause-code">{{ selectedRecord()?.objectiveElementCode }}</span>
                          {{ selectedRecord()?.objectiveElementName }}
                        </div>
                      </div>
                      <div class="context-item">
                        <label>Linked Task</label>
                        <div class="context-value">{{ selectedRecord()?.linkedTask }}</div>
                      </div>
                    </div>
                  </div>
                </div>
              }

              @if (activeTab() === 'workflow') {
                <div class="workflow-section">
                  <div class="approval-status">
                    <label>Current Status</label>
                    <app-evidence-status-badge [status]="selectedRecord()?.status" />
                  </div>

                  @if (selectedRecord()?.reviewerName) {
                    <div class="review-info">
                      <div class="reviewer-details">
                        <label>Reviewed By</label>
                        <div>{{ selectedRecord()?.reviewerName }}</div>
                      </div>
                      <div class="reviewer-details">
                        <label>Review Date</label>
                        <div>{{ formatDate(selectedRecord()?.reviewDate) }}</div>
                      </div>
                      @if (selectedRecord()?.reviewComment) {
                        <div class="reviewer-comment">
                          <label>Review Comment</label>
                          <div class="comment-box">{{ selectedRecord()?.reviewComment }}</div>
                        </div>
                      }
                    </div>
                  }

                  @if (canOpenReviewQueue(selectedRecord()?.status)) {
                    <div class="workflow-actions">
                      <a
                        class="review-queue-link"
                        [routerLink]="reviewQueuePath"
                        [queryParams]="reviewQueueParams(selectedRecord()!)"
                      >
                        Open in Review Queue
                      </a>
                    </div>
                  }
                </div>
              }

              @if (activeTab() === 'history') {
                <div class="history-section history-section--vertical">
                  @if (libraryDetailLoading()) {
                    <div class="preview-state">Loading history…</div>
                  } @else if (!libraryDetail()) {
                    <div class="preview-state">No history data. Open Preview first or reselect the row.</div>
                  } @else {
                    <p class="history-order-hint">Newest first</p>
                    <ul class="history-vtimeline" role="list">
                      @for (ev of historyTimelineEntries(); track ev.id) {
                        <li class="history-vitem" role="listitem">
                          <span class="history-vdot" [attr.data-kind]="ev.kind"></span>
                          <div class="history-vbody">
                            <div class="history-vtitle">{{ ev.title }}</div>
                            <div class="history-vmeta">{{ ev.subtitle }}</div>
                          </div>
                        </li>
                      }
                    </ul>
                  }
                </div>
              }
            </div>
          </div>
        }
      </div>

      <div class="analytics-section">
        <h2 class="section-title">Evidence Analytics</h2>
        <div class="analytics-grid">
          <div class="chart-card">
            <h3 class="chart-title">Evidence by Chapter</h3>
            <div class="bar-chart">
              @for (item of chapterData(); track item.label) {
                <div class="bar-item">
                  <div class="bar-label">{{ item.label }}</div>
                  <div class="bar-wrapper">
                    <div class="bar-fill" [style.width.%]="(item.value / maxChapterValue()) * 100" [style.background]="item.color"></div>
                    <span class="bar-value">{{ item.value }}</span>
                  </div>
                </div>
              }
            </div>
          </div>

          <div class="chart-card">
            <h3 class="chart-title">Evidence by Department</h3>
            <div class="bar-chart">
              @for (item of departmentData(); track item.label) {
                <div class="bar-item">
                  <div class="bar-label">{{ item.label }}</div>
                  <div class="bar-wrapper">
                    <div class="bar-fill" [style.width.%]="(item.value / maxDepartmentValue()) * 100" [style.background]="item.color"></div>
                    <span class="bar-value">{{ item.value }}</span>
                  </div>
                </div>
              }
            </div>
          </div>

          <div class="chart-card">
            <h3 class="chart-title">Approval Status</h3>
            <div class="donut-legend">
              @for (item of approvalData(); track item.label) {
                <div class="legend-item">
                  <div class="legend-color" [style.background]="item.color"></div>
                  <div class="legend-label">{{ item.label }}</div>
                  <div class="legend-value">{{ item.value }}</div>
                </div>
              }
            </div>
          </div>

          <div class="chart-card chart-card--missing-evidence">
            <h3 class="chart-title">Missing Evidence</h3>
            <div class="missing-list">
              @for (item of missingEvidence(); track item.objectiveElementCode) {
                <div class="missing-item" [attr.data-priority]="item.priority">
                  <div class="missing-clause">
                    <span class="clause-code">{{ item.objectiveElementCode }}</span>
                    <span class="clause-name">{{ item.objectiveElementName }}</span>
                  </div>
                  <button class="upload-link" (click)="uploadForObjectiveElement(item)">Upload</button>
                </div>
              }
            </div>
          </div>
        </div>
      </div>

      <div class="activity-section">
        <h2 class="section-title">Recent Activity</h2>
        <div class="timeline">
          @for (group of timelineGroups(); track group.objectiveElementId) {
            <div class="timeline-group">
              <div class="timeline-group-header">
                <span class="oe-badge" [attr.data-variant]="group.badgeVariant">{{ group.code }}</span>
                <span class="oe-name">{{ group.name }}</span>
              </div>
              @for (row of group.rows; track row.evidenceId) {
                <div class="timeline-row" [class.is-pending]="row.status === 'pendingReview'">
                  <!-- Stage 1: Upload -->
                  <div class="timeline-stage stage-upload">
                    <div class="stage-node"></div>
                    <div class="stage-body">
                      <div class="file-card" [attr.title]="row.uploadedAtIso">
                        <div class="file-icon" [attr.data-type]="fileIconCategory(row.fileType)">
                          @switch (fileIconCategory(row.fileType)) {
                            @case ('pdf') {
                              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M13 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V9z" stroke-width="2"/><path d="M13 2v7h7" stroke-width="2"/></svg>
                            }
                            @case ('docx') {
                              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z" stroke-width="2"/><path d="M14 2v6h6M16 13H8m8 4H8m2-8H8" stroke-width="2"/></svg>
                            }
                            @case ('xlsx') {
                              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z" stroke-width="2"/><path d="M14 2v6h6M8 13h8M8 17h8M8 9h2" stroke-width="2"/></svg>
                            }
                            @case ('image') {
                              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor"><rect x="3" y="3" width="18" height="18" rx="2" stroke-width="2"/><circle cx="8.5" cy="8.5" r="1.5" fill="currentColor"/><path d="M21 15l-5-5L5 21" stroke-width="2"/></svg>
                            }
                            @case ('text') {
                              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z" stroke-width="2"/><path d="M10 9H8m2 4H8m2 4H8m5-12h2a2 2 0 012 2v10" stroke-width="2"/></svg>
                            }
                            @case ('csv') {
                              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z" stroke-width="2"/><path d="M8 13h8M8 17h5M8 9h8" stroke-width="2"/></svg>
                            }
                            @default {
                              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M13 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V9z" stroke-width="2"/><path d="M13 2v7h7" stroke-width="2"/></svg>
                            }
                          }
                        </div>
                        <div class="file-meta">
                          <div class="file-name">{{ row.fileName }}</div>
                          <div class="stage-meta">{{ row.uploadedByName }} · {{ row.uploadedAt }}</div>
                        </div>
                      </div>
                    </div>
                  </div>

                  <span class="timeline-connector" aria-hidden="true"></span>

                  <!-- Stage 2: Updated -->
                  <div class="timeline-stage stage-update">
                    <div class="stage-node"></div>
                    <div class="stage-body">
                      <span class="update-pill" [attr.title]="row.updatedAtIso">Updated {{ row.updatedAt }}</span>
                      <div class="stage-meta">{{ row.updatedByName }}</div>
                    </div>
                  </div>

                  <span class="timeline-connector" aria-hidden="true"></span>

                  <!-- Stage 3: Review (latest stage - subtly highlighted) -->
                  <div class="timeline-stage stage-review is-latest">
                    <div class="stage-node"></div>
                    <div class="stage-body">
                      <app-evidence-status-badge [status]="row.status" />
                      <div class="stage-meta" [attr.title]="row.verifiedAtIso">
                        @if (row.status === 'pendingReview') {
                          Awaiting review
                        } @else {
                          {{ row.reviewerName ?? 'Unknown user' }} · {{ row.uploadedAt }} → {{ row.verifiedAt }}
                        }
                      </div>
                    </div>
                  </div>
                </div>
              }
            </div>
          }
          @if (timelineGroups().length === 0) {
            <div class="timeline-empty">No recent evidence activity.</div>
          }
        </div>
      </div>

      @if (showUploadModal()) {
        <div class="modal-overlay" (click)="closeUploadModal()">
          <div class="modal-container" (click)="$event.stopPropagation()">
            <div class="modal-header">
              <h2 class="modal-title">Upload Evidence</h2>
              <button type="button" class="close-button" (click)="closeUploadModal()">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                  <path d="M18 6L6 18M6 6l12 12" stroke-width="2" stroke-linecap="round"/>
                </svg>
              </button>
            </div>
            <div class="modal-content">
              @if (uploadContextLoading()) {
                <p class="preview-state">Loading upload context…</p>
              } @else if (!uploadJourneyId()) {
                <p class="inline-error modal-banner">
                  No active compliance journey for this framework. Start or select a journey before uploading.
                </p>
              }
              <input
                #libraryEvidenceFileInput
                type="file"
                class="library-evidence-file-input"
                (change)="onLibraryEvidenceFilesSelected($event)"
              />
              <div
                class="upload-area"
                (click)="onUploadDropzoneClick($event)"
                (dragover)="onUploadDragOver($event)"
                (drop)="onUploadDrop($event)"
              >
                <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                  <path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4M17 8l-5-5-5 5M12 3v12" stroke-width="2" stroke-linecap="round"/>
                </svg>
                <p class="upload-text">Drag and drop a file here or click to browse</p>
                <p class="upload-hint">Single file upload (same contract as Objective Element Evidence tab).</p>
                <button type="button" class="browse-button" (click)="triggerLibraryEvidenceFilePick($event)">Browse Files</button>
              </div>
              @if (uploadFilesPending().length > 0) {
                <ul class="upload-file-list">
                  @for (f of uploadFilesPending(); track f.name + f.size) {
                    <li>{{ f.name }} — {{ formatFileSizeBytes(f.size) }}</li>
                  }
                </ul>
              }
              @if (uploadValidationError()) {
                <p class="inline-error">{{ uploadValidationError() }}</p>
              }

              <div class="upload-form upload-form--stacked">
                <div class="form-group full-width">
                  <label>Objective Element</label>
                  <select [(ngModel)]="uploadObjectiveElementId">
                    <option value="">Select Objective Element</option>
                    @for (m of missingEvidence(); track m.objectiveElementId) {
                      <option [value]="m.objectiveElementId">{{ m.objectiveElementCode }} — {{ m.objectiveElementName }}</option>
                    }
                  </select>
                </div>

                <div class="form-group full-width">
                  <label>Description (optional)</label>
                  <textarea
                    placeholder="Brief description..."
                    rows="3"
                    [(ngModel)]="uploadDescription"
                  ></textarea>
                </div>
              </div>
            </div>
            <div class="modal-footer">
              <button type="button" class="cancel-button" (click)="closeUploadModal()">Cancel</button>
              <button
                type="button"
                class="submit-button"
                [disabled]="uploadInFlight() || !uploadJourneyId() || uploadContextLoading()"
                (click)="submitLibraryEvidenceUpload()"
              >
                {{ uploadInFlight() ? 'Uploading…' : 'Upload Evidence' }}
              </button>
            </div>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .evidence-page {
      min-height: 100vh;
      background: var(--bg-primary);
      padding: var(--page-padding-block-start) var(--page-padding-inline) var(--page-padding-block-end);
      color: var(--text-primary);
    }

    .page-banner.error {
      padding: 12px 16px;
      margin-bottom: 16px;
      border-radius: 8px;
      background: rgba(239, 68, 68, 0.1);
      color: #ef4444;
      font-size: 14px;
    }

    .page-header {
      display: flex;
      flex-direction: column;
      gap: 16px;
      margin-bottom: 24px;
    }

    .page-header .header-row {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      gap: 16px;
      flex-wrap: wrap;
      width: 100%;
    }

    .module-tabs {
      display: flex;
      gap: 0.5rem;
      border-bottom: 1px solid var(--border-color, #e5e7eb);
      padding-bottom: 0.5rem;
      width: 100%;
    }

    .module-tabs a {
      padding: 0.5rem 1rem;
      text-decoration: none;
      color: #4b5563;
      border-radius: 6px 6px 0 0;
      font-size: 14px;
    }

    .module-tabs a.active {
      color: var(--color-primary-hover);
      font-weight: 600;
      border-bottom: 2px solid var(--color-primary-hover);
    }

    .page-header-actions {
      display: flex;
      align-items: center;
      gap: 12px;
      flex-shrink: 0;
    }

    .btn-header-secondary {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 12px 20px;
      background: var(--bg-secondary);
      color: var(--text-primary);
      border: 1px solid var(--border-color);
      border-radius: 8px;
      font-size: 14px;
      font-weight: 500;
      cursor: pointer;
      transition: background 0.2s ease;
    }

    .btn-header-secondary:hover {
      background: var(--hover-bg);
    }

    .header-content {
    }

    .page-title {
      font-size: var(--font-size-page-title);
      font-weight: 700;
      color: var(--text-primary);
      margin-bottom: 8px;
    }

    .page-description {
      font-size: var(--font-size-subtitle);
      color: var(--text-secondary);
      line-height: 1.5;
    }

    .upload-button {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 12px 24px;
      background: var(--primary-color);
      color: white;
      border: none;
      border-radius: 8px;
      font-size: 14px;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .upload-button:hover {
      background: var(--primary-hover);
      transform: translateY(-1px);
    }

    .summary-section {
      margin-bottom: 24px;
    }

    .summary-cards {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 16px;
    }

    .summary-card {
      display: flex;
      align-items: center;
      gap: 16px;
      padding: 20px;
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      transition: all 0.2s ease;
    }

    .summary-card:hover {
      transform: translateY(-2px);
      box-shadow: var(--shadow-md);
    }

    .summary-card[data-color="green"] {
      border-left: 3px solid #22c55e;
    }

    .summary-card[data-color="yellow"] {
      border-left: 3px solid #eab308;
    }

    .summary-card[data-color="red"] {
      border-left: 3px solid #ef4444;
    }

    .summary-card[data-color="orange"] {
      border-left: 3px solid #f97316;
    }

    .summary-card[data-color="blue"] {
      border-left: 3px solid #3b82f6;
    }

    .card-icon {
      width: 48px;
      height: 48px;
      display: flex;
      align-items: center;
      justify-content: center;
      background: var(--bg-tertiary);
      border-radius: 10px;
      color: var(--primary-color);
    }

    .summary-card[data-color="green"] .card-icon {
      color: #22c55e;
      background: rgba(34, 197, 94, 0.1);
    }

    .summary-card[data-color="yellow"] .card-icon {
      color: #eab308;
      background: rgba(234, 179, 8, 0.1);
    }

    .summary-card[data-color="red"] .card-icon {
      color: #ef4444;
      background: rgba(239, 68, 68, 0.1);
    }

    .summary-card[data-color="orange"] .card-icon {
      color: #f97316;
      background: rgba(249, 115, 22, 0.1);
    }

    .card-content {
      flex: 1;
    }

    .card-value {
      font-size: 28px;
      font-weight: 700;
      color: var(--text-primary);
      line-height: 1;
      margin-bottom: 4px;
    }

    .card-label {
      font-size: 13px;
      color: var(--text-secondary);
      font-weight: 500;
    }

    .filters-section {
      display: flex;
      gap: 16px;
      margin-bottom: 24px;
      flex-wrap: wrap;
    }

    .filter-controls {
      display: flex;
      gap: 12px;
      flex-wrap: wrap;
      flex: 1;
    }

    .filter-dropdown {
      padding: 10px 16px;
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 8px;
      color: var(--text-primary);
      font-size: 14px;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .filter-dropdown:hover {
      border-color: var(--primary-color);
    }

    .filter-dropdown:focus {
      outline: none;
      border-color: var(--primary-color);
    }

    .search-box {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 10px 16px;
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 8px;
      min-width: 350px;
    }

    .search-box svg {
      color: var(--text-secondary);
    }

    .search-box input {
      flex: 1;
      background: transparent;
      border: none;
      outline: none;
      color: var(--text-primary);
      font-size: 14px;
    }

    .search-box input::placeholder {
      color: var(--text-secondary);
    }

    .content-layout {
      display: grid;
      grid-template-columns: 1fr;
      gap: 24px;
      margin-bottom: 32px;
    }

    .evidence-table-section {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      overflow: hidden;
    }

    .inline-error {
      padding: 12px 16px;
      color: #ef4444;
      font-size: 14px;
      border-top: 1px solid var(--border-color);
    }

    .table-toolbar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      flex-wrap: wrap;
      gap: 12px;
      padding: 10px 16px;
      border-top: 1px solid var(--border-color);
      background: var(--bg-secondary);
    }

    .page-size-label {
      display: flex;
      align-items: center;
      gap: 8px;
      font-size: 13px;
      color: var(--text-secondary);
    }

    .page-size-select {
      min-width: 80px;
    }

    .table-pagination {
      display: flex;
      align-items: center;
      justify-content: flex-end;
      gap: 16px;
    }

    .page-nav {
      padding: 8px 14px;
      border-radius: 8px;
      border: 1px solid var(--border-color);
      background: var(--card-bg);
      color: var(--text-primary);
      cursor: pointer;
    }

    .page-nav:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .page-info {
      font-size: 13px;
      color: var(--text-secondary);
    }

    .table-container {
      overflow: auto;
      max-height: min(60vh, 560px);
    }

    .evidence-table thead th {
      position: sticky;
      top: 0;
      z-index: 1;
      background: var(--bg-secondary);
    }

    .evidence-table {
      width: 100%;
      border-collapse: collapse;
    }

    .evidence-table thead {
      background: var(--bg-secondary);
    }

    .evidence-table th {
      padding: var(--space-2) var(--table-cell-padding-inline);
      text-align: left;
      font-size: 13px;
      font-weight: 600;
      color: var(--text-secondary);
      border-bottom: 1px solid var(--border-color);
    }

    .evidence-row {
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .evidence-row:hover {
      background: var(--hover-bg);
    }

    .evidence-row.selected {
      background: var(--primary-bg-light);
    }

    .evidence-table td {
      padding: var(--table-cell-padding-block) var(--table-cell-padding-inline);
      font-size: 14px;
      color: var(--text-primary);
      border-bottom: 1px solid var(--border-color);
    }

    .file-cell {
    }

    .file-info {
      display: flex;
      align-items: center;
      gap: 12px;
    }

    .file-icon {
      width: 40px;
      height: 40px;
      display: flex;
      align-items: center;
      justify-content: center;
      background: var(--bg-tertiary);
      border-radius: 8px;
      color: var(--primary-color);
      flex-shrink: 0;
    }

    .file-icon[data-type="pdf"] {
      color: #ef4444;
      background: rgba(239, 68, 68, 0.1);
    }

    .file-icon[data-type="docx"] {
      color: #3b82f6;
      background: rgba(59, 130, 246, 0.1);
    }

    .file-icon[data-type="xlsx"] {
      color: #22c55e;
      background: rgba(34, 197, 94, 0.1);
    }

    .file-icon[data-type="jpg"] {
      color: #2f6bff;
      background: var(--color-primary-background);
    }

    .file-details {
      flex: 1;
    }

    .file-name {
      font-size: 14px;
      font-weight: 500;
      color: var(--text-primary);
      margin-bottom: 4px;
    }

    .file-meta {
      font-size: 12px;
      color: var(--text-secondary);
    }

    .clause-info {
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .clause-code {
      font-size: 12px;
      font-weight: 600;
      color: var(--color-primary-text);
      background: var(--color-primary-bg);
      border: 1px solid var(--color-primary-border);
      padding: 2px 6px;
      border-radius: 4px;
      display: inline-block;
      width: fit-content;
    }

    .clause-text {
      font-size: 12px;
      color: var(--text-secondary);
    }

    .chapter-badge {
      display: inline-block;
      padding: 4px 10px;
      background: var(--bg-tertiary);
      color: var(--text-primary);
      font-size: 12px;
      font-weight: 600;
      border-radius: 6px;
    }

    .status-badge {
      display: inline-block;
      padding: 4px 10px;
      border-radius: 6px;
      font-size: 12px;
      font-weight: 600;
    }

    .status-badge[data-status="approved"] {
      background: rgba(34, 197, 94, 0.1);
      color: #22c55e;
    }

    .status-badge[data-status="pendingReview"] {
      background: rgba(234, 179, 8, 0.1);
      color: #eab308;
    }

    .status-badge[data-status="rejected"] {
      background: rgba(239, 68, 68, 0.1);
      color: #ef4444;
    }

    .status-badge.large {
      padding: 8px 16px;
      font-size: 14px;
    }

    .action-buttons {
      display: flex;
      gap: 8px;
    }

    .icon-button {
      padding: 6px;
      background: transparent;
      border: none;
      color: var(--text-secondary);
      cursor: pointer;
      border-radius: 6px;
      transition: all 0.2s ease;
    }

    .icon-button:hover {
      background: var(--hover-bg);
      color: var(--text-primary);
    }

    .detail-panel {
      position: fixed;
      right: 0;
      top: 64px;
      width: 480px;
      height: calc(100vh - 64px);
      background: var(--card-bg);
      border-left: 1px solid var(--border-color);
      box-shadow: -4px 0 20px rgba(0, 0, 0, 0.1);
      z-index: 50;
      overflow-y: auto;
      animation: slideInRight 0.3s ease;
    }

    @keyframes slideInRight {
      from {
        transform: translateX(100%);
      }
      to {
        transform: translateX(0);
      }
    }

    .panel-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 20px;
      border-bottom: 1px solid var(--border-color);
      position: sticky;
      top: 0;
      background: var(--card-bg);
      z-index: 10;
    }

    .panel-title {
      font-size: 18px;
      font-weight: 600;
      color: var(--text-primary);
    }

    .close-button {
      padding: 8px;
      background: transparent;
      border: none;
      color: var(--text-secondary);
      cursor: pointer;
      border-radius: 6px;
      transition: all 0.2s ease;
    }

    .close-button:hover {
      background: var(--hover-bg);
      color: var(--text-primary);
    }

    .panel-tabs {
      display: flex;
      border-bottom: 1px solid var(--border-color);
      padding: 0 20px;
      position: sticky;
      top: 65px;
      background: var(--card-bg);
      z-index: 9;
      overflow-x: auto;
    }

    .panel-tab {
      padding: 14px 16px;
      background: transparent;
      border: none;
      border-bottom: 2px solid transparent;
      color: var(--text-secondary);
      font-size: 14px;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
      white-space: nowrap;
    }

    .panel-tab:hover {
      color: var(--text-primary);
    }

    .panel-tab.active {
      color: var(--primary-color);
      border-bottom-color: var(--primary-color);
    }

    .panel-content {
      padding: 20px;
    }

    .preview-section {
    }

    .preview-container {
      background: var(--bg-secondary);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      padding: 24px;
    }

    .preview-placeholder {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      min-height: 300px;
      background: var(--bg-tertiary);
      border-radius: 8px;
      margin-bottom: 16px;
    }

    .preview-placeholder svg {
      color: var(--text-secondary);
      margin-bottom: 12px;
    }

    .preview-placeholder p {
      font-size: 14px;
      color: var(--text-secondary);
      text-align: center;
      padding: 0 20px;
    }

    .preview-controls {
      display: flex;
      gap: 8px;
      justify-content: center;
      flex-wrap: wrap;
      margin-top: 12px;
    }

    .preview-state {
      padding: 24px;
      text-align: center;
      color: var(--text-secondary);
      font-size: 14px;
    }

    .preview-state.error {
      color: #f87171;
    }

    .preview-stage {
      overflow: auto;
      max-height: min(55vh, 420px);
      min-height: 200px;
      transform-origin: top left;
      border-radius: 8px;
      background: var(--bg-tertiary);
    }

    .preview-stage--zoom {
      margin-bottom: 8px;
    }

    .preview-iframe {
      width: 100%;
      min-height: 360px;
      border: none;
      display: block;
      background: #fff;
    }

    .preview-img {
      max-width: 100%;
      height: auto;
      display: block;
      margin: 0 auto;
    }

    .preview-text {
      margin: 0;
      padding: 16px;
      font-size: 12px;
      line-height: 1.5;
      white-space: pre-wrap;
      word-break: break-word;
      max-height: min(55vh, 420px);
      overflow: auto;
      font-family: ui-monospace, monospace;
      color: var(--text-primary);
    }

    .preview-unsupported {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 12px;
      min-height: 220px;
      padding: 24px;
      color: var(--text-secondary);
      text-align: center;
    }

    .preview-unsupported p {
      margin: 0;
      font-size: 14px;
      color: var(--text-primary);
    }

    .preview-unsupported-hint {
      font-size: 13px;
      color: var(--text-secondary);
    }

    .history-section--vertical {
      max-height: min(60vh, 480px);
      overflow-y: auto;
    }

    .history-order-hint {
      font-size: 12px;
      color: var(--text-secondary);
      margin: 0 0 12px;
    }

    .history-vtimeline {
      list-style: none;
      margin: 0;
      padding: 0 0 0 12px;
      border-left: 2px solid var(--border-color);
    }

    .history-vitem {
      position: relative;
      padding: 0 0 20px 16px;
    }

    .history-vitem:last-child {
      padding-bottom: 0;
    }

    .history-vdot {
      position: absolute;
      left: -21px;
      top: 4px;
      width: 12px;
      height: 12px;
      border-radius: 50%;
      background: var(--bg-tertiary);
      border: 2px solid var(--border-color);
      box-shadow: 0 0 0 3px var(--card-bg);
    }

    .history-vdot[data-kind='upload'],
    .history-vdot[data-kind='version'] {
      border-color: #3b82f6;
      background: #3b82f6;
    }

    .history-vdot[data-kind='approved'] {
      border-color: #22c55e;
      background: #22c55e;
    }

    .history-vdot[data-kind='rejected'] {
      border-color: #ef4444;
      background: #ef4444;
    }

    .history-vdot[data-kind='pending'] {
      border-color: #eab308;
      background: #eab308;
    }

    .history-vtitle {
      font-size: 14px;
      font-weight: 600;
      color: var(--text-primary);
      margin-bottom: 4px;
    }

    .history-vmeta {
      font-size: 13px;
      color: var(--text-secondary);
      line-height: 1.4;
    }

    .library-evidence-file-input {
      position: absolute;
      width: 0;
      height: 0;
      opacity: 0;
      overflow: hidden;
    }

    .upload-file-list {
      margin: 0 0 16px;
      padding-left: 18px;
      font-size: 13px;
      color: var(--text-secondary);
    }

    .modal-banner {
      margin-bottom: 16px;
    }

    .control-button {
      display: flex;
      align-items: center;
      gap: 6px;
      padding: 8px 14px;
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 6px;
      color: var(--text-primary);
      font-size: 13px;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .control-button:hover {
      background: var(--hover-bg);
    }

    .metadata-section {
    }

    .metadata-grid {
      display: grid;
      grid-template-columns: repeat(2, 1fr);
      gap: 20px;
    }

    .metadata-item {
      display: flex;
      flex-direction: column;
      gap: 6px;
    }

    .metadata-item.full-width {
      grid-column: 1 / -1;
    }

    .metadata-item label {
      font-size: 12px;
      color: var(--text-secondary);
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .metadata-value {
      font-size: 14px;
      color: var(--text-primary);
      font-weight: 500;
    }

    .context-section {
    }

    .context-card {
      background: var(--bg-secondary);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      padding: 20px;
    }

    .context-title {
      font-size: 16px;
      font-weight: 600;
      color: var(--text-primary);
      margin-bottom: 16px;
    }

    .context-grid {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .context-item {
      display: flex;
      flex-direction: column;
      gap: 6px;
    }

    .context-item label {
      font-size: 12px;
      color: var(--text-secondary);
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .context-value {
      font-size: 14px;
      color: var(--text-primary);
      font-weight: 500;
      display: flex;
      align-items: center;
      gap: 8px;
      flex-wrap: wrap;
    }

    .workflow-section {
    }

    .approval-status {
      display: flex;
      flex-direction: column;
      gap: 12px;
      padding: 20px;
      background: var(--bg-secondary);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      margin-bottom: 20px;
    }

    .approval-status label {
      font-size: 12px;
      color: var(--text-secondary);
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .review-info {
      padding: 20px;
      background: var(--bg-secondary);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      margin-bottom: 20px;
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .reviewer-details {
      display: flex;
      flex-direction: column;
      gap: 6px;
    }

    .reviewer-details label {
      font-size: 12px;
      color: var(--text-secondary);
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .reviewer-comment {
      display: flex;
      flex-direction: column;
      gap: 6px;
    }

    .reviewer-comment label {
      font-size: 12px;
      color: var(--text-secondary);
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .comment-box {
      padding: 12px;
      background: var(--bg-tertiary);
      border-radius: 8px;
      font-size: 14px;
      color: var(--text-primary);
      line-height: 1.5;
    }

    .workflow-actions {
      display: flex;
      gap: 12px;
      margin-bottom: 16px;
    }

    .review-queue-link {
      display: inline-flex;
      align-items: center;
      padding: 10px 16px;
      background: var(--primary-color, var(--color-primary));
      color: #fff;
      border-radius: 8px;
      font-size: 14px;
      font-weight: 600;
      text-decoration: none;
    }

    .review-queue-link:hover {
      filter: brightness(1.05);
    }

    .workflow-success {
      margin-top: 12px;
      color: #22c55e;
      font-size: 13px;
      font-weight: 500;
    }

    .approve-button {
      flex: 1;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 8px;
      padding: 12px 20px;
      background: #22c55e;
      color: white;
      border: none;
      border-radius: 8px;
      font-size: 14px;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .approve-button:hover {
      background: #16a34a;
      transform: translateY(-1px);
    }

    .reject-button {
      flex: 1;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 8px;
      padding: 12px 20px;
      background: #ef4444;
      color: white;
      border: none;
      border-radius: 8px;
      font-size: 14px;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .reject-button:hover {
      background: #dc2626;
      transform: translateY(-1px);
    }

    .comment-input {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .comment-input label {
      font-size: 12px;
      color: var(--text-secondary);
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .comment-input textarea {
      width: 100%;
      padding: 12px;
      background: var(--bg-secondary);
      border: 1px solid var(--border-color);
      border-radius: 8px;
      color: var(--text-primary);
      font-size: 14px;
      font-family: inherit;
      resize: vertical;
    }

    .history-section {
    }

    .version-control {
      margin-bottom: 32px;
    }

    .history-title {
      font-size: 16px;
      font-weight: 600;
      color: var(--text-primary);
      margin-bottom: 16px;
    }

    .version-list {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .version-item {
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 16px;
      background: var(--bg-secondary);
      border: 1px solid var(--border-color);
      border-radius: 10px;
    }

    .version-item.current {
      border-color: var(--primary-color);
      background: var(--primary-bg-light);
    }

    .version-badge {
      width: 40px;
      height: 40px;
      display: flex;
      align-items: center;
      justify-content: center;
      background: var(--bg-tertiary);
      border-radius: 8px;
      font-size: 13px;
      font-weight: 600;
      color: var(--text-primary);
      flex-shrink: 0;
    }

    .version-item.current .version-badge {
      background: var(--primary-color);
      color: white;
    }

    .version-info {
      flex: 1;
    }

    .version-label {
      font-size: 14px;
      font-weight: 500;
      color: var(--text-primary);
      margin-bottom: 4px;
    }

    .version-meta {
      font-size: 12px;
      color: var(--text-secondary);
    }

    .view-button {
      padding: 6px 12px;
      background: transparent;
      border: 1px solid var(--border-color);
      border-radius: 6px;
      color: var(--text-primary);
      font-size: 13px;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .view-button:hover {
      background: var(--hover-bg);
    }

    .activity-timeline {
    }

    .timeline-list {
      display: flex;
      flex-direction: column;
      gap: 16px;
    }

    .timeline-item {
      display: flex;
      gap: 12px;
    }

    .timeline-icon {
      width: 32px;
      height: 32px;
      display: flex;
      align-items: center;
      justify-content: center;
      border-radius: 8px;
      flex-shrink: 0;
    }

    .timeline-icon.approved {
      background: rgba(34, 197, 94, 0.1);
      color: #22c55e;
    }

    .timeline-icon.review {
      background: rgba(59, 130, 246, 0.1);
      color: #3b82f6;
    }

    .timeline-icon.upload {
      background: var(--color-primary-background);
      color: #2f6bff;
    }

    .timeline-content {
    }

    .timeline-title {
      font-size: 14px;
      font-weight: 500;
      color: var(--text-primary);
      margin-bottom: 4px;
    }

    .timeline-meta {
      font-size: 12px;
      color: var(--text-secondary);
    }

    .section-title {
      font-size: 18px;
      font-weight: 600;
      color: var(--text-primary);
      margin-bottom: 16px;
    }

    .analytics-section {
      margin-bottom: 32px;
    }

    .analytics-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: 20px;
    }

    .chart-card {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 12px;
      padding: 20px;
    }

    .chart-card--missing-evidence {
      max-height: 320px;
      display: flex;
      flex-direction: column;
      min-height: 0;
    }

    .chart-card--missing-evidence .chart-title {
      flex-shrink: 0;
    }

    .chart-card--missing-evidence .missing-list {
      flex: 1 1 auto;
      min-height: 0;
      overflow-y: auto;
      -webkit-overflow-scrolling: touch;
    }

    .chart-title {
      font-size: 15px;
      font-weight: 600;
      color: var(--text-primary);
      margin-bottom: 16px;
    }

    .bar-chart {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .bar-item {
      display: flex;
      flex-direction: column;
      gap: 6px;
    }

    .bar-label {
      font-size: 13px;
      color: var(--text-secondary);
      font-weight: 500;
    }

    .bar-wrapper {
      position: relative;
      display: flex;
      align-items: center;
      gap: 8px;
      height: 32px;
      background: var(--bg-tertiary);
      border-radius: 6px;
      overflow: hidden;
    }

    .bar-fill {
      height: 100%;
      background: var(--primary-color);
      border-radius: 6px;
      transition: width 0.6s ease;
    }

    .bar-value {
      position: absolute;
      right: 12px;
      font-size: 13px;
      font-weight: 600;
      color: var(--text-primary);
    }

    .donut-legend {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .legend-item {
      display: flex;
      align-items: center;
      gap: 12px;
    }

    .legend-color {
      width: 16px;
      height: 16px;
      border-radius: 4px;
      flex-shrink: 0;
    }

    .legend-label {
      flex: 1;
      font-size: 13px;
      color: var(--text-secondary);
    }

    .legend-value {
      font-size: 14px;
      font-weight: 600;
      color: var(--text-primary);
    }

    .missing-list {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .missing-item {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 12px;
      background: var(--bg-secondary);
      border: 1px solid var(--border-color);
      border-radius: 8px;
    }

    .missing-item[data-priority="high"] {
      border-left: 3px solid #ef4444;
    }

    .missing-item[data-priority="medium"] {
      border-left: 3px solid #f97316;
    }

    .missing-clause {
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .clause-name {
      font-size: 12px;
      color: var(--text-secondary);
    }

    .upload-link {
      padding: 6px 12px;
      background: var(--primary-color);
      color: white;
      border: none;
      border-radius: 6px;
      font-size: 12px;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .upload-link:hover {
      background: var(--primary-hover);
    }

    .activity-section {
      margin-bottom: 32px;
    }

    /* ---------- Evidence lifecycle timeline ---------- */
    .timeline {
      background: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 14px;
      padding: 20px 24px;
      display: flex;
      flex-direction: column;
      gap: 18px;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
    }

    .timeline-empty {
      padding: 32px 12px;
      text-align: center;
      color: var(--text-secondary);
      font-size: 13px;
    }

    .timeline-group {
      position: relative;
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .timeline-group + .timeline-group {
      padding-top: 14px;
      border-top: 1px dashed var(--border-color);
    }

    .timeline-group-header {
      display: flex;
      align-items: center;
      gap: 12px;
    }

    .oe-badge {
      display: inline-flex;
      align-items: center;
      padding: 4px 10px;
      border-radius: 999px;
      font-size: 11px;
      font-weight: 700;
      letter-spacing: 0.04em;
      text-transform: uppercase;
      border: 1px solid transparent;
      background: rgba(148, 163, 184, 0.12);
      color: #cbd5e1;
    }

    .oe-badge[data-variant="acc"],
    .oe-badge[data-variant="cop"],
    .oe-badge[data-variant="default"] {
      background: var(--color-primary-bg);
      color: var(--color-primary-text);
      border-color: var(--color-primary-border);
    }
    .oe-badge[data-variant="pcc"]     { background: rgba(34, 197, 94, 0.16);  color: #4ade80; border-color: rgba(34, 197, 94, 0.28); }
    .oe-badge[data-variant="moc"]     { background: rgba(249, 115, 22, 0.16); color: #fb923c; border-color: rgba(249, 115, 22, 0.28); }
    .oe-badge[data-variant="qms"]     { background: rgba(20, 184, 166, 0.16); color: #2dd4bf; border-color: rgba(20, 184, 166, 0.28); }

    .oe-name {
      font-size: 13px;
      font-weight: 600;
      color: var(--text-primary);
    }

    .timeline-row {
      display: grid;
      grid-template-columns: minmax(0, 1.35fr) 28px minmax(0, 1fr) 28px minmax(0, 1.3fr);
      align-items: center;
      gap: 8px;
      padding: 12px 14px;
      background: rgba(255, 255, 255, 0.02);
      border: 1px solid var(--border-color);
      border-radius: 10px;
      transition: border-color 0.2s ease, background 0.2s ease, box-shadow 0.2s ease;
    }

    .timeline-row:hover {
      border-color: var(--color-primary-border);
      background: var(--color-primary-bg);
    }

    .timeline-stage {
      position: relative;
      display: flex;
      align-items: center;
      gap: 10px;
      min-width: 0;
    }

    .timeline-stage .stage-node {
      width: 10px;
      height: 10px;
      border-radius: 50%;
      background: var(--bg-tertiary);
      border: 2px solid var(--border-color);
      flex-shrink: 0;
    }

    .timeline-stage .stage-body {
      display: flex;
      flex-direction: column;
      gap: 4px;
      min-width: 0;
      flex: 1;
    }

    .timeline-stage.stage-upload .stage-node  { background: var(--color-primary); border-color: color-mix(in srgb, var(--color-primary) 45%, transparent); }
    .timeline-stage.stage-update .stage-node  { background: #94a3b8; border-color: rgba(148, 163, 184, 0.45); }
    .timeline-stage.stage-review.is-latest .stage-node {
      box-shadow: var(--shadow-focus-ring-primary);
    }
    .timeline-row.is-pending .stage-review .stage-node { background: #eab308; border-color: rgba(234, 179, 8, 0.45); }
    .timeline-row:not(.is-pending) .stage-review .stage-node { background: #22c55e; border-color: rgba(34, 197, 94, 0.45); }

    .timeline-connector {
      height: 2px;
      width: 100%;
      background: linear-gradient(to right, var(--border-color), rgba(148, 163, 184, 0.35), var(--border-color));
      align-self: center;
    }

    .stage-meta {
      font-size: 11.5px;
      color: var(--text-secondary);
      line-height: 1.35;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .timeline-stage.stage-upload .file-card {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 6px 10px;
      border-radius: 8px;
      background: rgba(59, 130, 246, 0.07);
      border: 1px solid rgba(59, 130, 246, 0.18);
      transition: transform 0.18s ease, box-shadow 0.18s ease, border-color 0.18s ease;
      min-width: 0;
    }

    .timeline-stage.stage-upload .file-card:hover {
      transform: translateY(-1px);
      box-shadow: 0 4px 12px rgba(59, 130, 246, 0.15);
      border-color: rgba(59, 130, 246, 0.4);
    }

    .timeline-stage.stage-upload .file-icon {
      width: 30px;
      height: 30px;
      border-radius: 7px;
      display: flex;
      align-items: center;
      justify-content: center;
      background: rgba(59, 130, 246, 0.12);
      color: var(--color-primary);
      flex-shrink: 0;
    }

    .timeline-stage.stage-upload .file-meta {
      display: flex;
      flex-direction: column;
      gap: 2px;
      min-width: 0;
    }

    .timeline-stage.stage-upload .file-name {
      font-size: 13px;
      font-weight: 600;
      color: var(--text-primary);
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .update-pill {
      display: inline-flex;
      align-items: center;
      padding: 4px 10px;
      border-radius: 999px;
      font-size: 12px;
      font-weight: 600;
      background: rgba(148, 163, 184, 0.14);
      color: #cbd5e1;
      border: 1px solid rgba(148, 163, 184, 0.2);
      width: fit-content;
    }

    .timeline-stage.stage-review.is-latest .status-badge {
      box-shadow: var(--shadow-focus-ring-primary);
    }

    @media (max-width: 900px) {
      .timeline-row {
        grid-template-columns: 1fr;
        gap: 12px;
      }

      .timeline-connector {
        display: none;
      }
    }
    /* ---------- /Evidence lifecycle timeline ---------- */

    .modal-overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(0, 0, 0, 0.5);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 100;
      animation: fadeIn 0.2s ease;
    }

    @keyframes fadeIn {
      from {
        opacity: 0;
      }
      to {
        opacity: 1;
      }
    }

    .modal-container {
      width: 90%;
      max-width: 700px;
      max-height: 85vh;
      min-height: 0;
      background: var(--card-bg);
      border-radius: 16px;
      box-shadow: 0 20px 50px rgba(0, 0, 0, 0.3);
      animation: slideUp 0.3s ease;
      display: flex;
      flex-direction: column;
      overflow: hidden;
    }

    @keyframes slideUp {
      from {
        opacity: 0;
        transform: translateY(20px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .modal-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 24px;
      border-bottom: 1px solid var(--border-color);
      flex-shrink: 0;
    }

    .modal-title {
      font-size: 20px;
      font-weight: 600;
      color: var(--text-primary);
    }

    .modal-content {
      padding: 24px;
      overflow-y: auto;
      flex: 1;
      min-height: 0;
    }

    .upload-area {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 48px 24px;
      background: var(--bg-secondary);
      border: 2px dashed var(--border-color);
      border-radius: 12px;
      margin-bottom: 24px;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .upload-area:hover {
      border-color: var(--primary-color);
      background: var(--primary-bg-light);
    }

    .upload-area svg {
      color: var(--primary-color);
      margin-bottom: 16px;
    }

    .upload-text {
      font-size: 15px;
      font-weight: 500;
      color: var(--text-primary);
      margin-bottom: 8px;
    }

    .upload-hint {
      font-size: 13px;
      color: var(--text-secondary);
      margin-bottom: 16px;
      text-align: center;
    }

    .browse-button {
      padding: 10px 20px;
      background: var(--primary-color);
      color: white;
      border: none;
      border-radius: 8px;
      font-size: 14px;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .browse-button:hover {
      background: var(--primary-hover);
    }

    .upload-form {
      display: grid;
      grid-template-columns: repeat(2, 1fr);
      gap: 16px;
    }

    .upload-form--stacked {
      grid-template-columns: 1fr;
    }

    .form-group {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .form-group.full-width {
      grid-column: 1 / -1;
    }

    .form-group label {
      font-size: 13px;
      font-weight: 600;
      color: var(--text-primary);
    }

    .form-group select,
    .form-group textarea {
      padding: 10px 12px;
      background: var(--bg-secondary);
      border: 1px solid var(--border-color);
      border-radius: 8px;
      color: var(--text-primary);
      font-size: 14px;
      font-family: inherit;
    }

    .form-group select:focus,
    .form-group textarea:focus {
      outline: none;
      border-color: var(--primary-color);
    }

    .modal-footer {
      display: flex;
      justify-content: flex-end;
      gap: 12px;
      padding: 20px 24px;
      border-top: 1px solid var(--border-color);
      flex-shrink: 0;
    }

    .cancel-button {
      padding: 10px 20px;
      background: transparent;
      border: 1px solid var(--border-color);
      border-radius: 8px;
      color: var(--text-primary);
      font-size: 14px;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .cancel-button:hover {
      background: var(--hover-bg);
    }

    .submit-button {
      padding: 10px 20px;
      background: var(--primary-color);
      color: white;
      border: none;
      border-radius: 8px;
      font-size: 14px;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .submit-button:hover {
      background: var(--primary-hover);
    }

    @media (max-width: 1200px) {
      .detail-panel {
        width: 400px;
      }
    }

    @media (max-width: 768px) {
      .evidence-page {
        padding: var(--page-padding-block-start) var(--page-padding-inline) var(--page-padding-block-end);
      }

      .page-header {
        flex-direction: column;
        gap: 16px;
      }

      .page-header-actions {
        width: 100%;
        flex-direction: column;
        align-items: stretch;
      }

      .btn-header-secondary,
      .upload-button {
        width: 100%;
        justify-content: center;
      }

      .summary-cards {
        grid-template-columns: 1fr;
      }

      .filters-section {
        flex-direction: column;
      }

      .filter-controls {
        width: 100%;
      }

      .search-box {
        width: 100%;
        min-width: 100%;
      }

      .detail-panel {
        width: 100%;
        left: 0;
      }

      .analytics-grid {
        grid-template-columns: 1fr;
      }

      .upload-form {
        grid-template-columns: 1fr;
      }

      .metadata-grid {
        grid-template-columns: 1fr;
      }
    }
  `],
})
export class EvidenceComponent implements OnInit, OnDestroy {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly evidenceApi = inject(EvidenceApiService);
  private readonly complianceContext = inject(ComplianceContextService);
  private readonly organizationService = inject(OrganizationService);
  private readonly membershipScope = inject(MembershipScopeService);
  private readonly commonLookup = inject(CommonLookupService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly snackBar = inject(MatSnackBar);
  private readonly journeyData = inject(JourneyDataService);
  private readonly destroy$ = new Subject<void>();
  private readonly searchInput$ = new Subject<string>();
  /** Master department id → display name (from /common/master-departments). */
  private departmentNameById = new Map<string, string>();

  filterChapter = '';
  filterStandard = '';
  private filterObjectiveElementId = '';
  filterStatus = '';
  filterDepartment = '';
  filterFileType = '';
  searchQuery = '';
  uploadObjectiveElementId = '';
  uploadDescription = '';

  selectedRecord = signal<EvidenceRecord | null>(null);
  activeTab = signal<string>('preview');
  showUploadModal = signal(false);

  readonly libraryDetail = signal<LibraryEvidenceDetail | null>(null);
  readonly libraryDetailLoading = signal(false);
  readonly libraryDetailError = signal<string | null>(null);
  readonly previewMode = signal<PreviewDisplayMode>('idle');
  readonly previewSafeResourceUrl = signal<SafeResourceUrl | null>(null);
  readonly previewTextContent = signal('');
  readonly previewZoom = signal(1);
  readonly previewDownloadInFlight = signal(false);

  readonly reviewQueuePath = EVIDENCE_REVIEW_QUEUE_PATH;
  readonly uploadFilesPending = signal<File[]>([]);
  readonly uploadValidationError = signal<string | null>(null);
  readonly uploadInFlight = signal(false);
  readonly uploadJourneyId = signal<string | null>(null);
  readonly uploadContextLoading = signal(false);

  readonly pageLoading = signal(false);
  readonly listLoading = signal(false);
  readonly pageError = signal<string | null>(null);
  readonly listError = signal<string | null>(null);

  private pendingSelectedEvidenceId: string | null = null;
  private departmentsReady = false;

  readonly currentPage = signal(1);
  readonly pageSize = signal(20);
  readonly totalListCount = signal(0);
  readonly sortBy = signal('uploadDate');
  readonly sortOrder = signal<'asc' | 'desc'>('desc');

  readonly chapterFilterOptions = signal<{ id: string; label: string }[]>([]);
  /** Standards with evidence in this framework; options narrow when a chapter is selected. */
  private readonly standardFilterSource = signal<{ id: string; label: string; chapterId: string | null }[]>([]);
  readonly departmentFilterOptions = signal<{ id: string; label: string }[]>([]);

  readonly detailTabs = signal([
    { id: 'preview', label: 'Preview' },
    { id: 'metadata', label: 'Metadata' },
    { id: 'context', label: 'Context' },
    { id: 'workflow', label: 'Workflow' },
    { id: 'history', label: 'History' },
  ]);

  readonly summaryCards = signal<SummaryCard[]>([]);

  readonly pagedRows = signal<EvidenceRecord[]>([]);

  readonly chapterData = signal<ChartData[]>([]);
  readonly departmentData = signal<ChartData[]>([]);
  readonly approvalData = signal<ChartData[]>([]);
  readonly missingEvidence = signal<MissingEvidence[]>([]);
  readonly activityEvents = signal<ActivityEvent[]>([]);
  readonly timelineGroups = signal<TimelineGroup[]>([]);

  readonly maxChapterValue = computed(() => {
    const vals = this.chapterData().map(d => d.value);
    return vals.length ? Math.max(...vals) : 1;
  });

  readonly maxDepartmentValue = computed(() => {
    const vals = this.departmentData().map(d => d.value);
    return vals.length ? Math.max(...vals) : 1;
  });

  readonly totalPages = computed(() => {
    const n = this.totalListCount();
    const ps = this.pageSize();
    return Math.max(1, Math.ceil(n / ps));
  });

  private lastPreviewObjectUrl: string | null = null;

  @ViewChild('previewStage') private previewStage?: ElementRef<HTMLElement>;
  @ViewChild('libraryEvidenceFileInput') private libraryEvidenceFileInput?: ElementRef<HTMLInputElement>;

  readonly historyTimelineEntries = computed(() => {
    const d = this.libraryDetail();
    if (!d) {
      return [] as HistoryTimelineEntry[];
    }
    return this.buildHistoryTimeline(d);
  });

  ngOnInit(): void {
    this.applyRouteScopedFilters();

    this.searchInput$
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(() => {
        this.currentPage.set(1);
        this.loadEvidenceList();
      });

    forkJoin({
      org: this.organizationService.loadOrganizationDetails().pipe(catchError(() => of(null))),
      departments: this.commonLookup.loadDepartments().pipe(catchError(() => of([] as DepartmentLookup[]))),
    })
      .pipe(takeUntil(this.destroy$))
      .subscribe(({ departments }) => {
        this.departmentNameById = new Map((departments ?? []).map((d) => [d.id, d.name] as const));
        this.departmentsReady = true;
        this.applyDepartmentHeadScopeFilter();
        this.tryLoadDashboardBundle();
      });

    combineLatest([
      this.complianceContext.hospitalId$,
      this.complianceContext.frameworkId$,
    ])
      .pipe(
        debounceTime(50),
        distinctUntilChanged((prev, curr) => prev[0] === curr[0] && prev[1] === curr[1]),
        takeUntil(this.destroy$),
      )
      .subscribe(() => {
        if (this.departmentsReady) {
          this.tryLoadDashboardBundle();
        }
      });
  }

  private tryLoadDashboardBundle(): void {
    const hospitalId = this.complianceContext.getHospitalId();
    const frameworkId = this.complianceContext.getFrameworkId();
    if (!hospitalId || !frameworkId) {
      return;
    }
    this.loadDashboardBundle();
  }

  ngOnDestroy(): void {
    this.revokePreviewBlob();
    this.destroy$.next();
    this.destroy$.complete();
  }

  onSearchChange(value: string): void {
    this.searchQuery = value;
    this.filterObjectiveElementId = '';
    this.searchInput$.next(value);
  }

  onFilterChange(): void {
    this.filterObjectiveElementId = '';
    this.currentPage.set(1);
    this.loadEvidenceList();
  }

  onPageSizeChange(size: number): void {
    this.pageSize.set(size);
    this.currentPage.set(1);
    this.loadEvidenceList();
  }

  /** Chapter change clears standard filter (standards are chapter-scoped) and reloads the list. */
  onChapterFilterChange(_value: string): void {
    this.filterObjectiveElementId = '';
    this.filterStandard = '';
    this.currentPage.set(1);
    this.loadEvidenceList();
  }

  /** Department Head: pin evidence list to membership department when API supports departmentId. */
  private applyDepartmentHeadScopeFilter(): void {
    const qpDept = (this.route.snapshot.queryParamMap.get('departmentId') ?? '').trim();
    if (qpDept) {
      this.filterDepartment = qpDept;
      return;
    }
    if (this.filterDepartment) return;
    if (!this.membershipScope.isDepartmentHeadExperience()) return;
    const departmentId = this.membershipScope.getDepartmentId();
    if (departmentId) {
      this.filterDepartment = departmentId;
    }
  }

  private applyRouteScopedFilters(): void {
    const qp = this.route.snapshot.queryParamMap;
    const source = (qp.get('source') ?? '').trim().toLowerCase();
    const objectiveElementId = (qp.get('objectiveElementId') ?? '').trim();
    const objectiveElementCode = (qp.get('objectiveElementCode') ?? '').trim();
    const chapterId = (qp.get('chapterId') ?? '').trim();
    const standardId = (qp.get('standardId') ?? '').trim();
    const departmentId = (qp.get('departmentId') ?? '').trim();
    if (departmentId) {
      this.filterDepartment = departmentId;
    }

    if (source !== 'oe-wizard' && source !== 'journey-execution') {
      return;
    }
    if (!objectiveElementId) {
      return;
    }

    this.filterObjectiveElementId = objectiveElementId;
    this.filterChapter = chapterId;
    this.filterStandard = standardId;
    this.searchQuery = objectiveElementCode;
    this.currentPage.set(1);

    // Keep scoped filters in UI, but clear URL so browser refresh can open full library defaults.
    void this.router.navigate([], {
      relativeTo: this.route,
      replaceUrl: true,
      queryParams: {},
    });
  }

  /** Standards shown in the dropdown: all framework standards with evidence, or only those under the selected chapter. */
  standardFilterOptionsForUi(): { id: string; label: string }[] {
    const all = this.standardFilterSource();
    const ch = this.filterChapter;
    const rows = ch ? all.filter(x => x.chapterId === ch) : all;
    return rows.map(({ id, label }) => ({ id, label }));
  }

  goToPage(page: number): void {
    const tp = this.totalPages();
    const next = Math.min(Math.max(1, page), tp);
    this.currentPage.set(next);
    this.loadEvidenceList();
  }

  private loadDashboardBundle(): void {
    const frameworkId = this.complianceContext.getFrameworkId();
    if (!frameworkId) {
      this.pageError.set('Select a compliance framework in context to load evidence.');
      this.summaryCards.set(this.emptySummaryCards());
      return;
    }

    this.pageError.set(null);
    this.pageLoading.set(true);
    this.evidenceApi
      .getDashboard(frameworkId)
      .pipe(
        takeUntil(this.destroy$),
        catchError(err => {
          console.error(err);
          this.pageError.set('Unable to load evidence dashboard.');
          return of(null);
        }),
        finalize(() => this.pageLoading.set(false)),
      )
      .subscribe(dash => {
        if (!dash) {
          this.summaryCards.set(this.emptySummaryCards());
          return;
        }
        this.applyDashboard(dash);
        this.loadEvidenceList();
      });
  }

  private applyDashboard(dash: EvidenceDashboard): void {
    const s = dash.summary;
    this.summaryCards.set([
      {
        label: 'Total Evidence',
        value: s.total,
        color: 'blue',
        icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M13 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V9z" stroke-width="2"/><path d="M13 2v7h7" stroke-width="2"/></svg>',
      },
      {
        label: 'Approved',
        value: s.approved,
        color: 'green',
        icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M22 11.08V12a10 10 0 11-5.93-9.14" stroke-width="2"/><path d="M22 4L12 14.01l-3-3" stroke-width="2"/></svg>',
      },
      {
        label: 'Pending Review',
        value: s.pendingReview,
        color: 'yellow',
        icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><circle cx="12" cy="12" r="10" stroke-width="2"/><path d="M12 6v6l4 2" stroke-width="2"/></svg>',
      },
      {
        label: 'Rejected',
        value: s.rejected,
        color: 'red',
        icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><circle cx="12" cy="12" r="10" stroke-width="2"/><path d="M15 9l-6 6M9 9l6 6" stroke-width="2"/></svg>',
      },
      {
        label: 'Missing Evidence',
        value: s.missing,
        color: 'orange',
        icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z" stroke-width="2"/></svg>',
      },
    ]);

    const palette = ['#3b82f6', '#0d9488', '#10b981', '#f59e0b', '#ef4444', '#06b6d4'];
    this.chapterData.set(
      dash.analytics.byChapter.map((c, i) => ({
        label: c.name,
        value: c.count,
        color: palette[i % palette.length],
      })),
    );

    this.departmentData.set(
      dash.analytics.byDepartment.map((c, i) => ({
        label: c.name,
        value: c.count,
        color: palette[i % palette.length],
      })),
    );

    const ap = dash.analytics.approvalStatus;
    this.approvalData.set([
      { label: 'Approved', value: ap.approved, color: '#22c55e' },
      { label: 'Pending Review', value: ap.pendingReview, color: '#eab308' },
      { label: 'Rejected', value: ap.rejected, color: '#ef4444' },
    ]);

    this.chapterFilterOptions.set(
      dash.analytics.byChapter
        .filter(c => c.chapterId)
        .map(c => ({ id: c.chapterId as string, label: c.name })),
    );

    this.standardFilterSource.set(
      (dash.analytics.byStandard ?? [])
        .filter(s => s.standardId)
        .map(s => ({
          id: s.standardId as string,
          label: s.name,
          chapterId: s.chapterId ?? null,
        })),
    );

    const masterDeptOpts = this.departmentOptionsFromMaster();
    this.departmentFilterOptions.set(
      masterDeptOpts.length > 0
        ? masterDeptOpts
        : dash.analytics.byDepartment
            .filter(d => d.departmentId)
            .map(d => ({ id: d.departmentId as string, label: d.name })),
    );

    this.missingEvidence.set(
      dash.analytics.missingEvidence.map(m => ({
        objectiveElementId: m.objectiveElementId,
        objectiveElementCode: m.code,
        objectiveElementName: m.name,
        chapterLabel: m.chapter,
        priority: 'medium' as const,
      })),
    );

    const colors: Record<string, string> = {
      Uploaded: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4M17 8l-5-5-5 5M12 3v12" stroke-width="2"/></svg>',
      Approved: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M22 11.08V12a10 10 0 11-5.93-9.14" stroke-width="2"/><path d="M22 4L12 14.01l-3-3" stroke-width="2"/></svg>',
      Rejected: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor"><circle cx="12" cy="12" r="10" stroke-width="2"/><path d="M15 9l-6 6M9 9l6 6" stroke-width="2"/></svg>',
    };

    this.activityEvents.set(
      dash.recentActivity.map((a, i) => ({
        id: `${a.timestamp}-${i}`,
        type: 'upload',
        title: `${a.action}: ${a.entity}`,
        user: this.formatUploadedBy(a.user),
        timestamp: this.formatDate(a.timestamp),
        icon: colors[a.action] ?? colors['Uploaded'],
      })),
    );

    this.timelineGroups.set(this.buildTimelineGroups(dash.recentTimeline ?? []));
  }

  /**
   * Groups backend-sorted timeline items by Objective Element, preserving server order.
   * No client-side sorting; backend already returns the 10 most recent rows DESC by MostRecentAt.
   */
  private buildTimelineGroups(items: EvidenceTimelineItem[]): TimelineGroup[] {
    const groups = new Map<string, TimelineGroup>();

    for (const it of items) {
      const oeId = it.objectiveElementId;
      if (!oeId) continue;

      let group = groups.get(oeId);
      if (!group) {
        group = {
          objectiveElementId: oeId,
          code: it.objectiveElementCode || '',
          name: it.objectiveElementName || '',
          badgeVariant: this.resolveOeBadgeVariant(it.objectiveElementCode, it.chapterCode),
          rows: [],
        };
        groups.set(oeId, group);
      }

      const status: TimelineStatus =
        it.status === 'approved' || it.status === 'rejected' ? it.status : 'pendingReview';

      group.rows.push({
        evidenceId: it.evidenceId,
        fileName: it.fileName || 'Evidence',
        fileType: it.fileType || 'unknown',
        uploadedByName: this.resolveTimelineUserName(it.uploadedBy),
        uploadedAt: this.formatDate(it.uploadedAt),
        uploadedAtIso: it.uploadedAt ?? '',
        updatedByName: this.resolveTimelineUserName(it.updatedBy || it.uploadedBy),
        updatedAt: this.formatDate(it.updatedAt),
        updatedAtIso: it.updatedAt ?? '',
        hasUpdate: !!it.hasUpdate,
        status,
        reviewerName: it.verifiedBy ? this.resolveTimelineUserName(it.verifiedBy) : null,
        verifiedAt: it.verifiedAt ? this.formatDate(it.verifiedAt) : null,
        verifiedAtIso: it.verifiedAt ?? null,
      });
    }

    return Array.from(groups.values());
  }

  /**
   * Chapter/OE-code prefix -> badge color variant.
   * Falls back to the OE code's prefix if the explicit chapterCode is missing.
   */
  private resolveOeBadgeVariant(oeCode?: string | null, chapterCode?: string | null): OeBadgeVariant {
    const pick = (chapterCode && chapterCode.trim()) || this.extractCodePrefix(oeCode);
    switch (pick.toLowerCase()) {
      case 'acc':
        return 'acc';
      case 'cop':
        return 'cop';
      case 'pcc':
        return 'pcc';
      case 'moc':
        return 'moc';
      case 'qms':
        return 'qms';
      default:
        return 'default';
    }
  }

  private extractCodePrefix(code?: string | null): string {
    if (!code) return '';
    const trimmed = code.trim();
    if (!trimmed) return '';
    const head = trimmed.split(/[\s.\-_]/, 1)[0] ?? '';
    return head;
  }

  /** Timeline strictly forbids raw GUIDs; fall back to "Unknown user" instead of echoing an id. */
  private resolveTimelineUserName(raw: string | null | undefined): string {
    const s = (raw ?? '').trim();
    if (!s) return 'Unknown user';
    const resolved = this.formatUploadedBy(s);
    if (!resolved || resolved === '—' || resolved === s) {
      const isGuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(s);
      if (isGuid) return 'Unknown user';
    }
    return resolved;
  }

  private emptySummaryCards(): SummaryCard[] {
    return [
      {
        label: 'Total Evidence',
        value: 0,
        color: 'blue',
        icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M13 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V9z" stroke-width="2"/><path d="M13 2v7h7" stroke-width="2"/></svg>',
      },
      {
        label: 'Approved',
        value: 0,
        color: 'green',
        icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M22 11.08V12a10 10 0 11-5.93-9.14" stroke-width="2"/><path d="M22 4L12 14.01l-3-3" stroke-width="2"/></svg>',
      },
      {
        label: 'Pending Review',
        value: 0,
        color: 'yellow',
        icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><circle cx="12" cy="12" r="10" stroke-width="2"/><path d="M12 6v6l4 2" stroke-width="2"/></svg>',
      },
      {
        label: 'Rejected',
        value: 0,
        color: 'red',
        icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><circle cx="12" cy="12" r="10" stroke-width="2"/><path d="M15 9l-6 6M9 9l6 6" stroke-width="2"/></svg>',
      },
      {
        label: 'Missing Evidence',
        value: 0,
        color: 'orange',
        icon: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor"><path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z" stroke-width="2"/></svg>',
      },
    ];
  }

  private loadEvidenceList(): void {
    const frameworkId = this.complianceContext.getFrameworkId();
    if (!frameworkId) {
      this.pagedRows.set([]);
      return;
    }

    this.listError.set(null);
    this.listLoading.set(true);
    this.evidenceApi
      .getEvidenceList({
        frameworkId,
        objectiveElementId: this.filterObjectiveElementId || undefined,
        chapterId: this.filterChapter || undefined,
        standardId: this.filterStandard || undefined,
        status: this.filterStatus || undefined,
        departmentId: this.filterDepartment || undefined,
        fileType: this.filterFileType || undefined,
        search: this.searchQuery.trim() || undefined,
        page: this.currentPage(),
        pageSize: this.pageSize(),
        sortBy: this.sortBy(),
        sortOrder: this.sortOrder(),
      })
      .pipe(
        takeUntil(this.destroy$),
        catchError(err => {
          console.error(err);
          this.listError.set('Unable to load evidence list.');
          return of(null);
        }),
        finalize(() => this.listLoading.set(false)),
      )
      .subscribe(page => {
        if (!page) {
          this.pagedRows.set([]);
          return;
        }
        this.totalListCount.set(page.totalCount);
        const mapped = page.items.map(i => this.mapListItem(i));
        this.pagedRows.set(mapped);

        const selectedEvidenceId = this.pendingSelectedEvidenceId ?? this.selectedRecord()?.id ?? null;
        if (selectedEvidenceId) {
          const refreshed = mapped.find(x => x.id === selectedEvidenceId || x.id === this.selectedRecord()?.id);
          if (refreshed) {
            this.selectedRecord.set(refreshed);
          }
        }
        this.pendingSelectedEvidenceId = null;
      });
  }

  private mapListItem(i: EvidenceListItem): EvidenceRecord {
    return {
      id: i.id,
      evidenceId: i.evidenceId,
      fileName: i.fileName,
      fileType: i.fileType,
      fileSize: this.formatFileSize(i.fileSizeBytes),
      objectiveElementName: i.objectiveElementName,
      objectiveElementCode: i.objectiveElementCode,
      objectiveElementId: i.objectiveElementId ?? '',
      chapter: i.chapterName,
      chapterCode: i.chapterCode,
      department: this.resolveDepartmentDisplay(i.department, i.departmentId),
      uploadedBy: this.formatUploadedBy(i.uploadedBy),
      uploadDate: i.uploadDate,
      status: i.status,
      isApproved: i.isApproved ?? null,
      rowVersion: i.rowVersion ?? null,
      reviewerName: i.verifiedBy ? this.formatUploadedBy(i.verifiedBy) : undefined,
      reviewDate: i.verifiedAt ?? undefined,
      reviewComment: i.reviewComment ?? undefined,
      linkedTask: i.linkedTask ?? '—',
      description: i.description ?? '',
      version: i.version,
    };
  }

  private departmentOptionsFromMaster(): { id: string; label: string }[] {
    return Array.from(this.departmentNameById.entries()).map(([id, label]) => ({ id, label }));
  }

  /** Maps API file extension bucket to icon variant in the File column. */
  fileIconCategory(fileType: string): string {
    const t = (fileType || '').toLowerCase();
    if (t === 'jpeg') return 'image';
    if (['jpg', 'png', 'gif', 'webp', 'svg', 'bmp'].includes(t)) return 'image';
    if (t === 'pdf') return 'pdf';
    if (t === 'docx' || t === 'doc') return 'docx';
    if (t === 'xlsx' || t === 'xls') return 'xlsx';
    if (t === 'txt' || t === 'md') return 'text';
    if (t === 'csv') return 'csv';
    return 'generic';
  }

  fileTypeLabel(fileType: string): string {
    const t = (fileType || '').trim();
    if (!t || t.toLowerCase() === 'unknown') return 'FILE';
    return t.toUpperCase();
  }

  /**
   * `uploadedBy` from API is often a user id (GUID). Resolve via organization roster when possible.
   */
  formatUploadedBy(raw: string | null | undefined): string {
    const s = raw?.trim() ?? '';
    if (!s) return '—';
    const guidLike = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(s);
    if (guidLike) {
      const m = this.organizationService.getMemberByUserId(s);
      if (m) {
        const parts = [m.firstName, m.lastName].filter(Boolean) as string[];
        if (parts.length) return parts.join(' ');
        if (m.email?.trim()) return m.email.trim();
      }
      return s;
    }
    return s;
  }

  private resolveDepartmentDisplay(fallback: string, departmentId?: string | null): string {
    const trimmed = fallback?.trim();
    if (trimmed) return trimmed;
    const id = departmentId?.trim();
    if (id && this.departmentNameById.has(id)) {
      return this.departmentNameById.get(id)!;
    }
    return '';
  }

  private formatFileSize(bytes: number): string {
    if (bytes <= 0) {
      return '—';
    }
    const kb = bytes / 1024;
    if (kb < 1024) {
      return `${kb.toFixed(1)} KB`;
    }
    return `${(kb / 1024).toFixed(1)} MB`;
  }

  selectRecord(record: EvidenceRecord): void {
    this.revokePreviewBlob();
    this.libraryDetail.set(null);
    this.libraryDetailError.set(null);
    this.previewZoom.set(1);
    this.selectedRecord.set(record);
    this.activeTab.set('preview');
    this.loadLibraryDetailAndPreview();
  }

  closeDetailPanel(): void {
    this.revokePreviewBlob();
    this.libraryDetail.set(null);
    this.libraryDetailError.set(null);
    this.selectedRecord.set(null);
  }

  setActiveTab(tabId: string): void {
    this.activeTab.set(tabId);
  }

  goToEvidenceBinder(): void {
    void this.router.navigate(['/app/evidence/binder']);
  }

  openUploadModal(): void {
    this.uploadValidationError.set(null);
    this.uploadFilesPending.set([]);
    this.uploadObjectiveElementId = '';
    this.uploadDescription = '';
    this.showUploadModal.set(true);
    const fid = this.complianceContext.getFrameworkId();
    if (!fid) {
      this.uploadJourneyId.set(null);
      this.uploadContextLoading.set(false);
      this.snackBar.open('Select a compliance framework in context first.', 'Dismiss', { duration: 4000 });
      return;
    }
    this.uploadContextLoading.set(true);
    this.evidenceApi
      .getUploadContext(fid)
      .pipe(
        takeUntil(this.destroy$),
        catchError(err => {
          console.error(err);
          this.snackBar.open('Unable to resolve upload context.', 'Dismiss', { duration: 4000 });
          return of({ journeyId: null });
        }),
        finalize(() => this.uploadContextLoading.set(false)),
      )
      .subscribe(ctx => {
        this.uploadJourneyId.set(ctx.journeyId?.trim() ? ctx.journeyId : null);
      });
  }

  closeUploadModal(): void {
    this.showUploadModal.set(false);
    this.uploadFilesPending.set([]);
    this.uploadValidationError.set(null);
  }

  previewEvidence(event: Event, record: EvidenceRecord): void {
    event.stopPropagation();
    this.selectRecord(record);
    this.activeTab.set('preview');
  }

  downloadEvidence(event: Event, record: EvidenceRecord): void {
    event.stopPropagation();
    this.runEvidenceDownload(record.evidenceId, record.fileName);
  }

  downloadSelectedEvidence(): void {
    const r = this.selectedRecord();
    if (!r) {
      return;
    }
    this.runEvidenceDownload(r.evidenceId, r.fileName);
  }

  viewHistory(event: Event, record: EvidenceRecord): void {
    event.stopPropagation();
    this.selectRecord(record);
    this.activeTab.set('history');
  }

  uploadForObjectiveElement(item: MissingEvidence): void {
    this.uploadObjectiveElementId = item.objectiveElementId;
    this.openUploadModal();
  }

  canOpenReviewQueue(status: string | undefined): boolean {
    return canReviewEvidenceInQueue(status);
  }

  reviewQueueParams(record: EvidenceRecord): Record<string, string> {
    return evidenceReviewQueueQuery({
      evidenceId: record.evidenceId,
      objectiveElementId: record.objectiveElementId,
      objectiveElementCode: record.objectiveElementCode,
    });
  }

  private loadLibraryDetailAndPreview(): void {
    const rec = this.selectedRecord();
    if (!rec?.evidenceId) {
      return;
    }
    this.revokePreviewBlob();
    this.libraryDetailError.set(null);
    this.libraryDetailLoading.set(true);
    this.evidenceApi
      .getLibraryEvidenceDetail(rec.evidenceId)
      .pipe(
        takeUntil(this.destroy$),
        switchMap(detail => {
          this.libraryDetail.set(detail);
          return this.evidenceApi.downloadLibraryEvidenceFile(rec.evidenceId);
        }),
        catchError(err => {
          console.error(err);
          this.libraryDetailError.set('Unable to load evidence detail.');
          this.previewMode.set('error');
          return EMPTY;
        }),
        finalize(() => this.libraryDetailLoading.set(false)),
      )
      .subscribe(res => {
        if (res) {
          this.applyPreviewBlob(rec.fileName, res.blob, res.contentType);
        }
      });
  }

  private revokePreviewBlob(): void {
    if (this.lastPreviewObjectUrl) {
      URL.revokeObjectURL(this.lastPreviewObjectUrl);
      this.lastPreviewObjectUrl = null;
    }
    this.previewSafeResourceUrl.set(null);
    this.previewTextContent.set('');
    this.previewMode.set('idle');
  }

  private applyPreviewBlob(fileName: string, blob: Blob, contentType: string | null): void {
    const ct = (contentType || blob.type || '').toLowerCase();
    const ext = fileName.includes('.') ? fileName.split('.').pop()!.toLowerCase() : '';

    if (ct.includes('pdf') || ext === 'pdf') {
      const raw = URL.createObjectURL(blob);
      this.lastPreviewObjectUrl = raw;
      this.previewSafeResourceUrl.set(this.sanitizer.bypassSecurityTrustResourceUrl(raw));
      this.previewMode.set('pdf');
      return;
    }

    if (ct.startsWith('image/') || ['png', 'jpg', 'jpeg', 'gif', 'webp', 'svg', 'bmp'].includes(ext)) {
      const raw = URL.createObjectURL(blob);
      this.lastPreviewObjectUrl = raw;
      this.previewSafeResourceUrl.set(this.sanitizer.bypassSecurityTrustResourceUrl(raw));
      this.previewMode.set('image');
      return;
    }

    const textLike =
      ct.startsWith('text/') || ['txt', 'md', 'csv', 'tex', 'json', 'xml', 'log', 'ts', 'js'].includes(ext);
    if (textLike && blob.size <= 512 * 1024) {
      blob
        .text()
        .then(text => {
          this.previewTextContent.set(text);
          this.previewMode.set('text');
        })
        .catch(() => this.previewMode.set('unsupported'));
      return;
    }
    if (textLike && blob.size > 512 * 1024) {
      this.previewTextContent.set('(File too large to preview — use Download.)');
      this.previewMode.set('text');
      return;
    }

    this.previewMode.set('unsupported');
  }

  zoomInPreview(): void {
    const next = Math.min(2, Math.round((this.previewZoom() + 0.25) * 100) / 100);
    this.previewZoom.set(next);
  }

  resetZoomPreview(): void {
    this.previewZoom.set(1);
  }

  togglePreviewFullscreen(): void {
    const el = this.previewStage?.nativeElement;
    if (!el) {
      return;
    }
    const doc = document as Document & { webkitFullscreenElement?: Element };
    if (!document.fullscreenElement && !doc.webkitFullscreenElement) {
      const req = el.requestFullscreen ?? (el as unknown as { webkitRequestFullscreen?: () => void }).webkitRequestFullscreen;
      req?.call(el);
    } else {
      const exit = document.exitFullscreen ?? (document as unknown as { webkitExitFullscreen?: () => void }).webkitExitFullscreen;
      exit?.call(document);
    }
  }

  private runEvidenceDownload(evidenceId: string, fileName: string): void {
    this.previewDownloadInFlight.set(true);
    this.evidenceApi
      .downloadLibraryEvidenceFile(evidenceId)
      .pipe(
        takeUntil(this.destroy$),
        catchError(err => {
          console.error(err);
          this.snackBar.open('Download failed.', 'Dismiss', { duration: 4000 });
          return EMPTY;
        }),
        finalize(() => this.previewDownloadInFlight.set(false)),
      )
      .subscribe(({ blob }) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = fileName || 'evidence';
        a.click();
        URL.revokeObjectURL(url);
      });
  }

  triggerLibraryEvidenceFilePick(event: Event): void {
    event.stopPropagation();
    this.libraryEvidenceFileInput?.nativeElement.click();
  }

  onUploadDropzoneClick(event: MouseEvent): void {
    const t = event.target as HTMLElement | null;
    if (t?.closest('.browse-button')) {
      return;
    }
    event.preventDefault();
    this.libraryEvidenceFileInput?.nativeElement.click();
  }

  onUploadDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
  }

  onUploadDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    const files = event.dataTransfer?.files;
    if (files?.length) {
      this.setPendingUploadFiles(Array.from(files));
    }
  }

  onLibraryEvidenceFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = input.files ? Array.from(input.files) : [];
    this.setPendingUploadFiles(files);
    input.value = '';
  }

  private setPendingUploadFiles(files: File[]): void {
    this.uploadValidationError.set(null);
    const v = validateEvidenceFilesForMode(files, 'single');
    if (!v.ok) {
      this.uploadValidationError.set(v.message ?? 'Invalid file');
      this.uploadFilesPending.set([]);
      return;
    }
    this.uploadFilesPending.set(files.slice(0, 1));
  }

  submitLibraryEvidenceUpload(): void {
    const jid = this.uploadJourneyId()?.trim();
    const oeId = this.uploadObjectiveElementId?.trim();
    const files = this.uploadFilesPending();
    if (!jid) {
      this.snackBar.open('No active journey for upload.', 'Dismiss', { duration: 4000 });
      return;
    }
    if (!oeId) {
      this.snackBar.open('Select an objective element.', 'Dismiss', { duration: 4000 });
      return;
    }
    if (files.length === 0) {
      this.snackBar.open('Select a file to upload.', 'Dismiss', { duration: 4000 });
      return;
    }
    const v = validateEvidenceFilesForMode(files, 'single');
    if (!v.ok) {
      this.uploadValidationError.set(v.message ?? 'Invalid file');
      return;
    }
    const t = (OE_EVIDENCE_DEFAULT_EVIDENCE_TYPE || 'Document').trim() || 'Document';
    const fd = buildSingleEvidenceFormData({
      file: files[0],
      evidenceType: t,
      title: this.uploadDescription.trim() || undefined,
    });
    this.uploadInFlight.set(true);
    this.journeyData
      .uploadSingleEvidence(jid, oeId, fd)
      .pipe(
        takeUntil(this.destroy$),
        catchError(err => {
          console.error(err);
          this.snackBar.open('Upload failed.', 'Dismiss', { duration: 4000 });
          return EMPTY;
        }),
        finalize(() => this.uploadInFlight.set(false)),
      )
      .subscribe(() => {
        this.snackBar.open('Evidence uploaded.', 'Dismiss', { duration: 3000 });
        this.closeUploadModal();
        this.loadDashboardBundle();
      });
  }

  formatFileSizeBytes(bytes: number): string {
    return this.formatFileSize(bytes);
  }

  private buildHistoryTimeline(d: LibraryEvidenceDetail): HistoryTimelineEntry[] {
    type Row = { t: number; entry: HistoryTimelineEntry };
    const rows: Row[] = [];
    rows.push({
      t: new Date(d.uploadedOn).getTime(),
      entry: {
        id: 'init',
        kind: 'upload',
        title: 'Initial upload',
        subtitle: `${this.formatUploadedBy(d.uploadedBy)} · ${this.formatDateTime(d.uploadedOn)}`,
      },
    });
    for (const v of d.versions ?? []) {
      rows.push({
        t: new Date(v.createdAt).getTime(),
        entry: {
          id: `v-${v.versionNumber}-${v.createdAt}`,
          kind: 'version',
          title: `Version ${v.versionNumber}`,
          subtitle: `${(v.notes ?? '').trim() || 'File updated'} · ${this.formatDateTime(v.createdAt)}`,
        },
      });
    }
    const ver = d.verification;
    const st = (ver.status || '').toLowerCase();
    if (ver.verifiedAt) {
      const by = this.formatUploadedBy(ver.verifiedBy ?? null);
      const title =
        st === 'rejected' ? 'Rejected' : st === 'approved' || ver.isApproved === true ? 'Approved' : 'Review';
      let subtitle = `${by} · ${this.formatDateTime(ver.verifiedAt)}`;
      if (st === 'rejected' && ver.reviewComment?.trim()) {
        subtitle += ` — ${ver.reviewComment.trim()}`;
      }
      rows.push({
        t: new Date(ver.verifiedAt).getTime(),
        entry: {
          id: 'review',
          kind: st === 'rejected' ? 'rejected' : 'approved',
          title,
          subtitle,
        },
      });
    } else if (st.includes('pending')) {
      rows.push({
        t: 0,
        entry: {
          id: 'pending',
          kind: 'pending',
          title: 'Pending review',
          subtitle: 'Awaiting reviewer',
        },
      });
    }
    rows.sort((a, b) => b.t - a.t);
    return rows.map(r => r.entry);
  }

  formatDateTime(iso: string | undefined): string {
    if (!iso) {
      return '';
    }
    const date = new Date(iso);
    if (Number.isNaN(date.getTime())) {
      return iso;
    }
    return `${date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })} · ${date.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' })}`;
  }

  formatDate(dateStr: string | undefined): string {
    if (!dateStr) {
      return '';
    }
    const date = new Date(dateStr);
    if (Number.isNaN(date.getTime())) {
      return dateStr;
    }
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }
}
