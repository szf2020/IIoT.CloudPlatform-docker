// src/types/api.ts

// 1. 使用常量对象代替 enum
export const ResultStatus = {
  Ok: 0,
  Error: 1,
  Forbidden: 2,
  Unauthorized: 3,
  NotFound: 4,
  Invalid: 5
} as const;

// 2. 提取出它的类型，供接口使用 (此时 ResultStatusType 等价于 0 | 1 | 2 | 3 | 4 | 5)
export type ResultStatusType = typeof ResultStatus[keyof typeof ResultStatus];

// 3. 结果泛型接口
export interface ApiResult<T = any> {
  isSuccess: boolean;
  status: ResultStatusType; // 使用上面推导出的类型
  value?: T;
  errors?: string[] | any[];
}