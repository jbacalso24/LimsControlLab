export type ScheduleAdherenceStatus = 'OnTrack' | 'Due' | 'Overdue' | 'Missed';

export interface AdherenceSummary {
  onTrack: number;
  due: number;
  overdue: number;
  missed: number;
  total: number;
}

export interface ScheduleAdherenceItem {
  scheduleId: number;
  name: string;
  analysisType: string | null;
  shiftPattern: 'Day' | 'Shift' | 'Weekly';
  cadenceLabel: string;
  status: ScheduleAdherenceStatus;
  assignedToUserId: number | null;
  assignedToUsername: string | null;
  lastAnalysisAtUtc: string | null;
  missedPeriods: number;
  currentPeriodStartUtc: string;
  currentPeriodEndUtc: string;
}

export interface ScheduleAdherenceResponse {
  asOfUtc: string;
  summary: AdherenceSummary;
  schedules: ScheduleAdherenceItem[];
}
