import { Bar, BarChart, CartesianGrid, Cell, LabelList, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { Card } from './Card';
import { ChartTooltip } from './ChartTooltip';
import { fmt } from '../format';
import { useChartTheme } from '../useChartTheme';
import type { StorePerformance } from '../types';

interface Props {
  stores: StorePerformance[];
  highlight: number | null;
}

export function StoresChart({ stores, highlight }: Props) {
  const t = useChartTheme();

  return (
    <Card
      title="Chiffre d'affaires par magasin"
      subtitle="Classement du réseau sur la période"
      chart={
        <div className="chart" style={{ height: Math.max(180, stores.length * 40 + 30) }}>
          <ResponsiveContainer>
            <BarChart data={stores} layout="vertical" margin={{ top: 0, right: 64, bottom: 0, left: 0 }} barCategoryGap={8}>
              <CartesianGrid horizontal={false} stroke={t.grid} />
              <XAxis type="number" tickFormatter={fmt.euroCompact} stroke={t.axis} tick={{ fill: t.muted, fontSize: 12 }} tickLine={false} />
              <YAxis type="category" dataKey="name" width={150} stroke={t.axis} tick={{ fill: t.textSecondary, fontSize: 12 }} tickLine={false} />
              <Tooltip
                cursor={{ fill: t.hover }}
                content={({ active, payload }) => {
                  const s = active && payload?.[0]?.payload as StorePerformance | undefined;
                  return s ? (
                    <ChartTooltip
                      title={`${s.rank}. ${s.name}`}
                      rows={[
                        { label: "Chiffre d'affaires", value: fmt.euro(s.revenue), color: t.series[0] },
                        { label: 'Part du réseau', value: fmt.pct(s.shareOfRevenue) },
                        { label: 'Tickets', value: fmt.int(s.transactions) },
                        { label: 'Panier moyen', value: fmt.euroCents(s.averageBasket) },
                        { label: 'vs période précédente', value: s.revenueGrowth === null ? '–' : fmt.growth(s.revenueGrowth) },
                      ]}
                    />
                  ) : null;
                }}
              />
              <Bar dataKey="revenue" fill={t.series[0]} radius={[0, 4, 4, 0]} barSize={20} isAnimationActive={false}>
                {stores.map((s) => (
                  // A selected store stays at full strength, the others recede.
                  <Cell key={s.storeId} fill={t.series[0]} fillOpacity={highlight !== null && s.storeId !== highlight ? 0.35 : 1} />
                ))}
                <LabelList dataKey="revenue" position="right" formatter={fmt.euroCompact} fill={t.textSecondary} fontSize={12} />
              </Bar>
            </BarChart>
          </ResponsiveContainer>
        </div>
      }
      table={
        <table className="data">
          <thead>
            <tr>
              <th>#</th>
              <th>Magasin</th>
              <th className="num">CA</th>
              <th className="num">Part</th>
              <th className="num">Tickets</th>
              <th className="num">Panier moyen</th>
              <th className="num">Évolution</th>
            </tr>
          </thead>
          <tbody>
            {stores.map((s) => (
              <tr key={s.storeId}>
                <td>{s.rank}</td>
                <td>{s.name}</td>
                <td className="num">{fmt.euro(s.revenue)}</td>
                <td className="num">{fmt.pct(s.shareOfRevenue)}</td>
                <td className="num">{fmt.int(s.transactions)}</td>
                <td className="num">{fmt.euroCents(s.averageBasket)}</td>
                <td className="num">{s.revenueGrowth === null ? '–' : fmt.growth(s.revenueGrowth)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      }
    />
  );
}
