import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ProductType, CreateProductRequest, ProductDetail } from '../../models/product.model';
import { DestinationsService } from '../../services/destinations.service';
import { ProductsService } from '../../services/products.service';
import { PackagesComponent } from './packages.component';

describe('PackagesComponent', () => {
  it('shows an explicit empty state when no packages are returned', async () => {
    await TestBed.configureTestingModule({
      imports: [PackagesComponent],
      providers: [
        {
          provide: ProductsService,
          useValue: { list: () => of({ items: [], page: 1, pageSize: 12, totalCount: 0 }) }
        },
        { provide: DestinationsService, useValue: { list: () => of({ items: [], page: 1, pageSize: 100, totalCount: 0 }) } }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(PackagesComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const heading = fixture.nativeElement.querySelector('.empty-state h2') as HTMLElement | null;
    expect(heading?.textContent?.trim()).toBe('Todavía no hay paquetes turísticos');
  });

  it('creates a draft, assigns ordered destinations and refreshes the list', async () => {
    const calls: string[] = [];
    const createRequests: CreateProductRequest[] = [];
    const productDetail: ProductDetail = {
      product: {
        id: 21,
        name: 'Norte Increíble',
        code: 'NORTE-01',
        description: null,
        productType: ProductType.Package,
        durationDays: 4,
        durationNights: 3,
        basePrice: 450000,
        currency: 'ARS',
        minimumPassengers: 1,
        maximumPassengers: null,
        includes: null,
        excludes: null,
        recommendations: null,
        cancellationPolicy: null,
        isPublished: false,
        isActive: false,
        createdAt: '2026-01-01T00:00:00Z',
        updatedAt: null,
        primaryImageUrl: null
      },
      destinations: [],
      images: []
    };

    await TestBed.configureTestingModule({
      imports: [PackagesComponent],
      providers: [
        {
          provide: ProductsService,
          useValue: {
            list: () => of({ items: [], page: 1, pageSize: 12, totalCount: 0 }),
            create: (request: CreateProductRequest) => {
              calls.push('create');
              createRequests.push(request);
              return of(productDetail);
            },
            assignDestinations: () => {
              calls.push('destinations');
              return of([]);
            },
            get: () => {
              calls.push('refresh-details');
              return of(productDetail);
            }
          }
        },
        { provide: DestinationsService, useValue: { list: () => of({ items: [], page: 1, pageSize: 100, totalCount: 0 }) } }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(PackagesComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    component.form.name = 'Norte Increíble';
    component.form.code = 'NORTE-01';
    component.selectedDestinationIds = [7, 8];
    component.mainDestinationId = 8;

    component.submit(true);

    expect(createRequests[0]?.productType).toBe(ProductType.Package);
    expect(createRequests[0]?.isPublished).toBe(false);
    expect(createRequests[0]?.isActive).toBe(false);
    expect(calls).toEqual(['create', 'destinations', 'refresh-details']);
  });

  it('rejects a maximum passenger count below the minimum before calling the API', async () => {
    let createCalled = false;
    await TestBed.configureTestingModule({
      imports: [PackagesComponent],
      providers: [
        {
          provide: ProductsService,
          useValue: {
            list: () => of({ items: [], page: 1, pageSize: 12, totalCount: 0 }),
            create: () => {
              createCalled = true;
              return of();
            }
          }
        },
        { provide: DestinationsService, useValue: { list: () => of({ items: [], page: 1, pageSize: 100, totalCount: 0 }) } }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(PackagesComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    component.form.name = 'Paquete de prueba';
    component.form.code = 'TEST-01';
    component.form.minimumPassengers = 4;
    component.form.maximumPassengers = 2;
    component.selectedDestinationIds = [7];

    component.submit(true);

    expect(createCalled).toBe(false);
    expect(component.modalError).toContain('máximo de pasajeros');
  });

  it('normalizes decimal prices before sending the package payload', async () => {
    let createdPrice: number | undefined;
    const productDetail: ProductDetail = {
      product: {
        id: 22,
        name: 'Precio decimal',
        code: 'DECIMAL-01',
        description: null,
        productType: ProductType.Package,
        durationDays: 2,
        durationNights: 1,
        basePrice: 500000.5,
        currency: 'ARS',
        minimumPassengers: 5,
        maximumPassengers: 30,
        includes: null,
        excludes: null,
        recommendations: null,
        cancellationPolicy: null,
        isPublished: false,
        isActive: false,
        createdAt: '2026-01-01T00:00:00Z',
        updatedAt: null,
        primaryImageUrl: null
      },
      destinations: [],
      images: []
    };

    await TestBed.configureTestingModule({
      imports: [PackagesComponent],
      providers: [
        {
          provide: ProductsService,
          useValue: {
            list: () => of({ items: [], page: 1, pageSize: 12, totalCount: 0 }),
            create: (request: CreateProductRequest) => {
              createdPrice = request.basePrice;
              return of(productDetail);
            },
            assignDestinations: () => of([]),
            get: () => of(productDetail)
          }
        },
        { provide: DestinationsService, useValue: { list: () => of({ items: [], page: 1, pageSize: 100, totalCount: 0 }) } }
      ]
    }).compileComponents();

    const fixture = TestBed.createComponent(PackagesComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    component.form.name = 'Precio decimal';
    component.form.code = 'DECIMAL-01';
    component.form.basePrice = '500000,50';
    component.form.minimumPassengers = 5;
    component.form.maximumPassengers = 30;
    component.selectedDestinationIds = [7];
    component.mainDestinationId = 7;

    component.submit(true);

    expect(createdPrice).toBe(500000.5);
  });
});
