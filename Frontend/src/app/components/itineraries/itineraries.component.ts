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
  forkJoin,
  map,
  of,
  startWith,
  switchMap
} from 'rxjs';
import { Destination } from '../../models/destination.model';
import {
  Itinerary,
  ItineraryActivity,
  ItineraryActivityRequest,
  ItineraryActivityType,
  ItineraryDay,
  ItineraryDetail
} from '../../models/itinerary.model';
import { TourismProduct } from '../../models/product.model';
import { DestinationsService } from '../../services/destinations.service';
import { ItinerariesService } from '../../services/itineraries.service';
import { ProductsService } from '../../services/products.service';

type ItineraryModal = 'create' | 'editor' | null;
type ItineraryFilter = 'all' | 'active' | 'inactive';

interface ItineraryForm {
  productId: number | null;
  name: string;
  description: string;
}

interface DayForm {
  title: string;
  description: string;
}

interface ActivityForm {
  destinationId: number | null;
  name: string;
  description: string;
  activityType: ItineraryActivityType;
  startTime: string;
  endTime: string;
  durationMinutes: number;
  notes: string;
}

@Component({
  standalone: true,
  selector: 'app-itineraries',
  imports: [CommonModule, FormsModule],
  templateUrl: './itineraries.component.html',
  styleUrl: './itineraries.component.scss'
})
export class ItinerariesComponent implements OnInit {
  readonly pageSize = 12;
  readonly activityTypes = [
    { value: ItineraryActivityType.Visit, label: 'Visita' },
    { value: ItineraryActivityType.Excursion, label: 'Excursión' },
    { value: ItineraryActivityType.Transfer, label: 'Traslado' },
    { value: ItineraryActivityType.Meal, label: 'Comida' },
    { value: ItineraryActivityType.FreeTime, label: 'Tiempo libre' },
    { value: ItineraryActivityType.Accommodation, label: 'Alojamiento' },
    { value: ItineraryActivityType.Recreational, label: 'Recreativa' },
    { value: ItineraryActivityType.Other, label: 'Otra' }
  ];
  readonly filters: { value: ItineraryFilter; label: string }[] = [
    { value: 'all', label: 'Todas' },
    { value: 'active', label: 'Activas' },
    { value: 'inactive', label: 'Inactivas' }
  ];

