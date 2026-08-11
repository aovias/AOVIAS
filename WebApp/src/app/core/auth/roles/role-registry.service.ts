import { Injectable, inject } from '@angular/core';
import {
  UNKNOWN_ROLE_LABEL,
  ROLE_IDS,
  ROLE_LABELS,
  RoleId,
  isRoleId,
  normalizeRoleId,
} from './role-definitions';
import { LoggerService } from '../../services/logger.service';

export interface RoleOption {
  id: RoleId;
  label: string;
}

@Injectable({
  providedIn: 'root',
})
export class RoleRegistryService {
  private readonly logger = inject(LoggerService);
  private readonly warnedUnknown = new Set<string>();

  readonly unknownLabel = UNKNOWN_ROLE_LABEL;

  allRoles(): RoleOption[] {
    return ROLE_IDS.map((id) => ({
      id,
      label: ROLE_LABELS[id],
    }));
  }

  isValidRole(id: string): id is RoleId {
    return isRoleId(id);
  }

  /**
   * Maps API/session strings (including legacy labels) to a canonical {@link RoleId}, or `null`.
   */
  normalize(raw: string | null | undefined): RoleId | null {
    return normalizeRoleId(raw);
  }

  /**
   * UI label for a role id or legacy/raw string. Empty input → em dash.
   * Unknown non-empty values → {@link UNKNOWN_ROLE_LABEL} (logged once per distinct value).
   */
  labelOf(id: string | null | undefined): string {
    if (id == null || id.trim() === '') {
      return '—';
    }
    const t = id.trim();
    const canonical = normalizeRoleId(t);
    if (canonical) {
      return ROLE_LABELS[canonical];
    }
    if (!this.warnedUnknown.has(t)) {
      this.warnedUnknown.add(t);
      this.logger.warn(`Unknown role value from API or session (no canonical mapping): "${t}"`);
    }
    return UNKNOWN_ROLE_LABEL;
  }
}
