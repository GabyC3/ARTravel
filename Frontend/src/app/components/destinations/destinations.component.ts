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
  map,
  of,
  startWith,
  switchMap,
  toArray
} from 'rxjs';
import {
  AddDestinationImageRequest,
  CreateDestinationRequest,
  Destination,
  DestinationDetail,
  DestinationFields,
  DestinationType,
  UpdateDestinationRequest
} from '../../models/destination.model';
import { DestinationsService } from '../../services/destinations.service';

type DestinationStatusFilter = 'active' | 'inactive' | 'all';
type DestinationHierarchy = 'parent' | 'child';
type DestinationModal = 'create' | 'edit' | 'details' | 'delete' | null;

interface DestinationForm {
  name: string;
  description: string;
  destinationType: DestinationType;
  country: string;
  province: string;
  city: string;
  address: string;
  hierarchy: DestinationHierarchy;
  parentDestinationId: number | null;
}

interface DestinationImageOption {
  url: string;
  label: string;
  altText: string;
}

@Component({
  standalone: true,
  selector: 'app-destinations',
  imports: [CommonModule, FormsModule],
  templateUrl: './destinations.component.html',
  styleUrl: './destinations.component.scss'
})
export class DestinationsComponent implements OnInit {
  readonly availableImages: DestinationImageOption[] = [
    { url: '/images/cordoba.jpg', label: 'Córdoba', altText: 'Paisaje de Córdoba' },
    { url: '/images/carlos_paz.webp', label: 'Villa Carlos Paz', altText: 'Villa Carlos Paz' }
  ];

  readonly destinationTypes = [
    { value: DestinationType.Region, label: 'Región' },
    { value: DestinationType.City, label: 'Ciudad' },
    { value: DestinationType.NaturalAttraction, label: 'Atractivo natural' },
    { value: DestinationType.CulturalAttraction, label: 'Atractivo cultural' },
    { value: DestinationType.HistoricalSite, label: 'Sitio histórico' },
    { value: DestinationType.ProtectedArea, label: 'Área protegida' },
    { value: DestinationType.Other, label: 'Otro' }
  ];

  readonly pageSize = 12;
  readonly statusFilters: { value: DestinationStatusFilter; label: string }[] = [
    { value: 'active', label: 'Activos' },
    { value: 'inactive', label: 'Inactivos' },
    { value: 'all', label: 'Todos' }
  ];

  destinations: Destination[] = [];
  parentDestinations: Destination[] = [];
  selectedImageUrls: string[] = [];
  primaryImageUrl: string | null = null;
  selectedDestination: Destination | null = null;
  selectedDetail: DestinationDetail | null = null;
  form: DestinationForm = this.createEmptyForm();
  modal: DestinationModal = null;
  searchText = '';
  statusFilter: DestinationStatusFilter = 'active';
  currentPage = 1;
  totalCount = 0;
  isLoading = false;
  isSaving = false;
  isLoadingEditImages = false;
  isLoadingParents = false;
  parentsLoaded = false;
  isFilterOpen = false;
  openOptionsId: number | null = null;
  error = '';
  toastMessage = '';
  toastType: 'success' | 'error' = 'success';
  modalError = '';
  parentOptionsError = '';

  private readonly destroyRef: DestroyRef;
  private readonly loadRequests = new Subject<void>();
  private readonly searchChanges = new Subject<string>();
  private toastTimeout: ReturnType<typeof setTimeout> | null = null;

