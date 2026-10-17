interface Row {
  label: string;
  value: string;
  color?: string;
}

/** Value first (strong), label second, keyed by a short stroke of the series colour. */
export function ChartTooltip({ title, rows }: { title: string; rows: Row[] }) {
  return (
    <div className="tooltip">
      <div className="tooltip-title">{title}</div>
      {rows.map((r) => (
        <div key={r.label} className="tooltip-row">
          {r.color ? <span className="line-key" style={{ background: r.color }} /> : <span className="line-key empty" />}
          <strong>{r.value}</strong>
          <span>{r.label}</span>
        </div>
      ))}
    </div>
  );
}
