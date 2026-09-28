<script setup lang="ts">
import { ref, reactive } from 'vue'
import { Message } from '@arco-design/web-vue'
import apiClient from '../api'

const baseAssets = ['BTC', 'ETH', 'SOL', 'BNB', 'DOGE', 'XRP', 'ADA', 'DOT']
const quoteAssets = ['USDT', 'BTC', 'ETH', 'BUSD']
const categories = ['spot', 'future']
const timeframes = [
  { value: '1m', label: '1 分钟' },
  { value: '5m', label: '5 分钟' },
  { value: '15m', label: '15 分钟' },
  { value: '30m', label: '30 分钟' },
  { value: '1h', label: '1 小时' },
  { value: '4h', label: '4 小时' },
  { value: '1d', label: '1 天' },
]

const form = reactive({
  baseAsset: 'BTC',
  quoteAsset: 'USDT',
  category: 'spot',
  timeframe: '30m',
  startMonth: '2024-01',
  endMonth: '2024-06',
})

const pulling = ref(false)

const result = ref<{
  downloadedFiles: number
  totalKlines: number
  filePath: string
  errors: string[]
} | null>(null)

function prevMonth(n: number) {
  return `2024-${String(n).padStart(2, '0')}`
}

function quickPick(cat: string, tf: string, months: string) {
  form.category = cat
  form.timeframe = tf
  const [s, e] = months.split('~')
  form.startMonth = prevMonth(Number(s))
  form.endMonth = prevMonth(Number(e))
}

const presets = [
  { label: 'BTC 30m 半年', onClick: () => quickPick('spot', '30m', '1~6') },
  { label: 'BTC 5m 3个月', onClick: () => quickPick('spot', '5m', '4~6') },
  { label: 'ETH 30m 半年', onClick: () => quickPick('spot', '30m', '1~6') },
  { label: 'ETH 5m 3个月', onClick: () => quickPick('spot', '5m', '4~6') },
]

async function pullData() {
  pulling.value = true
  result.value = null
  try {
    const res = await apiClient.post('/data/pull', {
      category: form.category,
      baseAsset: form.baseAsset,
      quoteAsset: form.quoteAsset,
      timeframe: form.timeframe,
      startMonth: form.startMonth,
      endMonth: form.endMonth,
    })
    if (res.data.code === 0) {
      result.value = res.data.data
      Message.success(`拉取完成: ${res.data.data.totalKlines} 条 K 线`)
    } else {
      Message.error(res.data.message || '拉取失败')
    }
  } catch (e: any) {
    Message.error(e?.response?.data?.message || '请求失败')
  } finally {
    pulling.value = false
  }
}

function fileLabel(path: string | null): string {
  if (!path) return '-'
  const parts = path.split('/')
  return parts.slice(-3).join(' / ').replace(/\/+$/, '')
}
</script>

<template>
  <div class="data-page">
    <a-row :gutter="16">
      <a-col :span="16">
        <a-card title="拉取配置" :bordered="true">
          <a-form :model="form" layout="vertical">
            <a-row :gutter="12">
              <a-col :span="8">
                <a-form-item field="baseAsset" label="基础币">
                  <a-select v-model="form.baseAsset" allow-create>
                    <a-option v-for="b in baseAssets" :key="b" :value="b">{{ b }}</a-option>
                  </a-select>
                </a-form-item>
              </a-col>
              <a-col :span="8">
                <a-form-item field="quoteAsset" label="报价币">
                  <a-select v-model="form.quoteAsset" allow-create>
                    <a-option v-for="q in quoteAssets" :key="q" :value="q">{{ q }}</a-option>
                  </a-select>
                </a-form-item>
              </a-col>
              <a-col :span="8">
                <a-form-item field="category" label="交易类型">
                  <a-select v-model="form.category">
                    <a-option v-for="c in categories" :key="c" :value="c">{{ c }}</a-option>
                  </a-select>
                </a-form-item>
              </a-col>
            </a-row>

            <a-row :gutter="12">
              <a-col :span="8">
                <a-form-item field="timeframe" label="周期">
                  <a-select v-model="form.timeframe">
                    <a-option v-for="tf in timeframes" :key="tf.value" :value="tf.value">{{ tf.label }}</a-option>
                  </a-select>
                </a-form-item>
              </a-col>
              <a-col :span="8">
                <a-form-item field="startMonth" label="起始月份">
                  <a-month-picker v-model="form.startMonth" placeholder="2024-01" format="YYYY-MM" />
                </a-form-item>
              </a-col>
              <a-col :span="8">
                <a-form-item field="endMonth" label="结束月份">
                  <a-month-picker v-model="form.endMonth" placeholder="2024-06" format="YYYY-MM" />
                </a-form-item>
              </a-col>
            </a-row>

            <a-space>
              <a-button type="primary" :loading="pulling" @click="pullData">
                {{ pulling ? '拉取中...' : '⬇ 拉取数据' }}
              </a-button>
              <a-dropdown>
                <a-button>快捷选择</a-button>
                <template #content>
                  <a-doption v-for="p in presets" :key="p.label" @click="p.onClick()">{{ p.label }}</a-doption>
                </template>
              </a-dropdown>
            </a-space>
          </a-form>
        </a-card>
      </a-col>

      <a-col :span="8">
        <a-card title="拉取结果" :bordered="true">
          <template v-if="result">
            <a-descriptions :column="1" size="small">
              <a-descriptions-item label="下载文件数">
                <a-tag color="blue">{{ result.downloadedFiles }}</a-tag>
              </a-descriptions-item>
              <a-descriptions-item label="K 线总数">
                <a-tag color="green">{{ result.totalKlines }}</a-tag>
              </a-descriptions-item>
              <a-descriptions-item label="保存路径">
                <span class="path-text">{{ fileLabel(result.filePath) }}</span>
              </a-descriptions-item>
              <a-descriptions-item v-if="result.errors?.length" label="错误">
                <div v-for="e in result.errors" :key="e" class="error-msg">{{ e }}</div>
              </a-descriptions-item>
              <a-descriptions-item v-else label="状态">
                <a-tag color="green">成功</a-tag>
              </a-descriptions-item>
            </a-descriptions>
          </template>
          <template v-else>
            <div class="empty-state">未拉取数据</div>
          </template>
        </a-card>
      </a-col>
    </a-row>
  </div>
</template>

<style scoped>
.data-page {
  width: 100%;
}
.path-text {
  font-family: monospace;
  font-size: 12px;
  word-break: break-all;
}
.empty-state {
  color: var(--color-text-3);
  text-align: center;
  padding: 24px;
}
.error-msg {
  color: var(--color-danger-6);
  font-size: 12px;
}
</style>
