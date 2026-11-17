export enum ProductType {
  Excursion = 1,
  Package = 2
}

export interface TourismProduct {
  id: number;
  name: string;
  code: string;
  description: string | null;
  productType: ProductType;
  durationDays: number;
  durationNights: number;
  basePrice: number;
  currency: string;
  minimumPassengers: number;
  maximumPassengers: number | null;
  includes: string | null;
  excludes: string | null;
  recommendations: string | null;
  cancellationPolicy: string | null;
  isPublished: boolean;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
  primaryImageUrl: string | null;
}

export interface ProductDestination {
  productId: number;
  destinationId: number;
  visitOrder: number;
  isMainDestination: boolean;
  notes: string | null;
  destinationName: string;
  country: string;
  city: string | null;
  destinationType: number;
  province: string | null;
}

export interface ProductImage {
  id: number;
  productId: number;
  imageUrl: string;
  altText: string | null;
  isPrimary: boolean;
  displayOrder: number;
  createdAt: string;
}

export interface ProductDetail {
  product: TourismProduct;
  destinations: ProductDestination[];
  images: ProductImage[];
}

export interface PagedProducts {
  items: TourismProduct[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface ProductFields {
  name: string;
  code: string;
  description: string | null;
  productType: ProductType;
  durationDays: number;
  durationNights: number;
  basePrice: number;
  currency: string;
  minimumPassengers: number;
  maximumPassengers: number | null;
  includes: string | null;
  excludes: string | null;
  recommendations: string | null;
  cancellationPolicy: string | null;
}

export interface CreateProductRequest extends ProductFields {
  isPublished: boolean;
  isActive: boolean;
}

export type UpdateProductRequest = ProductFields;

export interface AssignProductDestinationsRequest {
  destinations: {
    destinationId: number;
    visitOrder: number;
    isMainDestination: boolean;
    notes: string | null;
  }[];
}

export interface AddProductImageRequest {
  imageUrl: string;
  altText: string | null;
  isPrimary: boolean;
  displayOrder: number;
}
