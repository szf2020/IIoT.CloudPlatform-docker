<template>
  <div class="device-log-page">
    <div class="page-header">
      <div>
        <h1 class="page-title">设备日志</h1>
        <p class="page-sub">按设备、级别、关键字和时间范围检索设备运行日志。</p>
      </div>
    </div>

    <div class="device-select-bar">
      <label>设备</label>
      <select v-model="selectedDeviceId" class="filter-input device-select" @change="onDeviceChange">
        <option value="">请先选择设备</option>
        <option v-for="device in allDevices" :key="device.id" :value="device.id">
          {{ device.deviceName }}
        </option>
      </select>
    </div>

    <div v-if="selectedDeviceId" class="query-modes">
      <button
        v-for="mode in queryModes"
        :key="mode.key"
        class="mode-btn"
        :class="{ active: currentMode === mode.key }"
        @click="switchMode(mode.key)"
      >
        {{ mode.label }}
      </button>
    </div>

    <div v-if="selectedDeviceId" class="filter-bar">
      <template v-if="currentMode === 'level'">
        <div class="filter-field">
          <label>日志级别</label>
          <select v-model="filters.level" class="filter-input">
            <option value="">全部级别</option>
            <option value="INFO">INFO</option>
            <option value="WARN">WARN</option>
            <option value="ERROR">ERROR</option>
          </select>
        </div>
      </template>

      <template v-if="currentMode === 'keyword'">
        <div class="filter-field filter-field--wide">
          <label>关键字</label>
          <input
            v-model="filters.keyword"
            class="filter-input"
            placeholder="搜索日志内容"
            @keyup.enter="doSearch"
          />
        </div>
      </template>

      <template v-if="currentMode === 'date'">
        <div class="filter-field filter-field--date">
          <label>日期</label>
          <input type="date" v-model="filters.date" class="filter-input" />
        </div>
      </template>

      <template v-if="currentMode === 'time-range'">
        <div class="filter-field filter-field--time">
          <label>开始时间</label>
          <input type="datetime-local" v-model="filters.startTime" class="filter-input" />
        </div>
        <div class="filter-field filter-field--time">
          <label>结束时间</label>
          <input type="datetime-local" v-model="filters.endTime" class="filter-input" />
        </div>
      </template>

      <template v-if="currentMode === 'date-keyword'">
        <div class="filter-field filter-field--date">
          <label>日期</label>
          <input type="date" v-model="filters.date" class="filter-input" />
        </div>
        <div class="filter-field filter-field--wide">
          <label>关键字</label>
          <input
            v-model="filters.keyword"
            class="filter-input"
            placeholder="搜索日志内容"
            @keyup.enter="doSearch"
          />
        </div>
      </template>

      <button class="btn btn-primary search-btn" @click="doSearch">
        <svg viewBox="0 0 16 16" fill="none">
          <circle cx="6.5" cy="6.5" r="4.5" stroke="currentColor" stroke-width="1.3" />
          <path d="M10 10l3 3" stroke="currentColor" stroke-width="1.3" stroke-linecap="round" />
        </svg>
        查询
      </button>
    </div>

    <div v-if="!selectedDeviceId" class="empty-cell">
      <div class="empty-state">
        <svg viewBox="0 0 48 48" fill="none">
          <rect x="8" y="8" width="32" height="32" rx="4" stroke="currentColor" stroke-width="1.5" opacity="0.25" />
          <path d="M18 20h12M18 26h8M18 32h10" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" opacity="0.25" />
        </svg>
        <p>请先选择一台设备。</p>
      </div>
    </div>

    <div v-if="selectedDeviceId" class="table-wrap">
      <div v-if="loading" class="skeleton-rows">
        <div v-for="index in 5" :key="index" class="skeleton-row">
          <div class="skel skel-sm"></div>
          <div class="skel skel-lg"></div>
          <div class="skel skel-md"></div>
        </div>
      </div>

      <div v-else-if="!searched" class="empty-cell">
        <div class="empty-state">
          <svg viewBox="0 0 48 48" fill="none">
            <circle cx="20" cy="20" r="14" stroke="currentColor" stroke-width="1.5" opacity="0.25" />
            <path d="M30 30l10 10" stroke="currentColor" stroke-width="2" stroke-linecap="round" opacity="0.25" />
          </svg>
          <p>设置条件后点击查询。</p>
        </div>
      </div>

      <table v-else-if="records.length > 0" class="data-table">
        <thead>
          <tr>
            <th class="th-level">级别</th>
            <th>日志内容</th>
            <th class="th-time">日志时间</th>
            <th class="th-time">接收时间</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="record in records"
            :key="record.id"
            class="table-row"
            @click="expandedId = expandedId === record.id ? null : record.id"
          >
            <td>
              <span class="level-tag" :class="levelClass(record.level)">
                {{ record.level }}
              </span>
            </td>
            <td>
              <div class="msg-cell" :class="{ expanded: expandedId === record.id }">
                {{ record.message }}
              </div>
            </td>
            <td class="time-cell">{{ formatTime(record.logTime) }}</td>
            <td class="time-cell">{{ formatTime(record.receivedAt) }}</td>
          </tr>
        </tbody>
      </table>

      <div v-else class="empty-cell">
        <div class="empty-state">
          <svg viewBox="0 0 48 48" fill="none">
            <rect x="8" y="6" width="28" height="36" rx="2" stroke="currentColor" stroke-width="1.5" opacity="0.25" />
            <path d="M16 20h12M16 26h8" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" opacity="0.25" />
          </svg>
          <p>未查询到匹配的日志记录。</p>
        </div>
      </div>
    </div>

    <div class="pagination" v-if="metaData.totalPages > 1">
      <button class="page-btn" :disabled="currentPage === 1" @click="goPage(currentPage - 1)">
        <svg viewBox="0 0 12 12" fill="none">
          <path d="M8 2L4 6l4 4" stroke="currentColor" stroke-width="1.3" stroke-linecap="round" />
        </svg>
      </button>
      <button
        v-for="page in pageNumbers"
        :key="page"
        class="page-btn"
        :class="{ active: page === currentPage }"
        @click="goPage(page)"
      >
        {{ page }}
      </button>
      <button class="page-btn" :disabled="currentPage === metaData.totalPages" @click="goPage(currentPage + 1)">
        <svg viewBox="0 0 12 12" fill="none">
          <path d="M4 2l4 4-4 4" stroke="currentColor" stroke-width="1.3" stroke-linecap="round" />
        </svg>
      </button>
      <span class="total-badge">共 {{ metaData.totalCount }} 条</span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import {
  getLogsByDeviceAndDateApi,
  getLogsByDeviceAndKeywordApi,
  getLogsByDeviceAndLevelApi,
  getLogsByDeviceAndTimeRangeApi,
  getLogsByDeviceDateAndKeywordApi,
  type DeviceLogListItemDto,
} from '../../api/deviceLog';
import type { PagedMetaData } from '../../api/employee';
import { getAllActiveDevicesApi, type DeviceSelectDto } from '../../api/device';

