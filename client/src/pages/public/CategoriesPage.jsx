import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { categoriesApi } from '../../api/endpoints';
import { Layers, ArrowRight, Folder } from 'lucide-react';
import { getImageUrl } from '../../utils/storage';
import { EmptyState } from '../../components/common/EmptyState';
import { useTranslation } from '../../context/LanguageContext';

export const CategoriesPage = () => {
  const { t } = useTranslation();
  const { data, isLoading } = useQuery({
    queryKey: ['categories-tree'],
    queryFn: () => categoriesApi.getTree(),
  });

  const categoryTree = data?.data || [];

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-8">
      <div>
        <h1 className="text-3xl font-bold text-slate-900 dark:text-white">{t('allDepartments')}</h1>
        <p className="text-sm text-slate-500 dark:text-slate-400 mt-1">{t('featuredSubtitle')}</p>
      </div>

      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {[...Array(6)].map((_, i) => (
            <div key={i} className="h-60 bg-slate-200 dark:bg-slate-800 rounded-3xl animate-pulse" />
          ))}
        </div>
      ) : categoryTree.length === 0 ? (
        <EmptyState
          title={t('noProductsFound')}
          description="Categories will appear here once added by administrators."
        />
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {categoryTree.map((category) => (
            <div
              key={category.id}
              className="bg-white dark:bg-slate-900 rounded-3xl border border-slate-200/80 dark:border-slate-800 p-6 shadow-sm hover:shadow-md transition flex flex-col justify-between overflow-hidden"
            >
              <div>
                <div className="flex items-center justify-between mb-4">
                  {category.imageUrl ? (
                    <div className="w-14 h-14 rounded-2xl overflow-hidden border border-slate-100 dark:border-slate-800 bg-slate-50 dark:bg-slate-800 flex items-center justify-center">
                      <img
                        src={getImageUrl(category.imageUrl)}
                        alt={category.name}
                        className="w-full h-full object-cover"
                      />
                    </div>
                  ) : (
                    <div className="w-12 h-12 rounded-2xl bg-emerald-50 dark:bg-emerald-950/40 text-emerald-600 dark:text-emerald-400 flex items-center justify-center font-bold">
                      <Layers className="w-6 h-6" />
                    </div>
                  )}
                  <span className="text-xs font-semibold px-2.5 py-1 bg-slate-100 dark:bg-slate-800 rounded-full text-slate-600 dark:text-slate-300">
                    {category.productCount || 0} {t('itemsCount')}
                  </span>
                </div>

                <Link
                  to={`/products?categoryId=${category.id}`}
                  className="text-xl font-bold text-slate-900 dark:text-white hover:text-emerald-600 dark:hover:text-emerald-400 transition"
                >
                  {category.name}
                </Link>
                <p className="text-xs text-slate-500 dark:text-slate-400 mt-2 leading-relaxed">
                  {category.description || 'Explore curated products in this department.'}
                </p>

                {/* Subcategories */}
                {category.subCategories && category.subCategories.length > 0 && (
                  <div className="mt-4 pt-4 border-t border-slate-100 dark:border-slate-800">
                    <p className="text-[11px] font-semibold uppercase tracking-wider text-slate-400 dark:text-slate-500 mb-2">
                      Subcategories
                    </p>
                    <div className="flex flex-wrap gap-2">
                      {category.subCategories.map((sub) => (
                        <Link
                          key={sub.id}
                          to={`/products?categoryId=${sub.id}`}
                          className="px-2.5 py-1 bg-slate-50 dark:bg-slate-800 hover:bg-emerald-50 dark:hover:bg-emerald-950 text-slate-600 dark:text-slate-300 hover:text-emerald-700 dark:hover:text-emerald-300 text-xs font-medium rounded-lg border border-slate-200 dark:border-slate-700 transition flex items-center gap-1"
                        >
                          <Folder className="w-3 h-3 text-slate-400 dark:text-slate-500" />
                          {sub.name}
                        </Link>
                      ))}
                    </div>
                  </div>
                )}
              </div>

              <div className="pt-6 mt-4">
                <Link
                  to={`/products?categoryId=${category.id}`}
                  className="w-full py-2.5 px-4 bg-slate-50 dark:bg-slate-800 hover:bg-emerald-600 hover:text-white dark:hover:bg-emerald-600 text-slate-700 dark:text-slate-300 font-semibold text-xs rounded-xl transition flex items-center justify-center gap-1.5"
                >
                  {t('exploreDepartments')} <ArrowRight className="w-3.5 h-3.5" />
                </Link>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
