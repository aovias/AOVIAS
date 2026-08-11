import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { ROLE_IDS, ROLE_LABELS } from '../../../../core/auth/roles/role-definitions';
import {
  CreateIdentityUserPayload,
  DepartmentTreeNode,
  IdentityUserDetail,
  UpdateIdentityUserPayload,
} from '../models/identity-user-admin.model';

export interface IdentityUserFormDialogData {
  mode: 'create' | 'edit';
  user?: IdentityUserDetail;
  departments: DepartmentTreeNode[];
}

export type IdentityUserFormDialogResult =
  | { action: 'create'; payload: CreateIdentityUserPayload }
  | { action: 'update'; payload: UpdateIdentityUserPayload };

@Component({
  selector: 'app-identity-user-form-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>{{ data.mode === 'create' ? 'Add User' : 'Edit User' }}</h2>
    <form [formGroup]="form" (ngSubmit)="submit()">
      <mat-dialog-content class="dialog-content">
        <div class="grid">
          <mat-form-field appearance="outline">
            <mat-label>First name</mat-label>
            <input matInput formControlName="firstName" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Last name</mat-label>
            <input matInput formControlName="lastName" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Email</mat-label>
            <input matInput formControlName="email" type="email" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Username</mat-label>
            <input matInput formControlName="userName" />
          </mat-form-field>
          @if (data.mode === 'create') {
            <mat-form-field appearance="outline" class="span-2">
              <mat-label>Temporary password</mat-label>
              <input matInput formControlName="temporaryPassword" type="password" />
            </mat-form-field>
          }
          <mat-form-field appearance="outline">
            <mat-label>Phone</mat-label>
            <input matInput formControlName="phoneNumber" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Role</mat-label>
            <mat-select formControlName="role">
              @for (role of roles; track role) {
                <mat-option [value]="role">{{ roleLabels[role] }}</mat-option>
              }
            </mat-select>
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Department</mat-label>
            <mat-select formControlName="departmentId">
              <mat-option [value]="null">None</mat-option>
              @for (dept of flatDepartments; track dept.id) {
                <mat-option [value]="dept.id">{{ dept.name }}</mat-option>
              }
            </mat-select>
          </mat-form-field>
          <mat-form-field appearance="outline" class="span-2">
            <mat-label>Designation</mat-label>
            <input matInput formControlName="designation" />
          </mat-form-field>
        </div>
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button mat-button type="button" (click)="dialogRef.close()">Cancel</button>
        <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid">
          Save
        </button>
      </mat-dialog-actions>
    </form>
  `,
  styles: [
    `
      .dialog-content {
        min-width: min(640px, 92vw);
      }
      .grid {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 12px;
      }
      .span-2 {
        grid-column: span 2;
      }
    `,
  ],
})
export class IdentityUserFormDialogComponent {
  readonly data = inject<IdentityUserFormDialogData>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(
    MatDialogRef<IdentityUserFormDialogComponent, IdentityUserFormDialogResult>,
  );
  private readonly fb = inject(FormBuilder);

  readonly roles = ROLE_IDS.filter((r) => r !== 'SUPER_ADMIN');
  readonly roleLabels = ROLE_LABELS;

  readonly flatDepartments = flattenDepartments(this.data.departments);

  readonly form = this.fb.group({
    firstName: [this.data.user?.firstName ?? ''],
    lastName: [this.data.user?.lastName ?? ''],
    email: [this.data.user?.email ?? '', [Validators.required, Validators.email]],
    userName: [this.data.user?.userName ?? '', Validators.required],
    temporaryPassword: [
      this.data.mode === 'create' ? generateTempPassword() : '',
      this.data.mode === 'create' ? Validators.required : [],
    ],
    phoneNumber: [this.data.user?.phoneNumber ?? ''],
    role: [this.data.user?.role ?? 'USER', Validators.required],
    departmentId: [this.data.user?.departmentId ?? (null as string | null)],
    designation: [this.data.user?.designation ?? ''],
  });

  submit(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    if (this.data.mode === 'create') {
      this.dialogRef.close({
        action: 'create',
        payload: {
          firstName: v.firstName || undefined,
          lastName: v.lastName || undefined,
          email: v.email!,
          userName: v.userName!,
          temporaryPassword: v.temporaryPassword!,
          phoneNumber: v.phoneNumber || undefined,
          role: v.role!,
          departmentId: v.departmentId || undefined,
          designation: v.designation || undefined,
        },
      });
      return;
    }

    this.dialogRef.close({
      action: 'update',
      payload: {
        firstName: v.firstName || undefined,
        lastName: v.lastName || undefined,
        email: v.email || undefined,
        userName: v.userName || undefined,
        phoneNumber: v.phoneNumber || undefined,
        role: v.role || undefined,
        departmentId: v.departmentId || undefined,
        designation: v.designation || undefined,
      },
    });
  }
}

function flattenDepartments(
  nodes: DepartmentTreeNode[],
  depth = 0,
): Array<{ id: string; name: string }> {
  const out: Array<{ id: string; name: string }> = [];
  for (const node of nodes) {
    out.push({ id: node.id, name: `${'— '.repeat(depth)}${node.name}` });
    out.push(...flattenDepartments(node.children ?? [], depth + 1));
  }
  return out;
}

function generateTempPassword(): string {
  const chars = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%';
  return Array.from({ length: 14 }, () => chars[Math.floor(Math.random() * chars.length)]).join('');
}
