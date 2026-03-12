import { createRouter, createWebHistory } from 'vue-router';
import type { RouteRecordRaw } from 'vue-router';

const routes: Array<RouteRecordRaw> = [
  {
    path: '/login',
    name: 'Login',
    component: () => import('../views/Login.vue')
  },
  {
    path: '/',
    name: 'Home',
    // 为了简单测试，首页我们直接内联一个极简组件
    component: { template: '<div><h1>欢迎来到 IIoT 生产管护系统首页!</h1><button @click="logout">退出登录</button></div>', methods: { logout() { localStorage.removeItem("token"); location.reload(); } } }
  }
];

const router = createRouter({
  history: createWebHistory(),
  routes
});

// 🌟 修复：最新版 Vue Router 推荐的拦截器写法
router.beforeEach((to, _from) => {
  const token = localStorage.getItem('token');
  
  // 如果没登录且不是去登录页，踢回登录页
  if (to.name !== 'Login' && !token) {
    return { name: 'Login' };
  } 
  // 如果已登录还想去登录页，送去首页
  else if (to.name === 'Login' && token) {
    return { name: 'Home' };
  } 
  // 其他情况直接放行（不用写 return true 或 next() 了）
});

export default router;