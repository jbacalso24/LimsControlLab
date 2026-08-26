import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { GlobalErrorHandler } from './error-handler';
import { vi, describe, it, expect } from 'vitest';

describe('GlobalErrorHandler', () => {
  let errorHandler: GlobalErrorHandler;
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [GlobalErrorHandler],
    });

    errorHandler = TestBed.inject(GlobalErrorHandler);
    router = TestBed.inject(Router);
  });

  it('should navigate to error page when error occurs', () => {
    const navigateSpy = vi.spyOn(router, 'navigate').mockReturnValue(Promise.resolve(true));
    const consoleErrorSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

    const testError = new Error('Test error');
    errorHandler.handleError(testError);

    expect(consoleErrorSpy).toHaveBeenCalledWith('Global error caught:', testError);
    expect(navigateSpy).toHaveBeenCalledWith(['/error'], { skipLocationChange: true });

    consoleErrorSpy.mockRestore();
    navigateSpy.mockRestore();
  });

  it('should handle navigation errors gracefully', () => {
    const navigateSpy = vi.spyOn(router, 'navigate').mockImplementation(() => {
      throw new Error('Navigation error');
    });
    const consoleErrorSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

    const testError = new Error('Test error');
    errorHandler.handleError(testError);

    expect(consoleErrorSpy).toHaveBeenCalledTimes(2);
    expect(consoleErrorSpy).toHaveBeenNthCalledWith(2, 'Error in error handler:', expect.any(Error));

    consoleErrorSpy.mockRestore();
    navigateSpy.mockRestore();
  });
});
