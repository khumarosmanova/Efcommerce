import React, { createContext, useContext, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate, useLocation } from 'react-router-dom';
import { cartApi } from '../api/endpoints';
import { useAuth } from './AuthContext';
import { useTranslation } from './LanguageContext';
import toast from 'react-hot-toast';

const CartContext = createContext(null);

export const CartProvider = ({ children }) => {
  const { isAuthenticated, isAdmin } = useAuth();
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const location = useLocation();

  // Clear query cache when not authenticated or admin so stale cart data doesn't linger
  useEffect(() => {
    if (!isAuthenticated || isAdmin) {
      queryClient.setQueryData(['cart'], null);
      queryClient.removeQueries({ queryKey: ['cart'] });
    }
  }, [isAuthenticated, isAdmin, queryClient]);

  const { data: cartData, isLoading } = useQuery({
    queryKey: ['cart'],
    queryFn: async () => {
      const res = await cartApi.getCart();
      return res.data;
    },
    enabled: !!isAuthenticated && !isAdmin,
    staleTime: 1000 * 60 * 2,
  });

  const cart = isAuthenticated && !isAdmin ? (cartData || { items: [] }) : { items: [] };
  const itemCount = isAuthenticated && !isAdmin ? (cart.items?.reduce((total, item) => total + item.quantity, 0) || 0) : 0;
  const totalAmount = isAuthenticated && !isAdmin ? (cart.items?.reduce((total, item) => total + item.unitPrice * item.quantity, 0) || 0) : 0;

  const addItemMutation = useMutation({
    mutationFn: (item) => cartApi.addItem(item),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['cart'] });
      toast.success('Added to cart!');
    },
    onError: (err) => {
      const msg = err.response?.data?.message || 'Could not add item to cart';
      toast.error(msg);
    },
  });

  const updateItemMutation = useMutation({
    mutationFn: ({ itemId, quantity }) => cartApi.updateItem(itemId, { quantity }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['cart'] });
    },
    onError: (err) => {
      const msg = err.response?.data?.message || 'Could not update quantity';
      toast.error(msg);
    },
  });

  const removeItemMutation = useMutation({
    mutationFn: (itemId) => cartApi.removeItem(itemId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['cart'] });
      toast.success('Item removed');
    },
    onError: (err) => {
      const msg = err.response?.data?.message || 'Could not remove item';
      toast.error(msg);
    },
  });

  const clearCartMutation = useMutation({
    mutationFn: () => cartApi.clearCart(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['cart'] });
      toast.success('Cart cleared');
    },
  });

  const addItem = (productId, quantity = 1) => {
    if (isAdmin) {
      toast.error(t('adminCannotCart'));
      return;
    }
    if (!isAuthenticated) {
      toast('Please log in to add items to your cart', { icon: '🔒' });
      navigate('/login', { state: { from: location } });
      return;
    }
    addItemMutation.mutate({ productId, quantity });
  };

  const updateQuantity = (itemId, quantity) => {
    if (quantity <= 0) {
      removeItemMutation.mutate(itemId);
    } else {
      updateItemMutation.mutate({ itemId, quantity });
    }
  };

  const value = {
    cart,
    itemCount,
    totalAmount,
    isLoading,
    addItem,
    updateQuantity,
    removeItem: (itemId) => removeItemMutation.mutate(itemId),
    clearCart: () => clearCartMutation.mutate(),
  };

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>;
};

export const useCart = () => {
  const context = useContext(CartContext);
  if (!context) {
    throw new Error('useCart must be used within a CartProvider');
  }
  return context;
};
