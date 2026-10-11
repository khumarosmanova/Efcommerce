import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { rolesApi } from '../../api/endpoints';
import { DataTable } from '../../components/admin/DataTable';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';
import { useAuth } from '../../context/AuthContext';
import {
  ShieldCheck,
  Plus,
  Edit2,
  Trash2,
  X,
  ShieldAlert,
  Users,
  CheckSquare,
  Square,
} from 'lucide-react';
import toast from 'react-hot-toast';

export const AdminRolesPage = () => {
  const { hasPermission } = useAuth();
  const queryClient = useQueryClient();

  const [modalOpen, setModalOpen] = useState(false);
  const [editingRole, setEditingRole] = useState(null);
  const [deleteCandidate, setDeleteCandidate] = useState(null);

  // Form states
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [selectedPermissions, setSelectedPermissions] = useState([]);

  // Fetch Roles
  const { data: rolesData, isLoading: loadingRoles } = useQuery({
    queryKey: ['admin-roles'],
    queryFn: () => rolesApi.getRoles(),
  });

  const roles = rolesData?.data || [];

  // Fetch Grouped Permissions Matrix
  const { data: permissionsData } = useQuery({
    queryKey: ['admin-permissions-matrix'],
    queryFn: () => rolesApi.getPermissionsGrouped(),
  });

  const permissionModules = permissionsData?.data || [];

  // Mutations
  const createMutation = useMutation({
    mutationFn: (data) => rolesApi.createRole(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-roles'] });
      toast.success('Role created successfully');
      handleCloseModal();
    },
    onError: (err) => {
      const msg = err.response?.data?.message || 'Failed to create role';
      toast.error(msg);
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }) => rolesApi.updateRole(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-roles'] });
      toast.success('Role updated successfully');
      handleCloseModal();
    },
    onError: (err) => {
      const msg = err.response?.data?.message || 'Failed to update role';
      toast.error(msg);
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id) => rolesApi.deleteRole(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-roles'] });
      toast.success('Role deleted successfully');
      setDeleteCandidate(null);
    },
    onError: (err) => {
      const msg = err.response?.data?.message || 'Cannot delete role with assigned users';
      toast.error(msg);
    },
  });

  const handleOpenCreate = () => {
    setEditingRole(null);
    setName('');
    setDescription('');
    setSelectedPermissions([]);
    setModalOpen(true);
  };

  const handleOpenEdit = (r) => {
    setEditingRole(r);
    setName(r.name);
    setDescription(r.description || '');
    setSelectedPermissions(r.permissions || []);
    setModalOpen(true);
  };

  const handleCloseModal = () => {
    setModalOpen(false);
    setEditingRole(null);
  };

  const togglePermission = (perm) => {
    setSelectedPermissions((prev) =>
      prev.includes(perm) ? prev.filter((p) => p !== perm) : [...prev, perm]
    );
  };

  const toggleModulePermissions = (modulePerms) => {
    const allSelected = modulePerms.every((p) => selectedPermissions.includes(p));
    if (allSelected) {
      setSelectedPermissions((prev) => prev.filter((p) => !modulePerms.includes(p)));
    } else {
      const merged = new Set([...selectedPermissions, ...modulePerms]);
      setSelectedPermissions(Array.from(merged));
    }
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    const payload = {
      name,
      description: description || null,
      permissions: selectedPermissions,
    };

    if (editingRole) {
      updateMutation.mutate({ id: editingRole.id, data: payload });
    } else {
      createMutation.mutate(payload);
    }
  };

  const columns = [
    {
      header: 'Role Name',
      accessor: (row) => (
        <div className="flex items-center gap-3">
          <div className="w-9 h-9 rounded-xl bg-purple-50 text-purple-600 flex items-center justify-center font-bold shrink-0">
            <ShieldCheck className="w-5 h-5" />
          </div>
          <div>
            <p className="font-bold text-slate-900">{row.name}</p>
            <p className="text-[11px] text-slate-400">{row.description || 'No description'}</p>
          </div>
        </div>
      ),
    },
    {
      header: 'Assigned Users',
      accessor: (row) => (
        <span className="inline-flex items-center gap-1.5 text-xs font-bold text-slate-700 bg-slate-100 px-2.5 py-1 rounded-lg">
          <Users className="w-3.5 h-3.5 text-slate-400" />
          {row.userCount || 0}
        </span>
      ),
    },
    {
      header: 'Granted Permissions',
      accessor: (row) => (
        <div className="flex flex-wrap gap-1 max-w-md">
          {row.name === 'SuperAdmin' ? (
            <span className="text-[10px] font-bold px-2 py-0.5 rounded-full bg-purple-100 text-purple-800">
              * (All System Permissions)
            </span>
          ) : row.permissions && row.permissions.length > 0 ? (
            row.permissions.slice(0, 4).map((p) => (
              <span
                key={p}
                className="text-[10px] font-semibold px-2 py-0.5 rounded-full bg-slate-100 text-slate-700 font-mono"
              >
                {p}
              </span>
            ))
          ) : (
            <span className="text-xs text-slate-400">None</span>
          )}
          {row.name !== 'SuperAdmin' && row.permissions && row.permissions.length > 4 && (
            <span className="text-[10px] font-bold text-slate-400 px-1">
              +{row.permissions.length - 4} more
            </span>
          )}
        </div>
      ),
    },
    {
      header: 'Actions',
      className: 'text-right',
      accessor: (row) => {
        const isSuperAdmin = row.name === 'SuperAdmin';

        return (
          <div className="flex items-center justify-end gap-1.5">
            {hasPermission('Role.Manage') && (
              <>
                <button
                  onClick={() => handleOpenEdit(row)}
                  disabled={isSuperAdmin}
                  className="p-1.5 hover:bg-slate-100 text-slate-600 rounded-lg transition disabled:opacity-30 disabled:cursor-not-allowed"
                  title={isSuperAdmin ? 'SuperAdmin is immutable' : 'Edit Role'}
                >
                  <Edit2 className="w-4 h-4" />
                </button>
                <button
                  onClick={() => setDeleteCandidate(row)}
                  disabled={isSuperAdmin || row.userCount > 0}
                  className="p-1.5 hover:bg-red-50 text-red-500 rounded-lg transition disabled:opacity-30 disabled:cursor-not-allowed"
                  title={
                    isSuperAdmin
                      ? 'SuperAdmin cannot be deleted'
                      : row.userCount > 0
                      ? 'Role still has assigned users'
                      : 'Delete Role'
                  }
                >
                  <Trash2 className="w-4 h-4" />
                </button>
              </>
            )}
          </div>
        );
      },
    },
  ];

  return (
    <div className="space-y-6">
      {/* Top Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-extrabold text-slate-900 tracking-tight">
            Roles & Permissions Matrix
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Configure permission claims grouped by business modules.
          </p>
        </div>

        {hasPermission('Role.Manage') && (
          <button
            onClick={handleOpenCreate}
            className="px-4 py-2.5 bg-emerald-600 hover:bg-emerald-500 text-white font-bold text-xs rounded-xl shadow-xs transition flex items-center gap-1.5"
          >
            <Plus className="w-4 h-4" /> Create Role
          </button>
        )}
      </div>

      {/* Roles Table */}
      <DataTable
        columns={columns}
        data={roles}
        isLoading={loadingRoles}
        totalCount={roles.length}
        emptyTitle="No roles found"
        emptyDescription="Add security roles to govern user access."
      />

      {/* Create / Edit Role Modal with Permission Matrix */}
      {modalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/50 backdrop-blur-xs animate-in fade-in">
          <div className="bg-white rounded-3xl max-w-2xl w-full p-6 sm:p-8 shadow-2xl border border-slate-100 max-h-[90vh] overflow-y-auto space-y-6">
            <div className="flex items-center justify-between pb-4 border-b border-slate-100">
              <h3 className="font-bold text-slate-900 text-lg">
                {editingRole ? `Edit Role: ${editingRole.name}` : 'Create Security Role'}
              </h3>
              <button onClick={handleCloseModal} className="p-1 text-slate-400 hover:text-slate-600">
                <X className="w-5 h-5" />
              </button>
            </div>

            <form onSubmit={handleSubmit} className="space-y-5 text-xs">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block font-bold text-slate-700 uppercase tracking-wider mb-1">
                    Role Name *
                  </label>
                  <input
                    type="text"
                    required
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    placeholder="e.g. InventoryManager"
                    className="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-none focus:border-emerald-500 text-sm"
                  />
                </div>

                <div>
                  <label className="block font-bold text-slate-700 uppercase tracking-wider mb-1">
                    Description
                  </label>
                  <input
                    type="text"
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    placeholder="Brief responsibility description"
                    className="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-200 rounded-xl focus:bg-white focus:outline-none focus:border-emerald-500 text-sm"
                  />
                </div>
              </div>

              {/* Permission Matrix */}
              <div className="space-y-3">
                <p className="font-bold text-slate-900 uppercase tracking-wider text-xs">
                  Permissions Matrix (Grouped by Module)
                </p>

                <div className="space-y-4">
                  {permissionModules.map((module) => {
                    const modulePerms = module.permissions || [];
                    const allSelected = modulePerms.every((p) =>
                      selectedPermissions.includes(p)
                    );

                    return (
                      <div
                        key={module.module}
                        className="bg-slate-50 p-4 rounded-2xl border border-slate-200/80 space-y-3"
                      >
                        <div className="flex items-center justify-between">
                          <span className="font-extrabold text-slate-900 text-sm">
                            {module.module} Module
                          </span>
                          <button
                            type="button"
                            onClick={() => toggleModulePermissions(modulePerms)}
                            className="text-[11px] font-bold text-emerald-700 hover:underline"
                          >
                            {allSelected ? 'Deselect All' : 'Select All'}
                          </button>
                        </div>

                        <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                          {modulePerms.map((perm) => {
                            const isChecked = selectedPermissions.includes(perm);
                            return (
                              <label
                                key={perm}
                                className={`flex items-center gap-2 p-2 rounded-xl border transition cursor-pointer ${
                                  isChecked
                                    ? 'bg-emerald-50 border-emerald-300 text-emerald-900 font-bold'
                                    : 'bg-white border-slate-200 text-slate-700 font-medium'
                                }`}
                              >
                                <input
                                  type="checkbox"
                                  checked={isChecked}
                                  onChange={() => togglePermission(perm)}
                                  className="w-4 h-4 text-emerald-600 rounded border-slate-300"
                                />
                                <span className="font-mono text-xs">{perm}</span>
                              </label>
                            );
                          })}
                        </div>
                      </div>
                    );
                  })}
                </div>
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
                  {createMutation.isPending || updateMutation.isPending ? 'Saving...' : 'Save Role'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Delete Role Confirmation */}
      <ConfirmDialog
        isOpen={!!deleteCandidate}
        title="Delete Role"
        message={`Permanently delete role "${deleteCandidate?.name}"?`}
        confirmText="Delete Role"
        onConfirm={() => deleteMutation.mutate(deleteCandidate.id)}
        onCancel={() => setDeleteCandidate(null)}
      />
    </div>
  );
};