  constructor(
    private readonly destinationsService: DestinationsService,
    private readonly changeDetector: ChangeDetectorRef,
    destroyRef: DestroyRef
  ) {
    this.destroyRef = destroyRef;
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

        return this.destinationsService.list(
          this.currentPage,
          this.pageSize,
          this.searchText,
          isActive
        ).pipe(
          catchError(error => {
            this.destinations = [];
            this.totalCount = 0;
            this.isLoading = false;
            this.error = this.getErrorMessage(error, 'No se pudieron cargar los destinos.');
            this.changeDetector.markForCheck();
            return EMPTY;
          })
        );
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(result => {
      this.destinations = result.items;
      this.totalCount = result.totalCount;
      this.isLoading = false;
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
      this.changeDetector.markForCheck();
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

  get detailImageUrl(): string | null {
    const images = this.selectedDetail?.images ?? [];
    return images.find(image => image.isPrimary)?.imageUrl ??
      images[0]?.imageUrl ??
      this.selectedDestination?.primaryImageUrl ??
      null;
  }

  get selectableImages(): DestinationImageOption[] {
    const detailImages = this.selectedDetail?.images.map(image => ({
      url: image.imageUrl,
      label: image.altText || 'Imagen del destino',
      altText: image.altText || this.selectedDestination?.name || 'Imagen del destino'
    })) ?? [];
    return [...this.availableImages, ...detailImages]
      .filter((image, index, images) => images.findIndex(item => item.url === image.url) === index);
  }

  onSearchChange(value: string): void {
    this.searchChanges.next(value.trim());
  }

  setStatusFilter(value: DestinationStatusFilter): void {
    this.statusFilter = value;
    this.currentPage = 1;
    this.isFilterOpen = false;
    this.loadRequests.next();
  }

  onHierarchyChange(hierarchy: DestinationHierarchy): void {
    this.form.hierarchy = hierarchy;
    if (hierarchy === 'parent') {
      this.form.parentDestinationId = null;
      this.parentOptionsError = '';
      return;
    }

    this.loadParentDestinations();
  }

  toggleImageSelection(imageUrl: string, event: Event): void {
    const input = event.target;
    if (!(input instanceof HTMLInputElement)) {
      return;
    }

    if (input.checked && !this.selectedImageUrls.includes(imageUrl)) {
      this.selectedImageUrls = [...this.selectedImageUrls, imageUrl];
      this.primaryImageUrl ??= imageUrl;
      return;
    }

    if (!input.checked) {
      this.selectedImageUrls = this.selectedImageUrls.filter(url => url !== imageUrl);
      if (this.primaryImageUrl === imageUrl) {
        this.primaryImageUrl = this.selectedImageUrls[0] ?? null;
      }
    }
  }

  setPrimaryImage(imageUrl: string): void {
    if (this.selectedImageUrls.includes(imageUrl)) {
      this.primaryImageUrl = imageUrl;
    }
  }

  changePage(page: number): void {
    if (page < 1 || page > this.totalPages || page === this.currentPage) {
      return;
    }

    this.currentPage = page;
    this.loadRequests.next();
  }

  openCreateDialog(): void {
    this.selectedDestination = null;
    this.selectedDetail = null;
    this.form = this.createEmptyForm();
    this.selectedImageUrls = [];
    this.primaryImageUrl = null;
    this.parentDestinations = [];
    this.parentsLoaded = false;
    this.modalError = '';
    this.parentOptionsError = '';
    this.modal = 'create';
    this.loadParentDestinations();
  }

  openEditDialog(destination: Destination): void {
    this.selectedDestination = destination;
    this.form = {
      name: destination.name,
      description: destination.description ?? '',
      destinationType: destination.destinationType,
      country: destination.country,
      province: destination.province ?? '',
      city: destination.city ?? '',
      address: destination.address ?? '',
      hierarchy: destination.parentDestinationId === null ? 'parent' : 'child',
      parentDestinationId: destination.parentDestinationId
    };
    this.selectedImageUrls = [];
    this.primaryImageUrl = null;
    this.modalError = '';
    this.parentOptionsError = '';
    this.isLoadingEditImages = false;
    this.modal = 'edit';
    this.openOptionsId = null;
    if (this.selectedDetail?.destination.id === destination.id) {
      this.setEditImages(this.selectedDetail);
    } else {
      this.selectedDetail = null;
      this.selectedImageUrls = [];
      this.primaryImageUrl = null;
      this.isLoadingEditImages = true;
      this.destinationsService.get(destination.id)
        .pipe(
          takeUntilDestroyed(this.destroyRef),
          finalize(() => {
            this.isLoadingEditImages = false;
            this.changeDetector.markForCheck();
          })
        )
        .subscribe({
          next: detail => {
            if (this.modal === 'edit' && this.selectedDestination?.id === destination.id) {
              this.setEditImages(detail);
            }
            this.changeDetector.markForCheck();
          },
          error: error => {
            this.modalError = this.getErrorMessage(error, 'No se pudieron cargar las imágenes del destino.');
            this.showToast(this.modalError, 'error');
            this.changeDetector.markForCheck();
          }
        });
    }
  }

  openDetailsDialog(destination: Destination): void {
    this.selectedDestination = destination;
    this.selectedDetail = null;
    this.modalError = '';
    this.parentOptionsError = '';
    this.isSaving = false;
    this.modal = 'details';
    this.destinationsService.get(destination.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: details => {
          if (this.modal === 'details' && this.selectedDestination?.id === destination.id) {
            this.selectedDetail = details;
          }
          this.changeDetector.markForCheck();
        },
        error: error => {
          this.modalError = this.getErrorMessage(error, 'No se pudieron cargar los detalles.');
          this.showToast(this.modalError, 'error');
          this.changeDetector.markForCheck();
        }
      });
  }

  openDeleteDialog(destination: Destination): void {
    this.selectedDestination = destination;
    this.modalError = '';
    this.modal = 'delete';
    this.openOptionsId = null;
  }

  closeDialog(): void {
    if (this.isSaving) {
      return;
    }
    this.modal = null;
    this.selectedDestination = null;
    this.selectedDetail = null;
    this.modalError = '';
    this.parentOptionsError = '';
  }

  toggleOptions(destinationId: number): void {
    this.openOptionsId = this.openOptionsId === destinationId ? null : destinationId;
  }

  saveDestination(isValid: boolean): void {
    if (!isValid || this.isSaving) {
      return;
    }

    if (this.modal === 'create' &&
        this.form.hierarchy === 'child' &&
        this.form.parentDestinationId === null) {
      this.modalError = this.parentOptionsError ||
        (this.parentDestinations.length === 0
          ? 'No hay destinos padre activos disponibles. Crea primero un destino padre.'
          : 'Selecciona el destino padre del nuevo destino.');
      this.showToast(this.modalError, 'error');
      return;
    }

    const fields = this.toDestinationFields();
    this.isSaving = true;
    this.modalError = '';

    if (this.modal === 'edit' && this.selectedDestination) {
      const request: UpdateDestinationRequest = fields;
      const destinationId = this.selectedDestination.id;
      this.destinationsService.update(destinationId, request).pipe(
        switchMap(() => this.syncImages(destinationId)),
        takeUntilDestroyed(this.destroyRef)
      ).subscribe({
        next: () => this.finishSave('Destino actualizado correctamente.'),
        error: error => this.handleSaveError(error)
      });
      return;
    }

    const primaryImageUrl = this.primaryImageUrl;
    const imageUrls = primaryImageUrl !== null && this.selectedImageUrls.includes(primaryImageUrl)
      ? [primaryImageUrl, ...this.selectedImageUrls.filter(url => url !== primaryImageUrl)]
      : this.selectedImageUrls;
    const request: CreateDestinationRequest = {
      ...fields,
      isActive: true,
      imageUrls
    };
    this.destinationsService.create(request).subscribe({
      next: () => this.finishSave('Destino creado correctamente.'),
      error: error => this.handleSaveError(error)
    });
  }

  confirmDelete(): void {
    if (!this.selectedDestination || this.isSaving) {
      return;
    }

    this.isSaving = true;
    this.modalError = '';
    this.destinationsService.delete(this.selectedDestination.id).subscribe({
      next: () => {
        this.showToast('Destino eliminado correctamente.', 'success');
        this.modal = null;
        this.selectedDestination = null;
        this.isSaving = false;
        if (this.destinations.length === 1 && this.currentPage > 1) {
          this.currentPage--;
        }
        this.loadRequests.next();
        this.changeDetector.markForCheck();
      },
      error: error => this.handleSaveError(error)
    });
  }

  typeLabel(type: DestinationType): string {
    return this.destinationTypes.find(option => option.value === type)?.label ?? 'Destino';
  }

  imageTheme(destination: Destination): string {
    const themes = ['lake', 'valley', 'waterfall'];
    return themes[destination.id % themes.length];
  }

  private finishSave(message: string): void {
    this.showToast(message, 'success');
    this.modal = null;
    this.selectedDestination = null;
    this.isSaving = false;
    this.changeDetector.markForCheck();
    this.currentPage = 1;
    this.loadRequests.next();
  }

  private handleSaveError(error: unknown): void {
    this.modalError = this.getErrorMessage(error, 'No se pudo completar la operación.');
    this.showToast(this.modalError, 'error');
    this.isSaving = false;
    this.changeDetector.markForCheck();
    if (this.modal === 'edit' && this.selectedDestination) {
      const destinationId = this.selectedDestination.id;
      this.destinationsService.get(destinationId)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: detail => {
            this.setEditImages(detail);
            this.loadRequests.next();
            this.changeDetector.markForCheck();
          },
          error: refreshError => {
            const refreshMessage = this.getErrorMessage(
              refreshError,
              'No se pudo actualizar el estado guardado del destino.'
            );
            this.showToast(`${this.toastMessage} ${refreshMessage}`, 'error');
            this.changeDetector.markForCheck();
          }
        });
    }
  }

