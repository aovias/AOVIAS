import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  HostListener,
  computed,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { A11yModule } from '@angular/cdk/a11y';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideAlertCircle,
  lucideArrowLeft,
  lucideBookOpen,
  lucideCheck,
  lucideClipboardCheck,
  lucideClipboardList,
  lucideDownload,
  lucideFile,
  lucideFileSpreadsheet,
  lucideFileText,
  lucideHistory,
  lucideImage,
  lucideListChecks,
  lucideLock,
  lucidePanelRightClose,
  lucideShield,
  lucideSparkles,
  lucideUpload,
  lucideX,
} from '@ng-icons/lucide';
import { Router } from '@angular/router';
import { JourneyWorkspaceStore } from '../../state/journey-workspace.store';
import {
  OE_WIZARD_STEP_LABELS,
  splitAuditChecklistLines,
} from '../../models/oe-execution-wizard.model';
import { ConfirmationDialogComponent } from '../../../../shared/ui/confirmation-dialog/confirmation-dialog.component';
import { JourneyWorkspacePrimaryHeaderComponent } from '../journey-workspace-primary-header/journey-workspace-primary-header.component';
import { CreateAssignTaskDrawerComponent } from '../create-assign-task-drawer/create-assign-task-drawer.component';
import { OeRemediationContextComponent } from '../oe-remediation-context/oe-remediation-context.component';
import { formatEvidenceHttpError } from '../../utils/evidence-http-error';
import { logEvidenceFormDataDebug } from '../../utils/evidence-form-data-debug';
import { KB_SCORE_SCALE_PRODUCT_NOTE } from '../../utils/knowledge-base-oe-wizard.mapper';
import {
  EVIDENCE_BULK_MAX_FILES,
  EvidenceUploadMode,
  OE_EVIDENCE_DEFAULT_EVIDENCE_TYPE,
  validateEvidenceFilesForMode,
} from '../../utils/oe-evidence-upload.constants';
import {
  buildBulkEvidenceFormData,
  buildSingleEvidenceFormData,
} from '../../utils/oe-evidence-upload.payload';
import { JourneyDataService } from '../../services/journey-data.service';
import { BulkEvidenceUploadResponseDto } from '../../models/journey-api.model';
import { OeEvidenceItem } from '../../models/objective-element.model';
import {
  EvidenceFileKind,
  evidenceFileKindFromItem,
  evidenceFileKindFromMime,
  evidenceKindFromEvidenceDetailLike,
  evidenceTypeLabel,
  probeEvidenceKindFromBlobPrefix,
} from '../../utils/oe-evidence-display.util';
import { environment } from '../../../../../environments/environment';
import { finalize, forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

@Component({
  selector: 'app-oe-execution-wizard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatSnackBarModule,
    MatTooltipModule,
    A11yModule,
    NgIcon,
    JourneyWorkspacePrimaryHeaderComponent,
    CreateAssignTaskDrawerComponent,
    OeRemediationContextComponent,
  ],
  viewProviders: [
    provideIcons({
      lucideAlertCircle,
      lucideArrowLeft,
      lucideBookOpen,
      lucideCheck,
      lucideClipboardCheck,
      lucideClipboardList,
      lucidePanelRightClose,
      lucideUpload,
      lucideHistory,
      lucideImage,
      lucideShield,
      lucideSparkles,
      lucideLock,
      lucideListChecks,
      lucideDownload,
      lucideFile,
      lucideFileSpreadsheet,
      lucideFileText,
      lucideX,
    }),
  ],
  templateUrl: './oe-execution-wizard.component.html',
  styleUrl: './oe-execution-wizard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OeExecutionWizardComponent {
  readonly store = inject(JourneyWorkspaceStore);
  private readonly dialog = inject(MatDialog);
  readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly journeyData = inject(JourneyDataService);
  private readonly destroyRef = inject(DestroyRef);

  /** Template: gate picker + upload UI without calling env each CD cycle in template. */
  readonly evidenceUploadFeatureOn = environment.enableOeWizardEvidenceUpload;

  readonly kbScoreScaleNote = KB_SCORE_SCALE_PRODUCT_NOTE;

  readonly stepLabels = OE_WIZARD_STEP_LABELS;
  readonly stepCount = OE_WIZARD_STEP_LABELS.length;

  readonly mainScroll = viewChild<ElementRef<HTMLElement>>('mainScroll');
  readonly evidenceFileInput = viewChild<ElementRef<HTMLInputElement>>('evidenceFileInput');

  /** Dropzone drag highlight (flag on + interactive only). */
  readonly evidenceUploadDragActive = signal(false);

  /** Mobile bottom sheet + tablet slide-out share one open state */
  readonly contextPanelOpen = signal(false);

  /** KB audit checklist lines — lines from `auditChecklist` field */
  readonly auditChecklistLines = computed(() => {
    const kb = this.store.oeWizardKnowledgeVm();
    return kb ? splitAuditChecklistLines(kb.auditChecklist) : [];
  });

  /** Evidence step (step index 2) — local selection until picker + API are enabled */
  readonly evidenceUploadFiles = signal<File[]>([]);
  readonly evidenceUploadValidationError = signal<string | null>(null);

  /** Server-backed evidence rows for selected OE (newest first). */
  readonly persistedWizardEvidence = computed((): OeEvidenceItem[] => {
    const id = this.store.selectedOeId();
    if (!id) return [];
    const found = this.store.findOe(id);
    const ev = found?.oe.evidence ?? [];
    return [...ev].sort((a, b) => b.uploadedAt.localeCompare(a.uploadedAt));
  });

  /** In-flight download — evidence ids (avoid double-submit). */
  readonly evidenceDownloadBusyIds = signal<ReadonlySet<string>>(new Set());

  /**
   * Session-only UI hints: **kind** from evidence-detail prefetch; **mimeType** from download headers / magic-byte probe.
   */
  readonly evidenceDisplayHintById = signal<
    Readonly<Record<string, { kind?: EvidenceFileKind; mimeType?: string }>>
  >({});

  private evidenceDetailPrefetchKey = '';

  readonly evidenceUploadMode = computed<EvidenceUploadMode>(() =>
    this.evidenceUploadFiles().length <= 1 ? 'single' : 'bulk',
  );

  readonly evidenceDropzoneDisabled = computed(() => {
    const w = this.store.operationalModeUi().wizard;
    return w.evidenceSection === 'readonly' || w.allStepsReadOnly || this.store.oeWizardRemoteCompleted();
  });

  readonly evidenceDropzoneTooltip = computed(() => {
    if (this.store.oeWizardRemoteCompleted()) {
      return 'This objective element is already completed. Evidence upload is disabled.';
    }
    if (this.evidenceDropzoneDisabled()) {
      return 'Evidence is read-only in this audit phase.';
    }
    if (!environment.enableOeWizardEvidenceUpload) {
      return 'Upload not enabled yet';
    }
    return 'Select or drop files';
  });

  /** Review step — evidence files attached to selected OE */
  readonly reviewEvidenceCounts = computed(() => {
    const rows = this.persistedWizardEvidence();
    let approved = 0;
    let pending = 0;
    let rejected = 0;

    for (const row of rows) {
      if (row.verificationStatus === 'verified') {
        approved++;
      } else if (row.verificationStatus === 'rejected') {
        rejected++;
      } else {
        pending++;
      }
    }

    return {
      total: rows.length,
      approved,
      pending,
      rejected,
    };
  });

  readonly reviewEvidenceFileCount = computed(() => {
    return this.reviewEvidenceCounts().total;
  });

  readonly reviewUnderstandingComplete = computed(() => this.store.wizardUnderstandAck());

  readonly reviewGuidanceComplete = computed(() => this.store.wizardExecuteReady());

  readonly reviewEvidenceComplete = computed(
    () => this.store.wizardEvidenceAck() && this.reviewEvidenceFileCount() > 0,
  );

  readonly reviewChecklistComplete = computed(() => {
    const total = this.auditChecklistLines().length;
    const submitted = this.store.wizardEvalSubmitted();
    if (total === 0) return submitted;
    const done = this.store.wizardAuditChecklistChecked().filter(Boolean).length;
    return submitted && done === total;
  });

  readonly reviewChecklistSubtitle = computed(() => {
    const total = this.auditChecklistLines().length;
    if (total === 0) {
      return this.store.wizardEvalSubmitted()
        ? 'Audit checklist submitted'
        : 'Complete audit checklist to continue';
    }
    const done = this.store.wizardAuditChecklistChecked().filter(Boolean).length;
    return `${done} of ${total} items verified`;
  });

  readonly reviewRequirementsComplete = computed(
    () =>
      this.reviewUnderstandingComplete() &&
      this.reviewGuidanceComplete() &&
      this.reviewEvidenceComplete() &&
      this.reviewChecklistComplete(),
  );

  readonly reviewReadyForSubmission = computed(
    () => this.reviewRequirementsComplete() && !this.store.oeWizardMarkCompleteDisabled(),
  );

  readonly reviewAlreadyCompleted = computed(() => this.store.oeWizardRemoteCompleted());

  readonly reviewSubmissionBlocked = computed(
    () => this.reviewRequirementsComplete() && this.store.oeWizardMarkCompleteDisabled(),
  );

  readonly markCompleteDisabledReason = computed(() => {
    if (!this.store.oeWizardMarkCompleteDisabled()) {
      return '';
    }
    if (this.store.oeWizardRemoteCompleted()) {
      return 'This objective element is already completed.';
    }
    if (this.store.oeWizardCompleteBlockedNoVerifiedEvidence()) {
      return 'Full score (5) requires at least one verified evidence item. Verify evidence or reduce the score.';
    }
    return 'This objective element cannot be marked complete right now.';
  });

  constructor() {
    effect(() => {
      if (!this.store.oeWizardOpen()) {
        this.contextPanelOpen.set(false);
        this.evidenceUploadFiles.set([]);
        this.evidenceUploadValidationError.set(null);
        this.evidenceDisplayHintById.set({});
        this.evidenceDetailPrefetchKey = '';
      }
    });
    effect(() => {
      const open = this.store.oeWizardOpen();
      const step = this.store.oeWizardStepIndex();
      if (!open) return;
      void step;
      queueMicrotask(() => {
        const el = this.mainScroll()?.nativeElement;
        el?.scrollTo({ top: 0, behavior: 'smooth' });
      });
    });
    effect(() => {
      const open = this.store.oeWizardOpen();
      const step = this.store.oeWizardStepIndex();
      const jid = this.store.snapshot()?.journeyId;
      const oeId = this.store.selectedOeId();
      const items = this.persistedWizardEvidence();
      if (!open || step !== 2 || !jid || !oeId || items.length === 0) {
        return;
      }
      const batchKey = `${jid}:${oeId}:${items.map((i) => i.id).join('|')}`;
      if (batchKey === this.evidenceDetailPrefetchKey) {
        return;
      }
      this.evidenceDetailPrefetchKey = batchKey;
      forkJoin(
        items.map((it) =>
          this.journeyData.getObjectiveElementEvidenceDetail(jid, oeId, it.id).pipe(
            map((detail) => ({ id: it.id, detail })),
            catchError(() => of({ id: it.id, detail: null })),
          ),
        ),
      )
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe((rows) => {
          this.evidenceDisplayHintById.update((prev) => {
            const next: Record<string, { kind?: EvidenceFileKind; mimeType?: string }> = {
              ...prev,
            };
            for (const row of rows) {
              if (!row.detail) continue;
              const kind = evidenceKindFromEvidenceDetailLike(row.detail);
              if (kind === 'other') continue;
              next[row.id] = { ...next[row.id], kind };
            }
            return next;
          });
        });
    });
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (!this.store.oeWizardOpen()) return;
    this.requestClose();
  }

  /** When the wizard is shown on `/execution/Progress`, return to Mission Control. */
  private afterWizardCloseNav(): void {
    if (this.router.url.includes('/execution/Progress')) {
      void this.router.navigate(['/app/journey/execution']);
    }
  }

  requestClose(): void {
    if (!this.store.oeWizardDirty()) {
      this.store.closeOeWizardWithoutConfirm();
      this.afterWizardCloseNav();
      return;
    }
    this.dialog
      .open(ConfirmationDialogComponent, {
        width: 'min(100vw - 2rem, 28rem)',
        data: {
          title: 'Leave execution workspace?',
          message: 'You have unsaved progress. Discard changes and close?',
          confirmText: 'Discard and close',
          cancelText: 'Keep editing',
        },
      })
      .afterClosed()
      .subscribe((confirmed) => {
        if (confirmed) {
          this.store.dismissOeWizardDirty();
          this.afterWizardCloseNav();
        }
      });
  }

  onExitWorkspace(): void {
    this.store.wizardExitAfterFinish();
    this.afterWizardCloseNav();
  }

  wizardSaveDraft(): void {
    this.store.flushWizardDraftToServer();
  }

  /** Footer Previous — disabled when there is no accessible prior step (matches stepper rules). */
  wizardPreviousDisabled(): boolean {
    const i = this.store.oeWizardStepIndex();
    if (i <= 0) return true;
    return !this.store.oeWizardCanAccessStep()(i - 1);
  }

  wizardGoPrevious(): void {
    if (this.wizardPreviousDisabled()) return;
    const i = this.store.oeWizardStepIndex();
    this.store.setWizardStep(i - 1);
  }

  selectStep(i: number): void {
    this.store.setWizardStep(i);
  }

  toggleContextPanel(): void {
    this.contextPanelOpen.update((v) => !v);
  }

  closeContextPanel(): void {
    this.contextPanelOpen.set(false);
  }

  stepOfLabel(): string {
    return `Step ${this.store.oeWizardStepIndex() + 1} of ${this.stepCount}`;
  }

  stepFractionLabel(): string {
    return `${this.store.oeWizardStepIndex() + 1} / ${this.stepCount}`;
  }

  stepStateLabel(i: number): string {
    const cur = this.store.oeWizardStepIndex();
    if (i === cur) return 'Active';
    if (!this.store.oeWizardCanAccessStep()(i)) return 'Locked';
    if (this.store.oeWizardStepCompleted()(i)) return 'Completed';
    return 'Open';
  }

  splitParagraphs(text: string): string[] {
    const t = text?.trim() ?? '';
    if (!t) return [];
    const parts = t.split(/(?<=[.!?])\s+/).filter(Boolean);
    return parts.length ? parts : [t];
  }

  splitLines(text: string): string[] {
    return splitAuditChecklistLines(text);
  }

  onAuditCheckChange(index: number, ev: Event): void {
    const input = ev.target as HTMLInputElement;
    this.store.setWizardAuditLineChecked(index, input.checked);
  }

  retryKnowledgeBase(): void {
    this.store.loadKnowledgeForSelectedOe();
  }

  goToExecutionWorkspace(): void {
    void this.router.navigate(['/app/journey/execution']);
  }

  quickAction(kind: 'upload' | 'task' | 'timeline'): void {
    if (kind === 'upload') {
      const oeId = this.store.selectedOeId();
      const found = oeId ? this.store.findOe(oeId) : null;
      void this.router.navigate(['/app/evidence'], {
        queryParams: {
          source: 'oe-wizard',
          objectiveElementId: oeId ?? undefined,
          objectiveElementCode: found?.oe.code ?? undefined,
          chapterId: found?.chapter.id ?? undefined,
          standardId: found?.standard.id ?? undefined,
        },
      });
      return;
    }
    if (kind === 'task') {
      if (this.store.oeWizardRemoteCompleted()) {
        return;
      }
      this.store.openTaskAssignmentDrawer();
      return;
    }
    /* timeline — stub until timeline route exists */
  }

  /**
   * Adds picked/dropped files to the list (merge + dedupe). Replaces nothing — each browse/drop adds to the selection until cap.
   */
  handleFileSelect(files: File[]): void {
    const incoming = files.filter(Boolean);
    const existing = this.evidenceUploadFiles();
    const fileKey = (f: File) => `${f.name}\0${f.size}\0${f.lastModified}`;
    const seen = new Set(existing.map((f) => fileKey(f)));
    const merged: File[] = [...existing];
    for (const f of incoming) {
      const k = fileKey(f);
      if (seen.has(k)) continue;
      seen.add(k);
      merged.push(f);
    }
    if (merged.length > EVIDENCE_BULK_MAX_FILES) {
      this.snackBar.open(
        `Only ${EVIDENCE_BULK_MAX_FILES} files can be selected per upload. Extra files were not added.`,
        undefined,
        { duration: 5000 },
      );
    }
    this.evidenceUploadFiles.set(merged.slice(0, EVIDENCE_BULK_MAX_FILES));
    this.evidenceUploadValidationError.set(null);
  }

  /** Future file input handler (multi-file / bulk UX). */
  handleBulkSelect(files: File[]): void {
    this.handleFileSelect(files);
  }

  /** Remove one file from the pending selection (picker list). */
  removeEvidenceFileAt(index: number): void {
    const cur = this.evidenceUploadFiles();
    if (index < 0 || index >= cur.length) return;
    this.evidenceUploadFiles.set(cur.filter((_, i) => i !== index));
    this.evidenceUploadValidationError.set(null);
  }

  buildSinglePayload(): FormData | null {
    const files = this.evidenceUploadFiles();
    if (files.length !== 1) return null;
    return buildSingleEvidenceFormData({
      file: files[0],
      evidenceType: OE_EVIDENCE_DEFAULT_EVIDENCE_TYPE,
    });
  }

  buildBulkPayload(): FormData | null {
    const files = this.evidenceUploadFiles();
    if (files.length < 2) return null;
    return buildBulkEvidenceFormData({
      files,
      evidenceType: OE_EVIDENCE_DEFAULT_EVIDENCE_TYPE,
    });
  }

  /**
   * Validates, builds FormData, then either debug-logs (flag off) or POSTs to Gateway (flag on).
   */
  triggerUpload(): void {
    const files = this.evidenceUploadFiles();
    const mode = this.evidenceUploadMode();
    const v = validateEvidenceFilesForMode(files, mode);
    if (!v.ok) {
      this.evidenceUploadValidationError.set(v.message);
      return;
    }
    this.evidenceUploadValidationError.set(null);
    const t = OE_EVIDENCE_DEFAULT_EVIDENCE_TYPE.trim() || 'Document';
    // Single → POST …/evidence (multipart: `file`, `evidenceType`). Bulk → POST …/evidence/bulk (`files`, `evidenceType`).
    const fd =
      mode === 'single'
        ? buildSingleEvidenceFormData({ file: files[0], evidenceType: t })
        : buildBulkEvidenceFormData({ files, evidenceType: t });

    if (!environment.enableOeWizardEvidenceUpload) {
      logEvidenceFormDataDebug(fd, 'preview (upload flag off)');
      return;
    }

    const jid = this.store.snapshot()?.journeyId;
    const oeId = this.store.selectedOeId();
    if (!jid || !oeId) {
      this.snackBar.open('Upload failed. Please try again.', undefined, { duration: 4000 });
      return;
    }

    logEvidenceFormDataDebug(fd, 'before POST');

    if (environment.enableDebugLogging) {
      // eslint-disable-next-line no-console -- optional diagnostics
      console.debug('[OE Evidence] upload', { journeyId: jid, objectiveElementId: oeId, mode });
    }

    if (mode === 'single') {
      this.journeyData
        .uploadSingleEvidence(jid, oeId, fd)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.snackBar.open('Evidence uploaded successfully', undefined, { duration: 4000 });
            this.evidenceUploadFiles.set([]);
            this.store.refreshSelectedObjectiveElementFromApi();
          },
          error: (err: unknown) =>
            this.snackBar.open(formatEvidenceHttpError(err), undefined, { duration: 5000 }),
        });
      return;
    }

    this.journeyData
      .uploadBulkEvidence(jid, oeId, fd)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res: BulkEvidenceUploadResponseDto) => {
          const msg = this.formatBulkEvidenceSuccessMessage(res);
          this.snackBar.open(msg, undefined, { duration: 5000 });
          this.evidenceUploadFiles.set([]);
          this.store.refreshSelectedObjectiveElementFromApi();
        },
        error: (err: unknown) =>
          this.snackBar.open(formatEvidenceHttpError(err), undefined, { duration: 5000 }),
      });
  }

  private formatBulkEvidenceSuccessMessage(res: BulkEvidenceUploadResponseDto): string {
    const ok = res.successCount ?? 0;
    const fail = res.failureCount ?? 0;
    if (fail > 0 && ok > 0) {
      return `${ok} files uploaded successfully. ${fail} files failed to upload.`;
    }
    if (fail > 0) {
      return `${fail} files failed to upload`;
    }
    if (ok > 0) {
      return `${ok} files uploaded successfully`;
    }
    return 'Bulk upload completed';
  }

  formatEvidenceFileSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  evidenceKindForUploadedItem(item: OeEvidenceItem): EvidenceFileKind {
    const h = this.evidenceDisplayHintById()[item.id];
    if (h?.kind && h.kind !== 'other') {
      return h.kind;
    }
    return evidenceFileKindFromItem(item.name, {
      mimeType: h?.mimeType ?? item.mimeType,
      fileUrl: item.fileUrl,
      fileName: item.fileName,
    });
  }

  evidenceTypeLabelForUploadedItem(item: OeEvidenceItem): string {
    return evidenceTypeLabel(this.evidenceKindForUploadedItem(item));
  }

  downloadEvidenceItem(item: OeEvidenceItem): void {
    if (this.evidenceDownloadBusyIds().has(item.id)) return;
    const jid = this.store.snapshot()?.journeyId;
    const oeId = this.store.selectedOeId();
    if (!jid || !oeId || !item.id) {
      this.snackBar.open('Download failed. Please try again.', undefined, { duration: 4000 });
      return;
    }
    this.evidenceDownloadBusyIds.update((s) => new Set(s).add(item.id));
    this.journeyData
      .downloadObjectiveElementEvidenceFile(jid, oeId, item.id)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.evidenceDownloadBusyIds.update((s) => {
            const n = new Set(s);
            n.delete(item.id);
            return n;
          });
        }),
      )
      .subscribe({
        next: async ({ blob, contentType }) => {
          let mime = (contentType ?? '').trim();
          const blobType = blob.type?.trim();
          if (!mime || mime === 'application/octet-stream') {
            if (blobType && blobType !== 'application/octet-stream') {
              mime = blobType;
            }
          }
          let probedKind: EvidenceFileKind | null = null;
          if (!mime || mime === 'application/octet-stream') {
            probedKind = await probeEvidenceKindFromBlobPrefix(blob);
            if (probedKind === 'image') {
              mime = 'image/png';
            } else if (probedKind === 'pdf') {
              mime = 'application/pdf';
            }
          }
          const fromMime = mime ? evidenceFileKindFromMime(mime) : null;
          const kind = fromMime ?? probedKind;
          if (mime || kind) {
            this.evidenceDisplayHintById.update((prev) => ({
              ...prev,
              [item.id]: {
                ...prev[item.id],
                ...(mime ? { mimeType: mime } : {}),
                ...(kind && kind !== 'other' ? { kind } : {}),
              },
            }));
          }
          const url = URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = item.name?.trim() || 'evidence';
          a.rel = 'noopener';
          a.click();
          URL.revokeObjectURL(url);
        },
        error: (err: unknown) =>
          this.snackBar.open(formatEvidenceHttpError(err), undefined, { duration: 5000 }),
      });
  }

  /** Flag on + not readonly: opens picker; flag off: legacy snackbar. */
  canUseEvidencePicker(): boolean {
    return environment.enableOeWizardEvidenceUpload && !this.evidenceDropzoneDisabled();
  }

  openEvidenceFilePicker(): void {
    this.evidenceFileInput()?.nativeElement?.click();
  }

  onEvidenceFilesInputChange(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    const list = input.files;
    const files = list && list.length > 0 ? Array.from(list) : [];
    if (files.length) {
      this.handleFileSelect(files);
    }
    input.value = '';
  }

  onEvidenceDragOver(ev: DragEvent): void {
    if (!this.canUseEvidencePicker()) return;
    ev.preventDefault();
    ev.stopPropagation();
  }

  onEvidenceDragEnter(ev: DragEvent): void {
    if (!this.canUseEvidencePicker()) return;
    ev.preventDefault();
    this.evidenceUploadDragActive.set(true);
  }

  onEvidenceDragLeave(ev: DragEvent): void {
    if (!this.canUseEvidencePicker()) return;
    ev.preventDefault();
    this.evidenceUploadDragActive.set(false);
  }

  onEvidenceDrop(ev: DragEvent): void {
    if (!this.canUseEvidencePicker()) return;
    ev.preventDefault();
    ev.stopPropagation();
    this.evidenceUploadDragActive.set(false);
    const dt = ev.dataTransfer;
    if (!dt?.files?.length) return;
    this.handleFileSelect(Array.from(dt.files));
  }

  onEvidenceDropzonePointer(ev: Event): void {
    ev.preventDefault();
    if (this.evidenceDropzoneDisabled()) return;
    if (!environment.enableOeWizardEvidenceUpload) {
      this.snackBar.open('Upload will be enabled shortly', undefined, { duration: 4000 });
      return;
    }
    this.openEvidenceFilePicker();
  }

  onEvidenceDropzoneKeydown(ev: KeyboardEvent): void {
    if (ev.key !== 'Enter' && ev.key !== ' ') return;
    ev.preventDefault();
    this.onEvidenceDropzonePointer(ev);
  }

}
