// src/api/capacity.ts
import http from '../utils/http';
import type { Pagination, PagedMetaData } from './employee';

export type { Pagination, PagedMetaData };

/** 分页返回包装 */
export interface PagedList<T> {
  items: T[];
  metaData: PagedMetaData;
}

// ==========================================
// 查询 API（云端后台产能看板）
// ==========================================

/** 所有机台小时级产能分页查询 — GET /api/v1/Capacity/hourly */
export const getHourlyCapacityPagedApi = (params: {
  PageNumber?: number;
  PageSize?: number;
  date?: string;
  deviceId?: string;
}) => {
  return http.get<PagedList<any>>('/capacity/hourly', {
    params: {
      PageNumber: params.PageNumber ?? 1,
      PageSize: params.PageSize ?? 10,
      date: params.date || undefined,
      deviceId: params.deviceId || undefined,
    },
  });
};

/** 单机台产能趋势查询 — GET /api/v1/Capacity/hourly/device/{deviceId} */
export const getDeviceCapacityTrendApi = (params: {
  deviceId: string;
  startDate: string;
  endDate: string;
}) => {
  return http.get<any[]>(`/capacity/hourly/device/${params.deviceId}`, {
    params: {
      startDate: params.startDate,
      endDate: params.endDate,
    },
  });
};