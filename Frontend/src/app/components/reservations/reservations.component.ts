import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectorRef, Component, DestroyRef, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import {
  EMPTY,
  Observable,
  Subject,
  catchError,
  concatMap,
  debounceTime,
  distinctUntilChanged,
  finalize,
  forkJoin,
  from,
  map,
  of,
  startWith,
  switchMap,
  toArray
} from 'rxjs';
import { Customer, CustomerType } from '../../models/customer.model';
import {
  Departure,
  DepartureAvailability,
  DeparturePrice,
  DepartureStatus
} from '../../models/departure.model';
import {
  CreateReservationPassengerRequest,
  DeparturePriceType,
  ReservationDetail,
  ReservationListItem,
  ReservationPayment,
  ReservationPaymentStatus,
  ReservationStatus
} from '../../models/reservation.model';
import { PagedPassengerTrips, Passenger } from '../../models/passenger.model';
import { DepartureManifestPassenger } from '../../models/departure.model';
import { CustomersService } from '../../services/customers.service';
import { DeparturesService } from '../../services/departures.service';
import { PassengersService } from '../../services/passengers.service';
import { ProductsService } from '../../services/products.service';
import { ReservationsService } from '../../services/reservations.service';

type ReservationFilter = 'all' | 'pending' | 'confirmed' | 'cancelled';
type ReservationModal = 'create' | 'details' | 'cancel' | null;

interface PassengerForm {
  passengerId: number | null;
  firstName: string;
  lastName: string;
  documentType: number;
  documentNumber: string;
  birthDate: string;
  email: string;
  phoneNumber: string;
  priceType: DeparturePriceType;
}

interface ReservationRow extends ReservationListItem {
  customerName: string;
  departureLabel: string;
}

@Component({
  standalone: true,
  selector: 'app-reservations',
  imports: [CommonModule, FormsModule],
  templateUrl: './reservations.component.html',
  styleUrl: './reservations.component.scss'
})
export class ReservationsComponent implements OnInit {
  readonly pageSize = 12;
  readonly statuses = ReservationStatus;
  readonly priceTypes = DeparturePriceType;
  readonly documentTypes = [
    { value: 1, label: 'DNI' },
    { value: 2, label: 'Pasaporte' },
    { value: 3, label: 'Otro' }
  ];
  readonly statusFilters: { value: ReservationFilter; label: string }[] = [
    { value: 'all', label: 'Todas' },
    { value: 'pending', label: 'Pendientes' },
    { value: 'confirmed', label: 'Confirmadas' },
    { value: 'cancelled', label: 'Canceladas' }
  ];

  reservations: ReservationRow[] = [];
  customers: Customer[] = [];
  departures: Departure[] = [];
  passengerProfiles: Passenger[] = [];
  departurePrices: DeparturePrice[] = [];
  availability: DepartureAvailability | null = null;
  selectedDetail: ReservationDetail | null = null;
  payments: ReservationPayment[] = [];
  manifest: DepartureManifestPassenger[] = [];
  passengerTrips: PagedPassengerTrips['items'] = [];
  formPassengers: PassengerForm[] = [this.createPassengerForm()];
  searchText = '';
  statusFilter: ReservationFilter = 'all';
  modal: ReservationModal = null;
  currentPage = 1;
  totalCount = 0;
  isLoading = false;
  isLoadingOptions = false;
  isLoadingDetail = false;
  isSaving = false;
  isFilterOpen = false;
  includeConfirmation = false;
  notes = '';
  discount = 0;
  cancelReason = '';
  refundAmount = 0;
  selectedCustomerId: number | null = null;
  selectedDepartureId: number | null = null;
  error = '';
  modalError = '';
  toastMessage = '';
  toastType: 'success' | 'error' = 'success';

  private readonly loadRequests = new Subject<void>();
  private readonly searchChanges = new Subject<string>();
  private toastTimeout: ReturnType<typeof setTimeout> | null = null;

