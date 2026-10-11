import React from 'react';
import { Navigate, Link } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { ShieldAlert } from 'lucide-react';

export const PermissionGuard = ({ permission, children, fallback = null }) => {
  const { hasPermission, loading } = useAuth();

  if (loading) {
    return (
      <div className="p-8 flex items-center justify-center">
        <div className="w-8 h-8 border-4 border-emerald-600 border-t-transparent rounded-full animate-spin"></div>
      </div>
    );
  }

  const authorized = hasPermission(permission);

  if (!authorized) {
    if (fallback) return fallback;

    return (
      <div className="min-h-[500px] flex items-center justify-center p-6">
        <div className="max-w-md w-full bg-white rounded-2xl shadow-sm border border-slate-200 p-8 text-center">
          <div className="w-16 h-16 bg-red-50 text-red-600 rounded-full flex items-center justify-center mx-auto mb-4">
            <ShieldAlert className="w-8 h-8" />
          </div>
          <h2 className="text-xl font-bold text-slate-900 mb-2">Access Denied</h2>
          <p className="text-slate-600 text-sm mb-6">
            You do not have the required permission <span className="font-semibold text-slate-800">"{permission}"</span> to view this page.
          </p>
          <div className="flex gap-3 justify-center">
            <Link
              to="/admin/statistics"
              className="px-4 py-2 bg-slate-900 hover:bg-slate-800 text-white text-sm font-medium rounded-lg transition"
            >
              Back to Dashboard
            </Link>
            <Link
              to="/"
              className="px-4 py-2 border border-slate-300 hover:bg-slate-50 text-slate-700 text-sm font-medium rounded-lg transition"
            >
              Storefront
            </Link>
          </div>
        </div>
      </div>
    );
  }

  return children;
};
