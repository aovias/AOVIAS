import { Injectable } from '@angular/core';
import type { AuditType } from './audit-management-api.service';

export interface AuditPrepTemplate {
  id: string;
  name: string;
  auditType: AuditType;
  defaultScope: string;
  defaultProgramName?: string;
  leadAuditorName?: string;
  teamNotes?: string;
  createdAt: string;
}

const STORAGE_KEY = 'praman.audit.prepTemplates.v1';

/** Local prep templates until a dedicated Templates API exists. */
@Injectable({ providedIn: 'root' })
export class AuditPrepTemplateService {
  list(): AuditPrepTemplate[] {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return this.seedDefaults();
      const parsed = JSON.parse(raw) as AuditPrepTemplate[];
      return Array.isArray(parsed) ? parsed : this.seedDefaults();
    } catch {
      return this.seedDefaults();
    }
  }

  save(templates: AuditPrepTemplate[]): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(templates));
  }

  upsert(
    template: Omit<AuditPrepTemplate, 'id' | 'createdAt'> & { id?: string },
  ): AuditPrepTemplate {
    const list = this.list();
    const next: AuditPrepTemplate = {
      id: template.id || crypto.randomUUID(),
      name: template.name.trim(),
      auditType: template.auditType,
      defaultScope: template.defaultScope.trim(),
      defaultProgramName: template.defaultProgramName?.trim() || undefined,
      leadAuditorName: template.leadAuditorName?.trim() || undefined,
      teamNotes: template.teamNotes?.trim() || undefined,
      createdAt: new Date().toISOString(),
    };
    const idx = list.findIndex((t) => t.id === next.id);
    if (idx >= 0) list[idx] = next;
    else list.unshift(next);
    this.save(list);
    return next;
  }

  remove(id: string): void {
    this.save(this.list().filter((t) => t.id !== id));
  }

  private seedDefaults(): AuditPrepTemplate[] {
    const defaults: AuditPrepTemplate[] = [
      {
        id: 'tpl-internal-q',
        name: 'Quarterly Internal Audit',
        auditType: 'InternalAudit',
        defaultScope: 'Hospital-wide internal audit — chapters AAC, COP, PRE',
        defaultProgramName: 'FY Internal Audit Plan',
        createdAt: new Date().toISOString(),
      },
      {
        id: 'tpl-surveillance',
        name: 'NABH Surveillance Prep',
        auditType: 'Surveillance',
        defaultScope: 'Surveillance focus — previously non-compliant OEs and high-risk chapters',
        defaultProgramName: 'Surveillance Cycle',
        createdAt: new Date().toISOString(),
      },
    ];
    this.save(defaults);
    return defaults;
  }
}
