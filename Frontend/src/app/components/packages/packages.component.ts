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
  from,
  map,
  of,
  startWith,
  switchMap,
  toArray
} from 'rxjs';
import { Destination } from '../../models/destination.model';
import {
  AddProductImageRequest,
  CreateProductRequest,
  ProductDetail,
  ProductFields,
  ProductImage,
  ProductType,
  TourismProduct,
  UpdateProductRequest
} from '../../models/product.model';
import { DestinationsService } from '../../services/destinations.service';
import { ProductsService } from '../../services/products.service';

type PackageStatusFilter = 'all' | 'published' | 'draft' | 'inactive';
type PackageModal = 'create' | 'edit' | 'details' | 'delete' | null;

interface PackageForm {
  name: string;
  code: string;
  description: string;
  durationDays: number;
  durationNights: number;
  basePrice: number | string;
  currency: string;
  minimumPassengers: number;
  maximumPassengers: number | null;
  includes: string;
  excludes: string;
  recommendations: string;
  cancellationPolicy: string;
  publishWhenReady: boolean;
}

interface PackageImageOption {
  url: string;
  label: string;
  altText: string;
}

@Component({
  standalone: true,
  selector: 'app-packages',
  imports: [CommonModule, FormsModule],
  templateUrl: './packages.component.html',
  styleUrl: './packages.component.scss'
})
export class PackagesComponent implements OnInit {
  readonly pageSize = 12;
  readonly statusFilters: { value: PackageStatusFilter; label: string }[] = [
    { value: 'all', label: 'Todos' },
    { value: 'published', label: 'Publicados' },
    { value: 'draft', label: 'Borradores' },
    { value: 'inactive', label: 'Inactivos' }
  ];
  readonly availableImages: PackageImageOption[] = [
    { url: '/images/cordoba.jpg', label: 'Córdoba', altText: 'Paisaje de Córdoba' },
    { url: '/images/carlos_paz.webp', label: 'Villa Carlos Paz', altText: 'Villa Carlos Paz' }
  ];

  packages: TourismProduct[] = [];
  destinations: Destination[] = [];
  selectedDestinationIds: number[] = [];
  mainDestinationId: number | null = null;
  selectedImageUrls: string[] = [];
  primaryImageUrl: string | null = null;
  selectedPackage: TourismProduct | null = null;
  selectedDetail: ProductDetail | null = null;
  form: PackageForm = this.createEmptyForm();
  modal: PackageModal = null;
  searchText = '';
  statusFilter: PackageStatusFilter = 'all';
  currentPage = 1;
  totalCount = 0;
  isLoading = false;
  isLoadingDestinations = false;
  isSaving = false;
  isFilterOpen = false;
  error = '';
  modalError = '';
  toastMessage = '';
  toastType: 'success' | 'error' = 'success';

  private readonly destroyRef: DestroyRef;
  private readonly loadRequests = new Subject<void>();
  private readonly searchChanges = new Subject<string>();
  private toastTimeout: ReturnType<typeof setTimeout> | null = null;

