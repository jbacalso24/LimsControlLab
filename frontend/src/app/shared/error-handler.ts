import { ErrorHandler, Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';

@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  private router = inject(Router);

  handleError(error: unknown): void {
    console.error('Global error caught:', error);

    try {
      this.router.navigate(['/error'], {
        skipLocationChange: true,
      });
    } catch (handlerError) {
      console.error('Error in error handler:', handlerError);
    }
  }
}
