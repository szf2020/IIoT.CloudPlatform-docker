<template>
  <div class="dashboard">
    <div class="page-header">
      <h1 class="page-title">系统概览</h1>
      <span class="page-date">{{ currentDate }}</span>
    </div>

    <!-- 欢迎横幅 -->
    <div class="welcome-banner">
      <div class="banner-left">
        <div class="banner-greeting">你好，<span class="highlight">{{ authStore.employeeNo }}</span></div>
        <div class="banner-role">当前角色：<span class="role-tag">{{ authStore.role || '未分配' }}</span></div>
      </div>
      <div class="banner-right">
        <div class="banner-perm-label">已获授权限点</div>
        <div class="banner-perm-count">{{ authStore.isAdmin ? '全部权限' : authStore.permissions.length + ' 个' }}</div>
      </div>
    </div>

    <!-- 权限点展示卡（方便调试） -->
    <div class="section-title">我的权限点</div>
    <div class="perm-grid" v-if="!authStore.isAdmin">
      <div
        v-for="perm in allPermissions"
        :key="perm"
        class="perm-chip"
        :class="{ owned: authStore.hasPermission(perm) }"
      >
        <span class="perm-dot"></span>
        {{ perm }}
      </div>
    </div>
    <div class="admin-badge" v-else>
      <svg viewBox="0 0 20 20" fill="none"><path d="M10 2l2.4 5h5.3l-4.3 3.1 1.7 5.2L10 12.2l-5.1 3.1 1.7-5.2L2.3 7h5.3L10 2z" fill="#d9b66d" opacity="0.88"/></svg>
      超级管理员 · 拥有系统全部权限
    </div>

    <!-- 快捷入口 -->
    <div class="section-title">快捷入口</div>
    <div class="quick-links">
      <router-link
        v-for="item in visibleQuickLinks"
        :key="item.name"
        :to="item.path"
        class="quick-card"
      >
        <div class="quick-icon" v-html="item.icon"></div>
        <div class="quick-label">{{ item.label }}</div>
        <div class="quick-desc">{{ item.desc }}</div>
        <svg class="quick-arrow" viewBox="0 0 16 16" fill="none"><path d="M4 8h8M9 5l3 3-3 3" stroke="currentColor" stroke-width="1.3" stroke-linecap="round" stroke-linejoin="round"/></svg>
      </router-link>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useAuthStore } from '../stores/auth';
import { Permissions } from '../types/permissions';

const authStore = useAuthStore();

const currentDate = computed(() => {
  return new Date().toLocaleDateString('zh-CN', { year: 'numeric', month: 'long', day: 'numeric', weekday: 'long' });
});

// 全量权限点列表，用于展示当前用户有哪些
const allPermissions = Object.values(Permissions).flatMap(group => Object.values(group));

const quickLinks = [
  {
    name: 'Employees', path: '/employees', label: '员工花名册', desc: '查看与管理操作人员档案',
    permission: Permissions.Employee.Read,
    icon: `<svg viewBox="0 0 24 24" fill="none"><circle cx="9" cy="7" r="4" stroke="currentColor" stroke-width="1.5"/><path d="M3 21c0-4 2.7-7 6-7h6" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/><path d="M16 14l2 2 4-4" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/></svg>`
  },
  {
    name: 'Devices', path: '/devices', label: '设备台账', desc: '注册与追踪车间物理设备',
    permission: Permissions.Device.Read,
    icon: `<svg viewBox="0 0 24 24" fill="none"><rect x="3" y="6" width="18" height="13" rx="2" stroke="currentColor" stroke-width="1.5"/><path d="M8 6V5a1 1 0 011-1h6a1 1 0 011 1v1" stroke="currentColor" stroke-width="1.5"/><circle cx="12" cy="12.5" r="2.5" stroke="currentColor" stroke-width="1.5"/></svg>`
  },
  {
    name: 'Recipes', path: '/recipes', label: '配方管理', desc: '维护工艺参数与配方数据',
    permission: Permissions.Recipe.Read,
    icon: `<svg viewBox="0 0 24 24" fill="none"><path d="M4 6h16M4 10h10M4 14h12M4 18h8" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/><circle cx="18" cy="17" r="3" stroke="currentColor" stroke-width="1.5"/><path d="M18 15.5v1.5l1 1" stroke="currentColor" stroke-width="1.3" stroke-linecap="round"/></svg>`
  },
];

