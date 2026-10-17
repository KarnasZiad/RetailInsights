import type { ChannelPerformance, DataRange, Filters, Granularity, ProductPerformance, Ranking, StoreInfo, StorePerformance, Summary, TrendPoint } from './types';

const BASE = import.meta.env.VITE_API_URL ?? '';

async function get<T>(path: string): Promise<T> {
  const response = await fetch(`${BASE}${path}`);
  if (!response.ok) {
    let message = `${response.status} ${response.statusText}`;
    try {
      const problem = await response.json();
      message = problem.detail ?? problem.title ?? message;
    } catch {
      // not JSON
    }
    throw new Error(message);
  }
  return (await response.json()) as T;
}

const query = (f: Filters, extra: Record<string, string> = {}) => {
  const params = new URLSearchParams({ from: f.from, to: f.to, ...extra });
  if (f.storeId !== null) params.set('storeId', String(f.storeId));
  return params.toString();
};

export const api = {
  stores: () => get<StoreInfo[]>('/api/stores'),
  dataRange: () => get<DataRange>('/api/data-range'),
  summary: (f: Filters) => get<Summary>(`/api/kpis/summary?${query(f)}`),
  storeRanking: (f: Filters) => get<StorePerformance[]>(`/api/kpis/stores?${query({ ...f, storeId: null })}`),
  trend: (f: Filters, granularity: Granularity) => get<TrendPoint[]>(`/api/kpis/trend?${query(f, { granularity })}`),
  topProducts: (f: Filters, by: Ranking) => get<ProductPerformance[]>(`/api/kpis/top-products?${query(f, { by, top: '10' })}`),
  channels: (f: Filters) => get<ChannelPerformance[]>(`/api/kpis/channels?${query(f)}`),
};
