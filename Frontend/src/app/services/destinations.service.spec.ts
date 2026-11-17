import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { DestinationType } from '../models/destination.model';
import { DestinationsService } from './destinations.service';

describe('DestinationsService', () => {
  let service: DestinationsService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(DestinationsService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTestingController.verify());

  it('searches destinations and filters by inactive state', () => {
    service.list(2, 12, 'Jujuy', false).subscribe();

    const request = httpTestingController.expectOne(
      request => request.url === 'http://localhost:5000/api/destinations'
    );
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('pageSize')).toBe('12');
    expect(request.request.params.get('search')).toBe('Jujuy');
    expect(request.request.params.get('isActive')).toBe('false');
    request.flush({ items: [], page: 2, pageSize: 12, totalCount: 0 });
  });

  it('loads the available parent destinations', () => {
    service.parents().subscribe();

    httpTestingController.expectOne({
      method: 'GET',
      url: 'http://localhost:5000/api/destinations/parents'
    }).flush([]);
  });

  it('uses the destination resource routes for creation, update, and removal', () => {
    service.create({
      name: 'Salinas Grandes',
      description: null,
      destinationType: DestinationType.NaturalAttraction,
      country: 'Argentina',
      province: 'Jujuy',
      city: null,
      address: null,
      latitude: null,
      longitude: null,
      altitudeMeters: null,
      parentDestinationId: null,
      isActive: true,
      imageUrls: ['/images/cordoba.jpg']
    }).subscribe();
    const createRequest = httpTestingController.expectOne({
      method: 'POST',
      url: 'http://localhost:5000/api/destinations'
    });
    expect(createRequest.request.body.imageUrls).toEqual(['/images/cordoba.jpg']);
    createRequest.flush({ destination: {}, images: [] });

    service.update(7, {
      name: 'Salinas Grandes Norte',
      description: null,
      destinationType: DestinationType.NaturalAttraction,
      country: 'Argentina',
      province: 'Jujuy',
      city: null,
      address: null,
      latitude: null,
      longitude: null,
      altitudeMeters: null,
      parentDestinationId: null
    }).subscribe();
    httpTestingController.expectOne({
      method: 'PUT',
      url: 'http://localhost:5000/api/destinations/7'
    }).flush({ destination: {}, images: [] });

    service.delete(7).subscribe();
    httpTestingController.expectOne({
      method: 'DELETE',
      url: 'http://localhost:5000/api/destinations/7'
    }).flush(null);

    service.addImage(7, {
      imageUrl: 'https://localhost/images/cordoba.jpg',
      altText: 'Córdoba',
      isPrimary: true,
      displayOrder: 0
    }).subscribe();
    httpTestingController.expectOne({
      method: 'POST',
      url: 'http://localhost:5000/api/destinations/7/images'
    }).flush({});

    service.setPrimaryImage(7, 3).subscribe();
    httpTestingController.expectOne({
      method: 'PATCH',
      url: 'http://localhost:5000/api/destinations/7/images/3/primary'
    }).flush({});

    service.removeImage(7, 3).subscribe();
    httpTestingController.expectOne({
      method: 'DELETE',
      url: 'http://localhost:5000/api/destinations/7/images/3'
    }).flush(null);
  });
});