const visibleQuickLinks = computed(() =>
  quickLinks.filter(item => authStore.hasPermission(item.permission))
);
</script>

<style scoped>
.dashboard {
  --accent: #5ab69f;
  --accent-soft: rgba(90, 182, 159, 0.16);
  --warm-soft: rgba(217, 182, 109, 0.16);
  --surface: rgba(255,255,255,0.04);
  --surface-hover: rgba(255,255,255,0.07);
  --border: rgba(255,255,255,0.08);
  --text-main: #edf1ee;
  --text-muted: rgba(237,241,238,0.72);
  --text-subtle: rgba(237,241,238,0.44);
}

.page-header { display: flex; align-items: baseline; justify-content: space-between; margin-bottom: 24px; }
.page-title { margin: 0; color: var(--text-main); font-size: 22px; font-weight: 600; }
.page-date { color: var(--text-subtle); font-size: 12px; }

.welcome-banner {
  display: flex; align-items: center; justify-content: space-between; margin-bottom: 32px; padding: 24px 28px;
  border: 1px solid var(--border); border-radius: 6px; background: linear-gradient(135deg, rgba(90,182,159,0.14), rgba(217,182,109,0.08));
}
.banner-greeting { margin-bottom: 8px; color: var(--text-main); font-size: 18px; font-weight: 500; }
.banner-greeting .highlight { color: #d8f1ea; font-size: 20px; font-weight: 700; }
.banner-role { color: var(--text-muted); font-size: 13px; }
.role-tag { padding: 2px 8px; border-radius: 999px; background: var(--accent-soft); color: #d8f1ea; font-size: 12px; }
.banner-right { text-align: right; }
.banner-perm-label { margin-bottom: 6px; color: var(--text-subtle); font-size: 11px; }
.banner-perm-count { color: #f1d8a6; font-size: 28px; font-weight: 700; }

.section-title { margin-bottom: 16px; color: var(--text-subtle); font-size: 11px; font-weight: 600; }

.perm-grid { display: flex; flex-wrap: wrap; gap: 8px; margin-bottom: 32px; }
.perm-chip {
  display: flex; align-items: center; gap: 6px; padding: 6px 12px; border: 1px solid var(--border); border-radius: 999px;
  background: var(--surface); color: var(--text-subtle); font-size: 12px; transition: all 0.18s ease;
}
.perm-chip.owned { border-color: rgba(90,182,159,0.22); background: var(--accent-soft); color: #d8f1ea; }
.perm-dot { width: 6px; height: 6px; flex-shrink: 0; border-radius: 50%; background: rgba(255,255,255,0.22); }
.perm-chip.owned .perm-dot { background: #5ab69f; }

.admin-badge {
  display: inline-flex; align-items: center; gap: 10px; margin-bottom: 32px; padding: 10px 18px;
  border: 1px solid rgba(217,182,109,0.24); border-radius: 6px; background: var(--warm-soft); color: #f1d8a6; font-size: 14px;
}
.admin-badge svg { width: 18px; height: 18px; }

.quick-links { display: grid; grid-template-columns: repeat(auto-fill, minmax(220px, 1fr)); gap: 16px; }
.quick-card {
  position: relative; display: block; overflow: hidden; padding: 22px; border: 1px solid var(--border); border-radius: 6px;
  background: var(--surface); color: inherit; text-decoration: none; transition: transform 0.18s ease, border-color 0.18s ease, background-color 0.18s ease;
}
.quick-card:hover { transform: translateY(-2px); border-color: rgba(90,182,159,0.22); background: var(--surface-hover); }
.quick-icon { width: 36px; height: 36px; margin-bottom: 14px; color: #d8f1ea; opacity: 0.9; }
.quick-icon :deep(svg) { width: 100%; height: 100%; }
.quick-label { margin-bottom: 6px; color: var(--text-main); font-size: 15px; font-weight: 600; }
.quick-desc { color: var(--text-muted); font-size: 12px; line-height: 1.5; }
.quick-arrow {
  position: absolute; top: 50%; right: 18px; width: 16px; height: 16px; color: rgba(237,241,238,0.2);
  transform: translateY(-50%); transition: all 0.18s ease;
}
.quick-card:hover .quick-arrow { color: #d8f1ea; transform: translateY(-50%) translateX(3px); }
</style>
