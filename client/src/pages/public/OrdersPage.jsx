import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { ordersApi } from '../../api/endpoints';
import { formatCurrency, formatDate } from '../../utils/storage';
import { EmptyState } from '../../components/common/EmptyState';
import { useTranslation } from '../../context/LanguageContext';
import {
  ShoppingBag,
  Clock,
  CheckCircle2,
  Truck,
  Package,
  XCircle,
  ChevronRight,
} from 'lucide-react';

export const getOrderStatusBadge = (status) => {
  const normalized = status?.toString()?.toLowerCase() || '';

  if (normalized === 'pending') {
    return (
      <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-bold bg-amber-50 dark:bg-amber-950/40 text-amber-700 dark:text-amber-400 border border-amber-200/60 dark:border-amber-800">
        <Clock className="w-3 h-3" /> Pending
      </span>
    );
  }
  if (normalized === 'confirmed') {
    return (
      <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-bold bg-blue-50 dark:bg-blue-950/40 text-blue-700 dark:text-blue-400 border border-blue-200/60 dark:border-blue-800">
        <CheckCircle2 className="w-3 h-3" /> Confirmed
      </span>
    );
  }
  if (normalized === 'shipped') {
    return (
      <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-bold bg-indigo-50 dark:bg-indigo-950/40 text-indigo-700 dark:text-indigo-400 border border-indigo-200/60 dark:border-indigo-800">
        <Truck className="w-3 h-3" /> Shipped
      </span>
    );
  }
  if (normalized === 'delivered') {
    return (
      <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-bold bg-emerald-50 dark:bg-emerald-950/40 text-emerald-700 dark:text-emerald-400 border border-emerald-200/60 dark:border-emerald-800">
        <Package className="w-3 h-3" /> Delivered
      </span>
    );
  }
  if (normalized === 'cancelled') {
    return (
      <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-bold bg-rose-50 dark:bg-rose-950/40 text-rose-700 dark:text-rose-400 border border-rose-200/60 dark:border-rose-800">
        <XCircle className="w-3 h-3" /> Cancelled
      </span>
    );
  }

  return (
    <span className="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-bold bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300">
      {status}
    </span>
  );
};

export const OrdersPage = () => {
  const { t } = useTranslation();
  const { data, isLoading } = useQuery({
    queryKey: ['my-orders'],
    queryFn: () => ordersApi.getMyOrders(1, 20),
  });

  const pagedOrders = data?.data || { items: [] };
  const orders = pagedOrders.items || [];

  if (isLoading) {
    return (
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16 flex items-center justify-center">
        <div className="w-10 h-10 border-4 border-emerald-600 border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  if (orders.length === 0) {
    return (
      <div className="py-16">
        <EmptyState
          icon={ShoppingBag}
          title={t('noOrders')}
          description="You haven't placed any orders with us yet. Check out our store and get your first delivery."
          actionLabel={t('shopNow')}
          actionLink="/products"
        />
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-8">
      <div>
        <h1 className="text-2xl sm:text-3xl font-black text-slate-900 dark:text-white tracking-tight">{t('myOrders')}</h1>
        <p className="text-xs text-slate-500 dark:text-slate-400 mt-1">Review your order history and track shipments</p>
      </div>

      <div className="space-y-4">
        {orders.map((order) => (
          <div
            key={order.id}
            className="bg-white dark:bg-slate-900 p-6 rounded-3xl border border-slate-200/80 dark:border-slate-800 shadow-xs hover:border-slate-300 dark:hover:border-slate-700 transition duration-200 flex flex-col md:flex-row md:items-center justify-between gap-6"
          >
            <div className="space-y-2">
              <div className="flex flex-wrap items-center gap-3">
                <span className="font-mono text-sm font-bold text-slate-900 dark:text-white">
                  #{order.orderNumber}
                </span>
                {getOrderStatusBadge(order.status)}
              </div>
              <p className="text-xs text-slate-400 dark:text-slate-500">
                {formatDate(order.createdAt)} • {order.items?.length || 0} {t('itemsCount')}
              </p>
            </div>

            <div className="flex items-center justify-between md:justify-end gap-6 pt-4 md:pt-0 border-t md:border-0 border-slate-100 dark:border-slate-800">
              <div className="text-left md:text-right">
                <p className="text-[11px] font-semibold uppercase text-slate-400 dark:text-slate-500">{t('total')}</p>
                <p className="text-lg font-black text-slate-900 dark:text-white">
                  {formatCurrency(order.totalAmount)}
                </p>
              </div>

              <Link
                to={`/orders/${order.id}`}
                className="px-4 py-2 bg-slate-900 dark:bg-emerald-600 hover:bg-emerald-600 dark:hover:bg-emerald-700 text-white text-xs font-bold rounded-xl transition flex items-center gap-1"
              >
                View Details <ChevronRight className="w-3.5 h-3.5" />
              </Link>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
