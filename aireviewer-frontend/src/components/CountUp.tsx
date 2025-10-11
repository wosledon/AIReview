import React, { useEffect, useRef, useState } from 'react';

interface CountUpProps {
  to: number;
  duration?: number; // ms
  className?: string;
}

export const CountUp: React.FC<CountUpProps> = ({ to, duration = 900, className }) => {
  const [value, setValue] = useState(0);
  const start = useRef<number | null>(null);
  const raf = useRef<number | null>(null);
  const lastTo = useRef<number>(to);

  useEffect(() => {
    // restart when target changes
    if (lastTo.current !== to) {
      lastTo.current = to;
      start.current = null;
    }
    const animate = (ts: number) => {
      if (!start.current) start.current = ts;
      const progress = Math.min(1, (ts - start.current) / duration);
      setValue(Math.round(progress * to));
      if (progress < 1) {
        raf.current = requestAnimationFrame(animate);
      }
    };
    raf.current = requestAnimationFrame(animate);
    return () => {
      if (raf.current) cancelAnimationFrame(raf.current);
    };
  }, [to, duration]);

  return <span className={className}>{value.toLocaleString()}</span>;
};

export default CountUp;
