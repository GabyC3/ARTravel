import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, DestroyRef, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import {
  EMPTY,
  Subject,
  catchError,
  debounceTime,
  distinctUntilChanged,
  finalize,
  startWith,
  switchMap
} from 'rxjs';
import {
  CreateCustomerRequest,
  Customer,
  CustomerFields,
  CustomerType
} from '../../models/customer.model';
import { CustomersService } from '../../services/customers.service';

type CustomerFilter = 'all' | 'active' | 'inactive';
type CustomerModal = 'create' | 'edit' | 'details' | 'deactivate' | 'activate' | null;

interface CustomerForm {
  customerType: CustomerType;
  firstName: string;
  lastName: string;
  businessName: string;
  documentType: string;
  documentNumber: string;
  email: string;
  phoneNumber: string;
  address: string;
  city: string;
  country: string;
  notes: string;
}

@Component({
  standalone: true,
  selector: 'app-clients',
  imports: [CommonModule, FormsModule],
  templateUrl: './clients.component.html',
  styleUrl: './clients.component.scss'
})
export class ClientsComponent implements OnInit {
  readonly pageSize = 12;
  readonly customerTypes = CustomerType;
  readonly statusFilters: { value: CustomerFilter; label: string }[] = [
    { value: 'all', label: 'Todos' },
    { value: 'active', label: 'Activos' },
    { value: 'inactive', label: 'Inactivos' }
  ];
  customers: Customer[] = [];
  selectedCustomer: Customer | null = null;
  form = this.createEmptyForm();
  modal: CustomerModal = null;
  searchText = '';
  statusFilter: CustomerFilter = 'all';
  currentPage = 1;
  totalCount = 0;
  isLoading = false;
  isSaving = false;
  isFilterOpen = false;
  error = '';
  modalError = '';
  toastMessage = '';
  toastType: 'success' | 'error' = 'success';

  private readonly loadRequests = new Subject<void>();
  private readonly searchChanges = new Subject<string>();
  private toastTimeout: ReturnType<typeof setTimeout> | null = null;

