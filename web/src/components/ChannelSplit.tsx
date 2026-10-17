import { useState } from 'react';
import { Card } from './Card';
import { channelLabels, fmt } from '../format';
import { useChartTheme } from '../useChartTheme';
import type { ChannelPerformance } from '../types';

interface Props {
  channels: ChannelPerformance[];
}

/**
 * Part-to-whole of three channels: one 100% bar with a 2px surface gap between segments,
 * a legend that carries the values (direct labels), and a per-segment tooltip.
 */
export function ChannelSplit({ channels }: Props) {
  const t = useChartTheme();
  const [hovered, setHovered] = useState<string | null>(null);
  const visible = channels.filter((c) => c.revenue > 0);
  const colorOf = (channel: string) => t.series[channels.findIndex((c) => c.channel === channel)] ?? t.series[0];

  return (
    <Card
      title="Répartition du chiffre d'affaires par canal"
      subtitle="Ventes en magasin, click & collect et livraison"
      chart={
        <div className="channels">
          <div className="stack" role="img" aria-label={visible.map((c) => `${channelLabels[c.channel]} ${fmt.pct(c.shareOfRevenue)}`).join(', ')}>
            {visible.map((c) => (
              <div
                key={c.channel}
                className={`segment ${hovered && hovered !== c.channel ? 'dimmed' : ''}`}
                style={{ flexGrow: c.shareOfRevenue, background: colorOf(c.channel) }}
                tabIndex={0}
                onPointerEnter={() => setHovered(c.channel)}
                onPointerLeave={() => setHovered(null)}
                onFocus={() => setHovered(c.channel)}
                onBlur={() => setHovered(null)}
              >
                {hovered === c.channel && (
                  <div className="tooltip floating">
                    <div className="tooltip-title">{channelLabels[c.channel]}</div>
                    <div className="tooltip-row">
                      <span className="line-key" style={{ background: colorOf(c.channel) }} />
                      <strong>{fmt.euro(c.revenue)}</strong>
                      <span>{fmt.pct(c.shareOfRevenue)} du CA</span>
                    </div>
                    <div className="tooltip-row">
                      <span className="line-key empty" />
                      <strong>{fmt.int(c.transactions)}</strong>
                      <span>tickets · panier {fmt.euroCents(c.averageBasket)}</span>
                    </div>
                  </div>
                )}
              </div>
            ))}
          </div>
          <ul className="legend">
            {channels.map((c) => (
              <li key={c.channel}>
                <span className="swatch" style={{ background: colorOf(c.channel) }} />
                <span className="legend-label">{channelLabels[c.channel]}</span>
                <strong>{fmt.pct(c.shareOfRevenue)}</strong>
                <span className="legend-value">{fmt.euro(c.revenue)}</span>
              </li>
            ))}
          </ul>
        </div>
      }
      table={
        <table className="data">
          <thead>
            <tr>
              <th>Canal</th>
              <th className="num">CA</th>
              <th className="num">Part</th>
              <th className="num">Tickets</th>
              <th className="num">Panier moyen</th>
            </tr>
          </thead>
          <tbody>
            {channels.map((c) => (
              <tr key={c.channel}>
                <td>{channelLabels[c.channel]}</td>
                <td className="num">{fmt.euro(c.revenue)}</td>
                <td className="num">{fmt.pct(c.shareOfRevenue)}</td>
                <td className="num">{fmt.int(c.transactions)}</td>
                <td className="num">{fmt.euroCents(c.averageBasket)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      }
    />
  );
}
