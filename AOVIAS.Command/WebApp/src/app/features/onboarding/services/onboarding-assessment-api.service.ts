import { Injectable, inject } from '@angular/core';

import { Observable, forkJoin, of } from 'rxjs';

import { map, switchMap } from 'rxjs/operators';

import { BaseApiService } from '../../../shared/services/base-api.service';
import { CommonLookupService } from '../../../shared/services/common-lookup.service';

import { AssessmentQuestion } from '../models/onboarding-state.model';

/** Gateway-only route: GET /api/v1/onboarding/core-assessment/{frameworkId}?count=40 */
const CORE_ASSESSMENT_PATH = '/onboarding/core-assessment';

/** Core assessment should never exceed this; full questionnaire is ~503. */
export const CORE_ASSESSMENT_MAX_QUESTIONS = 60;

/** Shape of a question returned by GET /onboarding/core-assessment/{frameworkId}. */
interface CoreAssessmentQuestionRaw {
  wizardQuestionId?: string;
  objectiveElementId?: string;
  questionTitle?: string;
  questionText?: string;
  helpText?: string;
  assessmentGuidance?: string;
  expectedEvidence?: string;
  category?: string;
  weight?: number;
  isMandatory?: boolean;
  isCritical?: boolean;
  displayPriority?: number;
}

/** Envelope returned by GET /onboarding/core-assessment/{frameworkId}. */
interface CoreAssessmentDto {
  frameworkId?: string;
  frameworkName?: string;
  maturityLevel?: string;
  questions?: CoreAssessmentQuestionRaw[];
}

/**
 * Loads onboarding Core assessment questions via Gateway only.
 *
 * Resolution strategy:
 *   1. Selected wizard codes (e.g. "CP00000001") are PlatformId values from Compliance Service.
 *   2. Resolve them via CommonLookupService (GET /frameworks/all-frameworks through Gateway).
 *   3. Call GET /onboarding/core-assessment/{frameworkUuid}?count=40 for each match.
 */
@Injectable({
  providedIn: 'root',
})
export class OnboardingAssessmentApiService extends BaseApiService {
  private readonly commonLookup = inject(CommonLookupService);

  /**
   * Loads the risk-selected Core maturity questions for the selected frameworks.
   * Always calls Gateway — never KnowledgeBase or Compliance directly.
   */
  loadCoreQuestions(frameworkCodes: string[], count = 40): Observable<AssessmentQuestion[]> {
    const codes = (frameworkCodes ?? []).map((c) => c.trim()).filter(Boolean);
    if (codes.length === 0) return of([]);

    const codeSet = new Set(codes.map((c) => c.toLowerCase()));

    return this.commonLookup.loadFrameworks().pipe(
      switchMap((allFrameworks) => {
        const matched = (allFrameworks ?? []).filter((f) =>
          codeSet.has((f.code ?? '').trim().toLowerCase()),
        );

        if (matched.length === 0) return of([] as AssessmentQuestion[]);

        return forkJoin(
          matched.map((framework) =>
            this.get<CoreAssessmentDto>(
              `${CORE_ASSESSMENT_PATH}/${framework.platformId}?count=${count}`,
            ).pipe(map((dto) => this.mapCoreAssessmentToQuestions(dto))),
          ),
        ).pipe(map((groups) => this.deduplicateByQuestionId(groups.flat())));
      }),
    );
  }

  private mapCoreAssessmentToQuestions(dto: CoreAssessmentDto | null | undefined): AssessmentQuestion[] {
    const rawQuestions = this.readQuestions(dto);
    if (!rawQuestions.length) return [];

    const frameworkName = this.readString(dto, 'frameworkName', 'FrameworkName') ?? 'Framework';

    const result: AssessmentQuestion[] = [];

    for (const q of rawQuestions) {
      const objectiveElementId = this.readString(q, 'objectiveElementId', 'ObjectiveElementId');
      const questionText = this.readString(q, 'questionText', 'QuestionText');
      if (!objectiveElementId || !questionText) continue;

      // Extract the OE code from the question title prefix (format: "{OE.Code}: {OE.Title}").
      // This code bridges the KB ComplianceNode.Id / CS ObjectiveElement.Id GUID boundary on the backend.
      const questionTitle = this.readString(q, 'questionTitle', 'QuestionTitle');
      const objectiveElementCode = this.extractOeCodeFromTitle(questionTitle);

      result.push({
        id: objectiveElementId,
        question: questionText,
        category: this.readString(q, 'category', 'Category')?.trim() || frameworkName,
        helpText: this.readString(q, 'helpText', 'HelpText')?.trim() || undefined,
        weight: this.readNumber(q, 'weight', 'Weight'),
        isMandatory: this.readBool(q, 'isMandatory', 'IsMandatory'),
        isCritical: this.readBool(q, 'isCritical', 'IsCritical'),
        objectiveElementCode: objectiveElementCode || undefined,
      });
    }

    return result;
  }

  private readQuestions(dto: CoreAssessmentDto | null | undefined): CoreAssessmentQuestionRaw[] {
    if (!dto || typeof dto !== 'object') return [];
    const record = dto as Record<string, unknown>;
    const questions = record['questions'] ?? record['Questions'];
    return Array.isArray(questions) ? (questions as CoreAssessmentQuestionRaw[]) : [];
  }

  private readString(
    obj: Record<string, unknown> | CoreAssessmentQuestionRaw | CoreAssessmentDto | null | undefined,
    camel: string,
    pascal: string,
  ): string | undefined {
    if (!obj || typeof obj !== 'object') return undefined;
    const record = obj as Record<string, unknown>;
    const value = record[camel] ?? record[pascal];
    return typeof value === 'string' && value.trim() ? value.trim() : undefined;
  }

  private readNumber(
    obj: CoreAssessmentQuestionRaw,
    camel: string,
    pascal: string,
  ): number | undefined {
    const record = obj as Record<string, unknown>;
    const value = record[camel] ?? record[pascal];
    return typeof value === 'number' ? value : undefined;
  }

  private readBool(obj: CoreAssessmentQuestionRaw, camel: string, pascal: string): boolean | undefined {
    const record = obj as Record<string, unknown>;
    const value = record[camel] ?? record[pascal];
    return typeof value === 'boolean' ? value : undefined;
  }

  /** When multiple frameworks share objective elements, keep the first occurrence. */
  private deduplicateByQuestionId(questions: AssessmentQuestion[]): AssessmentQuestion[] {
    const seen = new Set<string>();
    const result: AssessmentQuestion[] = [];

    for (const q of questions) {
      if (seen.has(q.id)) continue;
      seen.add(q.id);
      result.push(q);
    }

    return result;
  }

  /**
   * Parses the OE Code from a question title in the format "{OE.Code}: {OE.Title}".
   * Returns the code segment (e.g. "AAC.1.a") or undefined if the title is missing or
   * does not match the expected prefix pattern.
   */
  private extractOeCodeFromTitle(title: string | undefined): string | undefined {
    if (!title) return undefined;
    const colonIdx = title.indexOf(':');
    if (colonIdx <= 0) return undefined;
    const candidate = title.substring(0, colonIdx).trim();
    // Validate that the candidate looks like an OE code (e.g. "AAC.1.a", "MOM.2", "HIC.3.b")
    return /^[A-Z]{2,6}\.\d+(\.[a-z0-9]+)*$/.test(candidate) ? candidate : undefined;
  }
}
