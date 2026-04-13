// src/api/employee.ts
import http from '../utils/http';

// ==========================================
// DTO 类型定义（完全对齐后端 C# Record）
// ==========================================

/** 分页参数 */
export interface Pagination {
  PageNumber: number;
  PageSize: number;
}

/** 分页元数据 */
export interface PagedMetaData {
  totalCount: number;
  pageSize: number;
  currentPage: number;
  totalPages: number;
}

/** 分页列表返回结构（后端 PagedList<T> 序列化后是数组 + MetaData） */
export interface PagedList<T> {
  items: T[];
  metaData: PagedMetaData;
}

/** 员工列表项 DTO */
export interface EmployeeListItemDto {
  id: string;
  employeeNo: string;
  realName: string;
  isActive: boolean;
  deviceCount: number;
}

/** 员工详情 DTO */
export interface EmployeeDetailDto {
  id: string;
  employeeNo: string;
  realName: string;
  isActive: boolean;
  deviceIds: string[];
}

/** 员工设备管辖权 DTO */
export interface EmployeeAccessDto {
  deviceIds: string[];
}

/** 入职指令（对齐 OnboardEmployeeCommand） */
export interface OnboardEmployeePayload {
  employeeNo: string;
  realName: string;
  password: string;
  roleName?: string;
  deviceIds?: string[];
}

/** 更新档案指令（对齐 UpdateEmployeeProfileCommand） */
export interface UpdateProfilePayload {
  employeeId: string;
  realName: string;
  isActive: boolean;
}

/** 更新管辖权指令（对齐 UpdateEmployeeAccessCommand） */
export interface UpdateAccessPayload {
  employeeId: string;
  deviceIds: string[];
}

// ==========================================
// API 调用函数
// ==========================================

/** 获取员工分页列表 */
export const getEmployeePagedListApi = (params: {
  PaginationParams?: Pagination;
  Keyword?: string;
}) => {
  return http.get<PagedList<EmployeeListItemDto>>('/Employee', {
    params: {
      'PaginationParams.PageNumber': params.PaginationParams?.PageNumber ?? 1,
      'PaginationParams.PageSize': params.PaginationParams?.PageSize ?? 10,
      Keyword: params.Keyword || undefined,
    }
  });
};

/** 获取员工详情 */
export const getEmployeeDetailApi = (id: string) => {
  return http.get<EmployeeDetailDto>(`/Employee/${id}`);
};

/** 获取员工双维管辖权 */
export const getEmployeeAccessApi = (id: string) => {
  return http.get<EmployeeAccessDto>(`/Employee/${id}/access`);
};

/** 员工入职建档 */
export const onboardEmployeeApi = (payload: OnboardEmployeePayload) => {
  return http.post<string>('/Employee', payload);
};

/** 更新员工基础档案 */
export const updateEmployeeProfileApi = (id: string, payload: UpdateProfilePayload) => {
  return http.put<boolean>(`/Employee/${id}/profile`, payload);
};

/** 更新员工双维管辖权 */
export const updateEmployeeAccessApi = (id: string, payload: UpdateAccessPayload) => {
  return http.put<boolean>(`/Employee/${id}/access`, payload);
};

/** 停用员工（软删除） */
export const deactivateEmployeeApi = (id: string) => {
  return http.put<boolean>(`/Employee/${id}/deactivate`);
};

/** 离职员工（硬删除） */
export const terminateEmployeeApi = (id: string) => {
  return http.delete<boolean>(`/Employee/${id}`);
};

// ==========================================
// Identity 相关（获取角色列表，入职时用）
// ==========================================
export const getAllRolesApi = () => {
  return http.get<string[]>('/Identity/roles');
};
