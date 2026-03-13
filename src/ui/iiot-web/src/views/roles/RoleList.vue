<template>
  <div class="role-list">
    <div class="page-header">
      <div>
        <h1 class="page-title">角色与权限</h1>
        <p class="page-sub">定义系统角色并配置行为权限点策略</p>
      </div>
      <button class="btn btn-primary" v-permission="'Role.Define'" @click="openCreateModal">
        <svg viewBox="0 0 16 16" fill="none"><path d="M8 2v12M2 8h12" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/></svg>
        定义新角色
      </button>
    </div>

    <div v-if="loading" class="skeleton-rows">
      <div v-for="i in 3" :key="i" class="skeleton-row"><div class="skel skel-lg"></div><div class="skel skel-md"></div></div>
    </div>

    <div v-else-if="roles.length === 0" class="empty-state">
      <svg viewBox="0 0 48 48" fill="none"><rect x="8" y="10" width="32" height="28" rx="3" stroke="currentColor" stroke-width="1.5" opacity="0.3"/><path d="M16 22h16M16 30h10" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" opacity="0.3"/></svg>
      <p>暂无角色数据</p>
    </div>

    <div v-else class="role-grid">
      <div v-for="role in roles" :key="role" class="role-card" :class="{ admin: role === 'Admin' }">
        <div class="role-card-header">
          <span class="role-name">{{ role }}</span>
          <span v-if="role === 'Admin'" class="admin-badge">系统内置</span>
        </div>
        <div class="role-card-body">
          <div v-if="rolePermissions[role]" class="perm-chips">
            <span v-for="perm in rolePermissions[role]" :key="perm" class="perm-chip">{{ perm }}</span>
            <span v-if="rolePermissions[role].length === 0" class="no-perm">暂无权限点</span>
          </div>
          <div v-else class="perm-loading">加载权限中...</div>
        </div>
        <div class="role-card-footer" v-if="role !== 'Admin'">
          <button class="btn btn-sm btn-ghost" v-permission="'Role.Update'" @click="openPermEditor(role)">
            <svg viewBox="0 0 16 16" fill="none"><path d="M11.5 2.5l2 2-8 8H3.5v-2l8-8z" stroke="currentColor" stroke-width="1.2" stroke-linejoin="round"/></svg>
            编辑权限
          </button>
        </div>
      </div>
    </div>

    <!-- 创建角色弹窗 -->
    <Teleport to="body">
      <div v-if="showCreateModal" class="modal-overlay" @click.self="showCreateModal=false">
        <div class="modal modal-lg">
          <div class="modal-header"><span class="modal-title">定义新角色</span><button class="modal-close" @click="showCreateModal=false">✕</button></div>
          <div class="modal-body">
            <div class="form-field">
              <label class="form-label">角色名称 <span class="required">*</span></label>
              <input class="form-input" v-model="createForm.RoleName" placeholder="如：Operator、Supervisor" />
              <p class="form-hint">角色名建议使用英文，创建后不可修改</p>
            </div>
            <div class="form-field">
              <label class="form-label">分配权限点</label>
              <div v-if="permGroupsLoading" class="perm-loading">加载权限定义中...</div>
              <div v-else class="perm-selector">
                <div v-for="group in permissionGroups" :key="group.groupName" class="perm-group">
                  <div class="perm-group-title">{{ group.groupName }}</div>
                  <div class="perm-group-items">
                    <label v-for="perm in group.permissions" :key="perm" class="perm-checkbox">
                      <input type="checkbox" :value="perm" v-model="createForm.Permissions" />
                      <span class="checkbox-box"></span>
                      <span class="checkbox-label">{{ perm }}</span>
                    </label>
                  </div>
                </div>
              </div>
            </div>
          </div>
          <div class="modal-footer">
            <button class="btn btn-ghost" @click="showCreateModal=false">取消</button>
            <button class="btn btn-primary" :disabled="submitting" @click="submitCreate">{{ submitting ? '创建中...' : '确认创建' }}</button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- 编辑权限弹窗 -->
    <Teleport to="body">
      <div v-if="showPermEditor" class="modal-overlay" @click.self="showPermEditor=false">
        <div class="modal modal-lg">
          <div class="modal-header">
            <div><span class="modal-title">编辑角色权限</span><span class="modal-subtitle">角色：{{ editRoleName }}</span></div>
            <button class="modal-close" @click="showPermEditor=false">✕</button>
          </div>
          <div class="modal-body">
            <div v-if="permGroupsLoading" class="perm-loading">加载权限定义中...</div>
            <div v-else class="perm-selector">
              <div v-for="group in permissionGroups" :key="group.groupName" class="perm-group">
                <div class="perm-group-title">{{ group.groupName }}</div>
                <div class="perm-group-items">
                  <label v-for="perm in group.permissions" :key="perm" class="perm-checkbox">
                    <input type="checkbox" :value="perm" v-model="editPermissions" />
                    <span class="checkbox-box"></span>
                    <span class="checkbox-label">{{ perm }}</span>
                  </label>
                </div>
              </div>
            </div>
          </div>
          <div class="modal-footer">
            <button class="btn btn-ghost" @click="showPermEditor=false">取消</button>
            <button class="btn btn-primary" :disabled="submitting" @click="submitPermissions">{{ submitting ? '保存中...' : '保存权限' }}</button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import {
  getAllRolesApi, defineRolePolicyApi, getRolePermissionsApi, updateRolePermissionsApi,
  getAllDefinedPermissionsApi, type PermissionGroupDto,
} from '../../api/identity';

