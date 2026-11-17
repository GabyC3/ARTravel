import { TestBed } from '@angular/core/testing';
import { Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  let isAuthenticated: () => boolean;

  beforeEach(() => {
    isAuthenticated = () => false;
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: { isAuthenticated: () => isAuthenticated() } }
      ]
    });
  });

  it('redirects users without a session to login', async () => {
    const router = TestBed.inject(Router);
    const result = await TestBed.runInInjectionContext(() =>
      authGuard({} as never, { url: '/dashboard.html/dashboard' } as RouterStateSnapshot)
    );

    expect(result).toBeInstanceOf(UrlTree);
    expect(router.serializeUrl(result as UrlTree)).toBe('/login?returnUrl=%2Fdashboard.html%2Fdashboard');
  });

  it('allows users with a session to access protected pages', async () => {
    isAuthenticated = () => true;
    const result = await TestBed.runInInjectionContext(() =>
      authGuard({} as never, { url: '/dashboard.html/dashboard' } as RouterStateSnapshot)
    );

    expect(result).toBe(true);
  });
});
