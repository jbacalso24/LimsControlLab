import { Injectable, inject } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { LimsApiService } from '../api/lims/lims-api.service';
import { CurrentUserService } from './current-user.service';
import { LoginRequest } from '../../generated/models/login-request';
import { LoginResponseDto } from '../../generated/models/login-response-dto';

@Injectable({
  providedIn: 'root',
})
export class AuthApiService extends LimsApiService {
  private currentUserService = inject(CurrentUserService);

  login(username: string, password: string): Observable<LoginResponseDto> {
    const request: LoginRequest = {
      username,
      password,
    };

    // The backend hands back role/site/userId/username directly alongside the token
    // (see LoginResponseDto) — no need to decode the JWT client-side for them.
    return this.post<LoginResponseDto>('/auth/login', request).pipe(
      tap((response) => {
        this.currentUserService.setToken(response.token);
        this.currentUserService.setUser({
          sub: String(response.userId),
          username: response.username,
          role: response.role as 'ControlLabAnalyst' | 'LabCoordinator',
          site: response.site,
        });
      })
    );
  }

  logout(): void {
    this.currentUserService.clearToken();
  }
}
