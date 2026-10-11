import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { categoriesApi } from '../../api/endpoints';
import { DataTable } from '../../components/admin/DataTable';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';
import { useAuth } from '../../context/AuthContext';
import {
  Layers,
  Plus,
  Edit2,
  Trash2,
  X,
  FolderTree,
  ChevronRight,
  Folder,
} from 'lucide-react';
import toast from 'react-hot-toast';

export const AdminCategoriesPage = () => {
  const { hasPermission } = useAuth();
  const queryClient = useQueryClient();

  const [modalOpen, setModalOpen] = useState(false);
  const [editingCategory, setEditingCategory] = useState(null);
  const [deleteCandidate, setDeleteCandidate] = useState(null);

  // Form states
  const [name, setName] = useState('');
  const [slug, setSlug] = useState('');
  const [description, setDescription] = useState('');
  const [imageUrl, setImageUrl] = useState('');
  const [parentCategoryId, setParentCategoryId] = useState('');

  // Fetch flat categories list
  const { data: categoriesData, isLoading } = useQuery({
    queryKey: ['admin-categories-all'],
    queryFn: () => categoriesApi.getAll(),
  });

  const categories = categoriesData?.data || [];

  // Mutations
  const createMutation = useMutation({
    mutationFn: (data) => categoriesApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-categories-all'] });
      queryClient.invalidateQueries({ queryKey: ['categories-tree'] });
      toast.success('Category created successfully');
      handleCloseModal();
    },
    onError: (err) => {
      const msg = err.response?.data?.message || 'Failed to create category';
      toast.error(msg);
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }) => categoriesApi.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-categories-all'] });
      queryClient.invalidateQueries({ queryKey: ['categories-tree'] });
      toast.success('Category updated successfully');
      handleCloseModal();
    },
    onError: (err) => {
      const msg = err.response?.data?.message || 'Failed to update category';
      toast.error(msg);
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id) => categoriesApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-categories-all'] });
      queryClient.invalidateQueries({ queryKey: ['categories-tree'] });
      queryClient.invalidateQueries({ queryKey: ['admin-trash-categories'] });
      toast.success('Category moved to trash (and products cascade-hidden)');
      setDeleteCandidate(null);
    },
    onError: (err) => {
      const msg = err.response?.data?.message || 'Failed to delete category';
      toast.error(msg);
    },
  });

  const handleOpenCreate = () => {
    setEditingCategory(null);
    setName('');
    setSlug('');
    setDescription('');
    setImageUrl('');
    setParentCategoryId('');
    setModalOpen(true);
  };

  const handleOpenEdit = (cat) => {
    setEditingCategory(cat);
    setName(cat.name);
    setSlug(cat.slug || '');
    setDescription(cat.description || '');
    setImageUrl(cat.imageUrl || '');
    setParentCategoryId(cat.parentCategoryId ? cat.parentCategoryId.toString() : '');
    setModalOpen(true);
  };

  const handleCloseModal = () => {
    setModalOpen(false);
    setEditingCategory(null);
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    const payload = {
      name,
      slug: slug || null,
      description: description || null,
      imageUrl: imageUrl || null,
      parentCategoryId: parentCategoryId ? parseInt(parentCategoryId, 10) : null,
    };

    if (editingCategory) {
      updateMutation.mutate({ id: editingCategory.id, data: payload });
    } else {
      createMutation.mutate(payload);
    }
  };

  const columns = [
    {
      header: 'Category Name',
      accessor: (row) => (
        <div className="flex items-center gap-3">
          <div className="w-9 h-9 rounded-xl bg-emerald-50 text-emerald-600 flex items-center justify-center font-bold shrink-0">
            {row.parentCategoryId ? <Folder className="w-4 h-4" /> : <Layers className="w-4 h-4" />}
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
      accessor: (row) => {
        const parent = categories.find((c) => c.id === row.parentCategoryId);
        return parent ? (
          <span className="text-xs font-semibold px-2.5 py-1 bg-slate-100 text-slate-700 rounded-lg">
            {parent.name}
          </span>
        ) : (
          <span className="text-xs text-slate-400 font-medium">Root Department</span>
        );
      },
    },
    {
      header: 'Products Count',
      accessor: (row) => (
        <span className="text-xs font-bold text-slate-700 font-mono">
          {row.productCount || 0}
        </span>
      ),
    },
    {
      header: 'Description',
      accessor: (row) => (
        <p className="text-xs text-slate-500 truncate max-w-xs">
          {row.description || '-'}
        </p>
      ),
    },
    {
      header: 'Actions',
      className: 'text-right',
      accessor: (row) => (
        <div className="flex items-center justify-end gap-1.5">
          {hasPermission('Category.Manage') && (
            <>
              <button
                onClick={() => handleOpenEdit(row)}
                className="p-1.5 hover:bg-slate-100 text-slate-600 rounded-lg transition"
                title="Edit Category"
              >
                <Edit2 className="w-4 h-4" />
              </button>
              <button
                onClick={() => setDeleteCandidate(row)}
                className="p-1.5 hover:bg-red-50 text-red-500 rounded-lg transition"
                title="Soft Delete Category"
              >
                <Trash2 className="w-4 h-4" />
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
            Departments & Categories
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Organize root departments, hierarchy, and navigation trees.
          </p>
        </div>

        {hasPermission('Category.Manage') && (
          <button
            onClick={handleOpenCreate}
            className="px-4 py-2.5 bg-emerald-600 hover:bg-emerald-500 text-white font-bold text-xs rounded-xl shadow-xs transition flex items-center gap-1.5"
          >
            <Plus className="w-4 h-4" /> New Category
          </button>
        )}
      </div>

      {/* Categories Table */}
      <DataTable
        columns={columns}
        data={categories}
        isLoading={isLoading}
        totalCount={categories.length}
        emptyTitle="No categories found"
        emptyDescription="Create your first catalog department."
      />

      {/* Create / Edit Modal */}
      {modalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/50 backdrop-blur-xs animate-in fade-in">
          <div className="bg-white rounded-3xl max-w-md w-full p-6 sm:p-8 shadow-2xl border border-slate-100 space-y-6">
            <div className="flex items-center justify-between pb-4 border-b border-slate-100">
              <h3 className="font-bold text-slate-900 text-lg">
                {editingCategory ? 'Edit Category' : 'Create Category'}
              </h3>
              <button onClick={handleCloseModal} className="p-1 text-slate-400 hover:text-slate-600">
                <X className="w-5 h-5" />
              </button>
            </div>

            <form onSubmit={handleSubmit} className="space-y-4 text-xs">
              <div>
                <label className="block font-bold text-slate-700 uppercase tracking-wider mb-1">
                  Category Name *
                </label>
                <input
                  type="text"
                  required
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder="e.g. Smart Wearables"
                  className="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-none focus:border-emerald-500 text-sm"
                />
              </div>

              <div>
                <label className="block font-bold text-slate-700 uppercase tracking-wider mb-1">
                  Parent Department (Optional)
                </label>
                <select
                  value={parentCategoryId}
                  onChange={(e) => setParentCategoryId(e.target.value)}
                  className="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-none focus:border-emerald-500 text-sm"
                >
                  <option value="">None (Top-Level Category)</option>
                  {categories
                    .filter((c) => !editingCategory || c.id !== editingCategory.id)
                    .map((c) => (
                      <option key={c.id} value={c.id}>
                        {c.name}
                      </option>
                    ))}
                </select>
              </div>

              <div>
                <label className="block font-bold text-slate-700 uppercase tracking-wider mb-1">
                  SEO Slug (Optional)
                </label>
                <input
                  type="text"
                  value={slug}
                  onChange={(e) => setSlug(e.target.value)}
                  placeholder="Leave empty to generate automatically"
                  className="w-full px-3.5 py-2 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-none focus:border-emerald-500 text-sm font-mono"
                />
              </div>

              <div>
                <label className="block font-bold text-slate-700 uppercase tracking-wider mb-1">
                  Description
                </label>
                <textarea
                  rows="3"
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder="Summary for catalog description..."
                  className="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-none focus:border-emerald-500 text-sm"
                />
              </div>

              <div className="flex items-center justify-end gap-2 pt-4 border-t border-slate-100">
                <button
                  type="button"
                  onClick={handleCloseModal}
                  className="px-4 py-2 border border-slate-200 rounded-xl text-slate-600 font-bold hover:bg-slate-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={createMutation.isPending || updateMutation.isPending}
                  className="px-5 py-2 bg-emerald-600 hover:bg-emerald-500 text-white font-bold rounded-xl shadow-xs"
                >
                  {createMutation.isPending || updateMutation.isPending ? 'Saving...' : 'Save Category'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Confirm Soft Delete */}
      <ConfirmDialog
        isOpen={!!deleteCandidate}
        title="Soft Delete Category"
        message={`Delete "${deleteCandidate?.name}"? Notice: all active products directly linked to this category will also be cascade soft-deleted.`}
        confirmText="Move to Trash"
        onConfirm={() => deleteMutation.mutate(deleteCandidate.id)}
        onCancel={() => setDeleteCandidate(null)}
      />
    </div>
  );
};
