import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, DestroyRef, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import {
  EMPTY,
  Subject,
  catchError,
  concatMap,
  debounceTime,
  distinctUntilChanged,
  finalize,
  from,
  startWith,
  switchMap,
  toArray
} from 'rxjs';
import { UserSummary } from '../../models/auth.model';
import {
  CreateRoleRequest,
  CreateUserRequest,
  Role,
  UpdateUserRequest
} from '../../models/identity-management.model';
import { RolesService } from '../../services/roles.service';
import { UsersService } from '../../services/users.service';

type UserModal = 'create' | 'edit' | 'details' | 'status' | 'role' | null;

interface UserForm {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  phoneNumber: string;
}

interface RoleForm {
  name: string;
  description: string;
}

@Component({
  standalone: true,
  selector: 'app-user-management',
  imports: [CommonModule, FormsModule],
  templateUrl: './user-management.component.html',
  styleUrl: './user-management.component.scss'
})
export class UserManagementComponent implements OnInit {
  readonly pageSize = 12;
  users: UserSummary[] = [];
  roles: Role[] = [];
  selectedUser: UserSummary | null = null;
  form = this.createEmptyUserForm();
  roleForm: RoleForm = this.createEmptyRoleForm();
  modal: UserModal = null;
  searchText = '';
  currentPage = 1;
  totalCount = 0;
  selectedRoleId: number | null = null;
  isLoading = false;
  isLoadingRoles = false;
  isSaving = false;
  error = '';
  roleError = '';
  modalError = '';
  toastMessage = '';
  toastType: 'success' | 'error' = 'success';

  private readonly loadRequests = new Subject<void>();
  private readonly searchChanges = new Subject<string>();
  private toastTimeout: ReturnType<typeof setTimeout> | null = null;

  constructor(
    private readonly usersService: UsersService,
    private readonly rolesService: RolesService,
    private readonly changeDetector: ChangeDetectorRef,
    private readonly destroyRef: DestroyRef
  ) {
    this.destroyRef.onDestroy(() => this.clearToast());
  }