  constructor(
    private readonly reservationsService: ReservationsService,
    private readonly customersService: CustomersService,
    private readonly departuresService: DeparturesService,
    private readonly passengersService: PassengersService,
    private readonly productsService: ProductsService,
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
        const status = this.statusFilter === 'pending'
          ? ReservationStatus.Pending
          : this.statusFilter === 'confirmed'
            ? ReservationStatus.Confirmed
            : this.statusFilter === 'cancelled' ? ReservationStatus.Cancelled : null;
        return this.reservationsService.list(
          this.currentPage,
          this.pageSize,
          this.searchText,
          status
        ).pipe(
          switchMap(result => this.enrichRows(result.items).pipe(
            map(rows => ({ ...result, items: rows }))
          )),
          catchError(error => {
            this.reservations = [];
            this.totalCount = 0;
            this.error = this.getErrorMessage(error, 'No se pudieron cargar las reservas.');
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
      this.reservations = result.items;
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

  get paidAmount(): number {
    return this.payments
      .filter(payment => payment.status === ReservationPaymentStatus.Paid)
      .reduce((sum, payment) => sum + payment.amount, 0);
  }

  get canCancel(): boolean {
    const status = this.selectedDetail?.reservation.status;
    return status === ReservationStatus.Pending ||
      status === ReservationStatus.Confirmed ||
      status === ReservationStatus.PartiallyPaid ||
      status === ReservationStatus.Paid;
  }

  onSearchChange(value: string): void {
    this.searchChanges.next(value.trim());
  }

  setStatusFilter(filter: ReservationFilter): void {
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

  openCreateDialog(): void {
    this.modalError = '';
    this.modal = 'create';
    this.notes = '';
    this.discount = 0;
    this.includeConfirmation = false;
    this.selectedCustomerId = null;
    this.selectedDepartureId = null;
    this.departurePrices = [];
    this.availability = null;
    this.formPassengers = [this.createPassengerForm()];
    this.customers = [];
    this.departures = [];
    this.passengerProfiles = [];
    this.isLoadingOptions = true;
    forkJoin({
      customers: this.customersService.list(1, 100, '', true),
      departures: this.departuresService.list(1, 100, DepartureStatus.Open),
      passengers: this.passengersService.list(1, 100, '', true)
    }).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        this.isLoadingOptions = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: result => {
        this.customers = result.customers.items;
        this.departures = result.departures.items;
        this.passengerProfiles = result.passengers.items;
      },
      error: error => {
        this.modalError = this.getErrorMessage(error, 'No se pudieron cargar clientes, salidas o pasajeros.');
      }
    });
  }

  onDepartureChange(departureId: number | null): void {
    this.selectedDepartureId = departureId ? Number(departureId) : null;
    this.availability = null;
    this.departurePrices = [];
    if (this.selectedDepartureId === null) {
      return;
    }
    forkJoin({
      availability: this.departuresService.availability(this.selectedDepartureId),
      prices: this.departuresService.prices(this.selectedDepartureId)
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        this.availability = result.availability;
        this.departurePrices = result.prices;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.modalError = this.getErrorMessage(error, 'No se pudo consultar la disponibilidad de la salida.');
      }
    });
  }

  addPassengerRow(): void {
    this.formPassengers = [...this.formPassengers, this.createPassengerForm()];
  }

  removePassengerRow(index: number): void {
    if (this.formPassengers.length > 1) {
      this.formPassengers = this.formPassengers.filter((_, rowIndex) => rowIndex !== index);
    }
  }

  selectPassengerProfile(index: number, passengerId: number | null): void {
    const form = this.formPassengers[index];
    if (!form) {
      return;
    }
    const profile = this.passengerProfiles.find(item => item.id === Number(passengerId));
    form.passengerId = profile?.id ?? null;
    if (profile) {
      form.firstName = profile.firstName;
      form.lastName = profile.lastName;
      form.documentType = this.documentTypeValue(profile.documentType);
      form.documentNumber = profile.documentNumber;
      form.birthDate = profile.birthDate ?? '';
      form.email = profile.email ?? '';
      form.phoneNumber = profile.phoneNumber ?? '';
    }
  }

  priceTypeLabel(value: DeparturePriceType): string {
    return {
      [DeparturePriceType.Standard]: 'General',
      [DeparturePriceType.Child]: 'Menor',
      [DeparturePriceType.Adult]: 'Adulto',
      [DeparturePriceType.Promotional]: 'Promocional'
    }[value] ?? 'General';
  }

  statusLabel(status: ReservationStatus): string {
    return {
      [ReservationStatus.Pending]: 'Pendiente',
      [ReservationStatus.Confirmed]: 'Confirmada',
      [ReservationStatus.PartiallyPaid]: 'Pago parcial',
      [ReservationStatus.Paid]: 'Pagada',
      [ReservationStatus.Cancelled]: 'Cancelada',
      [ReservationStatus.Completed]: 'Completada',
      [ReservationStatus.Expired]: 'Expirada'
    }[status] ?? 'Desconocido';
  }

  openDetails(row: ReservationRow): void {
    this.modal = 'details';
    this.selectedDetail = null;
    this.payments = [];
    this.manifest = [];
    this.passengerTrips = [];
    this.modalError = '';
    this.isLoadingDetail = true;
    this.reservationsService.get(row.id).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        this.isLoadingDetail = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: detail => {
        this.selectedDetail = detail;
        this.notes = detail.reservation.notes ?? '';
        this.discount = detail.reservation.discount;
        this.reservationsService.payments(row.id).pipe(
          takeUntilDestroyed(this.destroyRef)
        ).subscribe({
          next: payments => {
            this.payments = payments;
            this.changeDetector.markForCheck();
          },
          error: error => {
            this.modalError = this.getErrorMessage(error, 'No se pudieron consultar los pagos registrados.');
          }
        });
      },
      error: error => {
        this.modalError = this.getErrorMessage(error, 'No se pudo cargar el detalle de la reserva.');
      }
    });
  }

