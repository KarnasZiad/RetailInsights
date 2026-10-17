import { useCallback, useEffect, useState } from 'react';
import { api } from './api';
import { dates } from './format';
import { FilterBar } from './components/FilterBar';
import { StatTiles } from './components/StatTiles';
import { TrendChart } from './components/TrendChart';
import { StoresChart } from './components/StoresChart';
import { TopProducts } from './components/TopProducts';
import { ChannelSplit } from './components/ChannelSplit';
import type { ChannelPerformance, Filters, Granularity, ProductPerformance, Ranking, StoreInfo, StorePerformance, Summary, TrendPoint } from './types';

interface Data {
  summary: Summary;
  stores: StorePerformance[];
  trend: TrendPoint[];
  products: ProductPerformance[];
  channels: ChannelPerformance[];
}

export default function App() {
  const [storeList, setStoreList] = useState<StoreInfo[]>([]);
  const [lastDay, setLastDay] = useState<string | null>(null);
  const [filters, setFilters] = useState<Filters | null>(null);
  const [granularity, setGranularity] = useState<Granularity>('Day');
  const [ranking, setRanking] = useState<Ranking>('Revenue');
  const [data, setData] = useState<Data | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    Promise.all([api.stores(), api.dataRange()])
      .then(([stores, range]) => {
        setStoreList(stores);
        const end = range.lastDay ?? new Date().toISOString().slice(0, 10);
        setLastDay(end);
        setFilters({ from: dates.addDays(end, -29), to: end, storeId: null });
      })
      .catch((e: Error) => setError(`API injoignable : ${e.message}`));
  }, []);

  const load = useCallback(async (f: Filters, g: Granularity, r: Ranking) => {
    setLoading(true);
    try {
      const [summary, stores, trend, products, channels] = await Promise.all([
        api.summary(f),
        api.storeRanking(f),
        api.trend(f, g),
        api.topProducts(f, r),
        api.channels(f),
      ]);
      setData({ summary, stores, trend, products, channels });
      setError(null);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (filters) void load(filters, granularity, ranking);
  }, [filters, granularity, ranking, load]);

  // Long periods default to coarser buckets, like the API does.
  const changeFilters = (next: Filters) => {
    const days = (new Date(next.to).getTime() - new Date(next.from).getTime()) / 86_400_000 + 1;
    setGranularity(days <= 62 ? 'Day' : days <= 186 ? 'Week' : 'Month');
    setFilters(next);
  };

  return (
    <div className="app">
      <header className="topbar">
        <div className="brand">
          <span className="logo" aria-hidden>
            <svg viewBox="0 0 24 24" width="20" height="20">
              <path d="M5 19v-6M10 19V7M15 19v-4M20 19V10" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" fill="none" />
            </svg>
          </span>
          <div>
            <h1>RetailInsights</h1>
            <p>Pilotage de la performance des ventes</p>
          </div>
        </div>
      </header>

      <main>
        {filters && lastDay && <FilterBar filters={filters} stores={storeList} lastDay={lastDay} onChange={changeFilters} />}
        {error && <div className="banner">{error}</div>}

        {data && filters && (
          <div className={loading ? 'content refreshing' : 'content'}>
            <StatTiles summary={data.summary} />
            <TrendChart points={data.trend} granularity={granularity} onGranularity={setGranularity} />
            <div className="grid-2">
              <StoresChart stores={data.stores} highlight={filters.storeId} />
              <TopProducts products={data.products} ranking={ranking} onRanking={setRanking} />
            </div>
            <ChannelSplit channels={data.channels} />
          </div>
        )}
      </main>
    </div>
  );
}
