import { TestBed, ComponentFixture } from '@angular/core/testing';
import { describe, it, expect, beforeEach } from 'vitest';
import axe from 'axe-core';
import { ActivatedRoute } from '@angular/router';
import { AuthenticatedLayout } from './authenticated.layout';
import { CurrentUserService } from '../../services/auth/current-user.service';

describe('AuthenticatedLayout - Accessibility', () => {
  let fixture: ComponentFixture<AuthenticatedLayout>;
  let currentUserService: CurrentUserService;

  beforeEach(async () => {
    const activatedRoute = {
      snapshot: {
        paramMap: {
          get: () => '1',
        },
      },
    };

    await TestBed.configureTestingModule({
      imports: [AuthenticatedLayout],
      providers: [
        CurrentUserService,
        { provide: ActivatedRoute, useValue: activatedRoute },
      ],
    }).compileComponents();

    currentUserService = TestBed.inject(CurrentUserService);
    currentUserService.setUser({
      sub: '1',
      username: 'testuser',
      role: 'ControlLabAnalyst',
      site: 'Site A',
    });

    fixture = TestBed.createComponent(AuthenticatedLayout);
    fixture.detectChanges();
  });

  it('should not have any a11y violations', async () => {
    const results = await axe.run(fixture.nativeElement, {
      rules: {
        'color-contrast': { enabled: false },
      },
    });

    expect(results.violations).toEqual([]);
  }, 15000);
});


