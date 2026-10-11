import React from 'react';
import { Star } from 'lucide-react';

export const Rating = ({ value = 4.8, count = 24, size = 'sm', showCount = true }) => {
  const starSize = size === 'lg' ? 'w-5 h-5' : size === 'md' ? 'w-4 h-4' : 'w-3.5 h-3.5';

  return (
    <div className="inline-flex items-center gap-1.5">
      <div className="flex items-center text-amber-400">
        {[1, 2, 3, 4, 5].map((index) => (
          <Star
            key={index}
            className={`${starSize} ${
              index <= Math.round(value)
                ? 'fill-amber-400 text-amber-400'
                : 'text-slate-300'
            }`}
          />
        ))}
      </div>
      <span className="text-xs font-bold text-slate-700">{value}</span>
      {showCount && <span className="text-[11px] text-slate-400 font-normal">({count})</span>}
    </div>
  );
};
