import { Organization } from './organization.model';
import { Hospital } from './hospital.model';
import { AdminUser } from './admin-user.model';

export interface OnboardingRequest {
  organization: Organization;
  hospital: Hospital;
  adminUser: AdminUser;
}
