
export interface LoginRequest {
    email: string;
    password: string;
}

export interface RegisterRequest {
    firstName: string;
    lastName: string;
    email: string;
    password: string;
    phoneNumber?: string;
}

export interface UserSummary {
    id: number;
    firstName: string;
    lastName: string;
    email: string;
    phoneNumber: string | null;
    isActive: boolean;
    createdAt: string;
    updatedAt: string | null;
    roles: string[];
}

export interface LoginResponse {
    accessToken: string;
    expiresAt: string;
    refreshToken: string;
    refreshTokenExpiresAt: string;
    user: UserSummary;
}