  constructor(
    private readonly productsService: ProductsService,
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
        const isActive = this.statusFilter === 'inactive' ? false : null;
        const isPublished = this.statusFilter === 'published'
          ? true
          : this.statusFilter === 'draft' ? false : null;

        return this.productsService.list(
          this.currentPage,
          this.pageSize,
          this.searchText,
          isActive,
          isPublished
        ).pipe(
          catchError(error => {
            this.packages = [];
            this.totalCount = 0;
            this.isLoading = false;
            this.error = this.getErrorMessage(error, 'No se pudieron cargar los paquetes turísticos.');
            this.changeDetector.markForCheck();
            return EMPTY;
          })
        );
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(result => {
      this.packages = result.items;
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

  get selectableImages(): PackageImageOption[] {
    const detailImages = this.selectedDetail?.images.map(image => ({
      url: image.imageUrl,
      label: image.altText || 'Imagen del paquete',
      altText: image.altText || this.selectedPackage?.name || 'Imagen del paquete'
    })) ?? [];
    return [...this.availableImages, ...detailImages]
      .filter((image, index, images) => images.findIndex(item => item.url === image.url) === index);
  }

  onSearchChange(value: string): void {
    this.searchChanges.next(value.trim());
  }

  setStatusFilter(value: PackageStatusFilter): void {
    this.statusFilter = value;
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
    this.selectedPackage = null;
    this.selectedDetail = null;
    this.form = this.createEmptyForm();
    this.selectedDestinationIds = [];
    this.mainDestinationId = null;
    this.selectedImageUrls = [];
    this.primaryImageUrl = null;
    this.modalError = '';
    this.modal = 'create';
    this.loadAvailableDestinations();
  }

  openDetailsDialog(product: TourismProduct): void {
    this.selectedPackage = product;
    this.selectedDetail = null;
    this.modalError = '';
    this.modal = 'details';
    this.productsService.get(product.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: detail => {
          this.selectedDetail = detail;
          this.changeDetector.markForCheck();
        },
        error: error => {
          this.modalError = this.getErrorMessage(error, 'No se pudo cargar el detalle del paquete.');
          this.showToast(this.modalError, 'error');
          this.changeDetector.markForCheck();
        }
      });
  }

  openEditDialog(): void {
    const detail = this.selectedDetail;
    if (!detail) {
      return;
    }

    this.selectedPackage = detail.product;
    this.form = {
      name: detail.product.name,
      code: detail.product.code,
      description: detail.product.description ?? '',
      durationDays: detail.product.durationDays,
      durationNights: detail.product.durationNights,
      basePrice: detail.product.basePrice,
      currency: detail.product.currency,
      minimumPassengers: detail.product.minimumPassengers,
      maximumPassengers: detail.product.maximumPassengers,
      includes: detail.product.includes ?? '',
      excludes: detail.product.excludes ?? '',
      recommendations: detail.product.recommendations ?? '',
      cancellationPolicy: detail.product.cancellationPolicy ?? '',
      publishWhenReady: false
    };
    this.selectedDestinationIds = detail.destinations
      .slice()
      .sort((a, b) => a.visitOrder - b.visitOrder)
      .map(destination => destination.destinationId);
    this.mainDestinationId = detail.destinations.find(destination => destination.isMainDestination)?.destinationId
      ?? this.selectedDestinationIds[0]
      ?? null;
    this.selectedImageUrls = detail.images.map(image => image.imageUrl);
    this.primaryImageUrl = detail.images.find(image => image.isPrimary)?.imageUrl
      ?? this.selectedImageUrls[0]
      ?? null;
    this.modalError = '';
    this.modal = 'edit';
    this.loadAvailableDestinations();
  }

  openDeleteDialog(): void {
    this.modalError = '';
    this.modal = 'delete';
  }

  closeDialog(): void {
    if (this.isSaving) {
      return;
    }
    this.modal = null;
    this.selectedPackage = null;
    this.selectedDetail = null;
    this.modalError = '';
  }

  onBasePriceInput(value: unknown): void {
    this.form.basePrice = typeof value === 'string' ? value : String(value ?? '');
  }

  private normalizeBasePrice(): void {
    const raw = String(this.form.basePrice ?? '').trim().replace(',', '.');
    if (!/^\d+(?:\.\d{1,2})?$/.test(raw)) {
      this.form.basePrice = 0;
      return;
    }
    const [whole, fraction = ''] = raw.split('.');
    this.form.basePrice = Number(`${whole}.${fraction.padEnd(2, '0')}`);
  }

  toggleDestination(destinationId: number, event: Event): void {
    const input = event.target;
    if (!(input instanceof HTMLInputElement)) {
      return;
    }

    if (input.checked && !this.selectedDestinationIds.includes(destinationId)) {
      this.selectedDestinationIds = [...this.selectedDestinationIds, destinationId];
      this.mainDestinationId ??= destinationId;
      return;
    }

    if (!input.checked) {
      this.selectedDestinationIds = this.selectedDestinationIds.filter(id => id !== destinationId);
      if (this.mainDestinationId === destinationId) {
        this.mainDestinationId = this.selectedDestinationIds[0] ?? null;
      }
    }
  }

  moveDestination(destinationId: number, direction: -1 | 1): void {
    const index = this.selectedDestinationIds.indexOf(destinationId);
    const targetIndex = index + direction;
    if (index < 0 || targetIndex < 0 || targetIndex >= this.selectedDestinationIds.length) {
      return;
    }

    const updated = [...this.selectedDestinationIds];
    [updated[index], updated[targetIndex]] = [updated[targetIndex], updated[index]];
    this.selectedDestinationIds = updated;
  }

  toggleImage(imageUrl: string, event: Event): void {
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

  submit(isValid: boolean): void {
    if (this.isSaving) {
      return;
    }

    this.normalizeBasePrice();
    if (this.selectedDestinationIds.length === 0) {
      const message = 'Selecciona al menos un destino para crear el paquete.';
      this.modalError = message;
      this.showToast(message, 'error');
      return;
    }
    const validationMessage = this.validatePackageForm();
    if (validationMessage) {
      this.modalError = validationMessage;
      this.showToast(validationMessage, 'error');
      return;
    }

    if (!isValid) {
      const message = 'Revisa los campos obligatorios y sus valores antes de continuar.';
      this.modalError = message;
      this.showToast(message, 'error');
      return;
    }

    this.mainDestinationId = Number(this.mainDestinationId);
    if (!this.selectedDestinationIds.includes(this.mainDestinationId ?? -1)) {
      this.mainDestinationId = this.selectedDestinationIds[0];
    }

    this.isSaving = true;
    this.modalError = '';
    const fields = this.toProductFields();
    const destinationRequest = {
      destinations: this.selectedDestinationIds.map((destinationId, index) => ({
        destinationId,
        visitOrder: index + 1,
        isMainDestination: destinationId === this.mainDestinationId,
        notes: null
      }))
    };
    const orderedImageUrls = this.getOrderedImageUrls();
    let createdProductId: number | null = null;
    let failedStage = this.modal === 'edit' ? 'actualizar los datos' : 'crear el paquete';

    let save$: Observable<ProductDetail>;
    if (this.modal === 'edit' && this.selectedPackage) {
      const request: UpdateProductRequest = fields;
      const productId = this.selectedPackage.id;
      save$ = this.productsService.update(productId, request).pipe(
        switchMap(() => {
          failedStage = 'asignar los destinos';
          return this.productsService.assignDestinations(productId, destinationRequest);
        }),
        switchMap(() => {
          failedStage = 'guardar las imágenes';
          return this.syncImages(productId, orderedImageUrls);
        }),
        switchMap(() => this.productsService.get(productId))
      );
    } else {
      const request: CreateProductRequest = {
        ...fields,
        isActive: false,
        isPublished: false
      };
      save$ = this.productsService.create(request).pipe(
        switchMap(detail => {
          createdProductId = detail.product.id;
          failedStage = 'asignar los destinos';
          return this.productsService.assignDestinations(detail.product.id, destinationRequest).pipe(
            switchMap(() => {
              failedStage = 'guardar las imágenes';
              return this.addImages(detail.product.id, orderedImageUrls);
            }),
            switchMap(() => {
              if (!this.form.publishWhenReady) {
                return of(undefined);
              }
              failedStage = 'activar y publicar el paquete';
              return this.publishProduct(detail.product.id);
            }),
            switchMap(() => {
              failedStage = 'recargar el paquete';
              return this.productsService.get(detail.product.id);
            })
          );
        })
      );
    }

    save$.pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        this.isSaving = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: () => this.finishSave(this.modal === 'edit'
        ? 'Paquete actualizado correctamente.'
        : 'Paquete creado correctamente.'),
      error: error => {
        const message = `Falló la etapa «${failedStage}»: ${
          this.getErrorMessage(error, 'No se pudo guardar el paquete.')
        }`;
        this.modalError = createdProductId === null
          ? message
          : `El paquete #${createdProductId} fue creado, pero no se completó la asignación de destinos, imágenes o publicación: ${message}`;
        this.showToast(this.modalError, 'error');
        if (createdProductId !== null) {
          this.modal = null;
          this.selectedPackage = null;
          this.selectedDetail = null;
          this.loadRequests.next();
        } else if (this.modal === 'edit' && this.selectedPackage) {
          this.refreshSelectedPackage(this.selectedPackage.id);
        }
        this.changeDetector.markForCheck();
      }
    });
  }

  confirmDelete(): void {
    if (!this.selectedPackage || this.isSaving) {
      return;
    }

    this.isSaving = true;
    this.modalError = '';
    this.productsService.delete(this.selectedPackage.id)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.isSaving = false;
          this.changeDetector.markForCheck();
        })
      )
      .subscribe({
        next: () => {
          this.showToast('Paquete eliminado correctamente.', 'success');
          this.modal = null;
          this.selectedPackage = null;
          this.selectedDetail = null;
          if (this.packages.length === 1 && this.currentPage > 1) {
            this.currentPage--;
          }
          this.loadRequests.next();
          this.changeDetector.markForCheck();
        },
        error: error => {
          this.modalError = this.getErrorMessage(
            error,
            'No se pudo eliminar el paquete. Si tiene itinerarios o salidas, desactívalo en lugar de eliminarlo.'
          );
          this.showToast(this.modalError, 'error');
          this.changeDetector.markForCheck();
        }
      });
  }

  togglePublication(): void {
    const product = this.selectedDetail?.product;
    if (!product || this.isSaving) {
      return;
    }

    this.isSaving = true;
    this.modalError = '';
    const publication$ = product.isPublished
      ? this.productsService.setPublication(product.id, false)
      : (product.isActive
        ? of(product)
        : this.productsService.setStatus(product.id, true)).pipe(
          switchMap(() => this.productsService.setPublication(product.id, true))
        );

    publication$.pipe(
      switchMap(() => this.productsService.get(product.id)),
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        this.isSaving = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: detail => {
        this.selectedDetail = detail;
        this.selectedPackage = detail.product;
        this.showToast(detail.product.isPublished
          ? 'Paquete publicado y disponible correctamente.'
          : 'Paquete despublicado correctamente.', 'success');
        this.loadRequests.next();
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.modalError = this.getErrorMessage(error, 'No se pudo actualizar la publicación del paquete.');
        this.showToast(this.modalError, 'error');
        this.loadRequests.next();
        this.changeDetector.markForCheck();
      }
    });
  }

  toggleStatus(): void {
    const product = this.selectedDetail?.product;
    if (!product || this.isSaving) {
      return;
    }

    this.isSaving = true;
    this.modalError = '';
    const statusChange$ = product.isActive
      ? (product.isPublished
        ? this.productsService.setPublication(product.id, false).pipe(
          switchMap(() => this.productsService.setStatus(product.id, false))
        )
        : this.productsService.setStatus(product.id, false))
      : this.productsService.setStatus(product.id, true);

    statusChange$.pipe(
      switchMap(() => this.productsService.get(product.id)),
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        this.isSaving = false;
        this.changeDetector.markForCheck();
      })
    ).subscribe({
      next: detail => {
        this.selectedDetail = detail;
        this.selectedPackage = detail.product;
        this.showToast(detail.product.isActive
          ? 'Paquete activado correctamente.'
          : 'Paquete desactivado correctamente.', 'success');
        this.loadRequests.next();
        this.changeDetector.markForCheck();
      },
      error: error => {
        this.modalError = this.getErrorMessage(error, 'No se pudo actualizar el estado del paquete.');
        this.showToast(this.modalError, 'error');
        this.loadRequests.next();
        this.changeDetector.markForCheck();
      }
    });
  }

  durationLabel(product: TourismProduct): string {
    const days = `${product.durationDays} ${product.durationDays === 1 ? 'día' : 'días'}`;
    if (product.durationNights === 0) {
      return days;
    }
    const nights = `${product.durationNights} ${product.durationNights === 1 ? 'noche' : 'noches'}`;
    return `${days} · ${nights}`;
  }

  packageTheme(product: TourismProduct): string {
    return product.id % 2 === 0 ? 'package-cover-cordoba' : 'package-cover-carlos-paz';
  }

  clearToast(): void {
    if (this.toastTimeout !== null) {
      clearTimeout(this.toastTimeout);
      this.toastTimeout = null;
    }
    this.toastMessage = '';
  }

  private loadAvailableDestinations(): void {
    if (this.isLoadingDestinations || this.destinations.length > 0) {
      return;
    }

    this.isLoadingDestinations = true;
    this.destinationsService.list(1, 100, '', true)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.isLoadingDestinations = false;
          this.changeDetector.markForCheck();
        })
      )
      .subscribe({
        next: result => {
          this.destinations = result.items;
          this.changeDetector.markForCheck();
        },
        error: error => {
          this.modalError = this.getErrorMessage(error, 'No se pudieron cargar los destinos disponibles.');
          this.changeDetector.markForCheck();
        }
      });
  }

  private addImages(productId: number, imageUrls: string[]): Observable<ProductImage[]> {
    return from(imageUrls).pipe(
      concatMap((url, index) => {
        const image: AddProductImageRequest = {
          imageUrl: this.toAbsoluteImageUrl(url),
          altText: this.selectableImages.find(option => option.url === url)?.altText ?? null,
          isPrimary: url === this.primaryImageUrl,
          displayOrder: index
        };
        return this.productsService.addImage(productId, image);
      }),
      toArray()
    );
  }

  private syncImages(productId: number, imageUrls: string[]): Observable<void> {
    const existing = this.selectedDetail?.images ?? [];
    const toRemove = existing.filter(image => !imageUrls.includes(image.imageUrl));
    const existingUrls = new Set(existing.map(image => image.imageUrl));
    const toAdd = imageUrls.filter(url => !existingUrls.has(url));

    return from(toRemove).pipe(
      concatMap(image => this.productsService.removeImage(productId, image.id)),
      toArray(),
      switchMap(() => this.addImages(productId, toAdd)),
      switchMap(addedImages => {
        const primaryUrl = this.primaryImageUrl;
        const primaryImage = [
          ...existing.filter(image => imageUrls.includes(image.imageUrl)),
          ...addedImages
        ].find(image => image.imageUrl === primaryUrl ||
          this.toAbsoluteImageUrl(primaryUrl ?? '') === image.imageUrl);

        if (!primaryImage || primaryImage.isPrimary) {
          return of(undefined);
        }
        return this.productsService.setPrimaryImage(productId, primaryImage.id).pipe(map(() => undefined));
      })
    );
  }

  private publishProduct(productId: number): Observable<TourismProduct> {
    return this.productsService.setStatus(productId, true).pipe(
      switchMap(() => this.productsService.setPublication(productId, true))
    );
  }

  private getOrderedImageUrls(): string[] {
    const primaryImageUrl = this.primaryImageUrl;
    return primaryImageUrl !== null && this.selectedImageUrls.includes(primaryImageUrl)
      ? [primaryImageUrl, ...this.selectedImageUrls.filter(url => url !== primaryImageUrl)]
      : this.selectedImageUrls;
  }

  private toProductFields(): ProductFields {
    return {
      name: this.form.name.trim(),
      code: this.form.code.trim(),
      description: this.form.description.trim() || null,
      productType: ProductType.Package,
      durationDays: Number(this.form.durationDays),
      durationNights: Number(this.form.durationNights),
      basePrice: Number(this.form.basePrice),
      currency: this.form.currency.trim().toUpperCase(),
      minimumPassengers: Number(this.form.minimumPassengers),
      maximumPassengers: this.form.maximumPassengers === null ||
        String(this.form.maximumPassengers).trim() === ''
        ? null
        : Number(this.form.maximumPassengers),
      includes: this.form.includes.trim() || null,
      excludes: this.form.excludes.trim() || null,
      recommendations: this.form.recommendations.trim() || null,
      cancellationPolicy: this.form.cancellationPolicy.trim() || null
    };
  }

  private validatePackageForm(): string | null {
    if (!this.form.name.trim()) {
      return 'El nombre del paquete es obligatorio.';
    }
    if (!this.form.code.trim()) {
      return 'El código del paquete es obligatorio.';
    }
    if (!/^[a-z]{3}$/i.test(this.form.currency.trim())) {
      return 'La moneda debe ser un código ISO de tres letras, por ejemplo ARS.';
    }

    const integerFields = [
      { name: 'La duración en días', value: Number(this.form.durationDays), minimum: 0 },
      { name: 'La duración en noches', value: Number(this.form.durationNights), minimum: 0 },
      { name: 'El mínimo de pasajeros', value: Number(this.form.minimumPassengers), minimum: 1 }
    ];
    const invalidInteger = integerFields.find(field =>
      !Number.isInteger(field.value) || field.value < field.minimum);
    if (invalidInteger) {
      return `${invalidInteger.name} debe ser un número entero mayor o igual a ${invalidInteger.minimum}.`;
    }

    const rawPrice = String(this.form.basePrice ?? '').trim().replace(',', '.');
    if (!/^\d+(?:\.\d{1,2})?$/.test(rawPrice)) {
      return 'Ingresa el precio con un máximo de dos decimales, por ejemplo 125000.50.';
    }
    const basePrice = Number(rawPrice);
    if (!Number.isFinite(basePrice) ||
        basePrice < 0 ||
        basePrice > 9999999999.99) {
      return 'El precio debe estar entre 0 y 9.999.999.999,99.';
    }

    const maximumPassengers = this.form.maximumPassengers;
    if (maximumPassengers !== null && String(maximumPassengers).trim() !== '' &&
        (!Number.isInteger(Number(maximumPassengers)) ||
          Number(maximumPassengers) < Number(this.form.minimumPassengers))) {
      return 'El máximo de pasajeros debe ser un entero igual o mayor al mínimo.';
    }

    return null;
  }

  private createEmptyForm(): PackageForm {
    return {
      name: '',
      code: '',
      description: '',
      durationDays: 3,
      durationNights: 2,
      basePrice: 0,
      currency: 'ARS',
      minimumPassengers: 1,
      maximumPassengers: null,
      includes: '',
      excludes: '',
      recommendations: '',
      cancellationPolicy: '',
      publishWhenReady: false
    };
  }

  private finishSave(message: string): void {
    this.showToast(message, 'success');
    this.modal = null;
    this.selectedPackage = null;
    this.selectedDetail = null;
    this.currentPage = 1;
    this.loadRequests.next();
  }

  private refreshSelectedPackage(productId: number): void {
    this.productsService.get(productId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: detail => {
          this.selectedDetail = detail;
          this.selectedPackage = detail.product;
          this.loadRequests.next();
          this.changeDetector.markForCheck();
        },
        error: error => {
          const message = this.getErrorMessage(
            error,
            'No se pudo actualizar el estado guardado del paquete.'
          );
          this.showToast(`${this.toastMessage} ${message}`, 'error');
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

  private toAbsoluteImageUrl(url: string): string {
    return new URL(url, globalThis.location.origin).toString();
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
