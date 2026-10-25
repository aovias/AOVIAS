export interface IdentityUserListItem {
  membershipId: string;
  userId: string;
  organizationId: string;
  firstName?: string | null;
  lastName?: string | null;
  fullName: string;
  email?: string | null;
  userName?: string | null;
  phoneNumber?: string | null;
  profilePicUrl?: string | null;
  departmentId?: string | null;
  departmentName?: string | null;
  designation?: string | null;
  role: string;
  roles: string[];
  membershipStatus: string;
  accountStatus: string;
  isDeleted: boolean;
  forcePasswordChange: boolean;
  isLockedOut: boolean;
  lastLoginAt?: string | null;
  createdAt: string;
  employeeNumber?: string | null;
  employmentId?: string | null;
  personId?: string | null;
}

export interface IdentityUserDetail extends IdentityUserListItem {
  platformId: string;
  passwordResetRequired: boolean;
  lockoutEnd?: string | null;
}

export interface PagedIdentityUsers {
  items: IdentityUserListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface CreateIdentityUserPayload {
  email: string;
  userName: string;
  temporaryPassword: string;
  phoneNumber?: string;
  role?: string;
  personId?: string;
  departmentId?: string;
  designation?: string;
  firstName?: string;
  lastName?: string;
  hospitalId?: string;
  employmentId?: string;
}

export interface UpdateIdentityUserPayload {
  email?: string;
  userName?: string;
  phoneNumber?: string;
  role?: string;
  status?: string;
  departmentId?: string;
  designation?: string;
  firstName?: string;
  lastName?: string;
  membershipStatus?: string;
}

export interface DepartmentTreeNode {
  id: string;
  code: string;
  name: string;
  children: DepartmentTreeNode[];
}

export interface IdentityUserListFilters {
  search?: string;
  role?: string;
  departmentId?: string;
  membershipStatus?: string;
  includeInactive?: boolean;
  page?: number;
  pageSize?: number;
}
