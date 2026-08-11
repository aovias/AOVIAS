import type { DeficiencyWorkspaceItem } from '../models/deficiency-command-center.model';

export function exportDeficienciesCsv(
  items: DeficiencyWorkspaceItem[],
  filename = 'deficiencies.csv',
): void {
  const headers = [
    'Code',

    'Title',

    'Severity',

    'Stage',

    'Chapter',

    'Standard',

    'Objective Element',

    'Status',

    'Lifecycle',

    'Risk Level',

    'Risk Score',

    'Primary Task',

    'Task Status',

    'Evidence Status',

    'Verification Status',

    'Owner',

    'Department',

    'Assigned Date',

    'Target Resolution',

    'Actual Resolution',

    'Blocking',

    'Root Cause',

    'Last Updated',
  ];

  const rows = items.map((d) =>
    [
      d.deficiencyCode,

      d.title,

      d.severity,

      d.maturityLevel,

      d.chapterCode,

      d.standardCode,

      `${d.objectiveElementCode} — ${d.objectiveElementName}`,

      d.status,

      d.lifecycleStatus ?? d.status,

      d.assessedRiskLevel ?? '',

      d.riskScore ?? '',

      d.primaryTaskTitle ?? '',

      d.primaryTaskStatus ?? '',

      d.evidenceStatus,

      d.verificationStatus,

      d.assignedOwner ?? '',

      d.department ?? '',

      d.assignedDate ?? '',

      d.targetResolutionDate ?? d.dueDate ?? '',

      d.actualResolutionDate ?? '',

      d.blockingStageLabel,

      d.rootCauseCategory ?? '',

      d.updatedAt,
    ]

      .map(csvEscape)

      .join(','),
  );

  const blob = new Blob([[headers.join(','), ...rows].join('\n')], {
    type: 'text/csv;charset=utf-8;',
  });

  const url = URL.createObjectURL(blob);

  const a = document.createElement('a');

  a.href = url;

  a.download = filename;

  a.click();

  URL.revokeObjectURL(url);
}

function csvEscape(value: string | number): string {
  const s = String(value);

  if (s.includes(',') || s.includes('"') || s.includes('\n')) {
    return `"${s.replace(/"/g, '""')}"`;
  }

  return s;
}
