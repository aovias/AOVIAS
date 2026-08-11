import { JourneyWorkspaceSnapshot } from '../models/journey-tree.model';
import { ComplianceStandard } from '../models/standard.model';
import { ObjectiveElement } from '../models/objective-element.model';
import { JourneyChapter } from '../models/chapter.model';
import { CreateTaskDraftVm, createEmptyCreateTaskDraftVm } from '../models/create-task-draft-vm.model';

export function buildTaskDraftFromOeContext(
  found: { chapter: JourneyChapter; standard: ComplianceStandard; oe: ObjectiveElement } | null,
  _snapshot: JourneyWorkspaceSnapshot | null,
): CreateTaskDraftVm {
  const base = createEmptyCreateTaskDraftVm();
  if (!found || !_snapshot) {
    return base;
  }
  const { standard, oe } = found;
  const oeTitle = (oe.title ?? oe.description ?? '').trim();
  const titleHint = oe.code ? `Follow-up: ${oe.code}` : '';
  return {
    ...base,
    title: titleHint,
    standardId: standard.id,
    standardLabel: `${standard.code} · ${standard.title}`.trim(),
    objectiveElementId: oe.id,
    objectiveLabel: `${oe.code} · ${oeTitle}`.trim(),
  };
}
