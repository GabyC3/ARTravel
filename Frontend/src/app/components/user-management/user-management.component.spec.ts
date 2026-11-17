import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { UserSummary } from '../../models/auth.model';
import { Role } from '../../models/identity-management.model';
import { RolesService } from '../../services/roles.service';
import { UsersService } from '../../services/users.service';
import { UserManagementComponent } from './user-management.component';

describe('UserManagementComponent', () => {
  const user: UserSummary = {
    id: 22,
    firstName: 'Ana',
    lastName: 'Martínez',
    email: 'ana@example.com',
    phoneNumber: null,
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: null,
    roles: ['USER']
  };
  const userRole: Role = {
    id: 2,
    name: 'USER',
    description: 'Default role',
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z'
  };
  const managerRole: Role = {
    id: 3,
    name: 'Manager',
    description: null,
    isActive: true,
    createdAt: '2026-01-02T00:00:00Z'
  };

  it('renders the user list and available roles', async () => {
    await TestBed.configureTestingModule({
      imports: [UserManagementComponent],
      providers: [
        {
          provide: UsersService,
          useValue: {
            list: () => of({ items: [user], page: 1, pageSize: 12, totalCount: 1 })
          }
        },
        { provide: RolesService, useValue: { list: () => of({ items: [userRole, managerRole], page: 1, pageSize: 100, totalCount: 2 }) } }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(UserManagementComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('tbody tr')?.textContent).toContain('ana@example.com');
    expect(element.querySelector('.role-chips')?.textContent).toContain('Manager');
  });

  it('assigns the selected role before removing the previous role', async () => {
    const calls: string[] = [];
    await TestBed.configureTestingModule({
      imports: [UserManagementComponent],
      providers: [
        { provide: UsersService, useValue: {
          list: () => of({ items: [user], page: 1, pageSize: 12, totalCount: 1 }),
          get: () => of({ ...user, roles: ['Manager'] })
        } },
        { provide: RolesService, useValue: {
          list: () => of({ items: [userRole, managerRole], page: 1, pageSize: 100, totalCount: 2 }),
          assign: () => { calls.push('assign'); return of(undefined); },
          remove: () => { calls.push('remove'); return of(undefined); }
        } }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(UserManagementComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    const component = fixture.componentInstance;
    component.selectedUser = user;
    component.selectedRoleId = managerRole.id;

    component.changeUserRole();

    expect(calls).toEqual(['assign', 'remove']);
    expect(component.selectedUser?.roles).toEqual(['Manager']);
  });

  it('creates a role and shows success feedback', async () => {
    let createdRoleName = '';
    await TestBed.configureTestingModule({
      imports: [UserManagementComponent],
      providers: [
        { provide: UsersService, useValue: {
          list: () => of({ items: [], page: 1, pageSize: 12, totalCount: 0 })
        } },
        { provide: RolesService, useValue: {
          list: () => of({ items: [userRole], page: 1, pageSize: 100, totalCount: 1 }),
          create: (request: { name: string }) => {
            createdRoleName = request.name;
            return of(managerRole);
          }
        } }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(UserManagementComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    component.openCreateRoleDialog();
    component.roleForm.name = 'Manager';
    component.roleForm.description = 'Team manager';

    component.submitRole(true);

    expect(createdRoleName).toBe('Manager');
    expect(component.toastMessage).toBe('Rol creado correctamente.');
    expect(component.toastType).toBe('success');
  });
});
