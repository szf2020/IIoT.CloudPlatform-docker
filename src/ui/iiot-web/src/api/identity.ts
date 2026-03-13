// src/api/identity.ts
import http from '../utils/http';

// ==========================================
// DTO 类型定义
// ==========================================

/** 角色权限详情 DTO — 对齐 RolePermissionsDto */
export interface RolePermissionsDto {
  roleName: string;
  permissions: string[];
}

/** 权限点分组 DTO — 对齐 PermissionGroupDto */
export interface PermissionGroupDto {
  groupName: string;
  permissions: string[];
}

/** 创建角色指令 — 对齐 DefineRolePolicyCommand */
export interface DefineRolePolicyPayload {
  RoleName: string;
  Permissions: string[];
}

/** 修改密码指令 — 对齐 ChangePasswordCommand */
export interface ChangePasswordPayload {
  UserId: string;
  CurrentPassword: string;
  NewPassword: string;
}

// ==========================================
// API 调用函数
// ==========================================

/** 获取全部角色列表 — GET /api/v1/identity/roles */
export const getAllRolesApi = () => {
  return http.get<string[]>('/identity/roles');
};

/** 创建角色并分配权限 — POST /api/v1/identity/roles */
export const defineRolePolicyApi = (payload: DefineRolePolicyPayload) => {
  return http.post<boolean>('/identity/roles', payload);
};

/** 获取指定角色的权限点 — GET /api/v1/identity/roles/{roleName}/permissions */
export const getRolePermissionsApi = (roleName: string) => {
  return http.get<RolePermissionsDto>(`/identity/roles/${roleName}/permissions`);
};

/** 更新角色权限点 — PUT /api/v1/identity/roles/{roleName}/permissions */
export const updateRolePermissionsApi = (roleName: string, permissions: string[]) => {
  return http.put<boolean>(`/identity/roles/${roleName}/permissions`, permissions);
};

/** 获取系统全部已定义的权限点 (动态聚合，按模块分组) — GET /api/v1/identity/permissions/all */
export const getAllDefinedPermissionsApi = () => {
  return http.get<PermissionGroupDto[]>('/identity/permissions/all');
};

/** 修改密码 — PUT /api/v1/identity/password */
export const changePasswordApi = (payload: ChangePasswordPayload) => {
  return http.put<boolean>('/identity/password', payload);
};
