import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { trashApi } from '../../api/endpoints';
import { DataTable } from '../../components/admin/DataTable';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';
import { formatCurrency, formatDate, getImageUrl } from '../../utils/storage';
import { useAuth } from '../../context/AuthContext';
import {
  Trash2,
  RotateCcw,
  AlertTriangle,
  Package,
  Layers,
  Search,
  CheckCircle2,
} from 'lucide-react';
import toast from 'react-hot-toast';

export const AdminTrashPage = () => {
  const { hasPermission } = useAuth();
  const queryClient = useQueryClient();

  const [activeTab, setActiveTab] = useState('products'); // 'products' | 'categories'
  const [productPage, setProductPage] = useState(1);
  const [categoryPage, setCategoryPage] = useState(1);
  const [search, setSearch] = useState('');

  // Confirm dialog candidate
  const [restoreCandidate, setRestoreCandidate] = useState(null);
  const [hardDeleteCandidate, setHardDeleteCandidate] = useState(null);

  // 1. Fetch Deleted Products
  const { data: productsData, isLoading: loadingProducts } = useQuery({
    queryKey: ['admin-trash-products', { productPage, search }],
    queryFn: () =>
      trashApi.getDeletedProducts({
        page: productPage,
        pageSize: 10,
        search: search || undefined,
      }),
    enabled: activeTab === 'products',
  });

  // 2. Fetch Deleted Categories
  const { data: categoriesData, isLoading: loadingCategories } = useQuery({
    queryKey: ['admin-trash-categories', { categoryPage, search }],
    queryFn: () =>
      trashApi.getDeletedCategories({
        page: categoryPage,
        pageSize: 10,
        search: search || undefined,
      }),
    enabled: activeTab === 'categories',
  });

  const pagedProducts = productsData?.data || { items: [], totalCount: 0 };
  const pagedCategories = categoriesData?.data || { items: [], totalCount: 0 };

  // Mutations: Restore
  const restoreProductMutation = useMutation({
    mutationFn: (id) => trashApi.restoreProduct(id),
    onSuccess: (res) => {
      queryClient.invalidateQueries({ queryKey: ['admin-trash-products'] });
      queryClient.invalidateQueries({ queryKey: ['admin-products'] });
      toast.success(res.message || 'Product restored successfully');
      setRestoreCandidate(null);
    },
    onError: (err) => {
      const msg = err.response?.data?.message || 'Failed to restore product';
      toast.error(msg);
    },
  });

  const restoreCategoryMutation = useMutation({
    mutationFn: (id) => trashApi.restoreCategory(id),
    onSuccess: (res) => {
      queryClient.invalidateQueries({ queryKey: ['admin-trash-categories'] });
      queryClient.invalidateQueries({ queryKey: ['admin-categories-all'] });
      toast.success(res.message || 'Category restored successfully');
      setRestoreCandidate(null);
    },
    onError: (err) => {
      const msg = err.response?.data?.message || 'Failed to restore category';
      toast.error(msg);
    },
  });

  // Mutations: Hard Delete
  const hardDeleteProductMutation = useMutation({
    mutationFn: (id) => trashApi.hardDeleteProduct(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-trash-products'] });
      toast.success('Product permanently purged from database');
      setHardDeleteCandidate(null);
    },
    onError: (err) => {
      const msg =
        err.response?.data?.message ||
        'Cannot purge product because it is referenced in past customer orders';
      toast.error(msg);
    },
  });

  const hardDeleteCategoryMutation = useMutation({
    mutationFn: (id) => trashApi.hardDeleteCategory(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-trash-categories'] });
      toast.success('Category permanently purged from database');
      setHardDeleteCandidate(null);
    },
    onError: (err) => {
      const msg =
        err.response?.data?.message ||
        'Cannot purge category because products or subcategories still reference it';
      toast.error(msg);
    },
  });

  const handleConfirmRestore = () => {
    if (!restoreCandidate) return;
    if (activeTab === 'products') {
      restoreProductMutation.mutate(restoreCandidate.id);
    } else {
      restoreCategoryMutation.mutate(restoreCandidate.id);
    }
  };

  const handleConfirmHardDelete = () => {
    if (!hardDeleteCandidate) return;
    if (activeTab === 'products') {
      hardDeleteProductMutation.mutate(hardDeleteCandidate.id);
    } else {
      hardDeleteCategoryMutation.mutate(hardDeleteCandidate.id);
    }
  };

  const productColumns = [
    {
      header: 'Product',
      accessor: (row) => (
        <div className="flex items-center gap-3">
          <div className="w-9 h-9 rounded-xl bg-slate-100 flex items-center justify-center p-1 border border-slate-200 shrink-0">
            {row.imageUrl ? (
              <img src={getImageUrl(row.imageUrl)} alt={row.name} className="max-w-full max-h-full object-contain" />
            ) : (
              <Package className="w-4 h-4 text-slate-400" />
            )}
          </div>
          <div>
            <p className="font-bold text-slate-900">{row.name}</p>
            <p className="text-[11px] text-slate-400 font-mono">SKU: {row.sku}</p>
          </div>
        </div>
      ),
    },
    {
      header: 'Category',
      accessor: (row) => (
        <span className="text-xs font-semibold px-2.5 py-1 bg-slate-100 text-slate-700 rounded-lg">
          {row.categoryName}
        </span>
      ),
    },
    {
      header: 'Price',
      accessor: (row) => (
        <span className="font-bold text-slate-900">{formatCurrency(row.price)}</span>
      ),
    },
    {
      header: 'Deleted At',
      accessor: (row) => (
        <span className="text-xs text-rose-600 font-medium">
          {formatDate(row.deletedAt)}
        </span>
      ),
    },
    {
      header: 'Actions',
      className: 'text-right',
      accessor: (row) => (
        <div className="flex items-center justify-end gap-2">
          {hasPermission('Trash.Manage') && (
            <>
              <button
                onClick={() => setRestoreCandidate(row)}
                className="px-3 py-1.5 bg-emerald-50 hover:bg-emerald-100 text-emerald-700 rounded-xl text-xs font-bold transition flex items-center gap-1"
                title="Restore product"
              >
                <RotateCcw className="w-3.5 h-3.5" /> Restore
              </button>
              <button
                onClick={() => setHardDeleteCandidate(row)}
                className="px-3 py-1.5 bg-rose-50 hover:bg-rose-100 text-rose-700 rounded-xl text-xs font-bold transition flex items-center gap-1"
                title="Hard Delete permanently"
              >
                <Trash2 className="w-3.5 h-3.5" /> Purge
              </button>
            </>
          )}
        </div>
      ),
    },
  ];

  const categoryColumns = [
    {
      header: 'Category',
      accessor: (row) => (
        <div className="flex items-center gap-3">
          <div className="w-9 h-9 rounded-xl bg-slate-100 text-slate-600 flex items-center justify-center shrink-0">
            <Layers className="w-4 h-4" />
          </div>
          <div>
            <p className="font-bold text-slate-900">{row.name}</p>
            <p className="text-[11px] text-slate-400 font-mono">/{row.slug}</p>
          </div>
        </div>
      ),
    },
    {
      header: 'Parent Department',
      accessor: (row) => (
        <span className="text-xs text-slate-500">
          {row.parentCategoryName || 'Root'}
        </span>
      ),
    },
    {
      header: 'Deleted At',
      accessor: (row) => (
        <span className="text-xs text-rose-600 font-medium">
          {formatDate(row.deletedAt)}
        </span>
      ),
    },
    {
      header: 'Actions',
      className: 'text-right',
      accessor: (row) => (
        <div className="flex items-center justify-end gap-2">
          {hasPermission('Trash.Manage') && (
            <>
              <button
                onClick={() => setRestoreCandidate(row)}
                className="px-3 py-1.5 bg-emerald-50 hover:bg-emerald-100 text-emerald-700 rounded-xl text-xs font-bold transition flex items-center gap-1"
                title="Restore category"
              >
                <RotateCcw className="w-3.5 h-3.5" /> Restore
              </button>
              <button
                onClick={() => setHardDeleteCandidate(row)}
                className="px-3 py-1.5 bg-rose-50 hover:bg-rose-100 text-rose-700 rounded-xl text-xs font-bold transition flex items-center gap-1"
                title="Hard Delete permanently"
              >
                <Trash2 className="w-3.5 h-3.5" /> Purge
              </button>
            </>
          )}
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
            Trash & Soft-Deleted Archive
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Safely recover deleted items or permanently purge records if referential rules permit.
          </p>
        </div>
      </div>

      {/* Tabs Switcher and Search */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div className="flex items-center gap-2 bg-slate-200/70 p-1 rounded-2xl w-fit">
          <button
            onClick={() => {
              setActiveTab('products');
              setSearch('');
            }}
            className={`px-4 py-2 rounded-xl text-xs font-bold transition flex items-center gap-2 ${
              activeTab === 'products'
                ? 'bg-white text-slate-900 shadow-xs'
                : 'text-slate-600 hover:text-slate-900'
            }`}
          >
            <Package className="w-4 h-4 text-emerald-600" />
            Deleted Products ({pagedProducts.totalCount})
          </button>
          <button
            onClick={() => {
              setActiveTab('categories');
              setSearch('');
            }}
            className={`px-4 py-2 rounded-xl text-xs font-bold transition flex items-center gap-2 ${
              activeTab === 'categories'
                ? 'bg-white text-slate-900 shadow-xs'
                : 'text-slate-600 hover:text-slate-900'
            }`}
          >
            <Layers className="w-4 h-4 text-indigo-600" />
            Deleted Categories ({pagedCategories.totalCount})
          </button>
        </div>

        <div className="relative w-full sm:w-64">
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search deleted records..."
            className="w-full pl-9 pr-3 py-2 bg-white border border-slate-200 rounded-xl text-xs focus:outline-none focus:border-emerald-500 shadow-2xs"
          />
          <Search className="w-4 h-4 text-slate-400 absolute left-3 top-2.5" />
        </div>
      </div>

      {/* Tab Data Table */}
      {activeTab === 'products' ? (
        <DataTable
          columns={productColumns}
          data={pagedProducts.items}
          isLoading={loadingProducts}
          totalCount={pagedProducts.totalCount}
          currentPage={productPage}
          pageSize={10}
          onPageChange={(p) => setProductPage(p)}
          emptyTitle="Trash is empty"
          emptyDescription="No deleted products found in the database archive."
        />
      ) : (
        <DataTable
          columns={categoryColumns}
          data={pagedCategories.items}
          isLoading={loadingCategories}
          totalCount={pagedCategories.totalCount}
          currentPage={categoryPage}
          pageSize={10}
          onPageChange={(p) => setCategoryPage(p)}
          emptyTitle="Trash is empty"
          emptyDescription="No deleted categories found in the database archive."
        />
      )}

      {/* Restore Confirmation Dialog */}
      <ConfirmDialog
        isOpen={!!restoreCandidate}
        title={`Restore ${activeTab === 'products' ? 'Product' : 'Category'}`}
        message={`Are you sure you want to restore "${restoreCandidate?.name}" back to the live catalog?`}
        confirmText="Restore"
        isDanger={false}
        onConfirm={handleConfirmRestore}
        onCancel={() => setRestoreCandidate(null)}
      />

      {/* Hard Delete Purge Confirmation Dialog */}
      <ConfirmDialog
        isOpen={!!hardDeleteCandidate}
        title={`Permanent Purge Warning`}
        message={`WARNING: This will permanently remove "${hardDeleteCandidate?.name}" from SQL Server! If this record is referenced by any customer invoices or dependencies, the purge will be blocked.`}
        confirmText="Permanently Delete"
        isDanger={true}
        onConfirm={handleConfirmHardDelete}
        onCancel={() => setHardDeleteCandidate(null)}
      />
    </div>
  );
};
