import { Component, inject, signal } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
} from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthApiService } from '../../../shared/services/auth/auth-api.service';
import { ZardButtonComponent } from '../../../shared/components/button/button.component';
import { ZardInputComponent } from '../../../shared/components/input/input.component';
import { ZardCardComponent, ZardCardContentComponent } from '../../../shared/components/card/card.component';

@Component({
  selector: 'lims-login',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    CommonModule,
    ZardButtonComponent,
    ZardInputComponent,
    ZardCardComponent,
    ZardCardContentComponent,
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private authApi = inject(AuthApiService);
  private router = inject(Router);

  isLoading = signal(false);
  errorMessage = signal('');

  loginForm: FormGroup;

  constructor() {
    this.loginForm = this.fb.group({
      username: ['', Validators.required],
      password: ['', Validators.required],
    });
  }

  get usernameControl() {
    return this.loginForm.get('username')!;
  }

  get passwordControl() {
    return this.loginForm.get('password')!;
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    const { username, password } = this.loginForm.value;
    this.authApi.login(username, password).subscribe({
      next: () => {
        this.router.navigate(['/analysis']);
      },
      error: (error) => {
        this.isLoading.set(false);
        if (error.status === 401) {
          this.errorMessage.set('Invalid username or password');
        } else if (error.error?.detail) {
          this.errorMessage.set(error.error.detail);
        } else {
          this.errorMessage.set('An error occurred during login. Please try again.');
        }
      },
    });
  }
}
