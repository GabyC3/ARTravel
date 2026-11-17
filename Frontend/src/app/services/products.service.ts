import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  AddProductImageRequest,
  AssignProductDestinationsRequest,
  CreateProductRequest,
  PagedProducts,
  ProductDetail,
  ProductDestination,
  ProductImage,
  ProductType,
  TourismProduct,
  UpdateProductRequest
} from '../models/product.model';

@Injectable({ providedIn: 'root' })
export class ProductsService {
  private readonly apiUrl = `${environment.apiUrl}/products`;

  constructor(private readonly http: HttpClient) {}

  list(
    page: number,
    pageSize: number,
    search: string,
    isActive: boolean | null,
    isPublished: boolean | null,
    productType: ProductType | null = ProductType.Package
  ): Observable<PagedProducts> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    if (productType !== null) {
      params = params.set('productType', productType);
    }

    if (search.trim()) {
      params = params.set('search', search.trim());
    }
    if (isActive !== null) {
      params = params.set('isActive', isActive);
    }
    if (isPublished !== null) {
      params = params.set('isPublished', isPublished);
    }

    return this.http.get<PagedProducts>(this.apiUrl, { params });
  }

  get(id: number): Observable<ProductDetail> {
    return this.http.get<ProductDetail>(`${this.apiUrl}/${id}`);
  }

  create(request: CreateProductRequest): Observable<ProductDetail> {
    return this.http.post<ProductDetail>(this.apiUrl, request);
  }

  update(id: number, request: UpdateProductRequest): Observable<ProductDetail> {
    return this.http.put<ProductDetail>(`${this.apiUrl}/${id}`, request);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  assignDestinations(
    id: number,
    request: AssignProductDestinationsRequest
  ): Observable<ProductDestination[]> {
    return this.http.post<ProductDestination[]>(`${this.apiUrl}/${id}/destinations`, request);
  }

  addImage(id: number, request: AddProductImageRequest): Observable<ProductImage> {
    return this.http.post<ProductImage>(`${this.apiUrl}/${id}/images`, request);
  }

  removeImage(id: number, imageId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}/images/${imageId}`);
  }

  setPrimaryImage(id: number, imageId: number): Observable<ProductImage> {
    return this.http.patch<ProductImage>(`${this.apiUrl}/${id}/images/${imageId}/primary`, {});
  }

  setStatus(id: number, isActive: boolean): Observable<TourismProduct> {
    return this.http.patch<TourismProduct>(`${this.apiUrl}/${id}/status`, { isActive });
  }

  setPublication(id: number, isPublished: boolean): Observable<TourismProduct> {
    return this.http.patch<TourismProduct>(`${this.apiUrl}/${id}/publication`, { isPublished });
  }
}
