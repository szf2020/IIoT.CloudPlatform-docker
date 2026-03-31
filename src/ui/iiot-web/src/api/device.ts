// src/api/device.ts
import http from '../utils/http';
import type { Pagination, PagedMetaData } from './employee';

export type { Pagination, PagedMetaData };

// ==========================================
// DTO 类型定义（完全对齐后端 C# Record）
// ==========================================

/** 设备列表项 DTO — 对齐 DeviceListItemDto */
export interface DeviceListItemDto {
  id: string;
  deviceName: string;
  processId: string;
  isActive: boolean;
}

/** 设备下拉选择 DTO — 对齐 DeviceSelectDto */
export interface DeviceSelectDto {
  id: string;
  deviceName: string;
  processId: string;
  isActive: boolean;
}

/** 设备注册指令 — 对齐 RegisterDeviceCommand */
export interface RegisterDevicePayload {
  DeviceName: string;
  MacAddress: string;
  ProcessId: string;
}

/** 设备档案更新指令 — 对齐 UpdateDeviceProfileCommand */
export interface UpdateDeviceProfilePayload {
  DeviceName: string;
}

/** 分页返回包装 */
export interface PagedList<T> {
  items: T[];
  metaData: PagedMetaData;
}

// ==========================================
// API 调用函数
// ==========================================

/** 获取我管辖范围内的设备分页列表 — GET /api/v1/device */
export const getDevicePagedListApi = (params: {
  PaginationParams?: Pagination;
  Keyword?: string;
}) => {
  return http.get<PagedList<DeviceListItemDto>>('/device', {
    params: {
      'PaginationParams.PageNumber': params.PaginationParams?.PageNumber ?? 1,
      'PaginationParams.PageSize': params.PaginationParams?.PageSize ?? 10,
      Keyword: params.Keyword || undefined,
    },
  });
};

/** 获取全量活跃设备列表 (管辖权分配选择器用，不做ABAC过滤) — GET /api/v1/device/all */
export const getAllActiveDevicesApi = () => {
  return http.get<DeviceSelectDto[]>('/device/all');
};

/** 注册新设备 — POST /api/v1/device */
export const registerDeviceApi = (payload: RegisterDevicePayload) => {
  return http.post<string>('/device', payload);
};

/** 更新设备档案 — PUT /api/v1/device/{id} */
export const updateDeviceProfileApi = (id: string, payload: UpdateDeviceProfilePayload) => {
  return http.put<boolean>(`/device/${id}`, payload);
};

/** 停用设备 — DELETE /api/v1/device/{id} */
export const deactivateDeviceApi = (id: string) => {
  return http.delete<boolean>(`/device/${id}`);
};
