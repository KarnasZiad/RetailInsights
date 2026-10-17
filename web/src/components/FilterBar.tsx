import { dates } from '../format';
import type { Filters, StoreInfo } from '../types';

interface Props {
  filters: Filters;
  stores: StoreInfo[];
  lastDay: string;
  onChange: (filters: Filters) => void;
}

const presets = [7, 30, 90, 180];

/** One row above the charts: the period first, then the store. Every figure below uses the same slice. */
export function FilterBar({ filters, stores, lastDay, onChange }: Props) {
  const active = filters.to === lastDay ? presets.find((d) => dates.addDays(lastDay, -(d - 1)) === filters.from) : undefined;

  return (
    <div className="filters" role="group" aria-label="Filtres">
      <div className="segmented" role="radiogroup" aria-label="Période">
        {presets.map((d) => (
          <button
            key={d}
            type="button"
            role="radio"
            aria-checked={active === d}
            className={active === d ? 'selected' : ''}
            onClick={() => onChange({ ...filters, from: dates.addDays(lastDay, -(d - 1)), to: lastDay })}
          >
            {d} j
          </button>
        ))}
      </div>
      <label className="date">
        Du
        <input type="date" value={filters.from} max={filters.to} onChange={(e) => e.target.value && onChange({ ...filters, from: e.target.value })} />
      </label>
      <label className="date">
        au
        <input type="date" value={filters.to} min={filters.from} max={lastDay} onChange={(e) => e.target.value && onChange({ ...filters, to: e.target.value })} />
      </label>
      <select
        aria-label="Magasin"
        value={filters.storeId ?? ''}
        onChange={(e) => onChange({ ...filters, storeId: e.target.value === '' ? null : Number(e.target.value) })}
      >
        <option value="">Tous les magasins</option>
        {stores.map((s) => (
          <option key={s.id} value={s.id}>
            {s.name}
          </option>
        ))}
      </select>
    </div>
  );
}