const roles = ref<string[]>([]);
const rolePermissions = ref<Record<string, string[]>>({});
const loading = ref(false);
const submitting = ref(false);

// 🌟 动态权限分组（从后端拉取，不再硬编码）
const permissionGroups = ref<PermissionGroupDto[]>([]);
const permGroupsLoading = ref(false);

const fetchPermGroups = async () => {
  permGroupsLoading.value = true;
  try {
    permissionGroups.value = await getAllDefinedPermissionsApi() as unknown as PermissionGroupDto[];
  } catch { permissionGroups.value = []; }
  finally { permGroupsLoading.value = false; }
};

const fetchRoles = async () => {
  loading.value = true;
  try {
    const list = await getAllRolesApi() as unknown as string[];
    roles.value = list;
    for (const role of list) {
      try {
        const dto = await getRolePermissionsApi(role) as unknown as { roleName: string; permissions: string[] };
        rolePermissions.value[role] = dto.permissions;
      } catch { rolePermissions.value[role] = []; }
    }
  } catch { roles.value = []; } finally { loading.value = false; }
};

// ── 创建角色 ──
const showCreateModal = ref(false);
const createForm = reactive({ RoleName: '', Permissions: [] as string[] });
const openCreateModal = async () => {
  createForm.RoleName = ''; createForm.Permissions = [];
  showCreateModal.value = true;
  await fetchPermGroups();
};
const submitCreate = async () => {
  if (!createForm.RoleName.trim()) { alert('角色名称不能为空'); return; }
  submitting.value = true;
  try { await defineRolePolicyApi({ RoleName: createForm.RoleName, Permissions: createForm.Permissions }); showCreateModal.value = false; fetchRoles(); }
  catch { } finally { submitting.value = false; }
};

// ── 编辑权限 ──
const showPermEditor = ref(false);
const editRoleName = ref('');
const editPermissions = ref<string[]>([]);
const openPermEditor = async (role: string) => {
  editRoleName.value = role;
  editPermissions.value = [...(rolePermissions.value[role] || [])];
  showPermEditor.value = true;
  await fetchPermGroups();
};
const submitPermissions = async () => {
  submitting.value = true;
  try { await updateRolePermissionsApi(editRoleName.value, editPermissions.value); rolePermissions.value[editRoleName.value] = [...editPermissions.value]; showPermEditor.value = false; }
  catch { } finally { submitting.value = false; }
};

