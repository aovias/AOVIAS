import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { CreateAssignTaskDrawerComponent } from './create-assign-task-drawer.component';
import { JourneyWorkspaceStore } from '../../state/journey-workspace.store';
import { createEmptyCreateTaskDraftVm } from '../../models/create-task-draft-vm.model';
import { SessionService } from '../../../../core/auth/session.service';
import { provideNativeDateAdapter } from '@angular/material/core';
import { environment } from '../../../../../environments/environment';

describe('CreateAssignTaskDrawerComponent', () => {
  let fixture: ComponentFixture<CreateAssignTaskDrawerComponent>;
  let http: HttpTestingController;

  const orgId = '3fa85f64-5717-4562-b3fc-2c963f66afa6';

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CreateAssignTaskDrawerComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNativeDateAdapter(),
        JourneyWorkspaceStore,
        {
          provide: SessionService,
          useValue: {
            getOrganizationId: () => orgId,
            getUserId: () => 'user-1',
          },
        },
      ],
    }).compileComponents();

    const store = TestBed.inject(JourneyWorkspaceStore);
    store.setTaskAssignmentDraft(createEmptyCreateTaskDraftVm());
    store.openTaskAssignmentDrawer();

    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(CreateAssignTaskDrawerComponent);
    fixture.detectChanges();

    http
      .expectOne(`${environment.apiBaseUrl}/organization-members/organization/${orgId}`)
      .flush([]);
    http.expectOne(`${environment.apiBaseUrl}/common/master-departments`).flush([]);
    fixture.detectChanges();
  });

  it('creates the form with title and assignedToUserId controls', () => {
    const cmp = fixture.componentInstance;
    expect(cmp.taskForm.get('title')).toBeTruthy();
    expect(cmp.taskForm.controls.title.hasError('required')).toBe(true);
    expect(cmp.taskForm.get('assignedToUserId')).toBeTruthy();
    expect(cmp.taskForm.get('departmentId')).toBeTruthy();
  });
});
