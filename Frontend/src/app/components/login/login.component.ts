import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { LoginRequest, RegisterRequest } from '../../models/auth.model';

@Component({
  standalone: true,
  selector: 'app-login',
  imports: [CommonModule, FormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  credentials: LoginRequest = { email: '', password: '' };
  registration: RegisterRequest = {
    firstName: '',
    lastName: '',
    email: '',
    password: '',
    phoneNumber: ''
  };
  error = '';
  success = '';
  isRegistering = false;
  isLoading = false;

  constructor(
    private readonly authService: AuthService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly changeDetector: ChangeDetectorRef
  ) {
    this.route.queryParamMap.subscribe(params => {
      if (params.get('registered') === '1') {
        this.success = 'Tu cuenta se creó correctamente. Ya puedes iniciar sesión.';
      }
    });
  }

  onLogin(): void {
    if (this.isLoading) {
      return;
    }

    this.error = '';
    this.success = '';
    if (!this.isStrongPassword(this.credentials.password)) {
      this.error = 'La contraseña debe tener al menos 8 caracteres e incluir mayúscula, minúscula, número y símbolo.';
      this.changeDetector.markForCheck();
      return;
    }
    this.isLoading = true;
    this.authService.login(this.credentials).subscribe({
      next: () => {
        this.isLoading = false;
        this.changeDetector.markForCheck();
        this.goToDashboard();
      },
      error: (error: unknown) => {
        this.error = this.getErrorMessage(error, 'No se pudo iniciar sesión. Verifica tus credenciales.');
        this.isLoading = false;
        this.changeDetector.markForCheck();
      }
    });
  }

  onRegister(): void {
    if (this.isLoading) {
      return;
    }

    this.error = '';
    this.success = '';
    if (!this.isStrongPassword(this.registration.password)) {
      this.error = 'La contraseña debe tener al menos 8 caracteres e incluir mayúscula, minúscula, número y símbolo.';
      this.changeDetector.markForCheck();
      return;
    }
    this.isLoading = true;
    this.changeDetector.markForCheck();
    this.authService.register(this.registration).subscribe({
      next: () => {
        this.redirectToLoginAfterRegistration();
      },
      error: (error: unknown) => {
        this.error = this.getErrorMessage(error, 'No se pudo crear la cuenta. Inténtalo nuevamente.');
        this.isLoading = false;
        this.changeDetector.markForCheck();
      }
    });
  }

  toggleMode(): void {
    this.isRegistering = !this.isRegistering;
    this.error = '';
    this.success = '';
    this.changeDetector.markForCheck();
  }

  private goToDashboard(): void {
    this.router.navigate(['/dashboard.html']);
  }

  private redirectToLoginAfterRegistration(): void {
    this.authService.logout();
    this.isLoading = false;
    this.error = '';
    this.success = 'Tu cuenta se creó correctamente. Ya puedes iniciar sesión.';
    this.isRegistering = false;
    this.registration = {
      firstName: '',
      lastName: '',
      email: '',
      password: '',
      phoneNumber: ''
    };
    this.credentials = { email: '', password: '' };
    this.changeDetector.markForCheck();
    void this.router.navigate(['/login'], {
      queryParams: { registered: '1' },
      replaceUrl: true
    }).then(() => {
      this.success = 'Tu cuenta se creó correctamente. Ya puedes iniciar sesión.';
      this.isRegistering = false;
      this.isLoading = false;
      this.changeDetector.markForCheck();
    });
  }

  isStrongPassword(password: string): boolean {
    return password.length >= 8 &&
      /[A-Z]/.test(password) &&
      /[a-z]/.test(password) &&
      /\d/.test(password) &&
      /[^A-Za-z0-9]/.test(password);
  }

  private getErrorMessage(error: unknown, fallback: string): string {
    if (!(error instanceof HttpErrorResponse)) {
      return fallback;
    }

    const body: unknown = error.error;
    if (typeof body === 'string' && body.trim()) {
      return body;
    }
    if (typeof body !== 'object' || body === null) {
      return fallback;
    }

    const payload = body as {
      message?: unknown;
      detail?: unknown;
      title?: unknown;
      errors?: Record<string, string[]>;
    };
    const message = [payload.message, payload.detail, payload.title]
      .find((value): value is string => typeof value === 'string' && value.trim().length > 0);
    if (message) {
      return message;
    }

    const validationMessage = payload.errors
      ? Object.values(payload.errors).flat().find(value => typeof value === 'string' && value.trim())
      : undefined;
    return validationMessage ?? fallback;
  }
}
