export enum ReservationStatus {
  Pending = 1,
  Confirmed = 2,
  PartiallyPaid = 3,
  Paid = 4,
  Cancelled = 5,
  Completed = 6,
  Expired = 7
}

export enum ReservationDocumentType {
  DNI = 1,
  Passport = 2,
  Other = 3
}

export enum DeparturePriceType {
  Standard = 1,
  Child = 2,
  Adult = 3,
  Promotional = 4
}

export enum ReservationPaymentStatus {
  Pending = 1,
  Paid = 2,
  Failed = 3,
  Refunded = 4
}

export interface ReservationListItem {
  id: number;
  reservationNumber: string;
  customerId: number;
  departureId: number;
  reservationDate: string;
  numberOfPassengers: number;
  subtotal: number;
  discount: number;
  totalAmount: number;
  currency: string;
  status: ReservationStatus;
  notes: string | null;
  createdByUserId: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface ReservationPassenger {
  id: number;
  reservationId: number;
  passengerId: number | null;
  firstName: string;
  lastName: string;
  documentType: ReservationDocumentType;
  documentNumber: string;
  birthDate: string | null;
  gender: string | null;
  phoneNumber: string | null;
  email: string | null;
  emergencyContactName: string | null;
  emergencyContactPhone: string | null;
  specialRequirements: string | null;
  notes: string | null;
  priceType: DeparturePriceType;
  unitPrice: number;
  currency: string;
  createdAt: string;
  updatedAt: string | null;
}

export interface ReservationCancellation {
  id: number;
  reservationId: number;
  reason: string;
  cancelledAt: string;
  cancelledByUserId: number;
  refundAmount: number;
}

export interface ReservationDetail {
  reservation: ReservationListItem;
  passengers: ReservationPassenger[];
  cancellation: ReservationCancellation | null;
}

export interface PagedReservations {
  items: ReservationListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface ReservationPayment {
  id: number;
  reservationId: number;
  amount: number;
  currency: string;
  paymentMethod: string;
  status: ReservationPaymentStatus;
  transactionReference: string | null;
  paidAt: string | null;
  notes: string | null;
  createdAt: string;
}

export interface CreateReservationRequest {
  customerId: number;
  departureId: number;
  notes: string | null;
}

export interface UpdateReservationRequest {
  notes: string | null;
  discount: number;
}

export interface CancelReservationRequest {
  reason: string;
  refundAmount: number;
}

export interface CreateReservationPassengerRequest {
  passengerId: number | null;
  firstName: string;
  lastName: string;
  documentType: ReservationDocumentType;
  documentNumber: string;
  birthDate: string | null;
  gender: string | null;
  phoneNumber: string | null;
  email: string | null;
  emergencyContactName: string | null;
  emergencyContactPhone: string | null;
  specialRequirements: string | null;
  notes: string | null;
  priceType: DeparturePriceType;
}
