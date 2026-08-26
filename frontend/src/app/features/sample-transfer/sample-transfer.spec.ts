import { TestBed, ComponentFixture } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';
import { SampleTransferComponent } from './sample-transfer.component';
import { SampleTransferApiService } from './services/sample-transfer-api.service';
import { CurrentUserService } from '../../shared/services/auth/current-user.service';

describe('SampleTransferComponent', () => {
  let component: SampleTransferComponent;
  let fixture: ComponentFixture<SampleTransferComponent>;
  let apiService: SampleTransferApiService;
  let currentUserService: CurrentUserService;

  const mockSample = {
    id: 1,
    identifier: 'SAMPLE-001',
    site: 'Site1',
    currentSite: 'Site1',
    status: 'InProgress',
    analysisTemplateId: 1,
    rowVersion: 'AQAAAAIAAAA=',
  };

  const mockTransfer = {
    id: 1,
    fromSite: 'Site1',
    toSite: 'Site2',
    transferredAtUtc: '2026-08-26T10:30:00Z',
    rowVersion: 'AQAAAAMAAAA=',
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SampleTransferComponent, HttpClientTestingModule],
      providers: [
        SampleTransferApiService,
        {
          provide: CurrentUserService,
          useValue: {
            user: () => ({
              sub: 'testuser',
              username: 'testuser',
              role: 'ControlLabAnalyst',
              site: 'Site1',
            }),
          },
        },
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: of({ get: (key: string) => (key === 'id' ? '1' : null) }),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SampleTransferComponent);
    component = fixture.componentInstance;
    apiService = TestBed.inject(SampleTransferApiService);
    currentUserService = TestBed.inject(CurrentUserService);
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Loading sample', () => {
    it('should load sample on init when id param is provided', () => {
      vi.spyOn(apiService, 'getSample').mockReturnValue(of(mockSample));

      component.ngOnInit();

      expect(component.sample()).toEqual(mockSample);
      expect(component.loading()).toBe(false);
    });

    it('should set loading state while fetching sample', () => {
      vi.spyOn(apiService, 'getSample').mockReturnValue(of(mockSample));

      component.ngOnInit();
      expect(component.loading()).toBe(false);
    });

    it('should handle 404 error', () => {
      vi.spyOn(apiService, 'getSample').mockReturnValue(
        throwError(() => ({ status: 404 }))
      );

      component.ngOnInit();

      expect(component.error404()).toBe(true);
      expect(component.error403()).toBe(false);
      expect(component.loading()).toBe(false);
    });

    it('should handle 403 error', () => {
      vi.spyOn(apiService, 'getSample').mockReturnValue(
        throwError(() => ({ status: 403 }))
      );

      component.ngOnInit();

      expect(component.error403()).toBe(true);
      expect(component.error404()).toBe(false);
      expect(component.loading()).toBe(false);
    });

    it('should handle other errors', () => {
      vi.spyOn(apiService, 'getSample').mockReturnValue(
        throwError(() => ({ status: 500 }))
      );

      component.ngOnInit();

      expect(component.errorOther()).toBe('Failed to load sample. Please try again.');
      expect(component.error404()).toBe(false);
      expect(component.error403()).toBe(false);
    });
  });

  describe('Read-only vs editable gating', () => {
    it('should allow transfer when current user site matches currentSite', () => {
      component.sample.set(mockSample);

      expect(component.canTransfer()).toBe(true);
    });

    it('should not allow transfer when current user site differs from currentSite', () => {
      component.sample.set(mockSample);
      vi.spyOn(currentUserService, 'user').mockReturnValue({
        sub: 'testuser',
        username: 'testuser',
        role: 'ControlLabAnalyst',
        site: 'Site2',
      });

      expect(component.canTransfer()).toBe(false);
    });

    it('should not allow transfer when sample is null', () => {
      component.sample.set(null);

      expect(component.canTransfer()).toBe(false);
    });
  });

  describe('Transfer dialog', () => {
    beforeEach(() => {
      component.sample.set(mockSample);
    });

    it('should open transfer dialog', () => {
      component.openTransferDialog();

      expect(component.showTransferDialog()).toBe(true);
    });

    it('should close transfer dialog', () => {
      component.showTransferDialog.set(true);

      component.closeTransferDialog();

      expect(component.showTransferDialog()).toBe(false);
      expect(component.transferError()).toBe('');
      expect(component.staleRowVersionError()).toBe(false);
    });

    it('should reset form on dialog open', () => {
      component.transferForm.patchValue({ toSite: 'Site2' });

      component.openTransferDialog();

      expect(component.transferForm.get('toSite')?.value).toBe('');
    });
  });

  describe('Transfer form validation', () => {
    it('should require toSite field', () => {
      const form = component.transferForm;

      form.patchValue({ toSite: '' });
      expect(form.invalid).toBe(true);

      form.patchValue({ toSite: 'Site2' });
      expect(form.valid).toBe(true);
    });

    it('should disable submit button when form is invalid', () => {
      component.transferForm.patchValue({ toSite: '' });

      expect(component.transferForm.invalid).toBe(true);
    });

    it('should enable submit button when form is valid', () => {
      component.transferForm.patchValue({ toSite: 'Site2' });

      expect(component.transferForm.valid).toBe(true);
    });
  });

  describe('Submitting transfer', () => {
    beforeEach(() => {
      component.sample.set(mockSample);
    });

    it('should not submit when form is invalid', () => {
      vi.spyOn(apiService, 'transferSample');
      component.transferForm.patchValue({ toSite: '' });

      component.submitTransfer();

      expect(apiService.transferSample).not.toHaveBeenCalled();
    });

    it('should transfer sample when form is valid', () => {
      vi.spyOn(apiService, 'transferSample').mockReturnValue(of(mockTransfer));
      vi.spyOn(apiService, 'getSample').mockReturnValue(of(mockSample));

      component.transferForm.patchValue({ toSite: 'Site2' });
      component.submitTransfer();

      expect(apiService.transferSample).toHaveBeenCalledWith(1, {
        toSite: 'Site2',
        rowVersion: 'AQAAAAIAAAA=',
      });
    });

    it('should set transferring state before completing', () => {
      vi.spyOn(apiService, 'transferSample').mockReturnValue(of(mockTransfer));
      vi.spyOn(apiService, 'getSample').mockReturnValue(of(mockSample));

      component.transferForm.patchValue({ toSite: 'Site2' });
      component.submitTransfer();

      // After completion, transferring should be false
      expect(component.transferring()).toBe(false);
    });

    it('should close dialog after successful transfer', () => {
      vi.spyOn(apiService, 'transferSample').mockReturnValue(of(mockTransfer));
      vi.spyOn(apiService, 'getSample').mockReturnValue(of(mockSample));

      component.openTransferDialog();
      component.transferForm.patchValue({ toSite: 'Site2' });
      component.submitTransfer();

      expect(component.showTransferDialog()).toBe(false);
    });

    it('should set error on 409 Conflict', () => {
      vi.spyOn(apiService, 'transferSample').mockReturnValue(
        throwError(() => ({ status: 409 }))
      );

      component.transferForm.patchValue({ toSite: 'Site2' });
      component.submitTransfer();

      expect(component.staleRowVersionError()).toBe(true);
      expect(component.transferring()).toBe(false);
    });

    it('should set generic error on other errors', () => {
      vi.spyOn(apiService, 'transferSample').mockReturnValue(
        throwError(() => ({
          status: 400,
          error: { message: 'Invalid request' },
        }))
      );

      component.transferForm.patchValue({ toSite: 'Site2' });
      component.submitTransfer();

      expect(component.transferError()).toBe('Invalid request');
      expect(component.transferring()).toBe(false);
    });

    it('should reload sample after successful transfer', () => {
      vi.spyOn(apiService, 'transferSample').mockReturnValue(of(mockTransfer));
      vi.spyOn(apiService, 'getSample').mockReturnValue(of(mockSample));

      component.transferForm.patchValue({ toSite: 'Site2' });
      component.submitTransfer();

      expect(apiService.getSample).toHaveBeenCalledWith(1);
    });
  });

  describe('rowVersion threading', () => {
    it('should include rowVersion in transfer request', () => {
      component.sample.set({
        ...mockSample,
        rowVersion: 'EXPECTED_ROW_VERSION',
      });

      vi.spyOn(apiService, 'transferSample').mockReturnValue(of(mockTransfer));
      vi.spyOn(apiService, 'getSample').mockReturnValue(of(mockSample));

      component.transferForm.patchValue({ toSite: 'Site2' });
      component.submitTransfer();

      expect(apiService.transferSample).toHaveBeenCalledWith(1, {
        toSite: 'Site2',
        rowVersion: 'EXPECTED_ROW_VERSION',
      });
    });

    it('should not submit when sample is null', () => {
      component.sample.set(null);
      vi.spyOn(apiService, 'transferSample');

      component.transferForm.patchValue({ toSite: 'Site2' });
      component.submitTransfer();

      expect(apiService.transferSample).not.toHaveBeenCalled();
    });
  });

  describe('Retry functionality', () => {
    it('should reload sample on retry', () => {
      component.sample.set(mockSample);
      vi.spyOn(apiService, 'getSample').mockReturnValue(of(mockSample));

      component.retry();

      expect(apiService.getSample).toHaveBeenCalledWith(1);
    });

    it('should not reload if no sample', () => {
      component.sample.set(null);
      vi.spyOn(apiService, 'getSample');

      component.retry();

      expect(apiService.getSample).not.toHaveBeenCalled();
    });
  });

  describe('Navigation', () => {
    it('should go back on goBack', () => {
      vi.spyOn(window.history, 'back');

      component.goBack();

      expect(window.history.back).toHaveBeenCalled();
    });
  });

  describe('SITES constant', () => {
    it('should have all 8 sites available', () => {
      expect(component.sites.length).toBe(8);
      expect(component.sites).toContain('Inkerman');
      expect(component.sites).toContain('Pioneer');
    });
  });
});
