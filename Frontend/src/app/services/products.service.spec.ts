import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ProductsService } from './products.service';

describe('ProductsService', () => {
  let service: ProductsService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(ProductsService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTestingController.verify());

  it('filters package search by package type and search text', () => {
    service.list(1, 12, 'Iguazú', null, null).subscribe();

    const request = httpTestingController.expectOne(
      request => request.url === 'http://localhost:5000/api/products'
    );
    expect(request.request.params.get('productType')).toBe('2');
    expect(request.request.params.get('search')).toBe('Iguazú');
    request.flush({ items: [], page: 1, pageSize: 12, totalCount: 0 });
  });

  it('uses the product endpoints to assign destinations, add images, publish and delete', () => {
    service.assignDestinations(9, {
      destinations: [{
        destinationId: 7,
        visitOrder: 1,
        isMainDestination: true,
        notes: null
      }]
    }).subscribe();
    const assignRequest = httpTestingController.expectOne({
      method: 'POST',
      url: 'http://localhost:5000/api/products/9/destinations'
    });
    expect(assignRequest.request.body.destinations[0].visitOrder).toBe(1);
    assignRequest.flush([]);

    service.addImage(9, {
      imageUrl: 'https://localhost/images/cordoba.jpg',
      altText: 'Córdoba',
      isPrimary: true,
      displayOrder: 0
    }).subscribe();
    httpTestingController.expectOne({
      method: 'POST',
      url: 'http://localhost:5000/api/products/9/images'
    }).flush({});

    service.setStatus(9, true).subscribe();
    httpTestingController.expectOne({
      method: 'PATCH',
      url: 'http://localhost:5000/api/products/9/status'
    }).flush({});

    service.setPublication(9, true).subscribe();
    httpTestingController.expectOne({
      method: 'PATCH',
      url: 'http://localhost:5000/api/products/9/publication'
    }).flush({});

    service.delete(9).subscribe();
    httpTestingController.expectOne({
      method: 'DELETE',
      url: 'http://localhost:5000/api/products/9'
    }).flush(null);
  });
});
