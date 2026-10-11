import React, { useState } from 'react';
import { Outlet, NavLink, Link, useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import {
  BarChart3,
  Package,
  Layers,
  ShoppingBag,
  Users,
  ShieldCheck,
  Trash2,
  ChevronLeft,
  ChevronRight,
  Menu,
  X,
  LogOut,
  Store,
  Shield,
  Bell,
} from 'lucide-react';

export const AdminLayout = () => {
  const { user, hasPermission, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const [collapsed, setCollapsed] = useState(false);
  const [mobileDrawerOpen, setMobileDrawerOpen] = useState(false);

  // Navigation definition with required permissions for permission-based filtering
  const navItems = [
    {
      name: 'Statistics & Sales',
      path: '/admin/statistics',
      icon: BarChart3,
      permission: 'Statistics.View',
    },
    {
      name: 'Products',
      path: '/admin/products',
      icon: Package,
      permission: 'Product.Manage',
    },
    {
      name: 'Categories',
      path: '/admin/categories',
      icon: Layers,
      permission: 'Category.Manage',
    },
    {
      name: 'Orders',
      path: '/admin/orders',
      icon: ShoppingBag,
      permission: 'Order.Manage',
    },
    {
      name: 'Users',
      path: '/admin/users',
      icon: Users,
      permission: 'User.Manage',
    },
    {
      name: 'Roles & Permissions',
      path: '/admin/roles',
      icon: ShieldCheck,
      permission: 'Role.Manage',
    },
    {
      name: 'Trash Bin',
      path: '/admin/trash',
      icon: Trash2,
      permission: 'Trash.Manage',
    },
  ];

  // Filter items matching user permissions or SuperAdmin
  const authorizedNavItems = navItems.filter((item) => hasPermission(item.permission));

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <div className="min-h-screen bg-slate-100 flex flex-col md:flex-row">
      {/* Mobile Drawer Backdrop */}
      {mobileDrawerOpen && (
        <div
          className="fixed inset-0 bg-slate-900/50 backdrop-blur-sm z-40 md:hidden animate-in fade-in"
          onClick={() => setMobileDrawerOpen(false)}
        />
      )}

      {/* Admin Sidebar */}
      <aside
        className={`fixed md:sticky top-0 h-screen z-50 bg-slate-900 text-slate-300 flex flex-col transition-all duration-300 border-r border-slate-800 ${
          collapsed ? 'md:w-20' : 'md:w-64'
        } ${
          mobileDrawerOpen ? 'translate-x-0 w-64' : '-translate-x-full md:translate-x-0'
        }`}
      >
        {/* Brand Header */}
        <div className="h-16 flex items-center justify-between px-4 border-b border-slate-800">
          <Link to="/admin/statistics" className="flex items-center gap-3 overflow-hidden">
            <div className="w-9 h-9 rounded-xl bg-emerald-600 flex items-center justify-center text-white shrink-0 shadow-sm shadow-emerald-500/20">
              <Shield className="w-5 h-5" />
            </div>
            {(!collapsed || mobileDrawerOpen) && (
              <span className="font-bold text-white text-base tracking-tight truncate">
                Admin<span className="text-emerald-500">Hub</span>
              </span>
            )}
          </Link>

          {/* Mobile Close Button */}
          <button
            onClick={() => setMobileDrawerOpen(false)}
            className="md:hidden p-1 rounded-lg text-slate-400 hover:text-white"
          >
            <X className="w-6 h-6" />
          </button>
        </div>

        {/* Navigation Menu */}
        <nav className="flex-1 py-4 px-3 space-y-1.5 overflow-y-auto">
          {authorizedNavItems.map((item) => {
            const Icon = item.icon;
            const isActive = location.pathname.startsWith(item.path);

            return (
              <NavLink
                key={item.path}
                to={item.path}
                onClick={() => setMobileDrawerOpen(false)}
                title={collapsed ? item.name : undefined}
                className={`flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium transition group ${
                  isActive
                    ? 'bg-emerald-600 text-white shadow-sm shadow-emerald-600/30'
                    : 'text-slate-400 hover:text-slate-100 hover:bg-slate-800/60'
                }`}
              >
                <Icon className={`w-5 h-5 shrink-0 transition ${isActive ? 'text-white' : 'text-slate-400 group-hover:text-slate-200'}`} />
                {(!collapsed || mobileDrawerOpen) && (
                  <span className="truncate">{item.name}</span>
                )}
              </NavLink>
            );
          })}
        </nav>

        {/* Sidebar Footer & Collapse Toggle */}
        <div className="p-3 border-t border-slate-800 space-y-2">
          {/* Storefront Link */}
          <Link
            to="/"
            className="flex items-center gap-3 px-3 py-2 rounded-xl text-sm font-medium text-slate-400 hover:text-slate-100 hover:bg-slate-800/60 transition"
            title="Go to Storefront"
          >
            <Store className="w-5 h-5 shrink-0" />
            {(!collapsed || mobileDrawerOpen) && <span>Storefront</span>}
          </Link>

          {/* Desktop Collapse Toggle */}
          <button
            onClick={() => setCollapsed(!collapsed)}
            className="hidden md:flex w-full items-center justify-center p-2 rounded-xl text-slate-400 hover:text-white hover:bg-slate-800 transition"
            title={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
          >
            {collapsed ? <ChevronRight className="w-5 h-5" /> : <ChevronLeft className="w-5 h-5" />}
          </button>
        </div>
      </aside>

      {/* Main Content Area */}
      <div className="flex-1 flex flex-col min-w-0">
        {/* Topbar */}
        <header className="sticky top-0 z-30 h-16 bg-white border-b border-slate-200 px-4 sm:px-6 flex items-center justify-between shadow-xs">
          {/* Left: Mobile hamburger */}
          <div className="flex items-center gap-3">
            <button
              onClick={() => setMobileDrawerOpen(true)}
              className="md:hidden p-2 rounded-lg text-slate-600 hover:bg-slate-100"
              aria-label="Open sidebar"
            >
              <Menu className="w-6 h-6" />
            </button>
            <h1 className="text-base sm:text-lg font-bold text-slate-800 truncate">
              Administration Portal
            </h1>
          </div>

          {/* Right: User and Actions */}
          <div className="flex items-center gap-3 sm:gap-4">
            {/* User Profile Pill */}
            <div className="flex items-center gap-2.5 px-3 py-1.5 bg-slate-50 border border-slate-200 rounded-xl">
              <div className="w-7 h-7 rounded-lg bg-emerald-100 text-emerald-800 font-bold text-xs flex items-center justify-center">
                {user?.firstName?.[0] || 'A'}
              </div>
              <div className="hidden sm:block text-left">
                <p className="text-xs font-semibold text-slate-800 leading-tight">
                  {user?.firstName} {user?.lastName}
                </p>
                <p className="text-[10px] text-emerald-600 font-medium leading-tight">
                  {user?.roles?.[0] || 'Admin'}
                </p>
              </div>
            </div>

            {/* Logout button */}
            <button
              onClick={handleLogout}
              className="p-2 text-slate-500 hover:text-red-600 hover:bg-red-50 rounded-xl transition"
              title="Log out"
            >
              <LogOut className="w-5 h-5" />
            </button>
          </div>
        </header>

        {/* Page Content */}
        <main className="flex-1 p-4 sm:p-6 lg:p-8 max-w-7xl w-full mx-auto">
          <Outlet />
        </main>
      </div>
    </div>
  );
};
