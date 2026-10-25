import type { FindingsWorkspaceItem } from '../models/findings-workspace.model';

export function exportFindingsCsv(items: FindingsWorkspaceItem[], filename = 'findings.csv'): void {
  const headers = [
    'Finding Number',
    'Title',
    'Type',
    'Source',
    'Severity',
    'Chapter',
    'Standard',
    'Objective Element',
    'Department',
    'Owner',
    'Status',
    'Created',
    'Updated',
  ];

  const rows = items.map((f) =>
    [
      f.findingNumber,
      f.title,
      f.classification,
      f.source,
      f.severity,
      f.chapterCode,
      f.standardCode,
      f.objectiveElementCode,
      f.department,
      f.owner ?? '',
      f.status,
      f.createdDate,
      f.lastUpdated,
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

function csvEscape(value: string): string {
  if (value.includes(',') || value.includes('"') || value.includes('\n')) {
    return `"${value.replace(/"/g, '""')}"`;
  }
  return value;
}
