import React from 'react';
import { Link } from 'react-router-dom';
import { ShoppingBag, Heart } from 'lucide-react';
import { useCart } from '../../context/CartContext';
import { useWishlist } from '../../context/WishlistContext';
import { useTranslation } from '../../context/LanguageContext';
import { formatCurrency, getImageUrl } from '../../utils/storage';
import { Rating } from './Rating';

export const ProductCard = ({ product }) => {
  const { addItem } = useCart();
  const { isInWishlist, toggleWishlist } = useWishlist();
  const { t } = useTranslation();

  if (!product) return null;

  const isFavorited = isInWishlist(product.id);

  return (
    <div className="bg-white dark:bg-slate-900 rounded-3xl border border-slate-200/80 dark:border-slate-800 overflow-hidden hover:shadow-xl hover:border-slate-300 dark:hover:border-slate-700 transition duration-300 flex flex-col group relative">
      {/* Product Image and Badges */}
      <div className="aspect-square bg-slate-50 dark:bg-slate-800/50 relative overflow-hidden flex items-center justify-center p-6 border-b border-slate-100 dark:border-slate-800/80">
        <Link to={`/products/${product.id}`} className="w-full h-full flex items-center justify-center">
          {product.imageUrl ? (
            <img
              src={getImageUrl(product.imageUrl)}
              alt={product.name}
              className="max-w-full max-h-full object-contain group-hover:scale-105 transition duration-300"
            />
          ) : (
            <ShoppingBag className="w-16 h-16 text-slate-300 dark:text-slate-600" />
          )}
        </Link>

        {/* Favorite Heart Button */}
        <button
          onClick={(e) => {
            e.preventDefault();
            toggleWishlist(product);
          }}
          className={`absolute top-3 right-3 p-2 rounded-full backdrop-blur-md transition shadow-sm z-10 ${
            isFavorited
              ? 'bg-rose-50 dark:bg-rose-950/80 text-rose-600 dark:text-rose-400'
              : 'bg-white/90 dark:bg-slate-800/90 text-slate-400 hover:text-rose-500 hover:bg-white dark:hover:bg-slate-700'
          }`}
          title={isFavorited ? t('removedFromWishlist') : t('addedToWishlist')}
          aria-label="Wishlist toggle"
        >
          <Heart
            className={`w-4 h-4 transition ${
              isFavorited ? 'fill-rose-600 text-rose-600 dark:fill-rose-400 dark:text-rose-400 scale-110' : ''
            }`}
          />
        </button>

        {/* Sale Tag */}
        {product.discountPrice && (
          <span className="absolute top-3 left-3 bg-rose-500 text-white text-[10px] font-extrabold px-2 py-0.5 rounded-full uppercase tracking-wider shadow-xs">
            Sale
          </span>
        )}

        {/* Stock tag if low */}
        {product.stockQuantity <= 5 && product.stockQuantity > 0 && !product.discountPrice && (
          <span className="absolute top-3 left-3 bg-amber-500/10 text-amber-700 dark:text-amber-300 text-[10px] font-bold px-2 py-0.5 rounded-full border border-amber-200/60 dark:border-amber-800">
            {product.stockQuantity} left
          </span>
        )}
      </div>

      {/* Product Information */}
      <div className="p-5 flex-1 flex flex-col justify-between space-y-3">
        <div className="space-y-1.5">
          <p className="text-[11px] font-bold text-emerald-600 dark:text-emerald-400 uppercase tracking-wider">
            {product.categoryName || 'General'}
          </p>

          <Link
            to={`/products/${product.id}`}
            className="font-bold text-slate-800 dark:text-slate-100 text-sm hover:text-emerald-600 dark:hover:text-emerald-400 transition line-clamp-2 leading-snug"
            title={product.name}
          >
            {product.name}
          </Link>

          <div className="pt-0.5">
            <Rating value={4.8} count={product.stockQuantity > 5 ? 12 : 6} size="sm" />
          </div>
        </div>

        {/* Price and Add to Cart Action */}
        <div className="pt-3 border-t border-slate-100 dark:border-slate-800 flex items-center justify-between">
          <div>
            {product.discountPrice ? (
              <div className="flex flex-col">
                <span className="font-black text-base text-slate-900 dark:text-white leading-tight">
                  {formatCurrency(product.discountPrice)}
                </span>
                <span className="text-xs text-slate-400 dark:text-slate-500 line-through leading-tight">
                  {formatCurrency(product.price)}
                </span>
              </div>
            ) : (
              <span className="font-black text-base text-slate-900 dark:text-white">
                {formatCurrency(product.price)}
              </span>
            )}
          </div>

          <button
            onClick={() => addItem(product.id, 1)}
            disabled={product.stockQuantity <= 0}
            className="px-3.5 py-1.5 bg-slate-900 dark:bg-emerald-600 hover:bg-emerald-600 dark:hover:bg-emerald-700 text-white text-xs font-bold rounded-xl transition shadow-xs disabled:opacity-40 disabled:cursor-not-allowed"
          >
            {product.stockQuantity > 0 ? t('addToCart') : t('outOfStock')}
          </button>
        </div>
      </div>
    </div>
  );
};