onMounted(() => fetchRoles());
</script>

<style scoped>
* { box-sizing: border-box; }
.role-list { font-family: 'Noto Sans SC', sans-serif; color: #e0e4ef; }
.page-header { display: flex; align-items: flex-start; justify-content: space-between; margin-bottom: 24px; }
.page-title { font-size: 22px; font-weight: 600; color: #fff; margin: 0 0 4px; }
.page-sub { font-size: 13px; color: rgba(255,255,255,0.35); margin: 0; }
.role-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(340px, 1fr)); gap: 16px; }
.role-card { background: rgba(255,255,255,0.02); border: 1px solid rgba(255,255,255,0.06); border-radius: 6px; overflow: hidden; transition: border-color 0.2s; }
.role-card:hover { border-color: rgba(0,229,255,0.15); }
.role-card.admin { border-color: rgba(255,179,0,0.2); }
.role-card-header { display: flex; align-items: center; justify-content: space-between; padding: 16px 18px 12px; }
.role-name { font-size: 16px; font-weight: 600; color: #fff; font-family: 'Rajdhani', 'Noto Sans SC', sans-serif; letter-spacing: 0.5px; }
.admin-badge { font-size: 10px; background: rgba(255,179,0,0.12); color: #ffb300; padding: 2px 8px; border-radius: 3px; border: 1px solid rgba(255,179,0,0.25); }
.role-card-body { padding: 0 18px 14px; }
.perm-chips { display: flex; flex-wrap: wrap; gap: 5px; }
.perm-chip { font-size: 11px; padding: 2px 8px; border-radius: 3px; background: rgba(0,229,255,0.06); border: 1px solid rgba(0,229,255,0.12); color: rgba(0,229,255,0.7); }
.no-perm { font-size: 12px; color: rgba(255,255,255,0.2); }
.perm-loading { font-size: 12px; color: rgba(255,255,255,0.2); text-align: center; padding: 16px 0; }
.role-card-footer { padding: 0 18px 14px; }
.perm-selector { display: flex; flex-direction: column; gap: 18px; max-height: 400px; overflow-y: auto; }
.perm-group-title { font-size: 11px; font-weight: 500; color: rgba(255,255,255,0.3); letter-spacing: 1.5px; text-transform: uppercase; margin-bottom: 10px; padding-bottom: 6px; border-bottom: 1px solid rgba(255,255,255,0.05); }
.perm-group-items { display: flex; flex-wrap: wrap; gap: 8px; }
.perm-checkbox { display: flex; align-items: center; gap: 7px; cursor: pointer; padding: 5px 10px; border-radius: 3px; background: rgba(255,255,255,0.03); border: 1px solid rgba(255,255,255,0.06); transition: all 0.15s; }
.perm-checkbox:hover { background: rgba(0,229,255,0.05); border-color: rgba(0,229,255,0.15); }
.perm-checkbox input { display: none; }
.checkbox-box { width: 14px; height: 14px; border: 1.5px solid rgba(255,255,255,0.2); border-radius: 2px; display: flex; align-items: center; justify-content: center; transition: all 0.15s; flex-shrink: 0; }
.perm-checkbox input:checked ~ .checkbox-box { background: #00e5ff; border-color: #00e5ff; }
.perm-checkbox input:checked ~ .checkbox-box::after { content: '✓'; font-size: 10px; color: #080c18; font-weight: 700; }
.checkbox-label { font-size: 12px; color: rgba(255,255,255,0.55); }
.perm-checkbox input:checked ~ .checkbox-label { color: rgba(0,229,255,0.9); }
.btn-sm { padding: 5px 12px; font-size: 12px; }
.btn-sm svg { width: 12px; height: 12px; }
.btn { display: inline-flex; align-items: center; gap: 6px; padding: 8px 16px; border-radius: 3px; border: none; font-size: 13px; font-family: 'Noto Sans SC', sans-serif; font-weight: 500; cursor: pointer; transition: all 0.18s; white-space: nowrap; }
.btn-primary { background: rgba(0,229,255,0.15); color: #00e5ff; border: 1px solid rgba(0,229,255,0.3); }
.btn-primary:hover:not(:disabled) { background: rgba(0,229,255,0.25); }
.btn-primary:disabled { opacity: 0.4; cursor: not-allowed; }
.btn-primary svg { width: 14px; height: 14px; }
.btn-ghost { background: rgba(255,255,255,0.05); color: rgba(255,255,255,0.55); border: 1px solid rgba(255,255,255,0.1); }
.btn-ghost:hover { background: rgba(255,255,255,0.08); color: rgba(255,255,255,0.75); }
.modal-overlay { position: fixed; inset: 0; z-index: 100; background: rgba(0,0,0,0.7); backdrop-filter: blur(4px); display: flex; align-items: center; justify-content: center; }
.modal { background: #0f1525; border: 1px solid rgba(255,255,255,0.08); border-radius: 6px; width: 520px; max-width: 95vw; max-height: 90vh; overflow: hidden; box-shadow: 0 24px 48px rgba(0,0,0,0.6); display: flex; flex-direction: column; }
.modal-lg { width: 640px; }
.modal-header { display: flex; align-items: center; justify-content: space-between; padding: 18px 22px; border-bottom: 1px solid rgba(255,255,255,0.06); flex-shrink: 0; }
.modal-title { font-size: 15px; font-weight: 600; color: #fff; display: block; }
.modal-subtitle { font-size: 12px; color: rgba(255,255,255,0.35); margin-top: 2px; display: block; }
.modal-close { background: none; border: none; color: rgba(255,255,255,0.3); font-size: 16px; cursor: pointer; padding: 0; line-height: 1; }
.modal-close:hover { color: rgba(255,255,255,0.7); }
.modal-body { padding: 22px; display: flex; flex-direction: column; gap: 16px; overflow-y: auto; flex: 1; }
.modal-footer { display: flex; justify-content: flex-end; gap: 10px; padding: 14px 22px; border-top: 1px solid rgba(255,255,255,0.06); flex-shrink: 0; }
.form-field { display: flex; flex-direction: column; gap: 6px; }
.form-label { font-size: 12px; color: rgba(255,255,255,0.45); font-weight: 500; }
.required { color: #ff8888; }
.form-input { background: rgba(255,255,255,0.04); border: 1px solid rgba(255,255,255,0.1); border-radius: 3px; padding: 8px 12px; color: rgba(255,255,255,0.8); font-size: 13px; font-family: 'Noto Sans SC', sans-serif; outline: none; transition: border-color 0.2s; }
.form-input:focus { border-color: rgba(0,229,255,0.4); }
.form-input::placeholder { color: rgba(255,255,255,0.2); }
.form-hint { font-size: 11px; color: rgba(255,255,255,0.2); margin: 0; }
.skeleton-rows { display: flex; flex-direction: column; gap: 12px; }
.skeleton-row { display: flex; gap: 16px; padding: 20px; background: rgba(255,255,255,0.02); border: 1px solid rgba(255,255,255,0.04); border-radius: 6px; }
.skel { background: rgba(255,255,255,0.06); border-radius: 3px; height: 16px; animation: shimmer 1.5s infinite; }
.skel-md { width: 140px; } .skel-lg { width: 220px; }
@keyframes shimmer { 0%,100% { opacity:0.5; } 50% { opacity:1; } }
.empty-state { display: flex; flex-direction: column; align-items: center; gap: 12px; padding: 60px 0; }
.empty-state svg { width: 48px; height: 48px; color: rgba(255,255,255,0.2); }
.empty-state p { font-size: 13px; color: rgba(255,255,255,0.25); margin: 0; }
</style>
