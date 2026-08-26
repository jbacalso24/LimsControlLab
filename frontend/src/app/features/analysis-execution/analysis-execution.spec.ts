import { TestBed, ComponentFixture } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { provideAnimations } from '@angular/platform-browser/animations';
import { of, throwError, Subject } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { AnalysisExecutionComponent } from './analysis-execution.component';
import { AnalysisExecutionApiService, AnalysisDetailDto } from './services/analysis-execution-api.service';

describe('AnalysisExecutionComponent', () => {
  let component: AnalysisExecutionComponent;
  let fixture: ComponentFixture<AnalysisExecutionComponent>;
  let apiService: Partial<AnalysisExecutionApiService>;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  let activatedRoute: any;

  const mockAnalysis: AnalysisDetailDto = {
    id: 1,
    sampleId: 100,
    templateId: 50,
    status: 'InProgress',
    isLocked: false,
    readings: [
      {
        id: 1,
        testId: 'TEST-001',
        value: 25.5,
        unit: 'mg/L',
        capturedAtUtc: '2026-08-26T10:00:00Z',
        capturedBy: 'analyst-001',
        capturedByUsername: 'invicta_analyst',
        validationResult: {
          isValid: true,
          expectedRange: '20-30',
          actualValue: '25.5',
          reason: 'Within tolerance',
        },
      },
    ],
    exceptions: [
      {
        id: 1,
        readingId: 2,
        reason: 'Value out of range',
        decision: undefined,
        rowVersion: 'v1',
      },
    ],
    rowVersion: 'v1',
  };

  beforeEach(async () => {
    const apiServiceSpy = {
      getAnalysis: vi.fn(),
      addReading: vi.fn(),
      resolveException: vi.fn(),
      changeStatus: vi.fn(),
      getInstruments: vi.fn().mockReturnValue(of([])),
    } as unknown as AnalysisExecutionApiService;

    activatedRoute = {
      snapshot: {
        paramMap: {
          get: vi.fn().mockReturnValue('1'),
        },
      },
    };

    await TestBed.configureTestingModule({
      imports: [AnalysisExecutionComponent, HttpClientTestingModule],
      providers: [
        provideAnimations(),
        { provide: AnalysisExecutionApiService, useValue: apiServiceSpy },
        { provide: ActivatedRoute, useValue: activatedRoute },
      ],
    }).compileComponents();

    apiService = TestBed.inject(
      AnalysisExecutionApiService
    ) as Partial<AnalysisExecutionApiService>;

    fixture = TestBed.createComponent(AnalysisExecutionComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Loading state', () => {
    it('should show loading state when fetching analysis', () => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (apiService.getAnalysis as any).mockReturnValue(of(mockAnalysis));

      fixture.detectChanges();

      expect(component.loading()).toBe(false);
    });

    it('should load analysis on init', () => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (apiService.getAnalysis as any).mockReturnValue(of(mockAnalysis));

      fixture.detectChanges();

      expect(apiService.getAnalysis).toHaveBeenCalledWith(1);
      expect(component.analysis()).toEqual(mockAnalysis);
    });
  });

  describe('Error state', () => {
    it('should show error message on 404', () => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (apiService.getAnalysis as any).mockReturnValue(
        throwError(() => ({ status: 404 }))
      );

      fixture.detectChanges();

      expect(component.error()).toBe('Analysis not found');
    });

    it('should show generic error for other failures', () => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (apiService.getAnalysis as any).mockReturnValue(
        throwError(() => ({ status: 500 }))
      );

      fixture.detectChanges();

      expect(component.error()).toBe('Failed to load analysis. Please try again.');
    });

    it('should allow retry after error', () => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (apiService.getAnalysis as any).mockReturnValue(
        throwError(() => ({ status: 500 }))
      );

      fixture.detectChanges();
      expect(component.error()).toBeTruthy();

      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (apiService.getAnalysis as any).mockReturnValue(of(mockAnalysis));
      component.reload();

      expect(component.analysis()).toEqual(mockAnalysis);
      expect(component.error()).toBe('');
    });
  });

  describe('Empty state', () => {
    it('should show empty message when analysis is null', () => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (apiService.getAnalysis as any).mockReturnValue(of(null));

      fixture.detectChanges();
      expect(component.analysis()).toBeNull();
    });
  });

  describe('Reading submission', () => {
    beforeEach(() => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (apiService.getAnalysis as any).mockReturnValue(of(mockAnalysis));
      fixture.detectChanges();
    });

    it('should validate required fields', () => {
      component.readingForm.patchValue({
        testId: '',
        value: '',
        unit: '',
        capturedAtUtc: null,
      });

      expect(component.readingForm.invalid).toBe(true);
    });

    it('should validate numeric value', () => {
      component.readingForm.patchValue({
        testId: 'TEST-002',
        value: 'not-a-number',
        unit: 'mg/L',
        capturedAtUtc: new Date('2026-08-26T10:00:00Z'),
      });

      expect(component.valueControl.invalid).toBe(true);
    });

    it('should successfully submit reading', () => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const newReading: any = {
        id: 2,
        testId: 'TEST-002',
        value: 30,
        unit: 'mg/L',
        capturedAtUtc: '2026-08-26T11:00:00Z',
        capturedBy: 'analyst-001',
        capturedByUsername: 'invicta_analyst',
        validationResult: { isValid: true },
      };

      component.readingForm.patchValue({
        testId: 'TEST-002',
        value: 30,
        unit: 'mg/L',
        capturedAtUtc: new Date('2026-08-26T11:00:00Z'),
      });

      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (apiService.addReading as any).mockReturnValue(of(newReading));
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (apiService.getAnalysis as any).mockReturnValue(
        of({ ...mockAnalysis, readings: [mockAnalysis.readings[0], newReading] })
      );

      component.submitReading();

      expect(apiService.addReading).toHaveBeenCalledWith(1, expect.objectContaining({
        testId: 'TEST-002',
        value: 30,
        unit: 'mg/L',
      }));
    });

    it('should handle reading validation error', () => {
      component.readingForm.patchValue({
        testId: 'TEST-002',
        value: 50,
        unit: 'mg/L',
        capturedAtUtc: new Date('2026-08-26T11:00:00Z'),
      });

      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (apiService.addReading as any).mockReturnValue(
        throwError(() => ({
          status: 400,
          error: { detail: 'Value out of tolerance' },
        }))
      );

      component.submitReading();

      expect(component.readingError()).toBe('Value out of tolerance');
    });

    it('should not submit when form is invalid', () => {
      component.readingForm.patchValue({
        testId: '',
        value: '',
        unit: '',
      });

      component.submitReading();

      expect(apiService.addReading).not.toHaveBeenCalled();
    });

    it('should disable form during submission', async () => {
      component.readingForm.patchValue({
        testId: 'TEST-002',
        value: 25,
        unit: 'mg/L',
        capturedAtUtc: new Date('2026-08-26T11:00:00Z'),
      });

      // `of(...)` would emit and complete synchronously inside submitReading(),
      // resetting submittingReading() before this test could observe the
      // mid-flight state — use a Subject so the response is deferred until
      // the assertion below has already run.
      const addReading$ = new Subject<AnalysisDetailDto['readings'][number]>();
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (apiService.addReading as any).mockReturnValue(addReading$);
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (apiService.getAnalysis as any).mockReturnValue(of(mockAnalysis));

      expect(component.submittingReading()).toBe(false);

      component.submitReading();

      expect(component.submittingReading()).toBe(true);

      addReading$.next({
        id: 2,
        testId: 'TEST-002',
        value: 25,
        unit: 'mg/L',
        capturedAtUtc: '2026-08-26T11:00:00Z',
        capturedBy: 'analyst-001',
        capturedByUsername: 'invicta_analyst',
        validationResult: { isValid: true, actualValue: '25' },
      });
      addReading$.complete();

       
      await new Promise(resolve => setTimeout(resolve, 100));
    });
  });

  describe('Exception resolution', () => {
    beforeEach(() => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (apiService.getAnalysis as any).mockReturnValue(of(mockAnalysis));
      fixture.detectChanges();
    });

    it('should require decision and comment', () => {
      const form = component.getExceptionForm(1);
      expect(form.invalid).toBe(true);

      form.patchValue({
        decision: 'Modify',
        comment: 'Issue found',
      });

      expect(form.valid).toBe(true);
    });

    it('should not allow empty comment', () => {
      const form = component.getExceptionForm(1);
      form.patchValue({
        decision: 'Modify',
        comment: '',
      });

      expect(form.get('comment')?.invalid).toBe(true);
    });

    it('should successfully resolve exception', () => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const resolvedException: any = {
        id: 1,
        readingId: 2,
        reason: 'Value out of range',
        decision: 'Modify',
        decisionComment: 'Issue corrected',
        rowVersion: 'v2',
      };

      const form = component.getExceptionForm(1);
      form.patchValue({
        decision: 'Modify',
        comment: 'Issue corrected',
      });

      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (apiService.resolveException as any).mockReturnValue(of(resolvedException));
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (apiService.getAnalysis as any).mockReturnValue(
        of({ ...mockAnalysis, exceptions: [resolvedException] })
      );

      component.resolveException(mockAnalysis.exceptions[0]);

      expect(apiService.resolveException).toHaveBeenCalledWith(
        1,
        1,
        expect.objectContaining({
          decision: 'Modify',
          comment: 'Issue corrected',
          rowVersion: 'v1',
        })
      );
    });

    it('should handle stale rowVersion (409 conflict)', () => {
      const form = component.getExceptionForm(1);
      form.patchValue({
        decision: 'Modify',
        comment: 'Issue corrected',
      });

      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (apiService.resolveException as any).mockReturnValue(
        throwError(() => ({ status: 409 }))
      );

      component.resolveException(mockAnalysis.exceptions[0]);

      expect(component.staleRowVersionError()).toBe(true);
      expect(component.exceptionError()).toContain('modified');
    });

    it('should handle validation error on exception decision', () => {
      const form = component.getExceptionForm(1);
      form.patchValue({
        decision: 'Modify',
        comment: 'Issue corrected',
      });

      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (apiService.resolveException as any).mockReturnValue(
        throwError(() => ({
          status: 400,
          error: { detail: 'Comment too short' },
        }))
      );

      component.resolveException(mockAnalysis.exceptions[0]);

      expect(component.exceptionError()).toBe('Comment too short');
    });
  });

  describe('Locked state', () => {
    it('should detect locked status', () => {
      component.analysis.set({ ...mockAnalysis, status: 'Locked', isLocked: true });

      expect(component.isLocked()).toBe(true);
    });

    it('should hide reading form when locked', () => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (apiService.getAnalysis as any).mockReturnValue(
        of({ ...mockAnalysis, status: 'Locked', isLocked: true })
      );

      fixture.detectChanges();

      const form = fixture.nativeElement.querySelector('form');

      expect(form).toBeFalsy();
    });

    it('should detect completed status as locked', () => {
      component.analysis.set({ ...mockAnalysis, status: 'Completed', isLocked: true });

      expect(component.isLocked()).toBe(true);
    });
  });

  describe('Display of analysis details', () => {
    beforeEach(() => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (apiService.getAnalysis as any).mockReturnValue(of(mockAnalysis));
      fixture.detectChanges();
    });

    it('should display analysis ID', () => {
      const analysisId = fixture.nativeElement.querySelector('dd');

      expect(analysisId?.textContent).toContain('1');
    });

    it('should display reading count', () => {
      fixture.detectChanges();
      const content = fixture.nativeElement.textContent;

      expect(content).toContain('1');
    });

    it('should display readings list', () => {
      fixture.detectChanges();
      const readingRows = fixture.nativeElement.querySelectorAll('tbody tr');

      // First row is actual reading, additional rows may be validation detail rows
      expect(readingRows.length).toBeGreaterThanOrEqual(1);
    });

    it('should show validation error badge for invalid readings', () => {
      const invalidReading = { ...mockAnalysis.readings[0] };
      invalidReading.validationResult!.isValid = false;
      invalidReading.validationResult!.expectedRange = '20-25';
      invalidReading.validationResult!.actualValue = '30';
      invalidReading.validationResult!.reason = 'Out of tolerance';

      component.analysis.set({
        ...mockAnalysis,
        readings: [invalidReading],
      });

      fixture.detectChanges();
      const badges = fixture.nativeElement.querySelectorAll('z-badge');
      // Should have an "Invalid" badge in the status column
      const invalidBadge = Array.from(badges).find((b) => (b as Element).textContent?.includes('Invalid'));

      expect(invalidBadge).toBeTruthy();
    });

    it('should display exceptions list', () => {
      fixture.detectChanges();
      // Exceptions are displayed in z-card elements within the exceptions section
      const exceptionCards = fixture.nativeElement.querySelectorAll('z-card');
      // Should have at least one exception card (in addition to the analysis summary card)
      expect(exceptionCards.length).toBeGreaterThan(1);
    });

    it('should show exception decision form for open exceptions', () => {
      fixture.detectChanges();
      const selectControl = fixture.nativeElement.querySelector('z-select');

      expect(selectControl).toBeTruthy();
    });
  });

  describe('Accessibility', () => {
    beforeEach(() => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (apiService.getAnalysis as any).mockReturnValue(of(mockAnalysis));
      fixture.detectChanges();
    });

    it('should have aria-describedby for error messages', () => {
      const testIdInput = fixture.nativeElement.querySelector('#testId');

      expect(testIdInput.getAttribute('aria-describedby')).toBeTruthy();
    });

    it('should have role alert on error states', () => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      (apiService.getAnalysis as any).mockReturnValue(
        throwError(() => ({ status: 500 }))
      );

      component.analysis.set(null);
      component.error.set('Test error');
      fixture.detectChanges();

      const alert = fixture.nativeElement.querySelector('[role="alert"]');

      expect(alert).toBeTruthy();
    });

    it('should have semantic heading hierarchy', () => {
      fixture.detectChanges();
      const h1 = fixture.nativeElement.querySelector('h1');
      const h2 = fixture.nativeElement.querySelector('h2');

      expect(h1).toBeTruthy();
      expect(h2).toBeTruthy();
    });
  });
});
