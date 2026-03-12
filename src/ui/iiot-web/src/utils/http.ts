// src/utils/http.ts

import axios from 'axios';
// 🌟 核心修复: 纯类型必须使用 import type
import type { AxiosInstance, AxiosResponse, InternalAxiosRequestConfig } from 'axios';

// 🌟 核心修复: ApiResult 是纯类型用 import type，ResultStatus 是常量对象直接 import
import { ResultStatus } from '../types/api';
import type { ApiResult } from '../types/api';

// 创建 axios 实例
const http: AxiosInstance = axios.create({
  baseURL: '/api/v1', // 配合 vite.config.ts 的代理，自动指向后端的 v1 版本接口
  timeout: 15000,     // 超时时间
  headers: {
    'Content-Type': 'application/json'
  }
});

// ==========================================
// 1. 请求拦截器：自动注入 JWT Token
// ==========================================
http.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    // 假设登录后 token 存在 localStorage 中
    const token = localStorage.getItem('token');
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// ==========================================
// 2. 响应拦截器：状态机核心防线
// ==========================================
http.interceptors.response.use(
  (response: AxiosResponse<ApiResult>) => {
    const res = response.data;

    // 如果返回的不是我们标准的 ApiResult 结构（比如直接下载文件），直接放行
    if (res.status === undefined) {
      return response.data;
    }

    // 🌟 核心状态机审判
    switch (res.status) {
      case ResultStatus.Ok:
        // 成功时：直接剥离 ApiResult 外壳，把核心业务数据 value 交给 Vue 组件！
        return res.value;

      case ResultStatus.Error:
      case ResultStatus.Invalid:
      case ResultStatus.NotFound:
        // 业务错误：如“设备编号已存在”、“密码错误”
        const errorMsg = res.errors?.join('\n') || '业务处理失败';
        console.error('业务拦截:', errorMsg);
        alert(`提示:\n${errorMsg}`); // TODO: 后续可替换为 UI 框架的 Message 组件
        return Promise.reject(res);

      case ResultStatus.Forbidden:
        // ABAC 越权拦截：对应后端的 ForbiddenException 或权限点校验失败
        console.warn('越权警告：您没有权限执行此操作！');
        alert('越权警告：您没有该模块或数据的管辖权！'); // TODO: 后续可实现路由重定向
        return Promise.reject(res);

      case ResultStatus.Unauthorized:
        // 凭证失效：对应后端拦截未登录请求
        console.warn('凭证已过期');
        localStorage.removeItem('token');
        alert('登录已过期，请重新登录！'); 
        window.location.href = '/login';
        return Promise.reject(res);

      default:
        return Promise.reject(res);
    }
  },
  (error) => {
    // ==========================================
    // 3. HTTP 层面异常处理 (兜底防线)
    // ==========================================
    // 对应后端 UseCaseExceptionHandler 抛出的 403 状态码
    if (error.response) {
      if (error.response.status === 401) {
        localStorage.removeItem('token');
        window.location.href = '/login';
      } else if (error.response.status === 403) {
        alert('系统拒绝访问：权限不足 (HTTP 403)');
      } else if (error.response.status === 400) {
        // 模型绑定报错兜底
        alert('请求参数格式有误 (HTTP 400)');
      } else if (error.response.status === 500) {
        alert('服务器内部发生致命崩溃，请联系管理员查看日志。');
      }
    } else {
      alert('网络连接异常，请检查后端服务是否正常启动。');
    }
    return Promise.reject(error);
  }
);

export default http;