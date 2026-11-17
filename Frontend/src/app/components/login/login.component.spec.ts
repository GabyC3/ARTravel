import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, provideRouter } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';
import { LoginComponent } from './login.component';
import { AuthService } from '../../services/auth.service';

describe('LoginComponent registration', () => {
  let fixture: ComponentFixture<LoginComponent>;
  let component: LoginComponent;
  let registerResult: 'success' | 'error';
  let router: Router;

  beforeEach(async () => {
    registerResult = 'success';
    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideRouter([
          { path: 'login', component: LoginComponent },
          { path: 'dashboard.html', component: LoginComponent }
        ]),
        {
          provide: AuthService,
          useValue: {
            register: () => registerResult === 'success'
              ? of({ accessToken: 'token', refreshToken: 'refresh' })
              : (() => { throw new Error('registration failed'); })(),
            logout: () => undefined,
            login: () => of({ accessToken: 'token', refreshToken: 'refresh' })
          }
        },
        {
          provide: ActivatedRoute,
          useValue: { queryParamMap: new BehaviorSubject(new Map<string, string>([['registered', '0']])) }
        }
      ]
    }).compileComponents();

    router = TestBed.inject(Router);
    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('changes to login mode and redirects after a successful registration', async () => {
    component.isRegistering = true;
    component.registration = {
      firstName: 'Ana',
      lastName: 'Perez',
      email: 'ana@example.com',
      password: 'Password1!',
      phoneNumber: ''
    };

    component.onRegister();
    await fixture.whenStable();

    expect(component.isRegistering).toBe(false);
    expect(component.isLoading).toBe(false);
    expect(component.success).toContain('creó correctamente');
    expect(router.url).toContain('/login');
  });

  it('stays in registration mode and shows an error for a weak password', async () => {
    component.isRegistering = true;
    component.registration.password = 'weak';

    component.onRegister();
    await fixture.whenStable();

    expect(component.isRegistering).toBe(true);
    expect(component.error).toContain('8 caracteres');
  });
});
