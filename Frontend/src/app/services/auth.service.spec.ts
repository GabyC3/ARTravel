import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthService } from './auth.service';
import { LoginResponse } from '../models/auth.model';

describe('AuthService', () => {
  let authService: AuthService;
  let httpTestingController: HttpTestingController;

  const authResponse: LoginResponse = {
    accessToken: 'new-access-token',
    expiresAt: '2026-11-17T12:00:00Z',
    refreshToken: 'new-refresh-token',
    refreshTokenExpiresAt: '2026-12-17T12:00:00Z',
    user: {
      id: 1,
      firstName: 'Test',
      lastName: 'User',
      email: 'test@example.com',
      phoneNumber: null,
      isActive: true,
      createdAt: '2026-11-17T00:00:00Z',
      updatedAt: null,
      roles: ['USER']
    }
  };

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    authService = TestBed.inject(AuthService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('refreshes an expired token and retries login with the entered credentials', () => {
    const credentials = { email: 'test@example.com', password: 'Password123!' };
    localStorage.setItem('refresh_token', 'previous-refresh-token');
    let result: LoginResponse | undefined;

    authService.login(credentials).subscribe(response => result = response);

    const initialLogin = httpTestingController.expectOne('http://localhost:5000/api/auth/login');
    initialLogin.flush(
      { message: 'Access token expired' },
      { status: 401, statusText: 'Unauthorized' }
    );

    const refresh = httpTestingController.expectOne('http://localhost:5000/api/auth/refresh-token');
    expect(refresh.request.body).toEqual({ refreshToken: 'previous-refresh-token' });
    refresh.flush(authResponse);

    const retriedLogin = httpTestingController.expectOne('http://localhost:5000/api/auth/login');
    expect(retriedLogin.request.body).toEqual(credentials);
    retriedLogin.flush(authResponse);

    expect(result).toEqual(authResponse);
    expect(authService.getToken()).toBe('new-access-token');
    expect(authService.getRefreshToken()).toBe('new-refresh-token');
  });

  it('does not refresh the token for invalid login credentials', () => {
    let receivedError = false;
    authService.login({ email: 'test@example.com', password: 'wrong-password' })
      .subscribe({ error: () => receivedError = true });

    httpTestingController.expectOne('http://localhost:5000/api/auth/login').flush(
      { message: 'Invalid credentials' },
      { status: 401, statusText: 'Unauthorized' }
    );

    httpTestingController.expectNone('http://localhost:5000/api/auth/refresh-token');
    expect(receivedError).toBe(true);
  });
});
