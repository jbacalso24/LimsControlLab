import { TestBed, ComponentFixture } from '@angular/core/testing';
import { describe, it, expect, beforeEach } from 'vitest';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import axe from 'axe-core';
import { SampleTransferComponent } from './sample-transfer.component';

describe('SampleTransferComponent - Accessibility', () => {
  let fixture: ComponentFixture<SampleTransferComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SampleTransferComponent],
      providers: [
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: of({ get: (key: string) => (key === 'id' ? '1' : null) }),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SampleTransferComponent);
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