  private setEditImages(detail: DestinationDetail): void {
    this.selectedDetail = detail;
    this.selectedImageUrls = detail.images.map(image => image.imageUrl);
    this.primaryImageUrl = detail.images.find(image => image.isPrimary)?.imageUrl
      ?? this.selectedImageUrls[0]
      ?? null;
  }

  private syncImages(destinationId: number) {
    const existing = this.selectedDetail?.images ?? [];
    const toRemove = existing.filter(image => !this.selectedImageUrls.includes(image.imageUrl));
    const existingUrls = new Set(existing.map(image => image.imageUrl));
    const toAdd = this.selectedImageUrls.filter(url => !existingUrls.has(url));

    return from(toRemove).pipe(
      concatMap(image => this.destinationsService.removeImage(destinationId, image.id)),
      toArray(),
      switchMap(() => from(toAdd).pipe(
        concatMap((url, index) => {
          const request: AddDestinationImageRequest = {
            imageUrl: this.toAbsoluteImageUrl(url),
            altText: this.selectableImages.find(image => image.url === url)?.altText ?? null,
            isPrimary: url === this.primaryImageUrl,
            displayOrder: index
          };
          return this.destinationsService.addImage(destinationId, request);
        }),
        toArray()
      )),
      switchMap(addedImages => {
        const primaryUrl = this.primaryImageUrl;
        const primaryImage = [
          ...existing.filter(image => this.selectedImageUrls.includes(image.imageUrl)),
          ...addedImages
        ].find(image => image.imageUrl === primaryUrl ||
          this.toAbsoluteImageUrl(primaryUrl ?? '') === image.imageUrl);

        if (!primaryImage || primaryImage.isPrimary) {
          return of(undefined);
        }
        return this.destinationsService.setPrimaryImage(destinationId, primaryImage.id)
          .pipe(map(() => undefined));
      })
    );
  }

