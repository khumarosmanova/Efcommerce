import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { PublicLayout } from '../layouts/PublicLayout';
import { AdminLayout } from '../layouts/AdminLayout';
import { ProtectedRoute } from './ProtectedRoute';
import { PermissionGuard } from './PermissionGuard';

// Public & Customer Pages
import { HomePage } from '../pages/public/HomePage';
import { ProductsPage } from '../pages/public/ProductsPage';
import { ProductDetailPage } from '../pages/public/ProductDetailPage';
import { CategoriesPage } from '../pages/public/CategoriesPage';
import { CartPage } from '../pages/public/CartPage';
import { CheckoutPage } from '../pages/public/CheckoutPage';
import { OrdersPage } from '../pages/public/OrdersPage';
import { OrderDetailPage } from '../pages/public/OrderDetailPage';
import { WishlistPage } from '../pages/public/WishlistPage';

// Auth Pages
import { LoginPage } from '../pages/auth/LoginPage';
import { RegisterPage } from '../pages/auth/RegisterPage';

// Admin Pages
import { StatisticsDashboardPage } from '../pages/admin/StatisticsDashboardPage';
import { AdminProductsPage } from '../pages/admin/AdminProductsPage';
import { AdminCategoriesPage } from '../pages/admin/AdminCategoriesPage';
import { AdminOrdersPage } from '../pages/admin/AdminOrdersPage';
import { AdminUsersPage } from '../pages/admin/AdminUsersPage';
import { AdminRolesPage } from '../pages/admin/AdminRolesPage';
import { AdminTrashPage } from '../pages/admin/AdminTrashPage';

export const AppRoutes = () => {
  return (
    <Routes>
      {/* Public Storefront Routes */}
      <Route element={<PublicLayout />}>
        <Route path="/" element={<HomePage />} />
        <Route path="/products" element={<ProductsPage />} />
        <Route path="/products/:id" element={<ProductDetailPage />} />
        <Route path="/categories" element={<CategoriesPage />} />
        <Route path="/cart" element={<CartPage />} />
        <Route path="/wishlist" element={<WishlistPage />} />

        {/* Customer Checkout & Order History (Protected) */}
        <Route
          path="/checkout"
          element={
            <ProtectedRoute>
              <CheckoutPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/orders"
          element={
            <ProtectedRoute>
              <OrdersPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/orders/:id"
          element={
            <ProtectedRoute>
              <OrderDetailPage />
            </ProtectedRoute>
          }
        />
      </Route>

      {/* Standalone Auth Pages */}
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />

      {/* Admin Protected Routes with Permission Guards */}
      <Route
        path="/admin"
        element={
          <ProtectedRoute>
            <AdminLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<Navigate to="/admin/statistics" replace />} />
        <Route
          path="statistics"
          element={
            <PermissionGuard permission="Statistics.View">
              <StatisticsDashboardPage />
            </PermissionGuard>
          }
        />
        <Route
          path="products"
          element={
            <PermissionGuard permission="Product.Manage">
              <AdminProductsPage />
            </PermissionGuard>
          }
        />
        <Route
          path="categories"
          element={
            <PermissionGuard permission="Category.Manage">
              <AdminCategoriesPage />
            </PermissionGuard>
          }
        />
        <Route
          path="orders"
          element={
            <PermissionGuard permission="Order.Manage">
              <AdminOrdersPage />
            </PermissionGuard>
          }
        />
        <Route
          path="users"
          element={
            <PermissionGuard permission="User.Manage">
              <AdminUsersPage />
            </PermissionGuard>
          }
        />
        <Route
          path="roles"
          element={
            <PermissionGuard permission="Role.Manage">
              <AdminRolesPage />
            </PermissionGuard>
          }
        />
        <Route
          path="trash"
          element={
            <PermissionGuard permission="Trash.Manage">
              <AdminTrashPage />
            </PermissionGuard>
          }
        />
      </Route>

      {/* Fallback */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
};
