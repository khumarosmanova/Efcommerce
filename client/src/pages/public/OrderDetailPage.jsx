import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { useParams, Link } from 'react-router-dom';
import { ordersApi } from '../../api/endpoints';
import { formatCurrency, formatDate } from '../../utils/storage';
import { getOrderStatusBadge } from './OrdersPage';
import {
  ArrowLeft,
  MapPin,
  Calendar,
  Package,
  ShoppingBag,
  ShieldCheck,
  AlertCircle,
} from 'lucide-react';

export const OrderDetailPage = () => {
  const { id } = useParams();

  const { data, isLoading, error } = useQuery({
    queryKey: ['order-detail', id],
    queryFn: () => ordersApi.getMyOrderById(id),
  });

  const order = data?.data;

  if (isLoading) {
    return (
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16 flex items-center justify-center">
        <div className="w-10 h-10 border-4 border-emerald-600 border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  if (error || !order) {
    return (
      <div className="max-w-md mx-auto px-4 py-16 text-center space-y-4">
        <AlertCircle className="w-12 h-12 text-red-500 mx-auto" />
        <h2 className="text-xl font-bold text-slate-800">Order not found</h2>
        <p className="text-xs text-slate-500">The requested order could not be located in your history.</p>
        <Link to="/orders" className="inline-block px-5 py-2.5 bg-emerald-600 text-white rounded-xl text-xs font-bold">
          Back to Orders
        </Link>
      </div>
    );
  }

  return (
    <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-8">
      {/* Return to orders link */}
      <Link to="/orders" className="inline-flex items-center gap-2 text-xs font-bold text-slate-600 hover:text-emerald-600 transition">
        <ArrowLeft className="w-4 h-4" /> All Orders
      </Link>

      {/* Order Top Banner */}
      <div className="bg-white p-6 sm:p-8 rounded-3xl border border-slate-200/80 shadow-xs flex flex-col sm:flex-row sm:items-center justify-between gap-6">
        <div>
          <div className="flex items-center gap-3">
            <h1 className="text-xl sm:text-2xl font-black text-slate-900 font-mono">
              #{order.orderNumber}
            </h1>
            {getOrderStatusBadge(order.status)}
          </div>
          <p className="text-xs text-slate-400 mt-1 flex items-center gap-1.5">
            <Calendar className="w-3.5 h-3.5" />
            Ordered on {formatDate(order.createdAt)}
          </p>
        </div>

        <div className="text-left sm:text-right">
          <p className="text-[11px] font-semibold uppercase text-slate-400">Total Paid</p>
          <p className="text-2xl font-black text-emerald-600">
            {formatCurrency(order.totalAmount)}
          </p>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-8 items-start">
        {/* Order Items snapshot list */}
        <div className="md:col-span-2 bg-white p-6 sm:p-8 rounded-3xl border border-slate-200/80 shadow-xs space-y-6">
          <h3 className="font-bold text-slate-900 text-base flex items-center gap-2">
            <Package className="w-4 h-4 text-emerald-600" />
            Purchased Items ({order.items?.length || 0})
          </h3>

          <div className="divide-y divide-slate-100">
            {order.items?.map((item) => (
              <div key={item.id} className="py-4 flex items-center justify-between gap-4">
                <div className="flex items-center gap-3 min-w-0">
                  <div className="w-12 h-12 bg-slate-50 rounded-xl flex items-center justify-center p-2 border border-slate-100 shrink-0">
                    <ShoppingBag className="w-6 h-6 text-slate-400" />
                  </div>
                  <div className="min-w-0">
                    <p className="font-bold text-slate-900 text-sm truncate">{item.productName}</p>
                    <p className="text-xs text-slate-400 mt-0.5">
                      {formatCurrency(item.unitPrice)} × {item.quantity} unit(s)
                    </p>
                  </div>
                </div>

                <span className="font-black text-slate-900 text-sm shrink-0">
                  {formatCurrency(item.unitPrice * item.quantity)}
                </span>
              </div>
            ))}
          </div>

          <div className="border-t border-slate-100 pt-4 flex justify-between text-sm font-bold text-slate-800">
            <span>Subtotal</span>
            <span>{formatCurrency(order.totalAmount)}</span>
          </div>
        </div>

        {/* Delivery destination card */}
        <div className="bg-white p-6 sm:p-8 rounded-3xl border border-slate-200/80 shadow-xs space-y-4">
          <h3 className="font-bold text-slate-900 text-base flex items-center gap-2">
            <MapPin className="w-4 h-4 text-emerald-600" />
            Delivery Address
          </h3>

          <p className="text-xs text-slate-600 leading-relaxed bg-slate-50 p-4 rounded-2xl border border-slate-100">
            {order.shippingAddress || 'Standard Shipping Address'}
          </p>

          <div className="pt-2 flex items-center gap-2 text-[11px] text-slate-400">
            <ShieldCheck className="w-4 h-4 text-emerald-600" />
            <span>Order history logged securely</span>
          </div>
        </div>
      </div>
    </div>
  );
};
