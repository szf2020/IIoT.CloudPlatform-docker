// src/api/identity.ts
import http from '../utils/http';

// ==========================================
// DTO 类型定义
// ==========================================

export interface RolePermissionsDto {
  roleName: string;
  permissions: string[];
}

export interface PermissionGroupDto {
  groupName: string;
  permissions: string[];
}

export interface DefineRolePolicyPayload {
  roleName: string;
  permissions: string[];
}

export interface ChangePasswordPayload {
  userId: string;
  currentPassword: string;
  newPassword: string;
}

export interface ResetPasswordPayload {
  userId: string;
  newPassword: string;
}

export interface UpdateUserPermissionsPayload {
  userId: string;
  permissions: string[];
}

// ==========================================
// API 调用函数
// ==========================================

export const getAllRolesApi = () => {
  return http.get<string[]>('/Identity/roles');
};

export const defineRolePolicyApi = (payload: DefineRolePolicyPayload) => {
  return http.post<boolean>('/Identity/roles', payload);
};

export const getRolePermissionsApi = (roleName: string) => {
  return http.get<RolePermissionsDto>(`/Identity/roles/${roleName}/permissions`);
};

export const updateRolePermissionsApi = (roleName: string, permissions: string[]) => {
  return http.put<boolean>(`/Identity/roles/${roleName}/permissions`, permissions);
};

export const getAllDefinedPermissionsApi = () => {
  return http.get<PermissionGroupDto[]>('/Identity/permissions/all');
};

export const changePasswordApi = (payload: ChangePasswordPayload) => {
  return http.put<boolean>('/Identity/password', payload);
};

export const resetPasswordApi = (payload: ResetPasswordPayload) => {
  return http.put<boolean>('/Identity/password/reset', payload);
};

/** 获取指定员工的个人特批权限 — GET /api/v1/identity/users/{userId}/permissions */
export const getUserPersonalPermissionsApi = (userId: string) => {
  return http.get<string[]>(`/Identity/users/${userId}/permissions`);
};

/** 更新指定员工的个人特批权限 — PUT /api/v1/identity/users/{userId}/permissions */
export const updateUserPermissionsApi = (userId: string, payload: UpdateUserPermissionsPayload) => {
  return http.put<boolean>(`/Identity/users/${userId}/permissions`, payload);
};
