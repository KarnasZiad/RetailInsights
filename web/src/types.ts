export type Granularity = 'Day' | 'Week' | 'Month';
export type Ranking = 'Revenue' | 'Units';
export type Channel = 'InStore' | 'ClickAndCollect' | 'Online';

export interface StoreInfo {
  id: number;
  code: string;
  name: string;
  city: string;
}

export interface Figures {
  revenue: number;
  transactions: number;
  units: number;
  averageBasket: number;
  unitsPerTransaction: number;
  averageSellingPrice: number;
}

export interface Summary {
  from: string;
  to: string;
  previousFrom: string;
  previousTo: string;
  current: Figures;
  previous: Figures;
  growth: {
    revenue: number | null;
    transactions: number | null;
    averageBasket: number | null;
    unitsPerTransaction: number | null;
    averageSellingPrice: number | null;
  };
}

export interface StorePerformance {
  storeId: number;
  code: string;
  name: string;
  city: string;
  revenue: number;
  transactions: number;
  averageBasket: number;
  unitsPerTransaction: number;
  shareOfRevenue: number;
  revenueGrowth: number | null;
  rank: number;
}

export interface TrendPoint {
  period: string;
  revenue: number;
  transactions: number;
  averageBasket: number;
}

export interface ProductPerformance {
  rank: number;
  sku: string;
  name: string;
  category: string;
  units: number;
  revenue: number;
  shareOfRevenue: number;
}

export interface ChannelPerformance {
  channel: Channel;
  revenue: number;
  transactions: number;
  averageBasket: number;
  shareOfRevenue: number;
}

export interface DataRange {
  firstDay: string | null;
  lastDay: string | null;
}

export interface Filters {
  from: string;
  to: string;
  storeId: number | null;
}
