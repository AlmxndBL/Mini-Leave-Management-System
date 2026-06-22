export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  userId: number;
  email: string;
  firstName: string;
  lastName: string;
  role: number;
  departmentId?: number;
  accessToken: string;
}

export interface User {
  userId: number;
  email: string;
  firstName: string;
  lastName: string;
  role: number;
  departmentId?: number;
  hireDate: string;
  isActive: boolean;
}

export const UserRoles = {
  Employee: 0,
  Manager: 1,
  Admin: 2
};
