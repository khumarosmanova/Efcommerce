import React from 'react';
import { PackageOpen } from 'lucide-react';
import { Link } from 'react-router-dom';

export const EmptyState = ({
  icon: Icon = PackageOpen,
  title = 'No items found',
  description = 'Try searching with different terms or reset your filters.',
  actionLabel,
  actionLink,
  onAction,
}) => {
  return (
    <div className="bg-white rounded-3xl border border-slate-200/80 p-10 sm:p-14 text-center max-w-md mx-auto space-y-4 shadow-xs">
      <div className="w-16 h-16 rounded-2xl bg-slate-50 border border-slate-100 flex items-center justify-center mx-auto text-slate-400">
        <Icon className="w-8 h-8" />
      </div>
      <div>
        <h3 className="text-lg font-bold text-slate-900">{title}</h3>
        <p className="text-xs text-slate-500 mt-1 leading-relaxed max-w-xs mx-auto">
          {description}
        </p>
      </div>

      {(actionLabel && actionLink) && (
        <div className="pt-2">
          <Link
            to={actionLink}
            className="inline-flex items-center justify-center px-5 py-2.5 bg-emerald-600 hover:bg-emerald-500 text-white text-xs font-semibold rounded-xl shadow-sm transition"
          >
            {actionLabel}
          </Link>
        </div>
      )}

      {(actionLabel && onAction && !actionLink) && (
        <div className="pt-2">
          <button
            onClick={onAction}
            className="inline-flex items-center justify-center px-5 py-2.5 bg-slate-900 hover:bg-slate-800 text-white text-xs font-semibold rounded-xl shadow-sm transition"
          >
            {actionLabel}
          </button>
        </div>
      )}
    </div>
  );
};
