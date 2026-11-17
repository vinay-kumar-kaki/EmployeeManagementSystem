export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  success: boolean;
  message: string;
  token?: string;
  expiresAt?: string;
  role?: string;
  email?: string;
  fullName?: string;
  employeeId?: number;
}

export interface RegisterAdminRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}

export interface RegisterEmployeeRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}

export interface CurrentUser {
  userId: string;
  email: string;
  fullName: string;
  role: string;
  employeeId?: number;
}
