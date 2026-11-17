export enum ItineraryActivityType {
  Visit = 1,
  Excursion = 2,
  Transfer = 3,
  Meal = 4,
  FreeTime = 5,
  Accommodation = 6,
  Recreational = 7,
  Other = 8
}

export interface Itinerary {
  id: number;
  productId: number;
  name: string;
  description: string | null;
  version: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface ItineraryActivity {
  id: number;
  itineraryDayId: number;
  destinationId: number;
  name: string;
  description: string | null;
  activityType: ItineraryActivityType;
  startTime: string | null;
  endTime: string | null;
  durationMinutes: number;
  displayOrder: number;
  notes: string | null;
}

export interface ItineraryDay {
  id: number;
  itineraryId: number;
  dayNumber: number;
  title: string;
  description: string | null;
  createdAt: string;
  activities: ItineraryActivity[];
}

export interface ItineraryDetail {
  itinerary: Itinerary;
  days: ItineraryDay[];
}

export interface PagedItineraries {
  items: Itinerary[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface CreateItineraryRequest {
  productId: number;
  name: string;
  description: string | null;
}

export type UpdateItineraryRequest = CreateItineraryRequest;

export interface ItineraryDayRequest {
  title: string;
  description: string | null;
}

export interface ItineraryActivityRequest {
  destinationId: number;
  name: string;
  description: string | null;
  activityType: ItineraryActivityType;
  startTime: string | null;
  endTime: string | null;
  durationMinutes: number;
  notes: string | null;
}

export interface AddItineraryActivityRequest extends ItineraryActivityRequest {
  displayOrder: number | null;
}
