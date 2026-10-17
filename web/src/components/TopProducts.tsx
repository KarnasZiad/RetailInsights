import { Bar, BarChart, CartesianGrid, LabelList, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { Card } from './Card';
import { ChartTooltip } from './ChartTooltip';
import { fmt } from '../format';
import { useChartTheme } from '../useChartTheme';
import type { ProductPerformance, Ranking } from '../types';

interface Props {
  products: ProductPerformance[];
  ranking: Ranking;
  onRanking: (r: Ranking) => void;
}

export function TopProducts({ products, ranking, onRanking }: Props) {
  const t = useChartTheme();
  const key = ranking === 'Revenue' ? 'revenue' : 'units';
  const format = ranking === 'Revenue' ? fmt.euroCompact : fmt.int;

  return (
    <Card
      title="Top 10 produits"
      subtitle={ranking === 'Revenue' ? "Par chiffre d'affaires" : 'Par unités vendues'}
      actions={
        <div className="segmented small" role="radiogroup" aria-label="Classement">
          {(['Revenue', 'Units'] as Ranking[]).map((r) => (
            <button key={r} type="button" role="radio" aria-checked={r === ranking} className={r === ranking ? 'selected' : ''} onClick={() => onRanking(r)}>
              {r === 'Revenue' ? 'CA' : 'Unités'}
            </button>
          ))}
        </div>
      }
      chart={
        <div className="chart" style={{ height: Math.max(180, products.length * 32 + 30) }}>
          <ResponsiveContainer>
            <BarChart data={products} layout="vertical" margin={{ top: 0, right: 56, bottom: 0, left: 0 }} barCategoryGap={6}>
              <CartesianGrid horizontal={false} stroke={t.grid} />
              <XAxis type="number" tickFormatter={format} stroke={t.axis} tick={{ fill: t.muted, fontSize: 12 }} tickLine={false} />
              <YAxis type="category" dataKey="name" width={160} stroke={t.axis} tick={{ fill: t.textSecondary, fontSize: 12 }} tickLine={false} />
              <Tooltip
                cursor={{ fill: t.hover }}
                content={({ active, payload }) => {
                  const p = active && payload?.[0]?.payload as ProductPerformance | undefined;
                  return p ? (
                    <ChartTooltip
                      title={`${p.rank}. ${p.name} · ${p.sku}`}
                      rows={[
                        { label: "Chiffre d'affaires", value: fmt.euro(p.revenue), color: ranking === 'Revenue' ? t.series[0] : undefined },
                        { label: 'Unités', value: fmt.int(p.units), color: ranking === 'Units' ? t.series[0] : undefined },
                        { label: 'Part du CA', value: fmt.pct(p.shareOfRevenue) },
                        { label: 'Catégorie', value: p.category },
                      ]}
                    />
                  ) : null;
                }}
              />
              <Bar dataKey={key} fill={t.series[0]} radius={[0, 4, 4, 0]} barSize={16} isAnimationActive={false}>
                <LabelList dataKey={key} position="right" formatter={format} fill={t.textSecondary} fontSize={12} />
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
              <th>Produit</th>
              <th>Catégorie</th>
              <th className="num">Unités</th>
              <th className="num">CA</th>
              <th className="num">Part</th>
            </tr>
          </thead>
          <tbody>
            {products.map((p) => (
              <tr key={p.sku}>
                <td>{p.rank}</td>
                <td>{p.name}</td>
                <td>{p.category}</td>
                <td className="num">{fmt.int(p.units)}</td>
                <td className="num">{fmt.euro(p.revenue)}</td>
                <td className="num">{fmt.pct(p.shareOfRevenue)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      }
    />
  );
}
