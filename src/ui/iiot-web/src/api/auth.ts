// src/api/auth.ts
import http from '../utils/http';

export interface LoginPayload {
  employeeNo: string;
  password?: string;
}

export const loginApi = (data: LoginPayload) => {
  return http.post<string>('/Identity/login', {
    employeeNo: data.employeeNo,
    password: data.password,
  });
};
