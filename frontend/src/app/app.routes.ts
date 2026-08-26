import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login/login.component';
import { authGuard } from './shared/guards/auth.guard';
import { featuresRoutes } from './features/features.routes';
import { ErrorFallbackComponent } from './shared/error-fallback/error-fallback.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'error', component: ErrorFallbackComponent },
  { path: 'analysis', canActivate: [authGuard], children: featuresRoutes },
  { path: '', redirectTo: '/analysis', pathMatch: 'full' },
];
