import { OrganizationMember } from '../../core/services/organization.service';

/**
 * Raw row from GET `/organization-members/organization/{organizationId}`.
 * Field names tolerate common PascalCase / camelCase variants until OpenAPI is fixed in repo.
 */
export type OrganizationMemberApiRow = Record<string, unknown>;

/** Maps API / shared shape to {@link OrganizationMember} (assignee value = `userId` for task APIs). */
export function mapOrganizationMemberApiRow(row: OrganizationMemberApiRow): OrganizationMember {
  const userId = pickString(row, 'userId', 'UserId') ?? pickString(row, 'id', 'Id') ?? '';
  return {
    id: pickString(row, 'id', 'Id') ?? pickString(row, 'organizationMemberId', 'OrganizationMemberId') ?? userId,
    userId,
    role: pickString(row, 'role', 'Role') ?? '',
    departmentId: pickNullableString(row, 'departmentId', 'DepartmentId'),
    departmentName: pickNullableString(row, 'departmentName', 'DepartmentName'),
    designation: pickNullableString(row, 'designation', 'Designation'),
    reportsToUserId: pickNullableString(row, 'reportsToUserId', 'ReportsToUserId'),
    firstName: pickNullableString(row, 'firstName', 'FirstName'),
    lastName: pickNullableString(row, 'lastName', 'LastName'),
    email: pickNullableString(row, 'email', 'Email'),
    phoneNumber: pickNullableString(row, 'phoneNumber', 'PhoneNumber'),
    alternatePhoneNumber: pickNullableString(row, 'alternatePhoneNumber', 'AlternatePhoneNumber'),
    profilePicUrl: pickNullableString(row, 'profilePicUrl', 'ProfilePicUrl'),
    preferences: row['preferences'] ?? row['Preferences'] ?? null,
    createdAt: pickString(row, 'createdAt', 'CreatedAt') ?? '',
  };
}

function pickString(row: OrganizationMemberApiRow, a: string, b: string): string | undefined {
  const v = row[a] ?? row[b];
  return typeof v === 'string' && v.length ? v : undefined;
}

function pickNullableString(row: OrganizationMemberApiRow, a: string, b: string): string | null {
  const v = row[a] ?? row[b];
  if (v === null || v === undefined) return null;
  return typeof v === 'string' ? v : String(v);
}
