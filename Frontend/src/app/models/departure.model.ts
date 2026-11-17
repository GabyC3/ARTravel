export enum DepartureStatus {
  Scheduled = 1,
  Open = 2,
  Full = 3,
  InProgress = 4,
  Completed = 5,
  Cancelled = 6,
  Suspended = 7
}

export interface Departure {
  id: number;
  productId: number;
  itineraryId: number;
  departureDate: string;
  returnDate: string;
  meetingPoint: string;
  meetingTime: string | null;
  capacity: number;
  minimumPassengers: number;
  basePrice: number;
  currency: string;
  status: DepartureStatus;
  notes: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface DepartureAvailability {
  departureId: number;
  capacity: number;
  passengerCount: number;
  availableSeats: number;
  minimumPassengers: number;
  isFull: boolean;
  status: DepartureStatus;
}

export interface DeparturePrice {
  id: number;
  departureId: number;
  priceType: number;
  amount: number;
  currency: string;
  description: string | null;
}

export interface PagedDepartures {
  items: Departure[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface DepartureManifestPassenger {
  reservationId: number;
  reservationNumber: string;
  reservationPassengerId: number;
  passengerId: number | null;
  firstName: string;
  lastName: string;
  documentType: number;
  documentNumber: string;
  birthDate: string | null;
  gender: string | null;
  phoneNumber: string | null;
  email: string | null;
  emergencyContactName: string | null;
  emergencyContactPhone: string | null;
  specialRequirements: string | null;
  notes: string | null;
}

export interface PagedDepartureManifest {
  items: DepartureManifestPassenger[];
  page: number;
  pageSize: number;
  totalCount: number;
}
