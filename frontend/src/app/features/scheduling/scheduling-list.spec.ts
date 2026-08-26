import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { describe, it, beforeEach, expect, vi } from 'vitest';
import { SchedulingListComponent } from './scheduling-list.component';
import { SchedulingApiService } from './services/scheduling-api.service';
import { CurrentUserService } from '../../shared/services/auth/current-user.service';
import { of, throwError } from 'rxjs';
import { ScheduleDto } from '../../shared/generated/models/schedule-dto';
import { provideRouter } from '@angular/router';

describe('SchedulingListComponent', () => {
  let component: SchedulingListComponent;
  let fixture: ComponentFixture<SchedulingListComponent>;
  let apiService: SchedulingApiService;
  let currentUserService: CurrentUserService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SchedulingListComponent, HttpClientTestingModule],
      providers: [
        SchedulingApiService,
        CurrentUserService,
        provideRouter([]),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SchedulingListComponent);
    component = fixture.componentInstance;
    apiService = TestBed.inject(SchedulingApiService);
    currentUserService = TestBed.inject(CurrentUserService);
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load schedules on init', () => {
    const mockSchedules: ScheduleDto[] = [
      {
        id: 1,
        name: 'Schedule 1',
        site: 'Site1',
        shiftPattern: '3x8',
        isActive: true,
        rowVersion: 'v1',
      },
    ];
    vi.spyOn(apiService, 'listSchedules').mockReturnValue(of(mockSchedules));

    fixture.detectChanges();

    expect(component.schedules()).toEqual(mockSchedules);
    expect(component.loading()).toBe(false);
  });

  it('should handle error loading schedules', () => {
    vi.spyOn(apiService, 'listSchedules').mockReturnValue(
      throwError(() => new Error('Failed to load'))
    );

    fixture.detectChanges();

    expect(component.loading()).toBe(false);
    expect(component.error()).toBeTruthy();
  });

  it('should show create button for lab coordinator', () => {
    currentUserService.setUser({
      sub: 'user1',
      username: 'user1',
      role: 'LabCoordinator',
      site: 'Site1',
    });
    vi.spyOn(apiService, 'listSchedules').mockReturnValue(of([]));

    fixture.detectChanges();

    expect(component.isLabCoordinator()).toBe(true);
  });

  it('should not show create button for analyst', () => {
    currentUserService.setUser({
      sub: 'user1',
      username: 'user1',
      role: 'ControlLabAnalyst',
      site: 'Site1',
    });
    vi.spyOn(apiService, 'listSchedules').mockReturnValue(of([]));

    fixture.detectChanges();

    expect(component.isLabCoordinator()).toBe(false);
  });

  it('should delete a schedule', () => {
    const mockSchedule: ScheduleDto = {
      id: 1,
      name: 'Schedule 1',
      site: 'Site1',
      shiftPattern: '3x8',
      isActive: true,
      rowVersion: 'v1',
    };
    currentUserService.setUser({
      sub: 'user1',
      username: 'user1',
      role: 'LabCoordinator',
      site: 'Site1',
    });
    vi.spyOn(apiService, 'listSchedules').mockReturnValue(of([mockSchedule]));
    const deleteSpy = vi.spyOn(apiService, 'deleteSchedule').mockReturnValue(of(void 0));
    vi.spyOn(window, 'confirm').mockReturnValue(true);

    component.delete(mockSchedule);

    expect(deleteSpy).toHaveBeenCalledWith(1);
  });

  it('should not delete if confirm is cancelled', () => {
    const mockSchedule: ScheduleDto = {
      id: 1,
      name: 'Schedule 1',
      site: 'Site1',
      shiftPattern: '3x8',
      isActive: true,
      rowVersion: 'v1',
    };
    const deleteSpy = vi.spyOn(apiService, 'deleteSchedule').mockReturnValue(of(void 0));
    vi.spyOn(window, 'confirm').mockReturnValue(false);

    component.delete(mockSchedule);

    expect(deleteSpy).not.toHaveBeenCalled();
  });
});
