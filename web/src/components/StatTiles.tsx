import { dates, fmt } from '../format';
import type { Summary } from '../types';

interface Props {
  summary: Summary;
}

function Delta({ value }: { value: number | null }) {
  if (value === null) return <span className="delta neutral">pas de comparaison</span>;
  const up = value > 0;
  const flat = value === 0;
  return (
    <span className={`delta ${flat ? 'neutral' : up ? 'good' : 'bad'}`}>
      <span aria-hidden>{flat ? '■' : up ? '▲' : '▼'}</span> {fmt.growth(value)}
    </span>
  );
}

export function StatTiles({ summary }: Props) {
  const c = summary.current;
  const g = summary.growth;
  const vs = `vs ${dates.short(summary.previousFrom)} – ${dates.short(summary.previousTo)}`;

  return (
    <section className="tiles" aria-label="Indicateurs clés">
      <div className="tile hero">
        <span className="tile-label">Chiffre d'affaires TTC</span>
        <strong className="tile-value">{fmt.euro(c.revenue)}</strong>
        <span className="tile-foot">
          <Delta value={g.revenue} /> {vs}
        </span>
      </div>
      <div className="tile">
        <span className="tile-label">Tickets</span>
        <strong className="tile-value">{fmt.int(c.transactions)}</strong>
        <span className="tile-foot">
          <Delta value={g.transactions} />
        </span>
      </div>
      <div className="tile">
        <span className="tile-label">Panier moyen</span>
        <strong className="tile-value">{fmt.euroCents(c.averageBasket)}</strong>
        <span className="tile-foot">
          <Delta value={g.averageBasket} />
        </span>
      </div>
      <div className="tile">
        <span className="tile-label">Indice de vente</span>
        <strong className="tile-value">{fmt.dec(c.unitsPerTransaction)}</strong>
        <span className="tile-foot">
          <Delta value={g.unitsPerTransaction} /> articles / ticket
        </span>
      </div>
      <div className="tile">
        <span className="tile-label">Prix moyen de vente</span>
        <strong className="tile-value">{fmt.euroCents(c.averageSellingPrice)}</strong>
        <span className="tile-foot">
          <Delta value={g.averageSellingPrice} />
        </span>
      </div>
    </section>
  );
}