type QueryMode = 'level' | 'keyword' | 'date' | 'time-range' | 'date-keyword';

const PAGE_SIZE = 20;

const queryModes: Array<{ key: QueryMode; label: string }> = [
  { key: 'level', label: '按级别' },
  { key: 'keyword', label: '按关键字' },
  { key: 'date', label: '按日期' },
  { key: 'time-range', label: '按时间范围' },
  { key: 'date-keyword', label: '日期 + 关键字' },
];

const localDate = () => {
  const date = new Date();
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
};

const defaultStartTime = () => `${localDate()}T00:00`;
const defaultEndTime = () => `${localDate()}T23:59`;
const toUtcIso = (localTime: string) => (localTime ? new Date(localTime).toISOString() : '');

const currentMode = ref<QueryMode>('level');
const selectedDeviceId = ref('');
const loading = ref(false);
const searched = ref(false);
const currentPage = ref(1);
const expandedId = ref<string | null>(null);
const records = ref<DeviceLogListItemDto[]>([]);
const metaData = ref<PagedMetaData>({ totalCount: 0, pageSize: PAGE_SIZE, currentPage: 1, totalPages: 1 });

const allDevices = ref<DeviceSelectDto[]>([]);

const filters = reactive({
  level: '',
  keyword: '',
  date: localDate(),
  startTime: defaultStartTime(),
  endTime: defaultEndTime(),
});

