import { TestBed, ComponentFixture } from '@angular/core/testing';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import { of } from 'rxjs';
import axe from 'axe-core';
import { ActivatedRoute } from '@angular/router';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { provideAnimations } from '@angular/platform-browser/animations';
import { AnalysisExecutionComponent } from './analysis-execution.component';
import { AnalysisExecutionApiService } from './services/analysis-execution-api.service';

describe('AnalysisExecutionComponent - Accessibility', () => {
  let fixture: ComponentFixture<AnalysisExecutionComponent>;

  beforeEach(async () => {
    const activatedRoute = {
      snapshot: {
        paramMap: {
          get: vi.fn().mockReturnValue('1'),
        },
      },
    };

    const apiServiceSpy = {
      getAnalysis: vi.fn().mockReturnValue(of(null)),
      getInstruments: vi.fn().mockReturnValue(of([])),
      addReading: vi.fn(),
      resolveException: vi.fn(),
      changeStatus: vi.fn(),
    } as unknown as AnalysisExecutionApiService;

    await TestBed.configureTestingModule({
      imports: [AnalysisExecutionComponent, HttpClientTestingModule],
      providers: [
        provideAnimations(),
        { provide: ActivatedRoute, useValue: activatedRoute },
        { provide: AnalysisExecutionApiService, useValue: apiServiceSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AnalysisExecutionComponent);
    fixture.detectChanges();
  });

  it('should not have any a11y violations', async () => {
    const results = await axe.run(fixture.nativeElement, {
      rules: {
        'color-contrast': { enabled: false },
      },
    });

    expect(results.violations).toEqual([]);
  });
});


