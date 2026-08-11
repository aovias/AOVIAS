import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TeamMember } from '../models/onboarding-state.model';
import { RoleRegistryService, RoleOption } from '../../../core/auth/roles/role-registry.service';

export interface DepartmentOption {
  id: string;
  name: string;
}

@Component({
  selector: 'app-team-invitation-step',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="team-container">
      <div class="team-header">
        <h2>Invite Team Members</h2>
        <p>We will email each colleague a secure invitation link. They create their account when they accept — no temporary passwords.</p>
      </div>

      <div *ngIf="allowedDepartments.length === 0" class="empty-departments-hint">
        Select at least one department in <strong>Hospital Profile</strong> before assigning team members to departments. You can still skip this step.
      </div>

      <div class="team-form">
        <div *ngFor="let member of teamMembers; let i = index" class="member-card">
          <button
            *ngIf="teamMembers.length > 1"
            class="remove-btn"
            (click)="removeMember(i)"
            type="button">
            <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
              <path d="M12 4L4 12M4 4L12 12" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
            </svg>
          </button>

          <div class="form-row">
            <div class="form-group">
              <label>Name</label>
              <input
                type="text"
                [(ngModel)]="member.name"
                placeholder="Enter name"
                class="form-control">
            </div>

            <div class="form-group">
              <label>Email</label>
              <input
                type="email"
                [(ngModel)]="member.email"
                placeholder="Enter email"
                class="form-control">
            </div>
          </div>

          <div class="form-row">
            <div class="form-group">
              <label>Role</label>
              <select [(ngModel)]="member.role" class="form-control">
                <option value="">Select role</option>
                <option *ngFor="let r of roleOptions" [value]="r.id">{{ r.label }}</option>
              </select>
            </div>

            <div class="form-group">
              <label>Department</label>
              <select
                [(ngModel)]="member.departmentId"
                class="form-control"
                [disabled]="allowedDepartments.length === 0">
                <option value="">Select department</option>
                <option *ngFor="let d of allowedDepartments" [value]="d.id">{{ d.name }}</option>
              </select>
            </div>
          </div>
        </div>

        <div *ngIf="duplicateEmailError" class="field-error">{{ duplicateEmailError }}</div>

        <button
          class="add-member-btn"
          (click)="addMember()"
          [disabled]="allowedDepartments.length === 0"
          type="button">
          <svg width="20" height="20" viewBox="0 0 20 20" fill="none">
            <path d="M10 4V16M4 10H16" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
          </svg>
          Add Another Member
        </button>
      </div>

      <div class="skip-note">
        <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
          <path d="M8 14C11.3137 14 14 11.3137 14 8C14 4.68629 11.3137 2 8 2C4.68629 2 2 4.68629 2 8C2 11.3137 4.68629 14 8 14Z" stroke="currentColor" stroke-width="1.5"/>
          <path d="M8 5V8" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>
          <circle cx="8" cy="10.5" r="0.5" fill="currentColor"/>
        </svg>
        <span>You can skip this step and invite team members later from settings</span>
      </div>

      <div class="team-actions">
        <button class="btn-secondary" (click)="onBack.emit()">
          <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
            <path d="M10 12L6 8L10 4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
          Back
        </button>
        <button class="btn-primary" (click)="handleNext()">
          {{ hasValidMembers() ? 'Continue' : 'Skip for now' }}
          <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
            <path d="M6 12L10 8L6 4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
        </button>
      </div>
    </div>
  `,
  styles: [`
    .team-container {
      padding: 32px;
      max-width: 800px;
      margin: 0 auto;
    }

    .team-header {
      margin-bottom: 32px;
      text-align: center;
    }

    .team-header h2 {
      margin: 0 0 8px 0;
      font-size: var(--font-size-section-title);
      font-weight: 700;
      color: var(--color-text-primary, #111827);
    }

    [data-theme="dark"] .team-header h2 {
      color: var(--color-text-primary, #f9fafb);
    }

    .team-header p {
      margin: 0;
      font-size: 14px;
      color: var(--color-text-secondary, #6b7280);
    }

    [data-theme="dark"] .team-header p {
      color: var(--color-text-secondary, #9ca3af);
    }

    .empty-departments-hint {
      padding: 12px 14px;
      margin-bottom: 16px;
      border-radius: 10px;
      border: 1px solid var(--border-color, #e5e7eb);
      background: var(--color-background, #f9fafb);
      font-size: 13px;
      color: var(--color-text-secondary, #6b7280);
      text-align: center;
    }

    [data-theme="dark"] .empty-departments-hint {
      background: var(--bg-tertiary, #1e293b);
      border-color: var(--border-color, #374151);
      color: var(--color-text-secondary, #9ca3af);
    }

    .field-error {
      color: #b91c1c;
      font-size: 13px;
      font-weight: 500;
      margin-bottom: 12px;
    }

    [data-theme="dark"] .field-error {
      color: #fca5a5;
    }

    .team-form {
      display: flex;
      flex-direction: column;
      gap: 16px;
      margin-bottom: 24px;
    }

    .member-card {
      position: relative;
      background: var(--card-bg, #ffffff);
      border: 1px solid var(--border-color, #e5e7eb);
      border-radius: 12px;
      padding: 24px;
      animation: slideIn 0.3s ease;
      box-shadow: var(--shadow-sm, 0 1px 2px rgba(0, 0, 0, 0.05));
      transition: all 0.2s ease;
    }

    [data-theme="dark"] .member-card {
      background: var(--card-bg, #1e293b);
      border-color: var(--border-color, rgba(255, 255, 255, 0.06));
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.3);
    }

    .member-card:hover {
      box-shadow: var(--shadow-md, 0 4px 6px rgba(0, 0, 0, 0.07));
    }

    [data-theme="dark"] .member-card:hover {
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.4);
    }

    @keyframes slideIn {
      from {
        opacity: 0;
        transform: translateY(-10px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .remove-btn {
      position: absolute;
      top: 16px;
      right: 16px;
      width: 32px;
      height: 32px;
      border: none;
      background: transparent;
      color: #ef4444;
      border-radius: 6px;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: all 0.2s ease;
    }

    .remove-btn:hover {
      background: #fee2e2;
    }

    [data-theme="dark"] .remove-btn:hover {
      background: #7f1d1d;
    }

    .form-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 16px;
      margin-bottom: 16px;
    }

    .form-row:last-child {
      margin-bottom: 0;
    }

    .form-group label {
      display: block;
      margin-bottom: 8px;
      font-size: 14px;
      font-weight: 500;
      color: var(--color-text-primary, #111827);
    }

    [data-theme="dark"] .form-group label {
      color: var(--color-text-primary, #f9fafb);
    }

    .form-control {
      width: 100%;
      padding: 11px 14px;
      border: 1.5px solid var(--border-color, #d1d5db);
      border-radius: 8px;
      font-size: 14px;
      background: var(--bg-primary, #ffffff);
      color: var(--text-primary, #111827);
      transition: all 0.2s ease;
      font-family: inherit;
    }

    [data-theme="dark"] .form-control {
      background: var(--bg-primary, #0f172a);
      border-color: var(--border-color, rgba(255, 255, 255, 0.1));
      color: var(--text-primary, #f1f5f9);
    }

    .form-control:hover {
      border-color: var(--primary-color, var(--color-primary));
    }

    .form-control:focus {
      outline: none;
      border-color: var(--primary-color, var(--color-primary));
      box-shadow: 0 0 0 3px var(--primary-bg-light, color-mix(in srgb, var(--color-primary) 10%, transparent));
    }

    [data-theme="dark"] .form-control:focus {
      box-shadow: 0 0 0 3px var(--primary-bg-light, color-mix(in srgb, var(--color-primary) 15%, transparent));
    }

    .form-control::placeholder {
      color: var(--text-muted, #9ca3af);
    }

    [data-theme="dark"] .form-control::placeholder {
      color: var(--text-muted, #64748b);
    }

    .add-member-btn {
      width: 100%;
      padding: 18px;
      background: var(--card-bg, #ffffff);
      border: 2px dashed var(--border-color, #d1d5db);
      border-radius: 12px;
      font-size: 14px;
      font-weight: 600;
      color: var(--primary-color, var(--color-primary));
      cursor: pointer;
      transition: all 0.25s cubic-bezier(0.4, 0, 0.2, 1);
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 8px;
    }

    [data-theme="dark"] .add-member-btn {
      background: var(--bg-tertiary, #1e293b);
      border-color: var(--border-color, rgba(255, 255, 255, 0.1));
      color: var(--primary-color, var(--color-primary));
    }

    .add-member-btn:hover {
      background: var(--primary-bg-light, color-mix(in srgb, var(--color-primary) 8%, transparent));
      border-color: var(--primary-color, var(--color-primary));
      border-style: solid;
      transform: translateY(-1px);
      box-shadow: var(--shadow-sm, 0 1px 3px rgba(0, 0, 0, 0.1));
    }

    [data-theme="dark"] .add-member-btn:hover {
      background: var(--primary-bg-light, color-mix(in srgb, var(--color-primary) 10%, transparent));
      border-color: var(--primary-color, var(--color-primary));
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.3);
    }

    .skip-note {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 16px;
      background: #fef3c7;
      border: 1px solid #fde68a;
      border-radius: 8px;
      font-size: 13px;
      color: #92400e;
      margin-bottom: 24px;
    }

    [data-theme="dark"] .skip-note {
      background: #422006;
      border-color: #78350f;
      color: #fde68a;
    }

    .skip-note svg {
      flex-shrink: 0;
    }

    .team-actions {
      display: flex;
      justify-content: space-between;
      gap: 12px;
      padding-top: 24px;
      border-top: 1px solid var(--color-border, #e5e7eb);
    }

    [data-theme="dark"] .team-actions {
      border-top-color: var(--color-border, #374151);
    }

    .btn-primary,
    .btn-secondary {
      padding: 12px 24px;
      border-radius: 10px;
      font-size: 14px;
      font-weight: 600;
      border: none;
      cursor: pointer;
      transition: all 0.25s cubic-bezier(0.4, 0, 0.2, 1);
      display: inline-flex;
      align-items: center;
      gap: 8px;
      font-family: inherit;
    }

    .btn-primary {
      background: var(--primary-color, var(--color-primary));
      color: white;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
    }

    [data-theme="dark"] .btn-primary {
      background: var(--primary-color, var(--color-primary));
      color: #0f172a;
    }

    .btn-primary:hover {
      background: var(--primary-hover, var(--color-primary-hover));
      transform: translateY(-2px);
      box-shadow: 0 6px 16px color-mix(in srgb, var(--color-primary) 35%, transparent);
    }

    [data-theme="dark"] .btn-primary:hover {
      background: var(--primary-hover, #3b82f6);
      box-shadow: 0 6px 20px color-mix(in srgb, var(--color-primary) 40%, transparent);
    }

    .btn-primary:active {
      transform: translateY(-1px);
    }

    .btn-secondary {
      background: var(--card-bg, #ffffff);
      color: var(--text-secondary, #6b7280);
      border: 1.5px solid var(--border-color, #e5e7eb);
    }

    [data-theme="dark"] .btn-secondary {
      background: var(--bg-tertiary, #1e293b);
      color: var(--text-secondary, #94a3b8);
      border-color: var(--border-color, rgba(255, 255, 255, 0.1));
    }

    .btn-secondary:hover {
      background: var(--hover-bg, #f3f4f6);
      border-color: var(--primary-color, var(--color-primary));
      color: var(--primary-color, var(--color-primary));
      transform: translateY(-1px);
    }

    [data-theme="dark"] .btn-secondary:hover {
      background: var(--hover-bg, rgba(255, 255, 255, 0.05));
      border-color: var(--primary-color, var(--color-primary));
      color: var(--primary-color, var(--color-primary));
    }

    .btn-secondary:active {
      transform: translateY(0);
    }

    @media (max-width: 768px) {
      .team-container {
        padding: 24px 16px;
      }

      .form-row {
        grid-template-columns: 1fr;
      }

      .team-actions {
        flex-direction: column-reverse;
      }

      .btn-primary,
      .btn-secondary {
        width: 100%;
        justify-content: center;
      }
    }
  `]
})
export class TeamInvitationStepComponent implements OnInit {
  @Input() teamMembers: TeamMember[] = [];
  /** Departments allowed for assignment (from Hospital Profile selections). */
  @Input() allowedDepartments: DepartmentOption[] = [];
  @Output() onNext = new EventEmitter<TeamMember[]>();
  @Output() onBack = new EventEmitter<void>();

  roleOptions: RoleOption[] = [];
  duplicateEmailError: string | null = null;

  constructor(private roleRegistry: RoleRegistryService) {}

  ngOnInit(): void {
    this.roleOptions = this.roleRegistry.allRoles();

    if (this.teamMembers.length === 0) {
      this.teamMembers.push(this.createEmptyMember());
    }
  }

  createEmptyMember(): TeamMember {
    return {
      name: '',
      email: '',
      role: '',
      departmentId: '',
    };
  }

  addMember(): void {
    this.teamMembers.push(this.createEmptyMember());
  }

  removeMember(index: number): void {
    this.teamMembers.splice(index, 1);
  }

  hasValidMembers(): boolean {
    return this.teamMembers.some((m) => m.name && m.email && m.role && m.departmentId);
  }

  private findDuplicateEmails(): string | null {
    const seen = new Set<string>();
    for (const m of this.teamMembers) {
      if (!m.email?.trim()) continue;
      const key = m.email.trim().toLowerCase();
      if (seen.has(key)) {
        return 'Each team member must have a unique email address.';
      }
      seen.add(key);
    }
    return null;
  }

  handleNext(): void {
    this.duplicateEmailError = null;
    const dup = this.findDuplicateEmails();
    if (dup) {
      this.duplicateEmailError = dup;
      return;
    }
    const validMembers = this.teamMembers.filter(
      (m) => m.name && m.email && m.role && m.departmentId,
    );
    this.onNext.emit(validMembers);
  }
}
