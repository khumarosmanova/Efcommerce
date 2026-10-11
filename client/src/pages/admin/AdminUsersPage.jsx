import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { usersApi, rolesApi } from '../../api/endpoints';
import { DataTable } from '../../components/admin/DataTable';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';
import { useAuth } from '../../context/AuthContext';
import { formatDate } from '../../utils/storage';
import {
  Users,
  Search,
  Shield,
  Lock,
  Unlock,
  Check,
  X,
  UserCheck,
} from 'lucide-react';
import toast from 'react-hot-toast';

export const AdminUsersPage = () => {
  const { user: currentLoggedUser, hasPermission } = useAuth();
  const queryClient = useQueryClient();

  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [roleFilter, setRoleFilter] = useState('');

  // Role Assignment Modal state
  const [selectedUserForRoles, setSelectedUserForRoles] = useState(null);
  const [assignedRoleNames, setAssignedRoleNames] = useState([]);

  // Lock Confirmation State
  const [lockCandidate, setLockCandidate] = useState(null);

  // Fetch users
  const { data: usersData, isLoading } = useQuery({
    queryKey: ['admin-users', { page, search, roleFilter }],
    queryFn: () =>
      usersApi.getUsers({
        page,
        pageSize: 10,
        search: search || undefined,
        role: roleFilter || undefined,
      }),
  });

  const pagedResult = usersData?.data || { items: [], totalCount: 0 };
  const users = pagedResult.items || [];

  // Fetch available roles
  const { data: rolesData } = useQuery({
    queryKey: ['admin-roles-list'],
    queryFn: () => rolesApi.getRoles(),
  });

  const availableRoles = rolesData?.data || [];

  // Update roles mutation
  const updateRolesMutation = useMutation({
    mutationFn: ({ id, roles }) => usersApi.updateRoles(id, roles),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-users'] });
      toast.success('Roles assigned successfully');
      setSelectedUserForRoles(null);
    },
    onError: (err) => {
      const msg = err.response?.data?.message || 'Could not update user roles';
      toast.error(msg);
    },
  });

  // Lock/Unlock mutation
  const updateStatusMutation = useMutation({
    mutationFn: ({ id, isLocked }) => usersApi.updateStatus(id, isLocked),
    onSuccess: (res) => {
      queryClient.invalidateQueries({ queryKey: ['admin-users'] });
      toast.success(res.message || 'User status updated');
      setLockCandidate(null);
    },
    onError: (err) => {
      const msg = err.response?.data?.message || 'Could not update user status';
      toast.error(msg);
    },
  });

  const handleOpenRoleModal = (u) => {
    setSelectedUserForRoles(u);
    setAssignedRoleNames(u.roles || []);
  };

  const handleToggleRoleSelection = (roleName) => {
    setAssignedRoleNames((prev) =>
      prev.includes(roleName)
        ? prev.filter((r) => r !== roleName)
        : [...prev, roleName]
    );
  };

  const handleSaveRoles = (e) => {
    e.preventDefault();
    if (!selectedUserForRoles) return;
    updateRolesMutation.mutate({
      id: selectedUserForRoles.id,
      roles: assignedRoleNames,
    });
  };

  const columns = [
    {
      header: 'User',
      accessor: (row) => (
        <div className="flex items-center gap-3">
          <div className="w-9 h-9 rounded-xl bg-slate-100 text-slate-700 font-bold text-xs flex items-center justify-center shrink-0 border border-slate-200">
            {row.firstName?.[0] || 'U'}
          </div>
          <div>
            <p className="font-bold text-slate-900">
              {row.firstName} {row.lastName}
            </p>
            <p className="text-[11px] text-slate-400">{row.email}</p>
          </div>
        </div>
      ),
    },
    {
      header: 'Assigned Roles',
      accessor: (row) => (
        <div className="flex flex-wrap gap-1">
          {row.roles?.map((r) => (
            <span
              key={r}
              className={`text-[10px] font-bold px-2 py-0.5 rounded-full ${
                r === 'SuperAdmin'
                  ? 'bg-purple-100 text-purple-800'
                  : r === 'Admin'
                  ? 'bg-blue-100 text-blue-800'
                  : r === 'Moderator'
                  ? 'bg-amber-100 text-amber-800'
                  : 'bg-slate-100 text-slate-700'
              }`}
            >
              {r}
            </span>
          ))}
        </div>
      ),
    },
    {
      header: 'Account Status',
      accessor: (row) => (
        <span
          className={`inline-flex items-center gap-1 text-[11px] font-bold px-2 py-0.5 rounded-full ${
            row.isLocked
              ? 'bg-rose-50 text-rose-700 border border-rose-200'
              : 'bg-emerald-50 text-emerald-700 border border-emerald-200'
          }`}
        >
          {row.isLocked ? <Lock className="w-3 h-3" /> : <Unlock className="w-3 h-3" />}
          {row.isLocked ? 'Locked' : 'Active'}
        </span>
      ),
    },
    {
      header: 'Created At',
      accessor: (row) => (
        <span className="text-xs text-slate-400">{formatDate(row.createdAt)}</span>
      ),
    },
    {
      header: 'Actions',
      className: 'text-right',
      accessor: (row) => {
        const isSelf = row.email === currentLoggedUser?.email;

        return (
          <div className="flex items-center justify-end gap-1.5">
            {hasPermission('User.Manage') && (
              <>
                <button
                  onClick={() => handleOpenRoleModal(row)}
                  className="px-2.5 py-1.5 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-lg text-xs font-bold transition flex items-center gap-1"
                  title="Assign Roles"
                >
                  <Shield className="w-3.5 h-3.5" /> Roles
                </button>

                <button
                  onClick={() => setLockCandidate(row)}
                  disabled={isSelf}
                  className={`p-1.5 rounded-lg transition ${
                    row.isLocked
                      ? 'hover:bg-emerald-50 text-emerald-600'
                      : 'hover:bg-rose-50 text-rose-600'
                  } disabled:opacity-30 disabled:cursor-not-allowed`}
                  title={isSelf ? 'Cannot lock yourself' : row.isLocked ? 'Unlock User' : 'Lock User'}
                >
                  {row.isLocked ? <Unlock className="w-4 h-4" /> : <Lock className="w-4 h-4" />}
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
            User Accounts
          </h1>
          <p className="text-xs text-slate-500 mt-1">
            Manage client and staff accounts, assign security roles, and control lock status.
          </p>
        </div>
      </div>

      {/* Filter and Search Bar */}
      <div className="bg-white p-4 rounded-3xl border border-slate-200/80 shadow-xs flex flex-wrap items-center justify-between gap-3">
        <div className="relative w-full sm:w-64">
          <input
            type="text"
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
            placeholder="Search email, name..."
            className="w-full pl-9 pr-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-xs focus:bg-white focus:outline-none focus:border-emerald-500"
          />
          <Search className="w-4 h-4 text-slate-400 absolute left-3 top-2.5" />
        </div>

        <select
          value={roleFilter}
          onChange={(e) => {
            setRoleFilter(e.target.value);
            setPage(1);
          }}
          className="px-3 py-2 bg-slate-50 border border-slate-200 rounded-xl text-xs text-slate-700 focus:bg-white focus:outline-none focus:border-emerald-500"
        >
          <option value="">All Roles</option>
          {availableRoles.map((r) => (
            <option key={r.id} value={r.name}>
              {r.name}
            </option>
          ))}
        </select>
      </div>

      {/* Users Table */}
      <DataTable
        columns={columns}
        data={users}
        isLoading={isLoading}
        totalCount={pagedResult.totalCount}
        currentPage={page}
        pageSize={10}
        onPageChange={(p) => setPage(p)}
        emptyTitle="No users found"
        emptyDescription="No registered users matching this query."
      />

      {/* Assign Roles Modal */}
      {selectedUserForRoles && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/50 backdrop-blur-xs animate-in fade-in">
          <div className="bg-white rounded-3xl max-w-md w-full p-6 sm:p-8 shadow-2xl border border-slate-100 space-y-6">
            <div className="flex items-center justify-between pb-4 border-b border-slate-100">
              <div>
                <h3 className="font-bold text-slate-900 text-lg">Assign Roles</h3>
                <p className="text-xs text-slate-500">{selectedUserForRoles.email}</p>
              </div>
              <button
                onClick={() => setSelectedUserForRoles(null)}
                className="p-1 text-slate-400 hover:text-slate-600 rounded-lg"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <form onSubmit={handleSaveRoles} className="space-y-4 text-xs">
              <div className="space-y-2">
                <p className="font-bold text-slate-700 uppercase tracking-wider">Select Roles</p>
                <div className="space-y-2">
                  {availableRoles.map((r) => {
                    const isChecked = assignedRoleNames.includes(r.name);
                    return (
                      <label
                        key={r.id}
                        className={`flex items-center justify-between p-3 rounded-2xl border transition cursor-pointer ${
                          isChecked
                            ? 'bg-emerald-50/50 border-emerald-300 text-emerald-950 font-bold'
                            : 'bg-slate-50 border-slate-200 text-slate-700 font-medium'
                        }`}
                      >
                        <div className="flex items-center gap-2">
                          <input
                            type="checkbox"
                            checked={isChecked}
                            onChange={() => handleToggleRoleSelection(r.name)}
                            className="w-4 h-4 text-emerald-600 rounded border-slate-300"
                          />
                          <span>{r.name}</span>
                        </div>
                        <span className="text-[11px] text-slate-400">{r.description || ''}</span>
                      </label>
                    );
                  })}
                </div>
              </div>

              <div className="flex items-center justify-end gap-2 pt-4 border-t border-slate-100">
                <button
                  type="button"
                  onClick={() => setSelectedUserForRoles(null)}
                  className="px-4 py-2 border border-slate-200 rounded-xl text-slate-600 font-bold hover:bg-slate-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={updateRolesMutation.isPending}
                  className="px-5 py-2 bg-emerald-600 hover:bg-emerald-500 text-white font-bold rounded-xl shadow-xs"
                >
                  {updateRolesMutation.isPending ? 'Saving...' : 'Save Roles'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Lock/Unlock Confirmation */}
      <ConfirmDialog
        isOpen={!!lockCandidate}
        title={lockCandidate?.isLocked ? 'Unlock Account' : 'Lock Account'}
        message={
          lockCandidate?.isLocked
            ? `Re-enable account login for "${lockCandidate?.email}"?`
            : `Lock "${lockCandidate?.email}" out? Their active refresh token will be revoked immediately.`
        }
        confirmText={lockCandidate?.isLocked ? 'Unlock' : 'Lock Account'}
        isDanger={!lockCandidate?.isLocked}
        onConfirm={() =>
          updateStatusMutation.mutate({
            id: lockCandidate.id,
            isLocked: !lockCandidate.isLocked,
          })
        }
        onCancel={() => setLockCandidate(null)}
      />
    </div>
  );
};
