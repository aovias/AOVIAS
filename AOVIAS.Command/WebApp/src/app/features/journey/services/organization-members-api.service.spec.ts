import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { OrganizationMembersApiService } from './organization-members-api.service';
import { environment } from '../../../../environments/environment';

describe('OrganizationMembersApiService', () => {
  let service: OrganizationMembersApiService;
  let http: HttpTestingController;

  const orgId = '3fa85f64-5717-4562-b3fc-2c963f66afa6';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), OrganizationMembersApiService],
    });
    service = TestBed.inject(OrganizationMembersApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('GETs members for organization and maps rows', async () => {
    const membersPromise = firstValueFrom(service.getMembersForOrganization(orgId));

    const req = http.expectOne(
      `${environment.apiBaseUrl}/organization-members/organization/${orgId}`,
    );
    expect(req.request.method).toBe('GET');
    req.flush([
      {
        id: 'om-1',
        userId: 'user-uuid-1',
        role: 'ORG_ADMIN',
        firstName: 'Ada',
        lastName: 'Lovelace',
        email: 'ada@example.com',
        phoneNumber: null,
        alternatePhoneNumber: null,
        profilePicUrl: null,
        preferences: null,
        createdAt: '2026-01-01T00:00:00Z',
      },
    ]);

    const members = await membersPromise;
    expect(members.length).toBe(1);
    expect(members[0].userId).toBe('user-uuid-1');
    expect(members[0].firstName).toBe('Ada');
  });

  it('returns empty array without calling HTTP when organization id is invalid', async () => {
    const members = await firstValueFrom(service.getMembersForOrganization('not-a-guid'));
    expect(members).toEqual([]);
  });

  it('propagates HTTP errors', async () => {
    const membersPromise = firstValueFrom(service.getMembersForOrganization(orgId));

    const req = http.expectOne(
      `${environment.apiBaseUrl}/organization-members/organization/${orgId}`,
    );
    req.flush('Not found', { status: 404, statusText: 'Not Found' });

    await expect(membersPromise).rejects.toMatchObject({ status: 404 });
  });
});
