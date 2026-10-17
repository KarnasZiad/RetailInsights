import { useEffect, useState } from 'react';

/** Chart colours, read from the CSS custom properties so that light and dark mode stay in one place (styles.css). */
export interface ChartTheme {
  surface: string;
  textPrimary: string;
  textSecondary: string;
  muted: string;
  grid: string;
  axis: string;
  hover: string;
  series: [string, string, string];
}

const read = (): ChartTheme => {
  const style = getComputedStyle(document.documentElement);
  const v = (name: string) => style.getPropertyValue(name).trim();
  return {
    surface: v('--surface-1'),
    textPrimary: v('--text-primary'),
    textSecondary: v('--text-secondary'),
    muted: v('--text-muted'),
    grid: v('--gridline'),
    axis: v('--baseline'),
    hover: v('--hover-wash'),
    series: [v('--series-1'), v('--series-2'), v('--series-3')],
  };
};

export function useChartTheme(): ChartTheme {
  const [theme, setTheme] = useState(read);

  useEffect(() => {
    const media = window.matchMedia('(prefers-color-scheme: dark)');
    const update = () => setTheme(read());
    media.addEventListener('change', update);
    const observer = new MutationObserver(update);
    observer.observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] });
    return () => {
      media.removeEventListener('change', update);
      observer.disconnect();
    };
  }, []);

  return theme;
}