  itineraries: Itinerary[] = [];
  products: TourismProduct[] = [];
  destinations: Destination[] = [];
  selectedDetail: ItineraryDetail | null = null;
  form: ItineraryForm = this.emptyItineraryForm();
  dayForm: DayForm = this.emptyDayForm();
  activityForm: ActivityForm = this.emptyActivityForm();
  modal: ItineraryModal = null;
  searchText = '';
  filter: ItineraryFilter = 'all';
  currentPage = 1;
  totalCount = 0;
  expandedDayId: number | null = null;
  editingDayId: number | null = null;
  editingActivityId: number | null = null;
  activeDayId: number | null = null;
  isLoading = false;
  isLoadingOptions = false;
  isLoadingDetail = false;
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
    private readonly itinerariesService: ItinerariesService,
    private readonly productsService: ProductsService,
    private readonly destinationsService: DestinationsService,
    private readonly changeDetector: ChangeDetectorRef,
    private readonly destroyRef: DestroyRef
  ) {
    this.destroyRef.onDestroy(() => this.clearToast());
  }

  ngOnInit(): void {
    this.productsService.list(1, 100, '', null, null, null).pipe(
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: result => {
        this.products = result.items;
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.error = this.getErrorMessage(error, 'No se pudieron cargar los productos asociados.');
      }
    });

    this.loadRequests.pipe(
      startWith(undefined),
      switchMap(() => {
        this.isLoading = true;
        this.error = '';
        const isActive = this.filter === 'all' ? null : this.filter === 'active';
        return this.itinerariesService.list(
          this.currentPage,
          this.pageSize,
          this.searchText,
          null,
          isActive
        ).pipe(
          catchError(error => {
            this.itineraries = [];
            this.totalCount = 0;
            this.error = this.getErrorMessage(error, 'No se pudieron cargar los itinerarios.');
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
      this.itineraries = result.items;
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

  productName(productId: number): string {
    return this.products.find(product => product.id === productId)?.name ?? `Producto #${productId}`;
  }

  activityTypeLabel(type: ItineraryActivityType): string {
    return this.activityTypes.find(item => item.value === type)?.label ?? 'Otra';
  }

  destinationName(destinationId: number): string {
    return this.destinations.find(destination => destination.id === destinationId)?.name ??
      `Destino #${destinationId}`;
  }

  onSearchChange(value: string): void {
    this.searchChanges.next(value.trim());
  }

  setFilter(filter: ItineraryFilter): void {
    this.filter = filter;
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
    this.form = this.emptyItineraryForm();
    this.modal = 'create';
    this.loadOptions();
  }

  openEditor(itinerary: Itinerary): void {
    this.modal = 'editor';
    this.selectedDetail = null;
    this.modalError = '';
    this.isLoadingDetail = true;
    this.loadOptions();
    this.loadDetails(itinerary.id);
  }

  submitItinerary(isValid: boolean): void {
    if (!isValid || this.isSaving || this.form.productId === null) {
      return;
    }
    this.isSaving = true;
    this.modalError = '';
    const request = {
      productId: Number(this.form.productId),
      name: this.form.name.trim(),
      description: this.form.description.trim() || null
    };
    this.itinerariesService.create(request).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        this.isSaving = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: result => {
        this.showToast('Itinerario creado como nueva versión activa.', 'success');
        this.modal = 'editor';
        this.loadRequests.next();
        this.loadDetails(result.itinerary.id);
      },
      error: error => {
        this.modalError = this.getErrorMessage(error, 'No se pudo crear el itinerario.');
        this.showToast(this.modalError, 'error');
      }
    });
  }

  saveItinerary(): void {
    const detail = this.selectedDetail;
    if (!detail || this.isSaving || this.form.productId === null) {
      return;
    }
    this.isSaving = true;
    this.modalError = '';
    this.itinerariesService.update(detail.itinerary.id, {
      productId: Number(this.form.productId),
      name: this.form.name.trim(),
      description: this.form.description.trim() || null
    }).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        this.isSaving = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: () => {
        this.showToast('Datos del itinerario actualizados.', 'success');
        this.loadDetails(detail.itinerary.id);
        this.loadRequests.next();
      },
      error: error => {
        this.modalError = this.getErrorMessage(error, 'No se pudo actualizar el itinerario.');
        this.showToast(this.modalError, 'error');
      }
    });
  }

  setStatus(): void {
    const detail = this.selectedDetail;
    if (!detail || this.isSaving) {
      return;
    }
    this.isSaving = true;
    const nextStatus = !detail.itinerary.isActive;
    this.itinerariesService.setStatus(detail.itinerary.id, nextStatus).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        this.isSaving = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: () => {
        this.showToast(nextStatus ? 'Itinerario activado.' : 'Itinerario desactivado.', 'success');
        this.loadDetails(detail.itinerary.id);
        this.loadRequests.next();
      },
      error: error => {
        this.modalError = this.getErrorMessage(error, 'No se pudo cambiar el estado.');
        this.showToast(this.modalError, 'error');
      }
    });
  }

  duplicate(): void {
    const detail = this.selectedDetail;
    if (!detail || this.isSaving) {
      return;
    }
    this.isSaving = true;
    this.itinerariesService.duplicate(detail.itinerary.id).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        this.isSaving = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: copy => {
        this.showToast(`Se creó la versión ${copy.itinerary.version} del itinerario.`, 'success');
        this.loadRequests.next();
        this.loadDetails(copy.itinerary.id);
      },
      error: error => {
        this.modalError = this.getErrorMessage(error, 'No se pudo duplicar el itinerario.');
        this.showToast(this.modalError, 'error');
      }
    });
  }

  beginDayEdit(day?: ItineraryDay): void {
    this.editingDayId = day?.id ?? -1;
    if (day) {
      this.expandedDayId = day.id;
    }
    this.dayForm = day
      ? { title: day.title, description: day.description ?? '' }
      : this.emptyDayForm();
  }

  saveDay(isValid: boolean): void {
    const detail = this.selectedDetail;
    const dayId = this.editingDayId;
    if (!detail || !isValid || this.isSaving || dayId === null) {
      return;
    }
    this.isSaving = true;
    this.modalError = '';
    const request = {
      title: this.dayForm.title.trim(),
      description: this.dayForm.description.trim() || null
    };
    const operation = dayId === -1
      ? this.itinerariesService.addDay(detail.itinerary.id, request)
      : this.itinerariesService.updateDay(detail.itinerary.id, dayId, request);
    operation.pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        this.isSaving = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: day => {
        this.expandedDayId = day.id;
        this.activeDayId = null;
        this.editingDayId = null;
        this.dayForm = this.emptyDayForm();
        this.showToast('Día guardado correctamente.', 'success');
        this.loadDetails(detail.itinerary.id);
      },
      error: error => {
        this.modalError = this.getErrorMessage(error, 'No se pudo guardar el día.');
        this.showToast(this.modalError, 'error');
      }
    });
  }

  deleteDay(day: ItineraryDay): void {
    const detail = this.selectedDetail;
    if (!detail || this.isSaving || !window.confirm(`¿Eliminar el día "${day.title}" y sus actividades?`)) {
      return;
    }
    this.isSaving = true;
    this.itinerariesService.removeDay(detail.itinerary.id, day.id).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        this.isSaving = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: () => {
        this.showToast('Día eliminado correctamente.', 'success');
        this.loadDetails(detail.itinerary.id);
      },
      error: error => {
        this.modalError = this.getErrorMessage(error, 'No se pudo eliminar el día.');
        this.showToast(this.modalError, 'error');
      }
    });
  }

  moveDay(index: number, direction: -1 | 1): void {
    const detail = this.selectedDetail;
    if (!detail || this.isSaving) {
      return;
    }
    const days = [...detail.days].sort((a, b) => a.dayNumber - b.dayNumber);
    const nextIndex = index + direction;
    if (nextIndex < 0 || nextIndex >= days.length) {
      return;
    }
    [days[index], days[nextIndex]] = [days[nextIndex], days[index]];
    this.isSaving = true;
    this.itinerariesService.reorderDays(detail.itinerary.id, days.map(day => day.id)).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        this.isSaving = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: () => this.loadDetails(detail.itinerary.id),
      error: error => {
        this.modalError = this.getErrorMessage(error, 'No se pudo reordenar los días.');
      }
    });
  }

  beginActivityEdit(dayId: number, activity?: ItineraryActivity): void {
    this.activeDayId = dayId;
    this.editingActivityId = activity?.id ?? null;
    this.activityForm = activity
      ? {
          destinationId: activity.destinationId,
          name: activity.name,
          description: activity.description ?? '',
          activityType: activity.activityType,
          startTime: activity.startTime?.slice(0, 5) ?? '',
          endTime: activity.endTime?.slice(0, 5) ?? '',
          durationMinutes: activity.durationMinutes,
          notes: activity.notes ?? ''
        }
      : this.emptyActivityForm();
  }

  saveActivity(isValid: boolean): void {
    const detail = this.selectedDetail;
    const dayId = this.activeDayId;
    if (!detail || !dayId || !isValid || this.activityForm.destinationId === null || this.isSaving) {
      return;
    }
    if (!!this.activityForm.startTime !== !!this.activityForm.endTime) {
      this.modalError = 'Completa tanto la hora de inicio como la de finalización.';
      return;
    }
    if (this.activityForm.startTime && this.activityForm.endTime <= this.activityForm.startTime) {
      this.modalError = 'La hora de finalización debe ser posterior a la de inicio.';
      return;
    }
    this.isSaving = true;
    this.modalError = '';
    const baseRequest: ItineraryActivityRequest = {
      destinationId: Number(this.activityForm.destinationId),
      name: this.activityForm.name.trim(),
      description: this.activityForm.description.trim() || null,
      activityType: this.activityForm.activityType,
      startTime: this.activityForm.startTime || null,
      endTime: this.activityForm.endTime || null,
      durationMinutes: Number(this.activityForm.durationMinutes) || 0,
      notes: this.activityForm.notes.trim() || null
    };
    const operation = this.editingActivityId === null
      ? this.itinerariesService.addActivity(dayId, {
          ...baseRequest,
          displayOrder: this.getActivities(dayId).length
        })
      : this.itinerariesService.updateActivity(dayId, this.editingActivityId, baseRequest);
    operation.pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        this.isSaving = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: () => {
        this.editingActivityId = null;
        this.activeDayId = null;
        this.activityForm = this.emptyActivityForm();
        this.showToast('Actividad guardada correctamente.', 'success');
        this.loadDetails(detail.itinerary.id);
      },
      error: error => {
        this.modalError = this.getErrorMessage(error, 'No se pudo guardar la actividad.');
        this.showToast(this.modalError, 'error');
      }
    });
  }

  deleteActivity(dayId: number, activity: ItineraryActivity): void {
    const detail = this.selectedDetail;
    if (!detail || this.isSaving || !window.confirm(`¿Eliminar la actividad "${activity.name}"?`)) {
      return;
    }
    this.isSaving = true;
    this.itinerariesService.removeActivity(dayId, activity.id).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        this.isSaving = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: () => {
        this.showToast('Actividad eliminada correctamente.', 'success');
        this.loadDetails(detail.itinerary.id);
      },
      error: error => {
        this.modalError = this.getErrorMessage(error, 'No se pudo eliminar la actividad.');
        this.showToast(this.modalError, 'error');
      }
    });
  }

  moveActivity(dayId: number, index: number, direction: -1 | 1): void {
    const detail = this.selectedDetail;
    if (!detail || this.isSaving) {
      return;
    }
    const activities = [...this.getActivities(dayId)].sort((a, b) => a.displayOrder - b.displayOrder);
    const nextIndex = index + direction;
    if (nextIndex < 0 || nextIndex >= activities.length) {
      return;
    }
    [activities[index], activities[nextIndex]] = [activities[nextIndex], activities[index]];
    this.isSaving = true;
    this.itinerariesService.reorderActivities(dayId, activities.map(activity => activity.id)).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        this.isSaving = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: () => this.loadDetails(detail.itinerary.id),
      error: error => {
        this.modalError = this.getErrorMessage(error, 'No se pudo reordenar las actividades.');
      }
    });
  }

  getActivities(dayId: number): ItineraryActivity[] {
    return this.selectedDetail?.days.find(day => day.id === dayId)?.activities ?? [];
  }

  closeDialog(): void {
    if (this.isSaving) {
      return;
    }
    this.modal = null;
    this.selectedDetail = null;
    this.modalError = '';
    this.editingDayId = null;
    this.editingActivityId = null;
  }

  clearToast(): void {
    if (this.toastTimeout !== null) {
      clearTimeout(this.toastTimeout);
      this.toastTimeout = null;
    }
    this.toastMessage = '';
  }

  private loadOptions(): void {
    this.isLoadingOptions = true;
    forkJoin({
      products: this.productsService.list(1, 100, '', true, null, null),
      destinations: this.destinationsService.list(1, 100, '', true)
    }).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        this.isLoadingOptions = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: result => {
        this.products = result.products.items;
        this.destinations = result.destinations.items;
        if (this.modal === 'create' && this.form.productId === null) {
          this.form.productId = this.products[0]?.id ?? null;
        }
      },
      error: error => {
        this.modalError = this.getErrorMessage(error, 'No se pudieron cargar productos y destinos.');
      }
    });
  }

  private loadDetails(id: number): void {
    this.isLoadingDetail = true;
    this.itinerariesService.get(id).pipe(
      switchMap(detail => this.itinerariesService.getDays(id).pipe(
        switchMap(days => {
          const activityRequests = days.map(day =>
            this.itinerariesService.activities(day.id).pipe(
              map(activities => ({
                ...day,
                activities: [...activities].sort((left, right) => left.displayOrder - right.displayOrder)
              }))
            )
          );
          const days$ = activityRequests.length > 0 ? forkJoin(activityRequests) : of([]);
          return days$.pipe(map(completeDays => ({ ...detail, days: completeDays })));
        })
      )),
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        this.isLoadingDetail = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: detail => {
        this.selectedDetail = {
          ...detail,
          days: [...detail.days].sort((a, b) => a.dayNumber - b.dayNumber)
        };
        this.form = {
          productId: detail.itinerary.productId,
          name: detail.itinerary.name,
          description: detail.itinerary.description ?? ''
        };
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.modalError = this.getErrorMessage(error, 'No se pudo cargar el itinerario y sus etapas.');
      }
    });
  }

  private emptyItineraryForm(): ItineraryForm {
    return { productId: null, name: '', description: '' };
  }

  private emptyDayForm(): DayForm {
    return { title: '', description: '' };
  }

  private emptyActivityForm(): ActivityForm {
    return {
      destinationId: null,
      name: '',
      description: '',
      activityType: ItineraryActivityType.Visit,
      startTime: '',
      endTime: '',
      durationMinutes: 0,
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
