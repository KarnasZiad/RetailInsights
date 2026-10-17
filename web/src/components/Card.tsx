import { useState, type ReactNode } from 'react';

interface Props {
  title: string;
  subtitle?: string;
  actions?: ReactNode;
  chart: ReactNode;
  table: ReactNode;
}

/** Every chart has a table view: values never depend on hovering or on colour. */
export function Card({ title, subtitle, actions, chart, table }: Props) {
  const [showTable, setShowTable] = useState(false);

  return (
    <section className="card">
      <div className="card-header">
        <div>
          <h2>{title}</h2>
          {subtitle && <p className="subtitle">{subtitle}</p>}
        </div>
        <div className="card-actions">
          {actions}
          <button type="button" className="ghost" aria-pressed={showTable} onClick={() => setShowTable((v) => !v)}>
            {showTable ? 'Graphique' : 'Tableau'}
          </button>
        </div>
      </div>
      {showTable ? <div className="table-scroll">{table}</div> : chart}
    </section>
  );
}
