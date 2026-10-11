import React, { useState } from 'react';
import { useCart } from '../../context/CartContext';
import { useAuth } from '../../context/AuthContext';
import { useTranslation } from '../../context/LanguageContext';
import { formatCurrency, getImageUrl } from '../../utils/storage';
import { EmptyState } from '../../components/common/EmptyState';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';
import { Link } from 'react-router-dom';
import {
  ShoppingCart,
  Trash2,
  ArrowRight,
  Minus,
  Plus,
  ShoppingBag,
  ShieldCheck,
} from 'lucide-react';

export const CartPage = () => {
  const { cart, itemCount, totalAmount, updateQuantity, removeItem, clearCart, isLoading } = useCart();
  const { isAuthenticated, isAdmin } = useAuth();
  const { t } = useTranslation();

  const [itemToDelete, setItemToDelete] = useState(null);
  const [clearDialogOpen, setClearDialogOpen] = useState(false);

  if (isAdmin) {
    return (
      <div className="py-16">
        <EmptyState
          icon={ShieldCheck}
          title={t('adminNoticeTitle')}
          description={t('adminNoticeDesc')}
          actionLabel={t('goToDashboard')}
          actionLink="/admin/statistics"
        />
      </div>
    );
  }

  if (!isAuthenticated) {
    return (
      <div className="py-16">
        <EmptyState
          icon={ShoppingCart}
          title={t('loginSubtitle')}
          description="Your cart is tied to your account. Sign in to see items you previously added or start adding new ones."
          actionLabel={t('login')}
          actionLink="/login"
        />
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16 flex items-center justify-center">
        <div className="w-10 h-10 border-4 border-emerald-600 border-t-transparent rounded-full animate-spin"></div>
      </div>
    );
  }

  if (itemCount === 0) {
    return (
      <div className="py-16">
        <EmptyState
          icon={ShoppingCart}
          title={t('emptyCart')}
          description={t('emptyCartDesc')}
          actionLabel={t('shopNow')}
          actionLink="/products"
        />
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-8">
      {/* Header */}
      <div className="flex items-center justify-between border-b border-slate-200 dark:border-slate-800 pb-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-black text-slate-900 dark:text-white tracking-tight">{t('shoppingCart')}</h1>
          <p className="text-xs text-slate-500 dark:text-slate-400 mt-1">{itemCount} {t('itemsCount')}</p>
        </div>
        <button
          onClick={() => setClearDialogOpen(true)}
          className="text-xs text-rose-600 dark:text-rose-400 hover:text-rose-700 font-bold flex items-center gap-1.5 p-2 rounded-xl hover:bg-rose-50 dark:hover:bg-rose-950/30 transition"
        >
          <Trash2 className="w-3.5 h-3.5" /> {t('clearFilters')}
        </button>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8 items-start">
        {/* Cart Items List */}
        <div className="lg:col-span-2 space-y-4">
          {cart.items.map((item) => (
            <div
              key={item.id}
              className="bg-white dark:bg-slate-900 p-4 sm:p-5 rounded-3xl border border-slate-200/80 dark:border-slate-800 shadow-xs flex flex-col sm:flex-row items-center gap-5"
            >
              <div className="w-20 h-20 bg-slate-50 dark:bg-slate-800 rounded-2xl p-2 shrink-0 flex items-center justify-center border border-slate-100 dark:border-slate-800">
                {item.productImageUrl ? (
                  <img src={getImageUrl(item.productImageUrl)} alt={item.productName} className="max-w-full max-h-full object-contain" />
                ) : (
                  <ShoppingBag className="w-8 h-8 text-slate-300 dark:text-slate-600" />
                )}
              </div>

              <div className="flex-1 min-w-0 text-center sm:text-left">
                <Link
                  to={`/products/${item.productId}`}
                  className="font-bold text-slate-900 dark:text-white text-sm hover:text-emerald-600 dark:hover:text-emerald-400 transition truncate block"
                >
                  {item.productName}
                </Link>
                <p className="text-xs text-slate-400 dark:text-slate-500 mt-0.5">{t('unitPrice')}: {formatCurrency(item.unitPrice)}</p>
              </div>

              {/* Quantity Selector */}
              <div className="flex items-center border border-slate-200 dark:border-slate-700 rounded-xl bg-slate-50 dark:bg-slate-800 p-1">
                <button
                  onClick={() => updateQuantity(item.id, item.quantity - 1)}
                  className="w-7 h-7 rounded-lg bg-white dark:bg-slate-700 border border-slate-200/60 dark:border-slate-600 flex items-center justify-center text-slate-600 dark:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-600 transition"
                >
                  <Minus className="w-3 h-3" />
                </button>
                <span className="w-10 text-center text-xs font-bold text-slate-800 dark:text-white">
                  {item.quantity}
                </span>
                <button
                  onClick={() => updateQuantity(item.id, item.quantity + 1)}
                  className="w-7 h-7 rounded-lg bg-white dark:bg-slate-700 border border-slate-200/60 dark:border-slate-600 flex items-center justify-center text-slate-600 dark:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-600 transition"
                >
                  <Plus className="w-3 h-3" />
                </button>
              </div>

              {/* Total & Remove */}
              <div className="flex sm:flex-col items-center sm:items-end justify-between w-full sm:w-auto gap-2">
                <span className="font-extrabold text-slate-900 dark:text-white text-base">
                  {formatCurrency(item.unitPrice * item.quantity)}
                </span>
                <button
                  onClick={() => setItemToDelete(item.id)}
                  className="text-slate-400 hover:text-rose-500 p-1 transition"
                  title="Remove item"
                >
                  <Trash2 className="w-4 h-4" />
                </button>
              </div>
            </div>
          ))}
        </div>

        {/* Order Summary & Proceed to Checkout */}
        <div className="bg-white dark:bg-slate-900 p-6 sm:p-8 rounded-3xl border border-slate-200/80 dark:border-slate-800 shadow-xs space-y-6 sticky top-24">
          <h3 className="font-extrabold text-slate-900 dark:text-white text-lg">{t('subtotal')}</h3>

          <div className="space-y-3.5 text-xs">
            <div className="flex justify-between text-slate-600 dark:text-slate-300">
              <span>{t('subtotal')} ({itemCount})</span>
              <span className="font-bold text-slate-900 dark:text-white">{formatCurrency(totalAmount)}</span>
            </div>
            <div className="flex justify-between text-slate-600 dark:text-slate-300">
              <span>{t('shipping')}</span>
              <span className="font-bold text-emerald-600 dark:text-emerald-400">{t('freeShipping')}</span>
            </div>

            <div className="border-t border-slate-100 dark:border-slate-800 pt-4 flex justify-between text-base">
              <span className="font-black text-slate-900 dark:text-white">{t('total')}</span>
              <span className="font-black text-xl text-emerald-600 dark:text-emerald-400">{formatCurrency(totalAmount)}</span>
            </div>
          </div>

          <Link
            to="/checkout"
            className="w-full py-4 px-6 bg-emerald-600 hover:bg-emerald-500 text-white font-bold text-sm rounded-2xl shadow-lg shadow-emerald-600/25 transition flex items-center justify-center gap-2 group"
          >
            {t('proceedToCheckout')}
            <ArrowRight className="w-4 h-4 group-hover:translate-x-1 transition" />
          </Link>

          <div className="pt-2 flex items-center justify-center gap-2 text-[11px] text-slate-400 dark:text-slate-500">
            <ShieldCheck className="w-4 h-4 text-emerald-600 dark:text-emerald-400" />
            <span>Guaranteed Safe & Secure Checkout</span>
          </div>
        </div>
      </div>

      {/* Confirmation Dialogs */}
      <ConfirmDialog
        isOpen={!!itemToDelete}
        title="Remove Item"
        message="Are you sure you want to remove this item from your shopping cart?"
        confirmText="Remove"
        onConfirm={() => {
          removeItem(itemToDelete);
          setItemToDelete(null);
        }}
        onCancel={() => setItemToDelete(null)}
      />

      <ConfirmDialog
        isOpen={clearDialogOpen}
        title="Clear Shopping Cart"
        message="This will remove all items currently in your cart. Do you wish to proceed?"
        confirmText="Clear All"
        onConfirm={() => {
          clearCart();
          setClearDialogOpen(false);
        }}
        onCancel={() => setClearDialogOpen(false)}
      />
    </div>
  );
};
