export enum DestinationType {
  Region = 1,
  City = 2,
  NaturalAttraction = 3,
  CulturalAttraction = 4,
  HistoricalSite = 5,
  ProtectedArea = 6,
  Other = 7
}

export interface Destination {
  id: number;
  name: string;
  description: string | null;
  destinationType: DestinationType;
  country: string;
  province: string | null;
  city: string | null;
  address: string | null;
  latitude: number | null;
  longitude: number | null;
  altitudeMeters: number | null;
  parentDestinationId: number | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
  primaryImageUrl: string | null;
}

export interface DestinationImage {
  id: number;
  destinationId: number;
  imageUrl: string;
  altText: string | null;
  isPrimary: boolean;
  displayOrder: number;
  createdAt: string;
}

export interface AddDestinationImageRequest {
  imageUrl: string;
  altText: string | null;
  isPrimary: boolean;
  displayOrder: number;
}

export interface DestinationDetail {
  destination: Destination;
  images: DestinationImage[];
}

export interface PagedDestinations {
  items: Destination[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface DestinationFields {
  name: string;
  description: string | null;
  destinationType: DestinationType;
  country: string;
  province: string | null;
  city: string | null;
  address: string | null;
  latitude: number | null;
  longitude: number | null;
  altitudeMeters: number | null;
  parentDestinationId: number | null;
}

export interface CreateDestinationRequest extends DestinationFields {
  isActive: boolean;
  imageUrls: string[];
}

export type UpdateDestinationRequest = DestinationFields;
