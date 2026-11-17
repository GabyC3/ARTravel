import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { UserSummary } from '../../models/auth.model';
import { AuthService } from '../../services/auth.service';
import { DashboardComponent } from './dashboard.component';

describe('DashboardComponent', () => {
  const user: UserSummary = {
    id: 8,
    firstName: 'María',
    lastName: 'López',
    email: 'maria@example.com',
    phoneNumber: null,
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: null,
    roles: ['ADMIN']
  };

  it('renders the current user as soon as the profile request completes', async () => {
    await TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: { getCurrentUser: () => of(user) } }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(DashboardComponent);
    fixture.detectChanges();
    await fixture.whenStable();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('.topbar-user-name')?.textContent?.trim()).toBe('María López');
    expect(element.querySelector('.account-role')?.textContent?.trim()).toBe('Administrador');
  });
});