const pageNumbers = computed(() => {
  const pages: number[] = [];
  for (let page = Math.max(1, currentPage.value - 2); page <= Math.min(metaData.value.totalPages, currentPage.value + 2); page += 1) {
    pages.push(page);
  }
  return pages;
});

const resetDateTime = () => {
  filters.date = localDate();
  filters.startTime = defaultStartTime();
  filters.endTime = defaultEndTime();
};

const switchMode = (mode: QueryMode) => {
  currentMode.value = mode;
  currentPage.value = 1;
  searched.value = false;
  records.value = [];
  expandedId.value = null;
  resetDateTime();
};

const onDeviceChange = () => {
  currentPage.value = 1;
  searched.value = false;
  records.value = [];
  expandedId.value = null;
};

const levelClass = (level: string) => {
  switch (level.toUpperCase()) {
    case 'INFO':
      return 'info';
    case 'WARN':
      return 'warn';
    case 'ERROR':
      return 'error';
    default:
      return 'info';
  }
};

const formatTime = (value?: string | null) => {
  if (!value) return '—';

  try {
    return new Date(value).toLocaleString('zh-CN', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    });
  } catch {
    return value;
  }
};

const fetchData = async () => {
  if (!selectedDeviceId.value) {
    alert('请先选择设备。');
    return;
  }

  loading.value = true;
  searched.value = true;

  try {
    const pagination = { PageNumber: currentPage.value, PageSize: PAGE_SIZE };
    const deviceId = selectedDeviceId.value;
    let response;

    switch (currentMode.value) {
      case 'level':
        response = await getLogsByDeviceAndLevelApi({
          pagination,
          deviceId,
          level: filters.level || undefined,
        });
        break;

      case 'keyword':
        if (!filters.keyword.trim()) {
          alert('请输入关键字。');
          return;
        }
        response = await getLogsByDeviceAndKeywordApi({
          pagination,
          deviceId,
          keyword: filters.keyword.trim(),
        });
        break;

      case 'date':
        if (!filters.date) {
          alert('请选择日期。');
          return;
        }
        response = await getLogsByDeviceAndDateApi({
          pagination,
          deviceId,
          date: filters.date,
        });
        break;

      case 'time-range':
        if (!filters.startTime || !filters.endTime) {
          alert('请选择完整时间范围。');
          return;
        }
        response = await getLogsByDeviceAndTimeRangeApi({
          pagination,
          deviceId,
          startTime: toUtcIso(filters.startTime),
          endTime: toUtcIso(filters.endTime),
        });
        break;

      case 'date-keyword':
        if (!filters.date || !filters.keyword.trim()) {
          alert('请选择日期并输入关键字。');
          return;
        }
        response = await getLogsByDeviceDateAndKeywordApi({
          pagination,
          deviceId,
          date: filters.date,
          keyword: filters.keyword.trim(),
        });
        break;
    }

    metaData.value = response.metaData;
    records.value = response.items;
  } catch {
    records.value = [];
  } finally {
    loading.value = false;
  }
};

const doSearch = async () => {
  currentPage.value = 1;
  await fetchData();
};

const goPage = async (page: number) => {
  currentPage.value = page;
  await fetchData();
};

onMounted(async () => {
  allDevices.value = await getAllActiveDevicesApi().catch(() => [] as DeviceSelectDto[]);
});
</script>

