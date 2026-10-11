import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { ordersApi } from '../../api/endpoints';
import { DataTable } from '../../components/admin/DataTable';
import { getOrderStatusBadge } from '../public/OrdersPage';
import { formatCurrency, formatDate } from '../../utils/storage';
import { useAuth } from '../../context/AuthContext';
import {
  ShoppingBag,
  Search,
  Eye,
  X,
  Package,
  Calendar,
  MapPin,
  Clock,
  CheckCircle2,
  AlertCircle,
  Truck,
} from 'lucide-react';
import toast from 'react-hot-toast';

export const AdminOrdersPage = () => {
  const { hasPermission } = useAuth();
  const queryClient = useQueryClient();

  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [drawerOrder, setDrawerOrder] = useState(null);
  const [newStatus, setNewStatus] = useState('');

  // Fetch admin orders
  const { data: ordersData, isLoading } = useQuery({
    queryKey: ['admin-orders', { page, search, statusFilter }],
    queryFn: () =>
      ordersApi.getAdminOrders({
        page,
        pageSize: 10,
        search: search || undefined,
        status: statusFilter || undefined,
      }),
  });

  const pagedResult = ordersData?.data || { items: [], totalCount: 0 };
  const orders = pagedResult.items || [];

  // Update status mutation
  const updateStatusMutation = useMutation({
    mutationFn: ({ id, status }) => ordersApi.updateOrderStatus(id, status),
    onSuccess: (res) => {
      queryClient.invalidateQueries({ queryKey: ['admin-orders'] });
      queryClient.invalidateQueries({ queryKey: ['admin-stats-summary'] });
      toast.success('Order status updated');
      if (drawerOrder) {
        setDrawerOrder(res.data);
      }
    },
    onError: (err) => {
      const msg = err.response?.data?.message || 'Invalid status transition';
      toast.error(msg);
    },
  });

  const handleOpenDrawer = async (orderSummary) => {
    try {
      const res = await ordersApi.getAdminOrderById(orderSummary.id);
      setDrawerOrder(res.data);
      setNewStatus(res.data.status);
    } catch {
      setDrawerOrder(orderSummary);
      setNewStatus(orderSummary.status);
    }
  };

  const handleStatusChange = (e) => {
    e.preventDefault();
    if (!drawerOrder || !newStatus) return;
    updateStatusMutation.mutate({ id: drawerOrder.id, status: newStatus });
  };

  const columns = [
    {
      header: 'Order #',
      accessor: (row) => (
        <div>
          <span className="font-mono font-bold text-slate-900">#{row.orderNumber}</span>
          <p className="text-[11px] text-slate-400">{formatDate(row.createdAt)}</p>
        </div>
      ),
    },
    {
      header: 'Status',
      accessor: (row) => getOrderStatusBadge(row.status),
    },
    {
      header: 'Total Paid',
      accessor: (row) => (
        <span className="font-extrabold text-slate-900 font-mono">
          {formatCurrency(row.totalAmount)}
        </span>
      ),
    },
    {
      header: 'Shipping Address',
      accessor: (row) => (
        <p className="text-xs text-slate-500 truncate max-w-xs">{row.shippingAddress}</p>
      ),
    },
    {
      header: 'Actions',
      className: 'text-right',
      accessor: (row) => (
        <div className="flex items-center justify-end">
          <button
            onClick={() => handleOpenDrawer(row)}
            className="px-3 py-1.5 bg-slate-900 hover:bg-emerald-600 text-white text-xs font-bold rounded-xl transition flex items-center gap-1"
          >
            <Eye className="w-3.5 h-3.5" /> Details
          </button>
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      {/* Top Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-extrabold text-slate-900 tracking-tight">
            Order Fulfillment
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Monitor client orders, inspect invoice line snapshots, and process transitions.
          </p>
        </div>
      </div>

      {/* Filter bar */}
      <div className="bg-white p-4 rounded-3xl border border-slate-200/80 shadow-xs flex flex-wrap items-center justify-between gap-3">
        <div className="relative w-full sm:w-64">
          <input
            type="text"
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
            placeholder="Search order #, address..."
            className="w-full pl-9 pr-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-xs focus:bg-white focus:outline-none focus:border-emerald-500"
          />
          <Search className="w-4 h-4 text-slate-400 absolute left-3 top-2.5" />
        </div>

        <select
          value={statusFilter}
          onChange={(e) => {
            setStatusFilter(e.target.value);
            setPage(1);
          }}
          className="px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-xs text-slate-700 focus:bg-white focus:outline-none focus:border-emerald-500"
        >
          <option value="">All Statuses</option>
          <option value="Pending">Pending</option>
          <option value="Confirmed">Confirmed</option>
          <option value="Shipped">Shipped</option>
          <option value="Delivered">Delivered</option>
          <option value="Cancelled">Cancelled</option>
        </select>
      </div>

      {/* Orders Table */}
      <DataTable
        columns={columns}
        data={orders}
        isLoading={isLoading}
        totalCount={pagedResult.totalCount}
        currentPage={page}
        pageSize={10}
        onPageChange={(p) => setPage(p)}
        emptyTitle="No orders found"
        emptyDescription="Orders will populate here when customers complete checkout."
      />

      {/* Order Detail Side Drawer */}
      {drawerOrder && (
        <div className="fixed inset-0 z-50 flex justify-end">
          <div
            className="fixed inset-0 bg-slate-900/40 backdrop-blur-xs animate-in fade-in"
            onClick={() => setDrawerOrder(null)}
          />

          <div className="relative w-full max-w-lg bg-white h-full shadow-2xl p-6 sm:p-8 z-50 overflow-y-auto space-y-6 flex flex-col justify-between">
            <div className="space-y-6">
              {/* Drawer Top */}
              <div className="flex items-center justify-between pb-4 border-b border-slate-100">
                <div>
                  <h3 className="font-mono font-bold text-slate-900 text-lg">
                    #{drawerOrder.orderNumber}
                  </h3>
                  <p className="text-xs text-slate-400">Placed on {formatDate(drawerOrder.createdAt)}</p>
                </div>
                <button
                  onClick={() => setDrawerOrder(null)}
                  className="p-1 text-slate-400 hover:text-slate-600 rounded-lg"
                >
                  <X className="w-5 h-5" />
                </button>
              </div>

              {/* Status Update Form */}
              <div className="bg-slate-50 p-4 rounded-2xl border border-slate-200/80 space-y-3">
                <div className="flex items-center justify-between">
                  <span className="text-xs font-bold uppercase tracking-wider text-slate-500">Current Status</span>
                  {getOrderStatusBadge(drawerOrder.status)}
                </div>

                {hasPermission('Order.Manage') && (
                  <form onSubmit={handleStatusChange} className="pt-2 flex items-center gap-2">
                    <select
                      value={newStatus}
                      onChange={(e) => setNewStatus(e.target.value)}
                      className="flex-1 px-3 py-2 bg-white border border-slate-200 rounded-xl text-xs font-bold text-slate-800 focus:outline-none focus:border-emerald-500"
                    >
                      <option value="Pending">Pending</option>
                      <option value="Confirmed">Confirmed</option>
                      <option value="Shipped">Shipped</option>
                      <option value="Delivered">Delivered</option>
                      <option value="Cancelled">Cancelled (Restores Stock)</option>
                    </select>
                    <button
                      type="submit"
                      disabled={updateStatusMutation.isPending || newStatus === drawerOrder.status}
                      className="px-4 py-2 bg-emerald-600 hover:bg-emerald-500 text-white font-bold text-xs rounded-xl shadow-xs disabled:opacity-40"
                    >
                      Update
                    </button>
                  </form>
                )}
              </div>

              {/* Shipping Address */}
              <div>
                <h4 className="text-xs font-bold uppercase tracking-wider text-slate-500 mb-2 flex items-center gap-1.5">
                  <MapPin className="w-3.5 h-3.5 text-emerald-600" />
                  Delivery Destination
                </h4>
                <div className="p-3 bg-slate-50 rounded-2xl border border-slate-100 text-xs text-slate-700 leading-relaxed">
                  {drawerOrder.shippingAddress}
                </div>
              </div>

              {/* Item Lines */}
              <div>
                <h4 className="text-xs font-bold uppercase tracking-wider text-slate-500 mb-2 flex items-center gap-1.5">
                  <Package className="w-3.5 h-3.5 text-emerald-600" />
                  Ordered Line Items ({drawerOrder.items?.length || 0})
                </h4>

                <div className="divide-y divide-slate-100 border border-slate-100 rounded-2xl p-2 bg-white">
                  {drawerOrder.items?.map((item) => (
                    <div key={item.id} className="py-2.5 px-2 flex items-center justify-between text-xs">
                      <div className="truncate max-w-[240px]">
                        <p className="font-bold text-slate-900 truncate">{item.productName}</p>
                        <p className="text-slate-400">
                          {formatCurrency(item.unitPrice)} × {item.quantity}
                        </p>
                      </div>
                      <span className="font-extrabold text-slate-900 font-mono">
                        {formatCurrency(item.unitPrice * item.quantity)}
                      </span>
                    </div>
                  ))}
                </div>
              </div>

              {/* Financial Total */}
              <div className="p-4 bg-emerald-50/50 rounded-2xl border border-emerald-100 flex items-center justify-between">
                <span className="font-bold text-slate-800 text-sm">Gross Total Paid</span>
                <span className="font-black text-xl text-emerald-600">
                  {formatCurrency(drawerOrder.totalAmount)}
                </span>
              </div>
            </div>

            <button
              onClick={() => setDrawerOrder(null)}
              className="w-full py-2.5 border border-slate-200 text-slate-600 font-bold text-xs rounded-xl hover:bg-slate-50"
            >
              Close Drawer
            </button>
          </div>
        </div>
      )}
    </div>
  );
};
