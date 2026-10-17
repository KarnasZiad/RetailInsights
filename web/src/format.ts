const euro0 = new Intl.NumberFormat('fr-FR', { style: 'currency', currency: 'EUR', maximumFractionDigits: 0 });
const euro2 = new Intl.NumberFormat('fr-FR', { style: 'currency', currency: 'EUR', minimumFractionDigits: 2, maximumFractionDigits: 2 });
const euroCompact = new Intl.NumberFormat('fr-FR', { style: 'currency', currency: 'EUR', notation: 'compact', maximumFractionDigits: 1 });
const integer = new Intl.NumberFormat('fr-FR');
const decimal = new Intl.NumberFormat('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const percent = new Intl.NumberFormat('fr-FR', { minimumFractionDigits: 1, maximumFractionDigits: 1 });

export const fmt = {
  euro: (v: number) => euro0.format(v),
  euroCents: (v: number) => euro2.format(v),
  euroCompact: (v: number) => euroCompact.format(v),
  int: (v: number) => integer.format(v),
  dec: (v: number) => decimal.format(v),
  pct: (v: number) => `${percent.format(v)} %`,
  growth: (v: number) => `${v > 0 ? '+' : ''}${percent.format(v)} %`,
};

const dayFormat = new Intl.DateTimeFormat('fr-FR', { day: '2-digit', month: '2-digit' });
const longFormat = new Intl.DateTimeFormat('fr-FR', { day: 'numeric', month: 'long', year: 'numeric' });
const monthFormat = new Intl.DateTimeFormat('fr-FR', { month: 'short', year: '2-digit' });

const parse = (iso: string) => new Date(`${iso}T00:00:00`);

export const dates = {
  short: (iso: string) => dayFormat.format(parse(iso)),
  long: (iso: string) => longFormat.format(parse(iso)),
  month: (iso: string) => monthFormat.format(parse(iso)),
  addDays: (iso: string, days: number) => {
    const d = parse(iso);
    d.setDate(d.getDate() + days);
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
  },
};

export const channelLabels: Record<string, string> = {
  InStore: 'En magasin',
  ClickAndCollect: 'Click & collect',
  Online: 'Livraison',
};
