import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { describe, it, beforeEach, afterEach, expect } from 'vitest';
import { SchedulingApiService } from './scheduling-api.service';
import { ScheduleDto } from '../../../shared/generated/models/schedule-dto';
import { CreateScheduleRequest } from '../../../shared/generated/models/create-schedule-request';
import { UpdateScheduleRequest } from '../../../shared/generated/models/update-schedule-request';
import { AssignScheduleRequest } from '../../../shared/generated/models/assign-schedule-request';

describe('SchedulingApiService', () => {
  let service: SchedulingApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [SchedulingApiService],
    });

    service = TestBed.inject(SchedulingApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should list schedules', () => {
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

    service.listSchedules().subscribe((result) => {
      expect(result).toEqual(mockSchedules);
    });

    const req = httpMock.expectOne('/schedules');
    expect(req.request.method).toBe('GET');
    req.flush(mockSchedules);
  });

  it('should get a schedule', () => {
    const mockSchedule: ScheduleDto = {
      id: 1,
      name: 'Schedule 1',
      site: 'Site1',
      shiftPattern: '3x8',
      isActive: true,
      rowVersion: 'v1',
    };

    service.getSchedule(1).subscribe((result) => {
      expect(result).toEqual(mockSchedule);
    });

    const req = httpMock.expectOne('/schedules/1');
    expect(req.request.method).toBe('GET');
    req.flush(mockSchedule);
  });

  it('should create a schedule', () => {
    const request: CreateScheduleRequest = {
      name: 'New Schedule',
      site: 'Site1',
      shiftPattern: '3x8',
    };
    const mockSchedule: ScheduleDto = {
      id: 1,
      ...request,
      isActive: true,
      rowVersion: 'v1',
    };

    service.createSchedule(request).subscribe((result) => {
      expect(result).toEqual(mockSchedule);
    });

    const req = httpMock.expectOne('/schedules');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(mockSchedule);
  });

  it('should update a schedule', () => {
    const request: UpdateScheduleRequest = {
      name: 'Updated Schedule',
      shiftPattern: '3x8',
      isActive: true,
      rowVersion: 'v1',
    };
    const mockSchedule: ScheduleDto = {
      id: 1,
      ...request,
      site: 'Site1',
    };

    service.updateSchedule(1, request).subscribe((result) => {
      expect(result).toEqual(mockSchedule);
    });

    const req = httpMock.expectOne('/schedules/1');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush(mockSchedule);
  });

  it('should delete a schedule', () => {
    service.deleteSchedule(1).subscribe(() => {
      expect(true).toBe(true);
    });

    const req = httpMock.expectOne('/schedules/1');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('should assign a schedule', () => {
    const request: AssignScheduleRequest = {
      userId: 1,
    };
    const mockSchedule: ScheduleDto = {
      id: 1,
      name: 'Schedule 1',
      site: 'Site1',
      shiftPattern: '3x8',
      isActive: true,
      assignedToUserId: 1,
      rowVersion: 'v1',
    };

    service.assignSchedule(1, request).subscribe((result) => {
      expect(result).toEqual(mockSchedule);
    });

    const req = httpMock.expectOne('/schedules/1/assign');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(mockSchedule);
  });
});
