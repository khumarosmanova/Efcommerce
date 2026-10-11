import React, { useState, useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useSearchParams } from 'react-router-dom';
import { productsApi, categoriesApi } from '../../api/endpoints';
import { ProductCard } from '../../components/common/ProductCard';
import { Pagination } from '../../components/common/Pagination';
import { EmptyState } from '../../components/common/EmptyState';
import { useTranslation } from '../../context/LanguageContext';
import {
  Search,
  Filter,
  X,
  SlidersHorizontal,
  ChevronDown,
  ChevronRight,
  FolderTree,
  RotateCcw,
} from 'lucide-react';

export const ProductsPage = () => {
  const [searchParams, setSearchParams] = useSearchParams();
  const { t } = useTranslation();

  // Search Param states
  const searchParam = searchParams.get('search') || '';
  const categoryIdParam = searchParams.get('categoryId') || '';
  const minPriceParam = searchParams.get('minPrice') || '';
  const maxPriceParam = searchParams.get('maxPrice') || '';
  const inStockParam = searchParams.get('inStock') === 'true';
  const sortByParam = searchParams.get('sortBy') || 'newest';
  const pageParam = parseInt(searchParams.get('page') || '1', 10);

  // Local state for debounced search box
  const [searchTerm, setSearchTerm] = useState(searchParam);
  const [minPrice, setMinPrice] = useState(minPriceParam);
  const [maxPrice, setMaxPrice] = useState(maxPriceParam);
  const [mobileFilterOpen, setMobileFilterOpen] = useState(false);
  const [expandedCategories, setExpandedCategories] = useState({});

  // Sync searchTerm with URL when URL changes externally
  useEffect(() => {
    setSearchTerm(searchParam);
  }, [searchParam]);

  // Debounced search query sync (400ms)
  useEffect(() => {
    const handler = setTimeout(() => {
      if (searchTerm !== searchParam) {
        const next = new URLSearchParams(searchParams);
        if (searchTerm.trim()) {
          next.set('search', searchTerm.trim());
        } else {
          next.delete('search');
        }
        next.set('page', '1');
        setSearchParams(next);
      }
    }, 400);

    return () => clearTimeout(handler);
  }, [searchTerm, searchParam, searchParams, setSearchParams]);

  // Load category tree
  const { data: treeData } = useQuery({
    queryKey: ['categories-tree'],
    queryFn: () => categoriesApi.getTree(),
  });

  const categories = treeData?.data || [];

  // Query Products API
  const { data: productsData, isLoading } = useQuery({
    queryKey: [
      'products-catalog',
      {
        search: searchParam,
        categoryId: categoryIdParam,
        minPrice: minPriceParam,
        maxPrice: maxPriceParam,
        inStock: inStockParam,
        sortBy: sortByParam,
        page: pageParam,
      },
    ],
    queryFn: () => {
      let sortKey = sortByParam;
      let sortDir = undefined;
      if (sortByParam === 'price_asc') {
        sortKey = 'price';
        sortDir = 'asc';
      } else if (sortByParam === 'price_desc') {
        sortKey = 'price';
        sortDir = 'desc';
      } else if (sortByParam === 'name_asc') {
        sortKey = 'name';
        sortDir = 'asc';
      }

      return productsApi.getAll({
        search: searchParam || undefined,
        categoryId: categoryIdParam ? parseInt(categoryIdParam, 10) : undefined,
        minPrice: minPriceParam ? parseFloat(minPriceParam) : undefined,
        maxPrice: maxPriceParam ? parseFloat(maxPriceParam) : undefined,
        inStock: inStockParam || undefined,
        sortBy: sortKey,
        sortDirection: sortDir,
        page: pageParam,
        pageSize: 9,
      });
    },
  });

  const pagedResult = productsData?.data || { items: [], totalCount: 0, totalPages: 1 };
  const products = pagedResult.items || [];

  const updateParam = (key, value) => {
    const next = new URLSearchParams(searchParams);
    if (value !== undefined && value !== null && value !== '') {
      next.set(key, value.toString());
    } else {
      next.delete(key);
    }
    // Only reset page to 1 when changing filters, not when navigating pages
    if (key !== 'page') {
      next.set('page', '1');
    }
    setSearchParams(next);
  };

  const handlePageChange = (newPage) => {
    updateParam('page', newPage);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  const handlePriceApply = () => {
    const next = new URLSearchParams(searchParams);
    if (minPrice) next.set('minPrice', minPrice);
    else next.delete('minPrice');

    if (maxPrice) next.set('maxPrice', maxPrice);
    else next.delete('maxPrice');

    next.set('page', '1');
    setSearchParams(next);
  };

  const resetFilters = () => {
    setSearchTerm('');
    setMinPrice('');
    setMaxPrice('');
    setSearchParams(new URLSearchParams());
  };

  const toggleCategoryExpand = (catId) => {
    setExpandedCategories((prev) => ({ ...prev, [catId]: !prev[catId] }));
  };

  // Reusable Sidebar Filter Content
  const filterContent = (
    <div className="space-y-6">
      {/* Category Tree Filter */}
      <div>
        <h4 className="text-xs font-bold uppercase tracking-wider text-slate-800 dark:text-slate-200 mb-3 flex items-center gap-1.5">
          <FolderTree className="w-4 h-4 text-emerald-600 dark:text-emerald-400" />
          {t('categories')}
        </h4>
        <div className="space-y-1 text-sm max-h-64 overflow-y-auto pr-1">
          <button
            onClick={() => updateParam('categoryId', '')}
            className={`w-full text-left px-2.5 py-1.5 rounded-xl text-xs font-medium transition ${
              !categoryIdParam
                ? 'bg-emerald-600 text-white font-bold'
                : 'text-slate-600 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800'
            }`}
          >
            {t('allCategories')}
          </button>
          {categories.map((cat) => {
            const isSelected = categoryIdParam === cat.id.toString();
            const hasSub = cat.subCategories && cat.subCategories.length > 0;
            const isExpanded = !!expandedCategories[cat.id];

            return (
              <div key={cat.id} className="space-y-1">
                <div className="flex items-center justify-between">
                  <button
                    onClick={() => updateParam('categoryId', cat.id)}
                    className={`flex-1 text-left px-2.5 py-1.5 rounded-xl text-xs transition truncate ${
                      isSelected
                        ? 'bg-emerald-600 text-white font-bold'
                        : 'text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 font-medium'
                    }`}
                  >
                    {cat.name} ({cat.productCount})
                  </button>
                  {hasSub && (
                    <button
                      onClick={() => toggleCategoryExpand(cat.id)}
                      className="p-1 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 rounded"
                    >
                      {isExpanded ? <ChevronDown className="w-3.5 h-3.5" /> : <ChevronRight className="w-3.5 h-3.5" />}
                    </button>
                  )}
                </div>

                {/* Subcategories */}
                {hasSub && isExpanded && (
                  <div className="pl-4 space-y-1 border-l-2 border-slate-100 dark:border-slate-800 ml-2">
                    {cat.subCategories.map((sub) => {
                      const isSubSelected = categoryIdParam === sub.id.toString();
                      return (
                        <button
                          key={sub.id}
                          onClick={() => updateParam('categoryId', sub.id)}
                          className={`w-full text-left px-2 py-1 rounded-lg text-[11px] transition truncate ${
                            isSubSelected
                              ? 'bg-emerald-100 dark:bg-emerald-950 text-emerald-800 dark:text-emerald-300 font-bold'
                              : 'text-slate-500 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white hover:bg-slate-50 dark:hover:bg-slate-800'
                          }`}
                        >
                          {sub.name}
                        </button>
                      );
                    })}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      </div>

      {/* Price Range Filter */}
      <div className="border-t border-slate-100 dark:border-slate-800 pt-5">
        <h4 className="text-xs font-bold uppercase tracking-wider text-slate-800 dark:text-slate-200 mb-3">
          {t('priceRange')}
        </h4>
        <div className="flex items-center gap-2">
          <input
            type="number"
            value={minPrice}
            onChange={(e) => setMinPrice(e.target.value)}
            placeholder={t('minPrice')}
            className="w-full px-3 py-1.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-slate-900 dark:text-white rounded-xl text-xs focus:bg-white dark:focus:bg-slate-900 focus:outline-none focus:border-emerald-500"
          />
          <span className="text-slate-400 text-xs">-</span>
          <input
            type="number"
            value={maxPrice}
            onChange={(e) => setMaxPrice(e.target.value)}
            placeholder={t('maxPrice')}
            className="w-full px-3 py-1.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-slate-900 dark:text-white rounded-xl text-xs focus:bg-white dark:focus:bg-slate-900 focus:outline-none focus:border-emerald-500"
          />
        </div>
        <button
          onClick={handlePriceApply}
          className="w-full mt-2 py-1.5 bg-slate-900 dark:bg-emerald-600 hover:bg-slate-800 dark:hover:bg-emerald-700 text-white text-xs font-bold rounded-xl transition"
        >
          {t('filters')}
        </button>
      </div>

      {/* In-Stock Toggle */}
      <div className="border-t border-slate-100 dark:border-slate-800 pt-5">
        <label className="flex items-center justify-between cursor-pointer">
          <span className="text-xs font-bold text-slate-800 dark:text-slate-200">{t('inStockOnly')}</span>
          <input
            type="checkbox"
            checked={inStockParam}
            onChange={(e) => updateParam('inStock', e.target.checked ? 'true' : '')}
            className="w-4 h-4 text-emerald-600 rounded-md border-slate-300 dark:border-slate-700 focus:ring-emerald-500"
          />
        </label>
      </div>

      {/* Reset Button */}
      <div className="border-t border-slate-100 dark:border-slate-800 pt-5">
        <button
          onClick={resetFilters}
          className="w-full py-2 border border-slate-200 dark:border-slate-700 hover:bg-slate-50 dark:hover:bg-slate-800 text-slate-600 dark:text-slate-300 text-xs font-semibold rounded-xl flex items-center justify-center gap-1.5 transition"
        >
          <RotateCcw className="w-3.5 h-3.5" /> {t('clearFilters')}
        </button>
      </div>
    </div>
  );

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-6">
      {/* Top Header & Search / Sort Bar */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-slate-200 dark:border-slate-800 pb-6">
        <div>
          <h1 className="text-2xl sm:text-3xl font-extrabold text-slate-900 dark:text-white tracking-tight">
            {t('products')}
          </h1>
          <p className="text-xs text-slate-500 dark:text-slate-400 mt-1">
            {products.length} / {pagedResult.totalCount} {t('itemsCount')}
          </p>
        </div>

        {/* Controls */}
        <div className="flex flex-wrap items-center gap-3">
          {/* Debounced Search Box */}
          <div className="relative w-full sm:w-72">
            <input
              type="text"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              placeholder={t('searchPlaceholder')}
              className="w-full pl-9 pr-8 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-900 dark:text-white rounded-xl text-sm focus:outline-none focus:border-emerald-500 shadow-2xs"
            />
            <Search className="w-4 h-4 text-slate-400 absolute left-3 top-2.5" />
            {searchTerm && (
              <button
                onClick={() => setSearchTerm('')}
                className="absolute right-2.5 top-2.5 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200"
              >
                <X className="w-4 h-4" />
              </button>
            )}
          </div>

          {/* Sort Dropdown */}
          <select
            value={sortByParam}
            onChange={(e) => updateParam('sortBy', e.target.value)}
            className="px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl text-xs font-semibold text-slate-700 dark:text-slate-200 focus:outline-none focus:border-emerald-500 shadow-2xs"
          >
            <option value="newest">{t('newest')}</option>
            <option value="price_asc">{t('priceLowHigh')}</option>
            <option value="price_desc">{t('priceHighLow')}</option>
            <option value="name_asc">{t('nameAsc')}</option>
          </select>

          {/* Mobile Filter Button */}
          <button
            onClick={() => setMobileFilterOpen(true)}
            className="lg:hidden p-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl text-slate-700 dark:text-slate-200 flex items-center gap-1.5 text-xs font-bold"
          >
            <SlidersHorizontal className="w-4 h-4" /> {t('filters')}
          </button>
        </div>
      </div>

      {/* Main Content Layout (Sidebar + Products Grid) */}
      <div className="grid grid-cols-1 lg:grid-cols-4 gap-8 items-start">
        {/* Desktop Sidebar Filters */}
        <aside className="hidden lg:block bg-white dark:bg-slate-900 p-6 rounded-3xl border border-slate-200/80 dark:border-slate-800 shadow-xs sticky top-24">
          {filterContent}
        </aside>

        {/* Mobile Filter Drawer */}
        {mobileFilterOpen && (
          <div className="fixed inset-0 z-50 lg:hidden">
            <div
              className="fixed inset-0 bg-slate-900/40 backdrop-blur-xs"
              onClick={() => setMobileFilterOpen(false)}
            />
            <div className="fixed inset-y-0 right-0 max-w-xs w-full bg-white dark:bg-slate-900 p-6 shadow-2xl z-50 overflow-y-auto space-y-6 flex flex-col justify-between">
              <div>
                <div className="flex items-center justify-between pb-4 border-b border-slate-100 dark:border-slate-800">
                  <h3 className="font-bold text-slate-900 dark:text-white text-base">{t('filters')}</h3>
                  <button onClick={() => setMobileFilterOpen(false)} className="p-1 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200">
                    <X className="w-5 h-5" />
                  </button>
                </div>
                <div className="pt-4">{filterContent}</div>
              </div>
              <button
                onClick={() => setMobileFilterOpen(false)}
                className="w-full py-3 bg-emerald-600 text-white font-bold text-xs rounded-xl"
              >
                {t('products')}
              </button>
            </div>
          </div>
        )}

        {/* Product Catalog Content */}
        <div className="lg:col-span-3 space-y-8">
          {isLoading ? (
            /* Loading Skeletons */
            <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-6">
              {[...Array(6)].map((_, i) => (
                <div key={i} className="bg-white dark:bg-slate-900 rounded-3xl p-5 border border-slate-200/60 dark:border-slate-800 shadow-xs space-y-4 animate-pulse">
                  <div className="aspect-square bg-slate-100 dark:bg-slate-800 rounded-2xl" />
                  <div className="h-4 bg-slate-100 dark:bg-slate-800 rounded w-1/3" />
                  <div className="h-5 bg-slate-100 dark:bg-slate-800 rounded w-4/5" />
                  <div className="h-6 bg-slate-100 dark:bg-slate-800 rounded w-1/2 pt-2" />
                </div>
              ))}
            </div>
          ) : products.length === 0 ? (
            /* Empty State */
            <EmptyState
              title={t('noProductsFound')}
              description={t('noProductsDesc')}
              actionLabel={t('clearFilters')}
              onAction={resetFilters}
            />
          ) : (
            /* Products Grid */
            <>
              <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-6">
                {products.map((product) => (
                  <ProductCard key={product.id} product={product} />
                ))}
              </div>

              {/* Pagination */}
              <Pagination
                currentPage={pageParam}
                totalPages={pagedResult.totalPages}
                onPageChange={handlePageChange}
              />
            </>
          )}
        </div>
      </div>
    </div>
  );
};
