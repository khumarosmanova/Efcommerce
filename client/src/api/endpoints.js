import api from './axios';

export const authApi = {
  login: async (credentials) => {
    const res = await api.post('/auth/login', credentials);
    return res.data;
  },
  register: async (data) => {
    const res = await api.post('/auth/register', data);
    return res.data;
  },
  getCurrentUser: async () => {
    const res = await api.get('/auth/me');
    return res.data;
  },
  refreshToken: async (payload) => {
    const res = await api.post('/auth/refresh', payload);
    return res.data;
  },
};

export const productsApi = {
  getAll: async (params) => {
    const res = await api.get('/products', { params });
    return res.data;
  },
  getAllAdmin: async (params) => {
    const res = await api.get('/products/admin', { params });
    return res.data;
  },
  getById: async (id) => {
    const res = await api.get(`/products/${id}`);
    return res.data;
  },
  getBySlug: async (slug) => {
    const res = await api.get(`/products/slug/${slug}`);
    return res.data;
  },
  create: async (data) => {
    const res = await api.post('/products', data);
    return res.data;
  },
  update: async (id, data) => {
    const res = await api.put(`/products/${id}`, data);
    return res.data;
  },
  delete: async (id) => {
    const res = await api.delete(`/products/${id}`);
    return res.data;
  },
  uploadImage: async (formData) => {
    const res = await api.post('/products/upload-image', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return res.data;
  },
};

export const categoriesApi = {
  getAll: async () => {
    const res = await api.get('/categories');
    return res.data;
  },
  getTree: async () => {
    const res = await api.get('/categories', { params: { tree: true } });
    return res.data;
  },
  getById: async (id) => {
    const res = await api.get(`/categories/${id}`);
    return res.data;
  },
  create: async (data) => {
    const res = await api.post('/categories', data);
    return res.data;
  },
  update: async (id, data) => {
    const res = await api.put(`/categories/${id}`, data);
    return res.data;
  },
  delete: async (id) => {
    const res = await api.delete(`/categories/${id}`);
    return res.data;
  },
};

export const cartApi = {
  getCart: async () => {
    const res = await api.get('/cart');
    return res.data;
  },
  addItem: async (item) => {
    const res = await api.post('/cart/items', item);
    return res.data;
  },
  updateItem: async (itemId, data) => {
    const res = await api.put(`/cart/items/${itemId}`, data);
    return res.data;
  },
  removeItem: async (itemId) => {
    const res = await api.delete(`/cart/items/${itemId}`);
    return res.data;
  },
  clearCart: async () => {
    const res = await api.delete('/cart');
    return res.data;
  },
};

export const ordersApi = {
  checkout: async (data) => {
    const res = await api.post('/orders/checkout', data);
    return res.data;
  },
  getMyOrders: async (page = 1, pageSize = 10) => {
    const res = await api.get('/orders/my', { params: { page, pageSize } });
    return res.data;
  },
  getMyOrderById: async (id) => {
    const res = await api.get(`/orders/my/${id}`);
    return res.data;
  },
  getAdminOrders: async (params) => {
    const res = await api.get('/admin/orders', { params });
    return res.data;
  },
  getAdminOrderById: async (id) => {
    const res = await api.get(`/admin/orders/${id}`);
    return res.data;
  },
  updateOrderStatus: async (id, status) => {
    const res = await api.put(`/admin/orders/${id}/status`, { status });
    return res.data;
  },
};

export const usersApi = {
  getUsers: async (params) => {
    const res = await api.get('/admin/users', { params });
    return res.data;
  },
  getUserById: async (id) => {
    const res = await api.get(`/admin/users/${id}`);
    return res.data;
  },
  updateRoles: async (id, roles) => {
    const res = await api.put(`/admin/users/${id}/roles`, { roles });
    return res.data;
  },
  updateStatus: async (id, isLocked) => {
    const res = await api.put(`/admin/users/${id}/status`, { isLocked });
    return res.data;
  },
};

export const rolesApi = {
  getRoles: async () => {
    const res = await api.get('/admin/roles');
    return res.data;
  },
  getRoleById: async (id) => {
    const res = await api.get(`/admin/roles/${id}`);
    return res.data;
  },
  getPermissionsGrouped: async () => {
    const res = await api.get('/admin/permissions');
    return res.data;
  },
  createRole: async (data) => {
    const res = await api.post('/admin/roles', data);
    return res.data;
  },
  updateRole: async (id, data) => {
    const res = await api.put(`/admin/roles/${id}`, data);
    return res.data;
  },
  deleteRole: async (id) => {
    const res = await api.delete(`/admin/roles/${id}`);
    return res.data;
  },
};

export const trashApi = {
  getDeletedProducts: async (params) => {
    const res = await api.get('/admin/trash/products', { params });
    return res.data;
  },
  getDeletedCategories: async (params) => {
    const res = await api.get('/admin/trash/categories', { params });
    return res.data;
  },
  restoreProduct: async (id) => {
    const res = await api.post(`/admin/trash/products/${id}/restore`);
    return res.data;
  },
  restoreCategory: async (id) => {
    const res = await api.post(`/admin/trash/categories/${id}/restore`);
    return res.data;
  },
  hardDeleteProduct: async (id) => {
    const res = await api.delete(`/admin/trash/products/${id}/hard-delete`);
    return res.data;
  },
  hardDeleteCategory: async (id) => {
    const res = await api.delete(`/admin/trash/categories/${id}/hard-delete`);
    return res.data;
  },
};

export const statisticsApi = {
  getSummary: async () => {
    const res = await api.get('/admin/statistics/summary');
    return res.data;
  },
  getSales: async (params) => {
    const res = await api.get('/admin/statistics/sales', { params });
    return res.data;
  },
  getTopProducts: async (top = 10) => {
    const res = await api.get('/admin/statistics/top-products', { params: { top } });
    return res.data;
  },
  getCategories: async () => {
    const res = await api.get('/admin/statistics/categories');
    return res.data;
  },
  getOrdersByStatus: async () => {
    const res = await api.get('/admin/statistics/orders-by-status');
    return res.data;
  },
};

export const reviewsApi = {
  getByProduct: async (productId) => {
    const res = await api.get(`/products/${productId}/reviews`);
    return res.data;
  },
  addReview: async (productId, data) => {
    const res = await api.post(`/products/${productId}/reviews`, data);
    return res.data;
  },
  deleteReview: async (id) => {
    const res = await api.delete(`/reviews/${id}`);
    return res.data;
  },
};

export const wishlistApi = {
  getAll: async () => {
    const res = await api.get('/wishlist');
    return res.data;
  },
  getIds: async () => {
    const res = await api.get('/wishlist/ids');
    return res.data;
  },
  toggle: async (productId) => {
    const res = await api.post(`/wishlist/${productId}/toggle`);
    return res.data;
  },
  add: async (productId) => {
    const res = await api.post(`/wishlist/${productId}`);
    return res.data;
  },
  remove: async (productId) => {
    const res = await api.delete(`/wishlist/${productId}`);
    return res.data;
  },
};

export const couponsApi = {
  getAll: async () => {
    const res = await api.get('/admin/coupons');
    return res.data;
  },
  getById: async (id) => {
    const res = await api.get(`/admin/coupons/${id}`);
    return res.data;
  },
  create: async (data) => {
    const res = await api.post('/admin/coupons', data);
    return res.data;
  },
  update: async (id, data) => {
    const res = await api.put(`/admin/coupons/${id}`, data);
    return res.data;
  },
  delete: async (id) => {
    const res = await api.delete(`/admin/coupons/${id}`);
    return res.data;
  },
  validate: async (code, amount) => {
    const res = await api.get('/coupons/validate', { params: { code, amount } });
    return res.data;
  },
};

