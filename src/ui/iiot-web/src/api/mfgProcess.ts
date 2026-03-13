// src/api/mfgProcess.ts
import http from '../utils/http';
import type { Pagination, PagedMetaData } from './employee';

export type { Pagination, PagedMetaData };

// ==========================================
// DTO 类型定义（完全对齐后端 C# Record）
// ==========================================

/** 工序列表项 DTO — 对齐 MfgProcessListItemDto */
export interface MfgProcessListItemDto {
  id: string;
  processCode: string;
  processName: string;
}

/** 工序下拉选择 DTO — 对齐 MfgProcessSelectDto */
export interface MfgProcessSelectDto {
  id: string;
  processCode: string;
  processName: string;
}

/** 创建工序指令 — 对齐 CreateMfgProcessCommand */
export interface CreateMfgProcessPayload {
  ProcessCode: string;
  ProcessName: string;
}

/** 更新工序指令 — 对齐 UpdateMfgProcessCommand */
export interface UpdateMfgProcessPayload {
  ProcessCode: string;
  ProcessName: string;
}

/** 分页返回包装 */
export interface PagedList<T> {
  items: T[];
  metaData: PagedMetaData;
}

// ==========================================
// API 调用函数
// ==========================================

/** 获取工序分页列表 — GET /api/v1/mfgprocess */
export const getMfgProcessPagedListApi = (params: {
  pagination?: Pagination;
  keyword?: string;
}) => {
  return http.get<PagedList<MfgProcessListItemDto>>('/mfgprocess', {
    params: {
      'pagination.PageNumber': params.pagination?.PageNumber ?? 1,
      'pagination.PageSize': params.pagination?.PageSize ?? 10,
      keyword: params.keyword || undefined,
    },
  });
};

/** 获取全量工序列表 (下拉选择器用) — GET /api/v1/mfgprocess/all */
export const getAllMfgProcessesApi = () => {
  return http.get<MfgProcessSelectDto[]>('/mfgprocess/all');
};

/** 创建新工序 — POST /api/v1/mfgprocess */
export const createMfgProcessApi = (payload: CreateMfgProcessPayload) => {
  return http.post<string>('/mfgprocess', payload);
};

/** 更新工序档案 — PUT /api/v1/mfgprocess/{id} */
export const updateMfgProcessApi = (id: string, payload: UpdateMfgProcessPayload) => {
  return http.put<boolean>(`/mfgprocess/${id}`, payload);
};

/** 删除工序 — DELETE /api/v1/mfgprocess/{id} */
export const deleteMfgProcessApi = (id: string) => {
  return http.delete<boolean>(`/mfgprocess/${id}`);
};
