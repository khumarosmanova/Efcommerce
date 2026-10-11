import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { wishlistApi, productsApi } from '../../api/endpoints';
import { useWishlist } from '../../context/WishlistContext';
import { useAuth } from '../../context/AuthContext';
import { useTranslation } from '../../context/LanguageContext';
import { ProductCard } from '../../components/common/ProductCard';
import { EmptyState } from '../../components/common/EmptyState';
import { Heart, ShoppingBag, ArrowRight, ShieldCheck } from 'lucide-react';

export const WishlistPage = () => {
  const { wishlistIds, loading: wishlistLoading } = useWishlist();
  const { isAdmin } = useAuth();
  const { t } = useTranslation();

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

  // If user has wishlist items, fetch them
  const { data: productsData, isLoading: productsLoading } = useQuery({
    queryKey: ['wishlist-products', wishlistIds],
    queryFn: async () => {
      if (!wishlistIds || wishlistIds.length === 0) return [];
      try {
        // Try getting full wishlist from API
        const res = await wishlistApi.getAll();
        if (res.data && res.data.length > 0) return res.data;
      } catch {
        // Fallback for guest or fetch
      }
      // Or fetch from catalog filtered by IDs
      const allRes = await productsApi.getAll({ pageSize: 50 });
      const catalog = allRes?.data?.items || [];
      return catalog.filter((p) => wishlistIds.includes(p.id));
    },
    enabled: wishlistIds.length > 0,
  });

  const products = productsData || [];
  const isLoading = wishlistLoading || (wishlistIds.length > 0 && productsLoading);

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-8">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 pb-6 border-b border-slate-200 dark:border-slate-800">
        <div>
          <div className="flex items-center gap-2.5">
            <div className="w-10 h-10 rounded-2xl bg-rose-50 dark:bg-rose-950/40 text-rose-600 flex items-center justify-center">
              <Heart className="w-5 h-5 fill-rose-600" />
            </div>
            <h1 className="text-3xl font-extrabold text-slate-900 dark:text-white">
              {t('myWishlist')}
            </h1>
          </div>
          <p className="text-sm text-slate-500 dark:text-slate-400 mt-1">
            {t('wishlistSubtitle')}
          </p>
        </div>

        <Link
          to="/products"
          className="inline-flex items-center gap-2 text-sm font-semibold text-emerald-600 hover:text-emerald-700 dark:text-emerald-400"
        >
          {t('exploreDepartments')} <ArrowRight className="w-4 h-4" />
        </Link>
      </div>

      {isLoading ? (
        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
          {[...Array(4)].map((_, i) => (
            <div key={i} className="h-80 bg-slate-200 dark:bg-slate-800 rounded-3xl animate-pulse" />
          ))}
        </div>
      ) : products.length === 0 ? (
        <div className="bg-white dark:bg-slate-900 rounded-3xl border border-slate-200 dark:border-slate-800 p-12 text-center">
          <div className="w-16 h-16 rounded-full bg-rose-50 dark:bg-rose-950/40 text-rose-500 mx-auto flex items-center justify-center mb-4">
            <Heart className="w-8 h-8" />
          </div>
          <h2 className="text-xl font-bold text-slate-900 dark:text-white mb-2">
            {t('emptyWishlist')}
          </h2>
          <p className="text-sm text-slate-500 dark:text-slate-400 max-w-md mx-auto mb-6">
            {t('emptyWishlistDesc')}
          </p>
          <Link
            to="/products"
            className="inline-flex items-center gap-2 px-6 py-3 bg-emerald-600 hover:bg-emerald-700 text-white font-semibold text-sm rounded-xl transition shadow-sm shadow-emerald-200 dark:shadow-none"
          >
            <ShoppingBag className="w-4 h-4" />
            {t('shopNow')}
          </Link>
        </div>
      ) : (
        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
          {products.map((product) => (
            <ProductCard key={product.id} product={product} />
          ))}
        </div>
      )}
    </div>
  );
};
