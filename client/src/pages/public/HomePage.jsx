import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { productsApi, categoriesApi } from '../../api/endpoints';
import { ProductCard } from '../../components/common/ProductCard';
import { useTranslation } from '../../context/LanguageContext';
import {
  ShoppingBag,
  ArrowRight,
  ShieldCheck,
  Truck,
  RefreshCw,
  Sparkles,
  Flame,
} from 'lucide-react';

export const HomePage = () => {
  const { t } = useTranslation();

  // Query 1: Newest Arrivals
  const { data: newestData, isLoading: loadingNewest } = useQuery({
    queryKey: ['home-newest-products'],
    queryFn: () => productsApi.getAll({ pageSize: 4, sortBy: 'newest' }),
  });

  // Query 2: Categories
  const { data: categoriesData, isLoading: loadingCategories } = useQuery({
    queryKey: ['home-categories'],
    queryFn: () => categoriesApi.getAll(),
  });

  // Query 3: Discounted Products
  const { data: popularData, isLoading: loadingPopular } = useQuery({
    queryKey: ['home-popular-products'],
    queryFn: () => productsApi.getAll({ pageSize: 4, sortBy: 'popularity' }),
  });

  const newestProducts = newestData?.data?.items || [];
  const popularProducts = popularData?.data?.items || [];
  const categories = categoriesData?.data || [];

  return (
    <div className="space-y-16 pb-20">
      {/* 1. Hero Banner */}
      <section className="relative overflow-hidden bg-gradient-to-br from-slate-950 via-slate-900 to-emerald-950 text-white py-20 lg:py-28 px-4 sm:px-6 lg:px-8">
        <div className="absolute top-1/4 -left-20 w-96 h-96 bg-emerald-500/15 rounded-full blur-3xl pointer-events-none" />
        <div className="absolute -bottom-20 right-10 w-96 h-96 bg-teal-500/10 rounded-full blur-3xl pointer-events-none" />

        <div className="max-w-7xl mx-auto grid grid-cols-1 lg:grid-cols-2 gap-12 items-center relative z-10">
          <div className="space-y-6">
            <div className="inline-flex items-center gap-2 px-3.5 py-1.5 rounded-full bg-emerald-500/10 border border-emerald-500/30 text-emerald-400 text-xs font-bold tracking-wide uppercase shadow-xs">
              <Sparkles className="w-3.5 h-3.5" />
              Verified Premium Store 2026
            </div>
            <h1 className="text-4xl sm:text-5xl lg:text-6xl font-black tracking-tight leading-[1.1]">
              {t('heroTitle')}{' '}
              <span className="text-transparent bg-clip-text bg-gradient-to-r from-emerald-400 to-teal-300">
                ShopHub.
              </span>
            </h1>
            <p className="text-slate-300 text-base sm:text-lg max-w-xl leading-relaxed">
              {t('heroSubtitle')}
            </p>
            <div className="flex flex-wrap gap-4 pt-3">
              <Link
                to="/products"
                className="px-7 py-3.5 bg-emerald-600 hover:bg-emerald-500 text-white font-bold rounded-2xl shadow-xl shadow-emerald-900/40 transition flex items-center gap-2 group"
              >
                {t('shopNow')}
                <ArrowRight className="w-4 h-4 group-hover:translate-x-1 transition duration-200" />
              </Link>
              <Link
                to="/categories"
                className="px-7 py-3.5 bg-white/5 hover:bg-white/10 text-white font-semibold rounded-2xl border border-white/10 backdrop-blur-xs transition"
              >
                {t('exploreDepartments')}
              </Link>
            </div>
          </div>

          {/* Hero Showcase Card */}
          <div className="hidden lg:flex justify-end">
            <div className="relative w-88 h-104 rounded-3xl bg-slate-900/60 border border-white/10 p-8 backdrop-blur-md flex flex-col justify-between shadow-2xl overflow-hidden group">
              <div className="absolute top-0 right-0 w-36 h-36 bg-emerald-500/20 rounded-full blur-2xl" />

              <div className="flex items-center justify-between z-10">
                <span className="px-3 py-1 bg-emerald-500/20 text-emerald-400 text-xs font-bold rounded-full border border-emerald-500/30">
                  Featured Choice
                </span>
                <span className="text-xs text-slate-400 font-mono">Stock Ready</span>
              </div>

              <div className="py-6 flex items-center justify-center">
                <div className="w-48 h-48 rounded-2xl bg-gradient-to-tr from-slate-800 to-slate-700/60 flex items-center justify-center p-4 shadow-inner border border-white/5 group-hover:scale-105 transition duration-300">
                  <ShoppingBag className="w-20 h-20 text-emerald-400" />
                </div>
              </div>

              <div className="space-y-2 z-10">
                <p className="text-xs font-bold text-emerald-400 uppercase tracking-wider">Electronics</p>
                <h3 className="text-xl font-bold text-white">Apple iPhone 15 Pro Max</h3>
                <div className="flex items-baseline justify-between pt-2">
                  <span className="text-2xl font-black text-white">$1,149.99</span>
                  <Link
                    to="/products"
                    className="text-xs text-emerald-400 font-bold hover:underline flex items-center gap-1"
                  >
                    {t('viewAll')} <ArrowRight className="w-3.5 h-3.5" />
                  </Link>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* 2. Value Propositions */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-6">
          <div className="bg-white dark:bg-slate-900 p-6 rounded-3xl border border-slate-200/80 dark:border-slate-800 shadow-xs flex items-center gap-4">
            <div className="w-12 h-12 rounded-2xl bg-emerald-50 dark:bg-emerald-950/40 text-emerald-600 dark:text-emerald-400 flex items-center justify-center shrink-0">
              <Truck className="w-6 h-6" />
            </div>
            <div>
              <h4 className="font-bold text-slate-900 dark:text-white text-sm">Free Global Delivery</h4>
              <p className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">Complimentary shipping on orders $50+</p>
            </div>
          </div>

          <div className="bg-white dark:bg-slate-900 p-6 rounded-3xl border border-slate-200/80 dark:border-slate-800 shadow-xs flex items-center gap-4">
            <div className="w-12 h-12 rounded-2xl bg-teal-50 dark:bg-teal-950/40 text-teal-600 dark:text-teal-400 flex items-center justify-center shrink-0">
              <ShieldCheck className="w-6 h-6" />
            </div>
            <div>
              <h4 className="font-bold text-slate-900 dark:text-white text-sm">Official Warranty</h4>
              <p className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">100% verified authentic merchandise</p>
            </div>
          </div>

          <div className="bg-white dark:bg-slate-900 p-6 rounded-3xl border border-slate-200/80 dark:border-slate-800 shadow-xs flex items-center gap-4">
            <div className="w-12 h-12 rounded-2xl bg-indigo-50 dark:bg-indigo-950/40 text-indigo-600 dark:text-indigo-400 flex items-center justify-center shrink-0">
              <RefreshCw className="w-6 h-6" />
            </div>
            <div>
              <h4 className="font-bold text-slate-900 dark:text-white text-sm">30-Day Hassle Free Returns</h4>
              <p className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">Prompt full refund guaranteed</p>
            </div>
          </div>
        </div>
      </section>

      {/* 3. Featured Categories */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between mb-8">
          <div>
            <h2 className="text-2xl sm:text-3xl font-extrabold text-slate-900 dark:text-white">{t('featuredCategories')}</h2>
            <p className="text-sm text-slate-500 dark:text-slate-400 mt-1">{t('featuredSubtitle')}</p>
          </div>
          <Link
            to="/categories"
            className="text-sm font-bold text-emerald-600 dark:text-emerald-400 hover:text-emerald-700 flex items-center gap-1 group"
          >
            {t('allDepartments')}
            <ArrowRight className="w-4 h-4 group-hover:translate-x-1 transition" />
          </Link>
        </div>

        {loadingCategories ? (
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-6 gap-4">
            {[...Array(6)].map((_, i) => (
              <div key={i} className="h-32 bg-slate-200 dark:bg-slate-800 rounded-3xl animate-pulse" />
            ))}
          </div>
        ) : (
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-6 gap-4">
            {categories.slice(0, 6).map((cat) => (
              <Link
                key={cat.id}
                to={`/products?categoryId=${cat.id}`}
                className="group bg-white dark:bg-slate-900 p-5 rounded-3xl border border-slate-200/80 dark:border-slate-800 hover:border-emerald-500 dark:hover:border-emerald-500 hover:shadow-lg transition duration-200 text-center flex flex-col items-center justify-center gap-2"
              >
                <div className="w-14 h-14 rounded-2xl bg-slate-50 dark:bg-slate-800 group-hover:bg-emerald-50 dark:group-hover:bg-emerald-950 text-slate-600 dark:text-slate-300 group-hover:text-emerald-600 dark:group-hover:text-emerald-400 flex items-center justify-center transition">
                  <ShoppingBag className="w-7 h-7" />
                </div>
                <span className="font-bold text-slate-800 dark:text-slate-100 text-sm group-hover:text-emerald-600 dark:group-hover:text-emerald-400 transition truncate max-w-full">
                  {cat.name}
                </span>
                <span className="text-[11px] text-slate-400 dark:text-slate-500 font-medium">
                  {cat.productCount} {t('itemsCount')}
                </span>
              </Link>
            ))}
          </div>
        )}
      </section>

      {/* 4. Newest Arrivals Products */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between mb-8">
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-xl bg-emerald-50 dark:bg-emerald-950/40 text-emerald-600 dark:text-emerald-400 flex items-center justify-center font-bold">
              <Sparkles className="w-5 h-5" />
            </div>
            <div>
              <h2 className="text-2xl font-extrabold text-slate-900 dark:text-white">{t('newArrivals')}</h2>
              <p className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">{t('newArrivalsSubtitle')}</p>
            </div>
          </div>
          <Link to="/products?sortBy=newest" className="text-sm font-bold text-emerald-600 dark:text-emerald-400 hover:underline">
            {t('viewAll')}
          </Link>
        </div>

        {loadingNewest ? (
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-6">
            {[...Array(4)].map((_, i) => (
              <div key={i} className="h-88 bg-slate-200 dark:bg-slate-800 rounded-3xl animate-pulse" />
            ))}
          </div>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-6">
            {newestProducts.map((product) => (
              <ProductCard key={product.id} product={product} />
            ))}
          </div>
        )}
      </section>

      {/* 5. Discounted & Top Deals */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between mb-8">
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-xl bg-rose-50 dark:bg-rose-950/40 text-rose-600 dark:text-rose-400 flex items-center justify-center font-bold">
              <Flame className="w-5 h-5" />
            </div>
            <div>
              <h2 className="text-2xl font-extrabold text-slate-900 dark:text-white">{t('discountedDeals')}</h2>
              <p className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">{t('discountedSubtitle')}</p>
            </div>
          </div>
          <Link to="/products" className="text-sm font-bold text-emerald-600 dark:text-emerald-400 hover:underline">
            {t('viewAll')}
          </Link>
        </div>

        {loadingPopular ? (
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-6">
            {[...Array(4)].map((_, i) => (
              <div key={i} className="h-88 bg-slate-200 dark:bg-slate-800 rounded-3xl animate-pulse" />
            ))}
          </div>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-6">
            {popularProducts.map((product) => (
              <ProductCard key={product.id} product={product} />
            ))}
          </div>
        )}
      </section>
    </div>
  );
};