  constructor(
    private readonly customersService: CustomersService,
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
        const isActive = this.statusFilter === 'all'
          ? null
          : this.statusFilter === 'active';
        return this.customersService.list(
          this.currentPage,
          this.pageSize,
          this.searchText,
          isActive
        ).pipe(
          catchError(error => {
            this.customers = [];
            this.totalCount = 0;
            this.error = this.getErrorMessage(error, 'No se pudieron cargar los clientes.');
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
      this.customers = result.items;
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

  onSearchChange(value: string): void {
    this.searchChanges.next(value.trim());
  }

  setStatusFilter(filter: CustomerFilter): void {
    this.statusFilter = filter;
    this.currentPage = 1;
    this.isFilterOpen = false;
    this.loadRequests.next();
  }

  changePage(page: number): void {
    if (page < 1 || page > this.totalPages || page === this.currentPage) {
      return;
    }
    this.currentPage = page;
    this.loadRequests.next();
  }

  customerName(customer: Customer): string {
    return customer.customerType === CustomerType.Organization
      ? customer.businessName || 'Empresa sin nombre'
      : `${customer.firstName ?? ''} ${customer.lastName ?? ''}`.trim() || 'Cliente sin nombre';
  }

  customerTypeLabel(customerType: CustomerType): string {
    return customerType === CustomerType.Organization ? 'Empresa' : 'Persona';
  }

  openCreateDialog(): void {
    this.selectedCustomer = null;
    this.form = this.createEmptyForm();
    this.modalError = '';
    this.modal = 'create';
  }

  openDetailsDialog(customer: Customer): void {
    this.selectedCustomer = customer;
    this.modalError = '';
    this.modal = 'details';
    this.customersService.get(customer.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: result => {
          this.selectedCustomer = result;
          this.changeDetector.markForCheck();
        },
        error: error => {
          this.modalError = this.getErrorMessage(error, 'No se pudieron cargar los datos del cliente.');
          this.showToast(this.modalError, 'error');
          this.changeDetector.markForCheck();
        }
      });
  }

  openEditDialog(customer: Customer): void {
    this.selectedCustomer = customer;
    this.form = {
      customerType: customer.customerType,
      firstName: customer.firstName ?? '',
      lastName: customer.lastName ?? '',
      businessName: customer.businessName ?? '',
      documentType: customer.documentType,
      documentNumber: customer.documentNumber,
      email: customer.email ?? '',
      phoneNumber: customer.phoneNumber ?? '',
      address: customer.address ?? '',
      city: customer.city ?? '',
      country: customer.country ?? '',
      notes: customer.notes ?? ''
    };
    this.modalError = '';
    this.modal = 'edit';
  }

  openStatusDialog(customer: Customer): void {
    this.selectedCustomer = customer;
    this.modalError = '';
    this.modal = customer.isActive ? 'deactivate' : 'activate';
  }

  submit(isValid: boolean): void {
    if (!isValid || this.isSaving) {
      return;
    }

    const customer = this.selectedCustomer;
    if (this.modal === 'edit' && !customer) {
      return;
    }

    this.isSaving = true;
    this.modalError = '';
    const fields = this.toFields();
    const request: CreateCustomerRequest = { ...fields, isActive: true };
    const save$ = this.modal === 'edit'
      ? this.customersService.update(customer!.id, fields)
      : this.customersService.create(request);

    save$.pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        this.isSaving = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: () => {
        this.showToast(
          this.modal === 'edit'
            ? 'Cliente actualizado correctamente.'
            : 'Cliente creado correctamente.',
          'success'
        );
        this.modal = null;
        this.selectedCustomer = null;
        this.currentPage = 1;
        this.loadRequests.next();
      },
      error: error => {
        this.modalError = this.getErrorMessage(error, 'No se pudo guardar el cliente.');
        this.showToast(this.modalError, 'error');
      }
    });
  }

  confirmStatusChange(): void {
    const customer = this.selectedCustomer;
    if (!customer || this.isSaving ||
        (this.modal !== 'deactivate' && this.modal !== 'activate')) {
      return;
    }

    const isActive = this.modal === 'activate';
    this.isSaving = true;
    this.modalError = '';
    this.customersService.setStatus(customer.id, isActive).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        this.isSaving = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: () => {
        this.showToast(
          isActive ? 'Cliente reactivado correctamente.' : 'Cliente desactivado correctamente.',
          'success'
        );
        this.modal = null;
        this.selectedCustomer = null;
        this.loadRequests.next();
      },
      error: error => {
        this.modalError = this.getErrorMessage(
          error,
          isActive ? 'No se pudo reactivar el cliente.' : 'No se pudo desactivar el cliente.'
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
    this.selectedCustomer = null;
    this.modalError = '';
  }

  clearToast(): void {
    if (this.toastTimeout !== null) {
      clearTimeout(this.toastTimeout);
      this.toastTimeout = null;
    }
    this.toastMessage = '';
  }

  private toFields(): CustomerFields {
    const isIndividual = this.form.customerType === CustomerType.Individual;
    return {
      customerType: this.form.customerType,
      firstName: isIndividual ? this.form.firstName.trim() : null,
      lastName: isIndividual ? this.form.lastName.trim() : null,
      businessName: isIndividual ? null : this.form.businessName.trim(),
      documentType: this.form.documentType.trim(),
      documentNumber: this.form.documentNumber.trim(),
      email: this.form.email.trim() || null,
      phoneNumber: this.form.phoneNumber.trim() || null,
      address: this.form.address.trim() || null,
      city: this.form.city.trim() || null,
      country: this.form.country.trim() || null,
      notes: this.form.notes.trim() || null
    };
  }

  private createEmptyForm(): CustomerForm {
    return {
      customerType: CustomerType.Individual,
      firstName: '',
      lastName: '',
      businessName: '',
      documentType: 'DNI',
      documentNumber: '',
      email: '',
      phoneNumber: '',
      address: '',
      city: '',
      country: 'Argentina',
      notes: ''
    };
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
