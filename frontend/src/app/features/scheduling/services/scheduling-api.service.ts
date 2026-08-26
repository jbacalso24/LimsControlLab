import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { LimsApiService } from '../../../shared/services/api/lims/lims-api.service';
import { ScheduleDto } from '../../../shared/generated/models/schedule-dto';
import { CreateScheduleRequest } from '../../../shared/generated/models/create-schedule-request';
import { UpdateScheduleRequest } from '../../../shared/generated/models/update-schedule-request';
import { AssignScheduleRequest } from '../../../shared/generated/models/assign-schedule-request';

@Injectable({
  providedIn: 'root',
})
export class SchedulingApiService extends LimsApiService {
  listSchedules(): Observable<ScheduleDto[]> {
    return this.get<ScheduleDto[]>('/schedules');
  }

  getSchedule(id: number): Observable<ScheduleDto> {
    return this.get<ScheduleDto>(`/schedules/${id}`);
  }

  createSchedule(request: CreateScheduleRequest): Observable<ScheduleDto> {
    return this.post<ScheduleDto>('/schedules', request);
  }

  updateSchedule(
    id: number,
    request: UpdateScheduleRequest
  ): Observable<ScheduleDto> {
    return this.put<ScheduleDto>(`/schedules/${id}`, request);
  }

  deleteSchedule(id: number): Observable<void> {
    return this.delete<void>(`/schedules/${id}`);
  }

  assignSchedule(id: number, request: AssignScheduleRequest): Observable<ScheduleDto> {
    return this.post<ScheduleDto>(`/schedules/${id}/assign`, request);
  }
}