  ngOnInit(): void {
    this.loadRequests.pipe(
      startWith(undefined),
      switchMap(() => {
        this.isLoading = true;
        this.error = '';
        return this.usersService.list(this.currentPage, this.pageSize, this.searchText).pipe(
          catchError(error => {
            this.users = [];
            this.totalCount = 0;
            this.error = this.getErrorMessage(error, 'No se pudieron cargar los usuarios.');
            return EMPTY;
          }),
          finalize(() => {
            this.isLoading = false;
            this.changeDetector.markForCheck();
          })
        );
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(result => {
      this.users = result.items;
      this.totalCount = result.totalCount;
      this.changeDetector.markForCheck();
    });

    this.searchChanges.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(search => {
      this.searchText = search;
      this.currentPage = 1;
      this.loadRequests.next();
    });

    this.loadRoles();
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  get firstVisibleResult(): number {
    return this.totalCount === 0 ? 0 : (this.currentPage - 1) * this.pageSize + 1;
  }

  get lastVisibleResult(): number {
    return Math.min(this.currentPage * this.pageSize, this.totalCount);
  }

  get activeRoles(): Role[] {
    return this.roles.filter(role => role.isActive);
  }

  onSearchChange(value: string): void {
    this.searchChanges.next(value.trim());
  }

  changePage(page: number): void {
    if (page < 1 || page > this.totalPages || page === this.currentPage) {
      return;
    }
    this.currentPage = page;
    this.loadRequests.next();
  }

  fullName(user: UserSummary): string {
    return `${user.firstName} ${user.lastName}`.trim();
  }

  roleLabel(role: string): string {
    const knownRoles: Record<string, string> = {
      ADMIN: 'Administrador',
      USER: 'Usuario'
    };
    return knownRoles[role.toUpperCase()] ??
      role.toLowerCase().replace(/\b\w/g, character => character.toUpperCase());
  }

  formatRoles(roles: string[]): string {
    return roles.map(role => this.roleLabel(role)).join(', ');
  }

  openCreateDialog(): void {
    this.selectedUser = null;
    this.form = this.createEmptyUserForm();
    this.modalError = '';
    this.modal = 'create';
  }

  openEditDialog(user: UserSummary): void {
    this.selectedUser = user;
    this.form = {
      firstName: user.firstName,
      lastName: user.lastName,
      email: user.email,
      password: '',
      phoneNumber: user.phoneNumber ?? ''
    };
    this.modalError = '';
    this.modal = 'edit';
  }

  openDetailsDialog(user: UserSummary): void {
    this.selectedUser = user;
    this.selectedRoleId = null;
    this.modalError = '';
    this.modal = 'details';
    this.usersService.get(user.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: result => {
          this.selectedUser = result;
          this.selectedRoleId = this.roles.find(role =>
            role.isActive && result.roles.some(userRole =>
              userRole.toUpperCase() === role.name.toUpperCase()))?.id ?? null;
          this.changeDetector.markForCheck();
        },
        error: error => {
          this.modalError = this.getErrorMessage(error, 'No se pudieron cargar los datos del usuario.');
          this.showToast(this.modalError, 'error');
          this.changeDetector.markForCheck();
        }
      });
  }

  openStatusDialog(user: UserSummary): void {
    this.selectedUser = user;
    this.modalError = '';
    this.modal = 'status';
  }

  openCreateRoleDialog(): void {
    this.roleForm = this.createEmptyRoleForm();
    this.modalError = '';
    this.modal = 'role';
  }

  submitUser(isValid: boolean): void {
    if (!isValid || this.isSaving) {
      return;
    }

    const user = this.selectedUser;
    if (this.modal === 'edit' && !user) {
      return;
    }

    this.isSaving = true;
    this.modalError = '';
    const result$ = this.modal === 'edit'
      ? this.usersService.update(user!.id, this.toUpdateUserRequest())
      : this.usersService.create(this.toCreateUserRequest());

    result$.pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        this.isSaving = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: () => {
        this.showToast(
          this.modal === 'edit'
            ? 'Usuario actualizado correctamente.'
            : 'Usuario creado correctamente con el rol predeterminado.',
          'success'
        );
        this.modal = null;
        this.selectedUser = null;
        this.currentPage = 1;
        this.loadRequests.next();
      },
      error: error => {
        this.modalError = this.getErrorMessage(error, 'No se pudo guardar el usuario.');
        this.showToast(this.modalError, 'error');
      }
    });
  }

  submitRole(isValid: boolean): void {
    if (!isValid || this.isSaving) {
      return;
    }
    const request: CreateRoleRequest = {
      name: this.roleForm.name.trim(),
      description: this.roleForm.description.trim() || null
    };
    this.isSaving = true;
    this.modalError = '';
    this.rolesService.create(request).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        this.isSaving = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: () => {
        this.modal = null;
        this.showToast('Rol creado correctamente.', 'success');
        this.loadRoles();
      },
      error: error => {
        this.modalError = this.getErrorMessage(error, 'No se pudo crear el rol.');
        this.showToast(this.modalError, 'error');
      }
    });
  }

  changeUserRole(): void {
    const user = this.selectedUser;
    const roleId = this.selectedRoleId;
    const targetRole = this.activeRoles.find(role => role.id === roleId);
    if (!user || !roleId || !targetRole || this.isSaving) {
      return;
    }

    const rolesToRemove = this.roles.filter(role =>
      user.roles.some(userRole => userRole.toUpperCase() === role.name.toUpperCase()) &&
      role.id !== roleId);
    if (rolesToRemove.length === 0 && user.roles.some(userRole =>
      userRole.toUpperCase() === targetRole.name.toUpperCase())) {
      this.showToast('El usuario ya tiene este rol.', 'success');
      return;
    }

    this.isSaving = true;
    this.modalError = '';
    this.rolesService.assign(user.id, roleId).pipe(
      switchMap(() => from(rolesToRemove).pipe(
        concatMap(role => this.rolesService.remove(user.id, role.id)),
        toArray()
      )),
      switchMap(() => this.usersService.get(user.id)),
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        this.isSaving = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: updatedUser => {
        this.selectedUser = updatedUser;
        this.selectedRoleId = roleId;
        this.showToast(`Rol cambiado a ${this.roleLabel(targetRole.name)} correctamente.`, 'success');
        this.loadRequests.next();
      },
      error: error => {
        this.modalError = this.getErrorMessage(
          error,
          'No se pudo completar el cambio de rol. Revisa las asignaciones actuales del usuario.'
        );
        this.showToast(this.modalError, 'error');
        this.refreshSelectedUser(user.id);
      }
    });
  }

  confirmStatusChange(): void {
    const user = this.selectedUser;
    if (!user || this.isSaving || this.modal !== 'status') {
      return;
    }

    const isActive = !user.isActive;
    this.isSaving = true;
    this.modalError = '';
    this.usersService.setStatus(user.id, isActive).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        this.isSaving = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: () => {
        this.modal = null;
        this.selectedUser = null;
        this.showToast(
          isActive ? 'Usuario reactivado correctamente.' : 'Usuario desactivado; sus sesiones fueron invalidadas.',
          'success'
        );
        this.loadRequests.next();
      },
      error: error => {
        this.modalError = this.getErrorMessage(
          error,
          isActive ? 'No se pudo reactivar el usuario.' : 'No se pudo desactivar el usuario.'
        );
        this.showToast(this.modalError, 'error');
      }
    });
  }

  closeDialog(): void {
    if (this.isSaving) {
      return;
    }
    this.modal = null;
    this.selectedUser = null;
    this.modalError = '';
  }

  clearToast(): void {
    if (this.toastTimeout !== null) {
      clearTimeout(this.toastTimeout);
      this.toastTimeout = null;
    }
    this.toastMessage = '';
  }

  private toCreateUserRequest(): CreateUserRequest {
    return {
      firstName: this.form.firstName.trim(),
      lastName: this.form.lastName.trim(),
      email: this.form.email.trim(),
      password: this.form.password,
      phoneNumber: this.form.phoneNumber.trim() || null
    };
  }

  private toUpdateUserRequest(): UpdateUserRequest {
    return {
      firstName: this.form.firstName.trim(),
      lastName: this.form.lastName.trim(),
      phoneNumber: this.form.phoneNumber.trim() || null
    };
  }

  private createEmptyUserForm(): UserForm {
    return { firstName: '', lastName: '', email: '', password: '', phoneNumber: '' };
  }

  private createEmptyRoleForm(): RoleForm {
    return { name: '', description: '' };
  }

  private loadRoles(): void {
    this.isLoadingRoles = true;
    this.roleError = '';
    this.rolesService.list().pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        this.isLoadingRoles = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: result => {
        this.roles = result.items;
        if (this.selectedUser && this.selectedRoleId === null) {
          this.selectedRoleId = this.roles.find(role =>
            role.isActive && this.selectedUser?.roles.some(userRole =>
              userRole.toUpperCase() === role.name.toUpperCase()))?.id ?? null;
        }
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.roleError = this.getErrorMessage(error, 'No se pudieron cargar los roles disponibles.');
        this.changeDetector.markForCheck();
      }
    });
  }

  private refreshSelectedUser(userId: number): void {
    this.usersService.get(userId).pipe(
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: user => {
        this.selectedUser = user;
        this.selectedRoleId = this.roles.find(role =>
          role.isActive && user.roles.some(userRole =>
            userRole.toUpperCase() === role.name.toUpperCase()))?.id ?? null;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.modalError = `${this.modalError} ${this.getErrorMessage(error, 'No se pudo actualizar la información del usuario.')}`;
        this.changeDetector.markForCheck();
      }
    });
  }

  private showToast(message: string, type: 'success' | 'error'): void {
    if (this.toastTimeout !== null) {
      clearTimeout(this.toastTimeout);
    }
    this.toastMessage = message;
    this.toastType = type;
    this.changeDetector.markForCheck();
    this.toastTimeout = setTimeout(() => {
      this.toastMessage = '';
      this.toastTimeout = null;
      this.changeDetector.markForCheck();
    }, 5000);
  }

  private getErrorMessage(error: unknown, fallback: string): string {
    if (!(error instanceof HttpErrorResponse)) {
      return fallback;
    }
    const payload = error.error as {
      detail?: unknown;
      message?: unknown;
      title?: unknown;
      errors?: Record<string, unknown>;
    } | null;
    const directMessage = [payload?.detail, payload?.message]
      .find((message): message is string =>
        typeof message === 'string' && message.trim().length > 0);
    if (directMessage) {
      return directMessage;
    }
    const validationMessages = Object.entries(payload?.errors ?? {})
      .flatMap(([field, messages]) =>
        (Array.isArray(messages) ? messages : [messages])
          .filter((message): message is string =>
            typeof message === 'string' && message.trim().length > 0)
          .map(message => `${field}: ${message}`));
    if (validationMessages.length > 0) {
      return validationMessages.join(' ');
    }
    return typeof payload?.title === 'string'
      ? `HTTP ${error.status}: ${payload.title}`
      : `HTTP ${error.status}: ${fallback}`;
  }
}