<style scoped>
.device-log-page {
  --accent: #57b49f;
  --accent-soft: rgba(87, 180, 159, 0.16);
  --surface: rgba(255, 255, 255, 0.04);
  --surface-strong: rgba(255, 255, 255, 0.06);
  --surface-hover: rgba(255, 255, 255, 0.08);
  --border: rgba(255, 255, 255, 0.09);
  --border-strong: rgba(255, 255, 255, 0.14);
  --text-main: #f0f3ef;
  --text-muted: rgba(240, 243, 239, 0.72);
  --text-subtle: rgba(240, 243, 239, 0.46);
  color: var(--text-main);
}

* { box-sizing: border-box; }
.page-header { display: flex; align-items: flex-start; justify-content: space-between; margin-bottom: 20px; }
.page-title { margin: 0 0 6px; font-size: 24px; font-weight: 600; color: var(--text-main); }
.page-sub { margin: 0; font-size: 13px; line-height: 1.6; color: var(--text-subtle); }

.device-select-bar {
  display: flex; gap: 12px; align-items: center; margin-bottom: 14px; padding: 14px 16px;
  border: 1px solid rgba(87, 180, 159, 0.22); border-radius: 6px; background: rgba(87, 180, 159, 0.06);
}
.device-select-bar label { flex: 0 0 auto; font-size: 13px; font-weight: 600; color: #c8f0e8; }
.device-select { max-width: 420px; flex: 1 1 320px; }

.query-modes { display: flex; flex-wrap: wrap; gap: 10px; margin-bottom: 16px; }
.mode-btn {
  min-height: 38px; padding: 0 14px; border: 1px solid var(--border); border-radius: 6px;
  background: rgba(255, 255, 255, 0.02); color: var(--text-muted); font: inherit; font-size: 13px; cursor: pointer;
  transition: border-color 0.18s ease, background-color 0.18s ease, color 0.18s ease;
}
.mode-btn:hover { border-color: var(--border-strong); background: var(--surface-hover); color: var(--text-main); }
.mode-btn.active { border-color: rgba(87, 180, 159, 0.34); background: var(--accent-soft); color: #c8f0e8; }

.filter-bar {
  display: flex; flex-wrap: wrap; gap: 12px; align-items: flex-end; margin-bottom: 18px; padding: 18px;
  border: 1px solid var(--border); border-radius: 6px; background: rgba(255, 255, 255, 0.025);
}
.filter-field { display: flex; min-width: 180px; flex: 1 1 220px; flex-direction: column; gap: 6px; }
.filter-field--wide { flex: 1.3 1 260px; }
.filter-field--date { max-width: 220px; min-width: 220px; flex: 0 1 220px; }
.filter-field--time { max-width: 228px; min-width: 228px; flex: 0 1 228px; }
.filter-field label { font-size: 12px; font-weight: 500; color: var(--text-subtle); }

.filter-input {
  min-height: 40px; padding: 0 12px; border: 1px solid var(--border); border-radius: 6px;
  background: var(--surface); color: var(--text-main); font: inherit; font-size: 13px; outline: none;
  transition: border-color 0.18s ease, background-color 0.18s ease;
}
.filter-input:focus { border-color: rgba(87, 180, 159, 0.42); background: var(--surface-strong); }
.filter-input::placeholder { color: var(--text-subtle); }
option { background: #1d231f; color: var(--text-main); }

.btn {
  display: inline-flex; align-items: center; justify-content: center; gap: 8px; min-height: 40px; padding: 0 16px;
  border: 1px solid transparent; border-radius: 6px; font: inherit; font-size: 13px; font-weight: 500;
  cursor: pointer; transition: transform 0.16s ease, background-color 0.16s ease;
}
.btn svg { width: 14px; height: 14px; }
.btn-primary { background: var(--accent); color: #102019; }
.btn-primary:hover { transform: translateY(-1px); background: #69c4af; }
.search-btn { min-width: 112px; flex: 0 0 auto; }

.table-wrap { overflow: auto; border: 1px solid var(--border); border-radius: 6px; background: rgba(255, 255, 255, 0.025); }
.data-table { width: 100%; min-width: 760px; border-collapse: collapse; }
.data-table thead tr { background: rgba(255, 255, 255, 0.04); }
.data-table th { padding: 12px 16px; text-align: left; font-size: 12px; font-weight: 600; color: var(--text-subtle); white-space: nowrap; }
.th-level { width: 82px; }
.th-time { width: 180px; }
.data-table td { padding: 13px 16px; border-top: 1px solid rgba(255, 255, 255, 0.05); font-size: 13px; color: var(--text-muted); vertical-align: top; }
.table-row { cursor: pointer; transition: background-color 0.16s ease; }
.table-row:hover { background: rgba(255, 255, 255, 0.04); }
.time-cell { color: var(--text-subtle); white-space: nowrap; }

.level-tag { display: inline-flex; min-width: 52px; justify-content: center; padding: 4px 8px; border-radius: 999px; font-size: 11px; font-weight: 600; }
.level-tag.info { background: rgba(98, 169, 255, 0.16); color: #b6d9ff; }
.level-tag.warn { background: rgba(224, 172, 84, 0.16); color: #ffd89c; }
.level-tag.error { background: rgba(227, 109, 90, 0.16); color: #ffb4a8; }

.msg-cell {
  overflow: hidden; max-height: 40px; color: var(--text-main); line-height: 1.55; text-overflow: ellipsis;
  word-break: break-word; display: -webkit-box; -webkit-box-orient: vertical; -webkit-line-clamp: 2; transition: max-height 0.2s ease;
}
.msg-cell.expanded { max-height: 420px; -webkit-line-clamp: unset; }

.skeleton-rows { padding: 8px 0; }
.skeleton-row { display: flex; gap: 16px; padding: 14px 16px; border-top: 1px solid rgba(255, 255, 255, 0.05); }
.skel { height: 14px; border-radius: 6px; background: rgba(255, 255, 255, 0.08); animation: shimmer 1.4s ease-in-out infinite; }
.skel-sm { width: 72px; }
.skel-md { width: 140px; }
.skel-lg { width: 280px; }
@keyframes shimmer { 0%, 100% { opacity: 0.45; } 50% { opacity: 1; } }

.empty-cell { padding: 56px 24px; text-align: center; }
.empty-state { display: flex; flex-direction: column; align-items: center; gap: 12px; }
.empty-state svg { width: 52px; height: 52px; color: rgba(255, 255, 255, 0.2); }
.empty-state p { margin: 0; font-size: 13px; color: var(--text-subtle); }

.pagination { display: flex; align-items: center; justify-content: center; gap: 8px; margin-top: 20px; }
.page-btn {
  display: inline-flex; width: 34px; height: 34px; align-items: center; justify-content: center;
  border: 1px solid var(--border); border-radius: 6px; background: rgba(255, 255, 255, 0.03);
  color: var(--text-muted); cursor: pointer; transition: border-color 0.16s ease, background-color 0.16s ease, color 0.16s ease;
}
.page-btn:hover:not(:disabled) { border-color: rgba(87, 180, 159, 0.28); color: #c8f0e8; }
.page-btn.active { border-color: rgba(87, 180, 159, 0.35); background: var(--accent-soft); color: #c8f0e8; }
.page-btn:disabled { opacity: 0.36; cursor: not-allowed; }
.page-btn svg { width: 12px; height: 12px; }
.total-badge { margin-left: 8px; font-size: 12px; color: var(--text-subtle); }

@media (max-width: 960px) {
  .filter-field--date, .filter-field--time { min-width: 180px; max-width: none; flex: 1 1 220px; }
  .search-btn { width: 100%; }
}

@media (max-width: 640px) {
  .page-title { font-size: 21px; }
  .page-sub { font-size: 12px; }
  .device-select-bar { align-items: stretch; flex-direction: column; }
  .filter-bar { padding: 14px; }
  .filter-field, .filter-field--wide, .filter-field--date, .filter-field--time { min-width: 100%; max-width: none; flex-basis: 100%; }
}
</style>