  saveReservation(isValid: boolean): void {
    if (!isValid || this.isSaving || this.selectedCustomerId === null ||
        this.selectedDepartureId === null || this.formPassengers.length === 0) {
      this.modalError = 'Selecciona un cliente, una salida y al menos un pasajero.';
      return;
    }
    const invalidPassenger = this.formPassengers.some(passenger =>
      !passenger.firstName.trim() || !passenger.lastName.trim() ||
      !passenger.documentNumber.trim());
    if (invalidPassenger) {
      this.modalError = 'Completa nombre, apellido y documento para cada pasajero.';
      return;
    }

    this.modalError = '';
    this.isSaving = true;
    let createdReservationId: number | null = null;
    const passengers = this.formPassengers.map(passenger => this.toPassengerRequest(passenger));

    this.reservationsService.create({
      customerId: this.selectedCustomerId,
      departureId: this.selectedDepartureId,
      notes: this.notes.trim() || null
    }).pipe(
      switchMap(created => {
        createdReservationId = created.reservation.id;
        return from(passengers).pipe(
          concatMap(passenger => this.reservationsService.addPassenger(created.reservation.id, passenger)),
          toArray(),
          switchMap(() => this.reservationsService.update(created.reservation.id, {
            notes: this.notes.trim() || null,
            discount: Number(this.discount) || 0
          })),
          switchMap(updated => this.includeConfirmation
            ? this.departuresService.availability(this.selectedDepartureId!).pipe(
                switchMap(availability => {
                  return availability.passengerCount >= availability.minimumPassengers
                    ? this.reservationsService.confirm(updated.reservation.id)
                    : of(updated);
                })
              )
            : of(updated))
        );
      }),
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        this.isSaving = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: () => {
        this.modal = null;
        this.showToast('La reserva se creó correctamente.', 'success');
        this.currentPage = 1;
        this.loadRequests.next();
      },
      error: error => {
        const message = this.getErrorMessage(error, 'No se pudo completar la creación de la reserva.');
        if (createdReservationId !== null) {
          this.modal = 'details';
          this.showToast(
            `La reserva ${createdReservationId} quedó creada como pendiente, pero una operación posterior falló: ${message}`,
            'error'
          );
          this.loadRequests.next();
          this.loadCreatedDetails(createdReservationId);
        } else {
          this.modalError = message;
        }
      }
    });
  }

  saveChanges(): void {
    const detail = this.selectedDetail;
    if (!detail || this.isSaving) {
      return;
    }
    this.isSaving = true;
    this.modalError = '';
    this.reservationsService.update(detail.reservation.id, {
      notes: this.notes.trim() || null,
      discount: Number(this.discount) || 0
    }).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        this.isSaving = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: updated => {
        this.selectedDetail = updated;
        this.showToast('Reserva actualizada correctamente.', 'success');
        this.loadRequests.next();
      },
      error: error => {
        this.modalError = this.getErrorMessage(error, 'No se pudo actualizar la reserva.');
        this.showToast(this.modalError, 'error');
      }
    });
  }

  confirmReservation(): void {
    const detail = this.selectedDetail;
    if (!detail || this.isSaving) {
      return;
    }
    this.isSaving = true;
    this.modalError = '';
    this.reservationsService.confirm(detail.reservation.id).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        this.isSaving = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: result => {
        this.selectedDetail = result;
        this.showToast('Reserva confirmada correctamente.', 'success');
        this.loadRequests.next();
      },
      error: error => {
        this.modalError = this.getErrorMessage(error, 'No se pudo confirmar la reserva.');
        this.showToast(this.modalError, 'error');
      }
    });
  }

  openCancelDialog(): void {
    this.modal = 'cancel';
    this.cancelReason = '';
    this.refundAmount = 0;
    this.modalError = '';
  }

  cancelReservation(isValid: boolean): void {
    const detail = this.selectedDetail;
    if (!detail || !isValid || this.isSaving) {
      return;
    }
    if (this.refundAmount > this.paidAmount) {
      this.modalError = `El reembolso no puede superar los pagos confirmados (${this.paidAmount.toFixed(2)}).`;
      return;
    }
    this.isSaving = true;
    this.modalError = '';
    this.reservationsService.cancel(detail.reservation.id, {
      reason: this.cancelReason.trim(),
      refundAmount: Number(this.refundAmount) || 0
    }).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        this.isSaving = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: result => {
        this.selectedDetail = result;
        this.modal = 'details';
        this.showToast('La reserva fue cancelada correctamente.', 'success');
        this.loadRequests.next();
      },
      error: error => {
        this.modalError = this.getErrorMessage(error, 'No se pudo cancelar la reserva.');
        this.showToast(this.modalError, 'error');
      }
    });
  }

  loadManifest(): void {
    const departureId = this.selectedDetail?.reservation.departureId;
    if (!departureId) {
      return;
    }
    this.reservationsService.departurePassengers(departureId).pipe(
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: result => {
        this.manifest = result.items;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.modalError = this.getErrorMessage(error, 'No se pudo cargar el manifiesto de la salida.');
      }
    });
  }

  loadPassengerTrips(passengerId: number | null): void {
    if (!passengerId) {
      return;
    }
    this.passengerTrips = [];
    this.passengersService.trips(passengerId, 1, 20).pipe(
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: result => {
        this.passengerTrips = result.items;
        this.showToast(`Historial cargado: ${result.totalCount} viajes registrados.`, 'success');
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.modalError = this.getErrorMessage(error, 'No se pudo consultar el historial del pasajero.');
      }
    });
  }

  closeDialog(): void {
    if (this.isSaving) {
      return;
    }
    this.modal = null;
    this.selectedDetail = null;
    this.modalError = '';
  }

  clearToast(): void {
    if (this.toastTimeout !== null) {
      clearTimeout(this.toastTimeout);
      this.toastTimeout = null;
    }
    this.toastMessage = '';
  }

  private enrichRows(items: ReservationListItem[]): Observable<ReservationRow[]> {
    if (items.length === 0) {
      return of([]);
    }
    const customerIds = [...new Set(items.map(item => item.customerId))];
    const departureIds = [...new Set(items.map(item => item.departureId))];
    const customerCalls = customerIds.map(id =>
      this.customersService.get(id).pipe(catchError(() => of(null))));
    const departureCalls = departureIds.map(id =>
      this.departuresService.get(id).pipe(catchError(() => of(null))));
    return forkJoin({
      customers: forkJoin(customerCalls),
      departures: forkJoin(departureCalls)
    }).pipe(
      switchMap(({ customers, departures }) => {
        const productIds = [...new Set(
          departures.flatMap(departure => departure ? [departure.productId] : [])
        )];
        const productCalls = productIds.map(id =>
          this.productsService.get(id).pipe(catchError(() => of(null))));
        const products$ = productCalls.length > 0
          ? forkJoin(productCalls)
          : of([]);
        return products$.pipe(map(products => {
          const customerNames = new Map<number, string>();
          customerIds.forEach((id, index) => {
            const customer = customers[index];
            customerNames.set(id, customer
              ? customer.customerType === CustomerType.Organization
                ? customer.businessName || 'Empresa'
                : `${customer.firstName ?? ''} ${customer.lastName ?? ''}`.trim() || 'Cliente'
              : `Cliente #${id}`);
          });
          const departureById = new Map<number, Departure | null>();
          departureIds.forEach((id, index) => departureById.set(id, departures[index]));
          const productNames = new Map<number, string>();
          productIds.forEach((id, index) => productNames.set(id, products[index]?.product.name ?? 'Salida'));
          return items.map(item => {
            const departure = departureById.get(item.departureId);
            return {
              ...item,
              customerName: customerNames.get(item.customerId) ?? `Cliente #${item.customerId}`,
              departureLabel: departure
                ? `${productNames.get(departure.productId) ?? 'Salida'} · ${this.formatDate(departure.departureDate)}`
                : `Salida #${item.departureId}`
            };
          });
        }));
      })
    );
  }

  private loadCreatedDetails(id: number): void {
    this.reservationsService.get(id).pipe(
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: detail => {
        this.selectedDetail = detail;
        this.notes = detail.reservation.notes ?? '';
        this.discount = detail.reservation.discount;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.modalError = this.getErrorMessage(error, 'La reserva se creó, pero no se pudo abrir su detalle.');
      }
    });
  }

  private toPassengerRequest(form: PassengerForm): CreateReservationPassengerRequest {
    return {
      passengerId: form.passengerId,
      firstName: form.firstName.trim(),
      lastName: form.lastName.trim(),
      documentType: form.documentType,
      documentNumber: form.documentNumber.trim(),
      birthDate: form.birthDate || null,
      gender: null,
      phoneNumber: form.phoneNumber.trim() || null,
      email: form.email.trim() || null,
      emergencyContactName: null,
      emergencyContactPhone: null,
      specialRequirements: null,
      notes: null,
      priceType: form.priceType
    };
  }

  private documentTypeValue(value: string): number {
    const normalized = value.trim().toLowerCase();
    return normalized.includes('pass') ? 2 : normalized === 'dni' ? 1 : 3;
  }

  private createPassengerForm(): PassengerForm {
    return {
      passengerId: null,
      firstName: '',
      lastName: '',
      documentType: 1,
      documentNumber: '',
      birthDate: '',
      email: '',
      phoneNumber: '',
      priceType: DeparturePriceType.Standard
    };
  }

  private formatDate(value: string): string {
    return new Date(value).toLocaleDateString('es-AR', {
      day: '2-digit',
      month: 'short',
      year: 'numeric'
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
    }, 5500);
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
    const direct = [payload?.detail, payload?.message]
      .find((message): message is string => typeof message === 'string' && !!message.trim());
    if (direct) {
      return direct;
    }
    const validation = Object.entries(payload?.errors ?? {}).flatMap(([field, messages]) =>
      (Array.isArray(messages) ? messages : [messages])
        .filter((message): message is string => typeof message === 'string' && !!message.trim())
        .map(message => `${field}: ${message}`));
    if (validation.length > 0) {
      return validation.join(' ');
    }
    return typeof payload?.title === 'string'
      ? `HTTP ${error.status}: ${payload.title}`
      : `HTTP ${error.status}: ${fallback}`;
  }
}
