import { Area, AreaChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { Card } from './Card';
import { ChartTooltip } from './ChartTooltip';
import { dates, fmt } from '../format';
import { useChartTheme } from '../useChartTheme';
import type { Granularity, TrendPoint } from '../types';

interface Props {
  points: TrendPoint[];
  granularity: Granularity;
  onGranularity: (g: Granularity) => void;
}

const labels: Record<Granularity, string> = { Day: 'Jour', Week: 'Semaine', Month: 'Mois' };

export function TrendChart({ points, granularity, onGranularity }: Props) {
  const t = useChartTheme();
  const tick = (iso: string) => (granularity === 'Month' ? dates.month(iso) : dates.short(iso));
  const periodLabel = (iso: string) =>
    granularity === 'Day' ? dates.long(iso) : granularity === 'Week' ? `Semaine du ${dates.long(iso)}` : dates.month(iso);

  return (
    <Card
      title="Évolution du chiffre d'affaires"
      subtitle={`Par ${labels[granularity].toLowerCase()}`}
      actions={
        <div className="segmented small" role="radiogroup" aria-label="Granularité">
          {(Object.keys(labels) as Granularity[]).map((g) => (
            <button key={g} type="button" role="radio" aria-checked={g === granularity} className={g === granularity ? 'selected' : ''} onClick={() => onGranularity(g)}>
              {labels[g]}
            </button>
          ))}
        </div>
      }
      chart={
        <div className="chart" style={{ height: 280 }}>
          <ResponsiveContainer>
            <AreaChart data={points} margin={{ top: 8, right: 16, bottom: 0, left: 8 }}>
              <CartesianGrid vertical={false} stroke={t.grid} />
              <XAxis dataKey="period" tickFormatter={tick} stroke={t.axis} tick={{ fill: t.muted, fontSize: 12 }} tickLine={false} minTickGap={24} />
              <YAxis tickFormatter={fmt.euroCompact} stroke={t.axis} tick={{ fill: t.muted, fontSize: 12 }} tickLine={false} axisLine={false} width={64} />
              <Tooltip
                cursor={{ stroke: t.axis, strokeWidth: 1 }}
                content={({ active, payload }) => {
                  const p = active && payload?.[0]?.payload as TrendPoint | undefined;
                  return p ? (
                    <ChartTooltip
                      title={periodLabel(p.period)}
                      rows={[
                        { label: "Chiffre d'affaires", value: fmt.euro(p.revenue), color: t.series[0] },
                        { label: 'Tickets', value: fmt.int(p.transactions) },
                        { label: 'Panier moyen', value: fmt.euroCents(p.averageBasket) },
                      ]}
                    />
                  ) : null;
                }}
              />
              <Area
                type="monotone"
                dataKey="revenue"
                stroke={t.series[0]}
                strokeWidth={2}
                fill={t.series[0]}
                fillOpacity={0.1}
                activeDot={{ r: 4, fill: t.series[0], stroke: t.surface, strokeWidth: 2 }}
                isAnimationActive={false}
              />
            </AreaChart>
          </ResponsiveContainer>
        </div>
      }
      table={
        <table className="data">
          <thead>
            <tr>
              <th>Période</th>
              <th className="num">CA</th>
              <th className="num">Tickets</th>
              <th className="num">Panier moyen</th>
            </tr>
          </thead>
          <tbody>
            {points.map((p) => (
              <tr key={p.period}>
                <td>{periodLabel(p.period)}</td>
                <td className="num">{fmt.euro(p.revenue)}</td>
                <td className="num">{fmt.int(p.transactions)}</td>
                <td className="num">{fmt.euroCents(p.averageBasket)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      }
    />
  );
}
