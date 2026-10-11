import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { statisticsApi } from '../../api/endpoints';
import { formatCurrency } from '../../utils/storage';
import {
  DollarSign,
  ShoppingBag,
  Users,
  Package,
  Clock,
  AlertTriangle,
  TrendingUp,
  PieChart as PieIcon,
  BarChart2,
  Calendar,
} from 'lucide-react';
import {
  ResponsiveContainer,
  AreaChart,
  Area,
  XAxis,
  YAxis,
  Tooltip,
  CartesianGrid,
  BarChart,
  Bar,
  PieChart,
  Pie,
  Cell,
} from 'recharts';

export const StatisticsDashboardPage = () => {
  const [groupBy, setGroupBy] = useState('month');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');

  // 1. Summary KPIs
  const { data: summaryData, isLoading: loadingSummary } = useQuery({
    queryKey: ['admin-stats-summary'],
    queryFn: () => statisticsApi.getSummary(),
  });

  // 2. Sales Time Series (with date filters)
  const { data: salesData, isLoading: loadingSales } = useQuery({
    queryKey: ['admin-stats-sales', { groupBy, from, to }],
    queryFn: () =>
      statisticsApi.getSales({
        groupBy,
        from: from || undefined,
        to: to || undefined,
      }),
  });

  // 3. Top Products
  const { data: topProductsData } = useQuery({
    queryKey: ['admin-stats-top-products'],
    queryFn: () => statisticsApi.getTopProducts(5),
  });

  // 4. Categories Breakdown
  const { data: categoriesData } = useQuery({
    queryKey: ['admin-stats-categories'],
    queryFn: () => statisticsApi.getCategories(),
  });

  // 5. Orders by Status
  const { data: statusData } = useQuery({
    queryKey: ['admin-stats-status'],
    queryFn: () => statisticsApi.getOrdersByStatus(),
  });

  const summary = summaryData?.data || {
    totalRevenue: 0,
    totalOrders: 0,
    totalCustomers: 0,
    totalProducts: 0,
    pendingOrders: 0,
    lowStockProductCount: 0,
  };

  const sales = salesData?.data || [];
  const topProducts = topProductsData?.data || [];
  const categoriesSales = categoriesData?.data || [];
  const orderStatuses = statusData?.data || [];

  const PIE_COLORS = ['#10b981', '#3b82f6', '#f59e0b', '#ec4899', '#8b5cf6', '#64748b'];

  return (
    <div className="space-y-8">
      {/* Page Title & Filter Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-extrabold text-slate-900 tracking-tight">
            Statistics & Financial Insights
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Aggregated store performance, gross sales trends, and categorical distribution.
          </p>
        </div>

        {/* Date Filter Controls */}
        <div className="flex items-center gap-2 bg-white p-2 rounded-2xl border border-slate-200/80 shadow-xs text-xs">
          <Calendar className="w-4 h-4 text-slate-400 ml-1" />
          <input
            type="date"
            value={from}
            onChange={(e) => setFrom(e.target.value)}
            className="p-1 border border-slate-200 rounded-lg text-slate-700 focus:outline-none"
            placeholder="From"
          />
          <span className="text-slate-400">-</span>
          <input
            type="date"
            value={to}
            onChange={(e) => setTo(e.target.value)}
            className="p-1 border border-slate-200 rounded-lg text-slate-700 focus:outline-none"
            placeholder="To"
          />
          {(from || to) && (
            <button
              onClick={() => {
                setFrom('');
                setTo('');
              }}
              className="text-[11px] font-bold text-red-600 hover:underline px-1"
            >
              Reset
            </button>
          )}
        </div>
      </div>

      {/* KPI Summary Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6 gap-4">
        <div className="bg-white p-5 rounded-3xl border border-slate-200/80 shadow-xs flex flex-col justify-between">
          <div className="flex items-center justify-between text-emerald-600 mb-2">
            <span className="text-xs font-bold uppercase tracking-wider text-slate-400">Gross Sales</span>
            <div className="w-8 h-8 rounded-xl bg-emerald-50 flex items-center justify-center font-bold">
              <DollarSign className="w-4 h-4" />
            </div>
          </div>
          <div>
            <div className="text-2xl font-black text-slate-900">{formatCurrency(summary.totalRevenue)}</div>
            <p className="text-[10px] text-emerald-600 font-bold mt-0.5">Excludes cancelled</p>
          </div>
        </div>

        <div className="bg-white p-5 rounded-3xl border border-slate-200/80 shadow-xs flex flex-col justify-between">
          <div className="flex items-center justify-between text-blue-600 mb-2">
            <span className="text-xs font-bold uppercase tracking-wider text-slate-400">Total Orders</span>
            <div className="w-8 h-8 rounded-xl bg-blue-50 flex items-center justify-center font-bold">
              <ShoppingBag className="w-4 h-4" />
            </div>
          </div>
          <div>
            <div className="text-2xl font-black text-slate-900">{summary.totalOrders}</div>
            <p className="text-[10px] text-slate-400 mt-0.5">All placed orders</p>
          </div>
        </div>

        <div className="bg-white p-5 rounded-3xl border border-slate-200/80 shadow-xs flex flex-col justify-between">
          <div className="flex items-center justify-between text-indigo-600 mb-2">
            <span className="text-xs font-bold uppercase tracking-wider text-slate-400">Customers</span>
            <div className="w-8 h-8 rounded-xl bg-indigo-50 flex items-center justify-center font-bold">
              <Users className="w-4 h-4" />
            </div>
          </div>
          <div>
            <div className="text-2xl font-black text-slate-900">{summary.totalCustomers}</div>
            <p className="text-[10px] text-slate-400 mt-0.5">Verified customer users</p>
          </div>
        </div>

        <div className="bg-white p-5 rounded-3xl border border-slate-200/80 shadow-xs flex flex-col justify-between">
          <div className="flex items-center justify-between text-purple-600 mb-2">
            <span className="text-xs font-bold uppercase tracking-wider text-slate-400">Active Products</span>
            <div className="w-8 h-8 rounded-xl bg-purple-50 flex items-center justify-center font-bold">
              <Package className="w-4 h-4" />
            </div>
          </div>
          <div>
            <div className="text-2xl font-black text-slate-900">{summary.totalProducts}</div>
            <p className="text-[10px] text-slate-400 mt-0.5">Live store catalog</p>
          </div>
        </div>

        <div className="bg-white p-5 rounded-3xl border border-slate-200/80 shadow-xs flex flex-col justify-between">
          <div className="flex items-center justify-between text-amber-600 mb-2">
            <span className="text-xs font-bold uppercase tracking-wider text-slate-400">Pending Orders</span>
            <div className="w-8 h-8 rounded-xl bg-amber-50 flex items-center justify-center font-bold">
              <Clock className="w-4 h-4" />
            </div>
          </div>
          <div>
            <div className="text-2xl font-black text-amber-600">{summary.pendingOrders}</div>
            <p className="text-[10px] text-slate-400 mt-0.5">Awaiting fulfillment</p>
          </div>
        </div>

        <div className="bg-white p-5 rounded-3xl border border-slate-200/80 shadow-xs flex flex-col justify-between">
          <div className="flex items-center justify-between text-rose-600 mb-2">
            <span className="text-xs font-bold uppercase tracking-wider text-slate-400">Low Stock</span>
            <div className="w-8 h-8 rounded-xl bg-rose-50 flex items-center justify-center font-bold">
              <AlertTriangle className="w-4 h-4" />
            </div>
          </div>
          <div>
            <div className="text-2xl font-black text-rose-600">{summary.lowStockProductCount}</div>
            <p className="text-[10px] text-slate-400 mt-0.5">&le; 5 units remaining</p>
          </div>
        </div>
      </div>

      {/* Sales Trend Chart */}
      <div className="bg-white p-6 sm:p-8 rounded-3xl border border-slate-200/80 shadow-xs space-y-4">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div className="flex items-center gap-2">
            <TrendingUp className="w-5 h-5 text-emerald-600" />
            <h3 className="font-bold text-slate-900 text-lg">Sales & Revenue Timeline</h3>
          </div>

          <div className="flex items-center bg-slate-100 p-1 rounded-xl text-xs font-semibold text-slate-600">
            <button
              onClick={() => setGroupBy('day')}
              className={`px-3 py-1.5 rounded-lg transition ${
                groupBy === 'day' ? 'bg-white text-slate-900 shadow-xs font-bold' : 'hover:text-slate-900'
              }`}
            >
              Daily Breakdown
            </button>
            <button
              onClick={() => setGroupBy('month')}
              className={`px-3 py-1.5 rounded-lg transition ${
                groupBy === 'month' ? 'bg-white text-slate-900 shadow-xs font-bold' : 'hover:text-slate-900'
              }`}
            >
              Monthly Aggregation
            </button>
          </div>
        </div>

        <div className="h-72 w-full pt-4">
          {loadingSales ? (
            <div className="h-full bg-slate-50 rounded-2xl animate-pulse" />
          ) : (
            <ResponsiveContainer width="100%" height="100%">
              <AreaChart data={sales} margin={{ top: 10, right: 10, left: -20, bottom: 0 }}>
                <defs>
                  <linearGradient id="revenueTimeline" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#10b981" stopOpacity={0.35} />
                    <stop offset="95%" stopColor="#10b981" stopOpacity={0.0} />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#f1f5f9" />
                <XAxis dataKey="period" stroke="#94a3b8" fontSize={11} tickLine={false} />
                <YAxis stroke="#94a3b8" fontSize={11} tickLine={false} />
                <Tooltip
                  contentStyle={{ backgroundColor: '#0f172a', borderRadius: '12px', border: 'none', color: '#fff', fontSize: '12px' }}
                  formatter={(val, name) => [name === 'revenue' ? formatCurrency(val) : val, name === 'revenue' ? 'Revenue' : 'Orders']}
                />
                <Area type="monotone" dataKey="revenue" stroke="#10b981" strokeWidth={2.5} fillOpacity={1} fill="url(#revenueTimeline)" />
              </AreaChart>
            </ResponsiveContainer>
          )}
        </div>
      </div>

      {/* Two columns: Category Breakdown & Orders by Status */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        {/* Category breakdown */}
        <div className="bg-white p-6 sm:p-8 rounded-3xl border border-slate-200/80 shadow-xs space-y-4">
          <div className="flex items-center gap-2">
            <PieIcon className="w-5 h-5 text-indigo-600" />
            <h3 className="font-bold text-slate-900 text-lg">Sales by Department</h3>
          </div>

          <div className="space-y-3 pt-2">
            {categoriesSales.map((c, i) => (
              <div key={c.categoryId} className="flex items-center justify-between text-xs">
                <div className="flex items-center gap-2 max-w-[200px]">
                  <span className="w-2.5 h-2.5 rounded-full shrink-0" style={{ backgroundColor: PIE_COLORS[i % PIE_COLORS.length] }} />
                  <span className="font-bold text-slate-800 truncate">{c.categoryName}</span>
                </div>
                <div className="flex items-center gap-4">
                  <span className="text-slate-400 font-mono">{c.totalQuantitySold} sold</span>
                  <span className="font-bold text-slate-900 w-20 text-right">{formatCurrency(c.totalRevenue)}</span>
                  <span className="text-slate-500 font-medium w-12 text-right">{c.revenuePercentage}%</span>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Orders by Status */}
        <div className="bg-white p-6 sm:p-8 rounded-3xl border border-slate-200/80 shadow-xs space-y-4">
          <div className="flex items-center gap-2">
            <BarChart2 className="w-5 h-5 text-blue-600" />
            <h3 className="font-bold text-slate-900 text-lg">Fulfillment Status</h3>
          </div>

          <div className="h-60 w-full pt-2">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={orderStatuses} margin={{ top: 10, right: 10, left: -20, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#f1f5f9" />
                <XAxis dataKey="status" stroke="#94a3b8" fontSize={11} tickLine={false} />
                <YAxis stroke="#94a3b8" fontSize={11} tickLine={false} />
                <Tooltip
                  contentStyle={{ backgroundColor: '#0f172a', borderRadius: '12px', border: 'none', color: '#fff', fontSize: '12px' }}
                />
                <Bar dataKey="orderCount" fill="#3b82f6" radius={[8, 8, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>
      </div>

      {/* Top 5 Best Sellers */}
      <div className="bg-white p-6 sm:p-8 rounded-3xl border border-slate-200/80 shadow-xs space-y-4">
        <h3 className="font-bold text-slate-900 text-lg">Top 5 Best-Selling Products</h3>
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="border-b border-slate-100 text-[11px] font-bold uppercase tracking-wider text-slate-400">
                <th className="pb-3">Product Name</th>
                <th className="pb-3">SKU</th>
                <th className="pb-3 text-right">Units Sold</th>
                <th className="pb-3 text-right">Gross Revenue</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {topProducts.map((p) => (
                <tr key={p.productId} className="hover:bg-slate-50/50 transition">
                  <td className="py-3 font-semibold text-slate-800 flex items-center gap-3">
                    <div className="w-9 h-9 rounded-xl bg-slate-100 flex items-center justify-center p-1 border border-slate-200 shrink-0">
                      {p.imageUrl ? (
                        <img src={p.imageUrl} alt={p.productName} className="max-w-full max-h-full object-contain" />
                      ) : (
                        <Package className="w-4 h-4 text-slate-400" />
                      )}
                    </div>
                    <span className="truncate max-w-sm">{p.productName}</span>
                  </td>
                  <td className="py-3 text-xs text-slate-400 font-mono">{p.sku || '-'}</td>
                  <td className="py-3 text-right font-bold text-slate-700">{p.totalQuantitySold}</td>
                  <td className="py-3 text-right font-extrabold text-slate-900">{formatCurrency(p.totalRevenue)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