  private toAbsoluteImageUrl(url: string): string {
    return new URL(url, globalThis.location.origin).toString();
  }

  private loadParentDestinations(): void {
    if (this.parentsLoaded || this.isLoadingParents) {
      return;
    }

    this.isLoadingParents = true;
    this.parentOptionsError = '';
    this.destinationsService.parents()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.changeDetector.markForCheck())
      )
      .subscribe({
        next: parents => {
          this.parentDestinations = parents;
          this.parentsLoaded = true;
          this.isLoadingParents = false;
          this.changeDetector.markForCheck();
        },
        error: error => {
          this.parentOptionsError = this.getErrorMessage(
            error,
            'No se pudieron cargar los destinos padre.'
          );
          this.isLoadingParents = false;
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

  clearToast(): void {
    if (this.toastTimeout !== null) {
      clearTimeout(this.toastTimeout);
      this.toastTimeout = null;
    }
  }

  private toDestinationFields(): DestinationFields {
    return {
      name: this.form.name.trim(),
      description: this.form.description.trim() || null,
      destinationType: this.form.destinationType,
      country: this.form.country.trim(),
      province: this.form.province.trim() || null,
      city: this.form.city.trim() || null,
      address: this.form.address.trim() || null,
      latitude: null,
      longitude: null,
      altitudeMeters: null,
      parentDestinationId: this.modal === 'create'
        ? this.form.hierarchy === 'child' ? this.form.parentDestinationId : null
        : this.selectedDestination?.parentDestinationId ?? null
    };
  }

  private createEmptyForm(): DestinationForm {
    return {
      name: '',
      description: '',
      destinationType: DestinationType.Region,
      country: 'Argentina',
      province: '',
      city: '',
      address: '',
      hierarchy: 'parent',
      parentDestinationId: null
    };
  }

  private getErrorMessage(error: unknown, fallback: string): string {
    if (!(error instanceof HttpErrorResponse)) {
      return fallback;
    }
    if (typeof error.error === 'string' && error.error.trim()) {
      return error.error;
    }
    if (typeof error.error !== 'object' || error.error === null) {
      return fallback;
    }

    const payload = error.error as {
      message?: unknown;
      detail?: unknown;
      title?: unknown;
      errors?: Record<string, unknown>;
    };
    const directMessage = [payload.detail, payload.message]
      .find((message): message is string =>
        typeof message === 'string' && message.trim().length > 0);
    if (directMessage) {
      return directMessage;
    }

    const validationMessages = Object.entries(payload.errors ?? {})
      .flatMap(([field, messages]) =>
        (Array.isArray(messages) ? messages : [messages])
          .filter((message): message is string =>
            typeof message === 'string' && message.trim().length > 0)
          .map(message => `${field}: ${message}`));
    if (validationMessages.length > 0) {
      return validationMessages.join(' ');
    }

    return typeof payload.title === 'string' && payload.title.trim()
      ? `HTTP ${error.status}: ${payload.title}`
      : `HTTP ${error.status}: ${fallback}`;
  }
}
