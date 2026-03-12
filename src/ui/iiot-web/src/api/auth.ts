import http from '../utils/http';

export interface LoginPayload {
  employeeNo: string;
  password?: string;
}

export const loginApi = (data: LoginPayload) => {
  // 🌟 核心防御：强制组装成与 C# Record 完全一致的首字母大写字段！
  const csharpPayload = {
    EmployeeNo: data.employeeNo,
    Password: data.password
  };
  return http.post<string>('/identity/login', csharpPayload);
};