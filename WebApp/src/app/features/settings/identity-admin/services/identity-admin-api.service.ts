import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { BaseApiService } from '../../../../shared/services/base-api.service';
import {
  CreateIdentityUserPayload,
  DepartmentTreeNode,
  IdentityUserDetail,
  IdentityUserListFilters,
  PagedIdentityUsers,
  UpdateIdentityUserPayload,
} from '../models/identity-user-admin.model';

@Injectable({ providedIn: 'root' })
export class IdentityAdminApiService extends BaseApiService {
  private readonly usersBase = '/identity/users';
  private readonly passwordBase = '/identity/password-management';

  listUsers(filters: IdentityUserListFilters = {}): Observable<PagedIdentityUsers> {
    return this.get<PagedIdentityUsers>(this.usersBase, filters);
  }

  getUser(userId: string): Observable<IdentityUserDetail> {
    return this.get<IdentityUserDetail>(`${this.usersBase}/${userId}`);
  }

  createUser(payload: CreateIdentityUserPayload): Observable<IdentityUserDetail> {
    return this.post<IdentityUserDetail>(this.usersBase, payload);
  }

  updateUser(userId: string, payload: UpdateIdentityUserPayload): Observable<IdentityUserDetail> {
    return this.put<IdentityUserDetail>(`${this.usersBase}/${userId}`, payload);
  }

  suspendUser(userId: string): Observable<void> {
    return this.post<void>(`${this.usersBase}/${userId}/suspend`, {}).pipe(map(() => undefined));
  }

  activateUser(userId: string): Observable<void> {
    return this.post<void>(`${this.usersBase}/${userId}/activate`, {}).pipe(map(() => undefined));
  }

  disableUser(userId: string): Observable<void> {
    return this.post<void>(`${this.usersBase}/${userId}/disable`, {}).pipe(map(() => undefined));
  }

  deleteUser(userId: string): Observable<void> {
    return this.delete<void>(`${this.usersBase}/${userId}`).pipe(map(() => undefined));
  }

  unlockUser(userId: string): Observable<void> {
    return this.post<void>(`${this.usersBase}/${userId}/unlock`, {}).pipe(map(() => undefined));
  }

  resetPassword(userId: string): Observable<{ message: string }> {
    return this.post<{ message: string }>(`${this.passwordBase}/${userId}/reset`, {});
  }

  forcePasswordChange(userId: string): Observable<void> {
    return this.post<void>(`${this.passwordBase}/${userId}/force-change`, {}).pipe(
      map(() => undefined),
    );
  }

  unlockPasswordManagement(userId: string): Observable<void> {
    return this.post<void>(`${this.passwordBase}/${userId}/unlock`, {}).pipe(map(() => undefined));
  }

  getDepartmentTree(organizationId: string): Observable<DepartmentTreeNode[]> {
    return this.get<DepartmentTreeNode[]>('/departments/tree', { organizationId });
  }
}
