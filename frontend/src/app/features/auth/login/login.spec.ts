import { TestBed, ComponentFixture } from '@angular/core/testing';
import { Router } from '@angular/router';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { provideAnimations } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { LoginComponent } from './login.component';
import { AuthApiService } from '../../../shared/services/auth/auth-api.service';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let authService: Partial<AuthApiService>;
  let router: Partial<Router>;

  beforeEach(async () => {
    const authServiceSpy: Partial<AuthApiService> = {
      login: vi.fn(),
      logout: vi.fn(),
    };
    const routerSpy: Partial<Router> = {
      navigate: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [LoginComponent, HttpClientTestingModule],
      providers: [
        provideAnimations(),
        { provide: AuthApiService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy },
      ],
    }).compileComponents();

    authService = TestBed.inject(AuthApiService) as Partial<AuthApiService>;
    router = TestBed.inject(Router) as Partial<Router>;

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize form with empty fields', () => {
    expect(component.loginForm.get('username')?.value).toBe('');
    expect(component.loginForm.get('password')?.value).toBe('');
  });

  it('should disable submit button when form is invalid', () => {
    component.loginForm.patchValue({
      username: '',
      password: '',
    });

    fixture.detectChanges();
    const submitButton = fixture.nativeElement.querySelector('button[type="submit"]');

    expect(submitButton.disabled).toBe(true);
  });

  it('should enable submit button when form is valid', () => {
    component.loginForm.patchValue({
      username: 'testuser',
      password: 'password123',
    });

    fixture.detectChanges();
    const submitButton = fixture.nativeElement.querySelector('button[type="submit"]');

    expect(submitButton.disabled).toBe(false);
  });

  it('should call login and navigate on successful login', () => {
    const credentials = { username: 'testuser', password: 'password123' };
    component.loginForm.patchValue(credentials);
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (authService.login as any).mockReturnValue(
      of({ access_token: 'test-token', token_type: 'Bearer', expires_in: 3600 })
    );

    component.onSubmit();

    expect(authService.login).toHaveBeenCalledWith(
      credentials.username,
      credentials.password
    );
    expect(router.navigate).toHaveBeenCalledWith(['/analysis']);
  });

  it('should show error message on 401 response', async () => {
    component.loginForm.patchValue({
      username: 'testuser',
      password: 'wrongpassword',
    });

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (authService.login as any).mockReturnValue(
      throwError(() => ({
        status: 401,
        error: { detail: 'Unauthorized' },
      }))
    );

    component.onSubmit();

     
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(component.errorMessage()).toBe('Invalid username or password');
    expect(component.isLoading()).toBe(false);
  });

  it('should show error detail from server response', async () => {
    component.loginForm.patchValue({
      username: 'testuser',
      password: 'password',
    });

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (authService.login as any).mockReturnValue(
      throwError(() => ({
        status: 400,
        error: { detail: 'Account locked' },
      }))
    );

    component.onSubmit();

     
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(component.errorMessage()).toBe('Account locked');
  });

  it('should show generic error message for unknown errors', async () => {
    component.loginForm.patchValue({
      username: 'testuser',
      password: 'password',
    });

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (authService.login as any).mockReturnValue(
      throwError(() => ({
        status: 500,
      }))
    );

    component.onSubmit();

     
    await new Promise(resolve => setTimeout(resolve, 100));
    expect(component.errorMessage()).toContain('error occurred during login');
  });

  it('should set loading state during submission', () => {
    component.loginForm.patchValue({
      username: 'testuser',
      password: 'password123',
    });

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (authService.login as any).mockReturnValue(
      of({ access_token: 'test-token', token_type: 'Bearer', expires_in: 3600 })
    );

    expect(component.isLoading()).toBe(false);

    component.onSubmit();

    expect(component.isLoading()).toBe(true);
  });

  it('should not submit if form is invalid', () => {
    component.loginForm.patchValue({
      username: '',
      password: '',
    });

    component.onSubmit();

    expect(authService.login).not.toHaveBeenCalled();
  });
});
