import { inject } from '@angular/core';
import {
  CanActivateFn,
  Router,
  ActivatedRouteSnapshot,
  RouterStateSnapshot,
} from '@angular/router';
import { CurrentUserService } from '../services/auth/current-user.service';

export const authGuard: CanActivateFn = (
  route: ActivatedRouteSnapshot,
  state: RouterStateSnapshot,
  router = inject(Router),
  currentUser = inject(CurrentUserService)
) => {
  if (currentUser.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/login']);
};
