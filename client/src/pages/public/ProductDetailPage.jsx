import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useParams, Link } from 'react-router-dom';
import { productsApi, reviewsApi } from '../../api/endpoints';
import { useCart } from '../../context/CartContext';
import { useWishlist } from '../../context/WishlistContext';
import { useAuth } from '../../context/AuthContext';
import { useTranslation } from '../../context/LanguageContext';
import { ProductCard } from '../../components/common/ProductCard';
import { Rating } from '../../components/common/Rating';
import { formatCurrency, formatDate, getImageUrl } from '../../utils/storage';
import toast from 'react-hot-toast';
import {
  ShoppingBag,
  Heart,
  Star,
  ShieldCheck,
  Truck,
  RotateCcw,
  Minus,
  Plus,
  ChevronRight,
  AlertCircle,
  MessageSquare,
  User,
  Send,
} from 'lucide-react';

export const ProductDetailPage = () => {
  const { id } = useParams();
  const queryClient = useQueryClient();
  const { addItem } = useCart();
  const { isInWishlist, toggleWishlist } = useWishlist();
  const { isAuthenticated } = useAuth();
  const { t } = useTranslation();

  const [selectedQuantity, setSelectedQuantity] = useState(1);
  const [newRating, setNewRating] = useState(5);
  const [newComment, setNewComment] = useState('');
  const [hoverRating, setHoverRating] = useState(0);

  // 1. Fetch current product
  const { data, isLoading, error } = useQuery({
    queryKey: ['product-detail', id],
    queryFn: () => productsApi.getById(id),
  });

  const product = data?.data;

  // 2. Fetch reviews & ratings
  const { data: reviewsData, isLoading: reviewsLoading } = useQuery({
    queryKey: ['product-reviews', id],
    queryFn: () => reviewsApi.getByProduct(id),
    enabled: !!id,
  });

  const ratingSummary = reviewsData?.data;
  const reviews = ratingSummary?.reviews || [];

  // 3. Post review mutation
  const reviewMutation = useMutation({
    mutationFn: (newReview) => reviewsApi.addReview(id, newReview),
    onSuccess: () => {
      toast.success(t('reviewSubmitted'));
      setNewComment('');
      setNewRating(5);
      queryClient.invalidateQueries(['product-reviews', id]);
    },
    onError: (err) => {
      const msg = err.response?.data?.message || err.response?.data?.errors?.[0] || 'Failed to submit review.';
      toast.error(msg);
    },
  });

  // 4. Fetch related products from the same category
  const { data: relatedData } = useQuery({
    queryKey: ['related-products', product?.categoryId],
    queryFn: () => productsApi.getAll({ categoryId: product.categoryId, pageSize: 4 }),
    enabled: !!product?.categoryId,
  });

  const relatedProducts = (relatedData?.data?.items || []).filter((p) => p.id !== parseInt(id, 10));

  const handleDecrease = () => {
    if (selectedQuantity > 1) setSelectedQuantity((q) => q - 1);
  };

  const handleIncrease = () => {
    if (product && selectedQuantity < product.stockQuantity) {
      setSelectedQuantity((q) => q + 1);
    }
  };

  const handleSubmitReview = (e) => {
    e.preventDefault();
    if (!newRating) return;
    reviewMutation.mutate({
      rating: newRating,
      comment: newComment.trim(),
    });
  };

  if (isLoading) {
    return (
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16 flex items-center justify-center">
        <div className="w-10 h-10 border-4 border-emerald-600 border-t-transparent rounded-full animate-spin"></div>
      </div>
    );
  }

  if (error || !product) {
    return (
      <div className="max-w-md mx-auto px-4 py-16 text-center space-y-4">
        <AlertCircle className="w-12 h-12 text-rose-500 mx-auto" />
        <h2 className="text-xl font-bold text-slate-800 dark:text-white">{t('noProductsFound')}</h2>
        <p className="text-sm text-slate-500 dark:text-slate-400">{t('noProductsDesc')}</p>
        <Link to="/products" className="inline-block px-5 py-2.5 bg-emerald-600 text-white rounded-xl text-xs font-bold">
          {t('exploreDepartments')}
        </Link>
      </div>
    );
  }

  const isFavorited = isInWishlist(product.id);
  const avgRating = ratingSummary?.averageRating || 4.8;
  const totalReviews = ratingSummary?.totalReviews || 0;

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-12">
      {/* Breadcrumb Navigation */}
      <nav className="flex items-center gap-2 text-xs font-medium text-slate-500 dark:text-slate-400 flex-wrap">
        <Link to="/" className="hover:text-emerald-600 dark:hover:text-emerald-400 transition">{t('home')}</Link>
        <ChevronRight className="w-3.5 h-3.5 text-slate-400" />
        <Link to="/products" className="hover:text-emerald-600 dark:hover:text-emerald-400 transition">{t('products')}</Link>
        <ChevronRight className="w-3.5 h-3.5 text-slate-400" />
        <Link to={`/products?categoryId=${product.categoryId}`} className="hover:text-emerald-600 dark:hover:text-emerald-400 transition">
          {product.categoryName}
        </Link>
        <ChevronRight className="w-3.5 h-3.5 text-slate-400" />
        <span className="text-slate-800 dark:text-slate-200 font-bold truncate max-w-xs">{product.name}</span>
      </nav>

      {/* Main Product Showcase Box */}
      <div className="bg-white dark:bg-slate-900 rounded-3xl border border-slate-200/80 dark:border-slate-800 p-6 sm:p-10 shadow-xs grid grid-cols-1 lg:grid-cols-2 gap-10 lg:gap-14">
        {/* Left: Product Image */}
        <div className="aspect-square bg-slate-50 dark:bg-slate-800/50 rounded-3xl flex items-center justify-center p-8 relative border border-slate-100 dark:border-slate-800 overflow-hidden">
          {product.imageUrl ? (
            <img src={getImageUrl(product.imageUrl)} alt={product.name} className="max-w-full max-h-full object-contain" />
          ) : (
            <ShoppingBag className="w-28 h-28 text-slate-300 dark:text-slate-600" />
          )}

          {product.discountPrice && (
            <span className="absolute top-4 left-4 bg-rose-500 text-white text-xs font-extrabold px-3 py-1 rounded-full uppercase tracking-wider shadow-xs">
              Special Discount
            </span>
          )}

          {/* Quick Wishlist Floating Button */}
          <button
            onClick={() => toggleWishlist(product)}
            className={`absolute top-4 right-4 p-3 rounded-full backdrop-blur-md transition shadow-sm ${
              isFavorited
                ? 'bg-rose-50 dark:bg-rose-950/80 text-rose-600 dark:text-rose-400 ring-2 ring-rose-200 dark:ring-rose-900'
                : 'bg-white/90 dark:bg-slate-800/90 text-slate-400 hover:text-rose-500 hover:bg-white dark:hover:bg-slate-700'
            }`}
            title={isFavorited ? t('removedFromWishlist') : t('addedToWishlist')}
          >
            <Heart className={`w-5 h-5 ${isFavorited ? 'fill-rose-600 text-rose-600 dark:fill-rose-400 dark:text-rose-400' : ''}`} />
          </button>
        </div>

        {/* Right: Product Details & Purchase Actions */}
        <div className="flex flex-col justify-between space-y-6">
          <div className="space-y-4">
            <span className="inline-block text-xs font-bold uppercase tracking-wider text-emerald-700 dark:text-emerald-400 bg-emerald-50 dark:bg-emerald-950/40 px-3 py-1 rounded-xl">
              {product.categoryName}
            </span>

            <h1 className="text-2xl sm:text-3xl font-black text-slate-900 dark:text-white leading-tight">
              {product.name}
            </h1>

            <div className="flex items-center gap-4">
              <Rating value={avgRating} count={totalReviews > 0 ? totalReviews : 14} size="md" />
              <span className="text-xs text-slate-300 dark:text-slate-700">|</span>
              <span className="text-xs font-mono text-slate-400 dark:text-slate-500">{t('sku')}: {product.sku}</span>
            </div>

            {/* Pricing Section */}
            <div className="pt-2">
              {product.discountPrice ? (
                <div className="flex items-baseline gap-3">
                  <span className="text-3xl sm:text-4xl font-black text-slate-900 dark:text-white">
                    {formatCurrency(product.discountPrice)}
                  </span>
                  <span className="text-lg text-slate-400 dark:text-slate-500 line-through">
                    {formatCurrency(product.price)}
                  </span>
                  <span className="text-xs font-bold text-rose-600 bg-rose-50 dark:bg-rose-950/40 px-2.5 py-1 rounded-lg">
                    Save {formatCurrency(product.price - product.discountPrice)}
                  </span>
                </div>
              ) : (
                <span className="text-3xl sm:text-4xl font-black text-slate-900 dark:text-white">
                  {formatCurrency(product.price)}
                </span>
              )}
            </div>

            {/* Stock status indicator */}
            <div className="flex items-center gap-2 pt-1">
              <span
                className={`w-2.5 h-2.5 rounded-full ${
                  product.stockQuantity > 0 ? 'bg-emerald-500 animate-pulse' : 'bg-rose-500'
                }`}
              />
              <span className="text-xs font-bold text-slate-700 dark:text-slate-300">
                {product.stockQuantity > 0
                  ? `${t('inStock')} (${product.stockQuantity} ${t('itemsCount')})`
                  : t('outOfStock')}
              </span>
            </div>

            {/* Description */}
            <div className="pt-2">
              <p className="text-sm text-slate-600 dark:text-slate-300 leading-relaxed">
                {product.description || 'This authentic item is crafted with premium quality standards for long-lasting everyday durability.'}
              </p>
            </div>
          </div>

          {/* Action Row: Quantity Selector + Add to Cart + Wishlist */}
          <div className="space-y-5 pt-6 border-t border-slate-100 dark:border-slate-800">
            <div className="flex flex-col sm:flex-row items-center gap-3">
              {/* Quantity Selector */}
              <div className="flex items-center border border-slate-200 dark:border-slate-700 rounded-2xl bg-slate-50 dark:bg-slate-800 p-1 w-full sm:w-auto">
                <button
                  onClick={handleDecrease}
                  disabled={selectedQuantity <= 1}
                  className="w-10 h-10 rounded-xl bg-white dark:bg-slate-700 border border-slate-200/60 dark:border-slate-600 flex items-center justify-center text-slate-600 dark:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-600 transition disabled:opacity-30 disabled:cursor-not-allowed"
                >
                  <Minus className="w-4 h-4" />
                </button>
                <span className="w-12 text-center text-sm font-bold text-slate-800 dark:text-white">
                  {selectedQuantity}
                </span>
                <button
                  onClick={handleIncrease}
                  disabled={selectedQuantity >= product.stockQuantity}
                  className="w-10 h-10 rounded-xl bg-white dark:bg-slate-700 border border-slate-200/60 dark:border-slate-600 flex items-center justify-center text-slate-600 dark:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-600 transition disabled:opacity-30 disabled:cursor-not-allowed"
                >
                  <Plus className="w-4 h-4" />
                </button>
              </div>

              {/* Add to Cart CTA */}
              <button
                onClick={() => addItem(product.id, selectedQuantity)}
                disabled={product.stockQuantity <= 0}
                className="flex-1 w-full py-4 px-6 bg-emerald-600 hover:bg-emerald-500 text-white font-bold rounded-2xl shadow-lg shadow-emerald-600/25 transition disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
              >
                <ShoppingBag className="w-5 h-5" />
                {t('addToCart')} ({formatCurrency((product.discountPrice ?? product.price) * selectedQuantity)})
              </button>

              {/* Wishlist CTA */}
              <button
                onClick={() => toggleWishlist(product)}
                className={`p-4 rounded-2xl border transition flex items-center justify-center ${
                  isFavorited
                    ? 'border-rose-200 bg-rose-50 dark:bg-rose-950/40 text-rose-600 dark:text-rose-400'
                    : 'border-slate-200 dark:border-slate-700 hover:bg-slate-50 dark:hover:bg-slate-800 text-slate-600 dark:text-slate-300'
                }`}
                title={t('wishlist')}
              >
                <Heart className={`w-5 h-5 ${isFavorited ? 'fill-rose-600 text-rose-600 dark:fill-rose-400' : ''}`} />
              </button>
            </div>

            {/* Badges */}
            <div className="grid grid-cols-3 gap-3 pt-2">
              <div className="p-3 bg-slate-50 dark:bg-slate-800/60 rounded-2xl text-center flex flex-col items-center gap-1.5">
                <Truck className="w-4 h-4 text-emerald-600 dark:text-emerald-400" />
                <span className="text-[11px] font-semibold text-slate-600 dark:text-slate-300">Free Express</span>
              </div>
              <div className="p-3 bg-slate-50 dark:bg-slate-800/60 rounded-2xl text-center flex flex-col items-center gap-1.5">
                <ShieldCheck className="w-4 h-4 text-emerald-600 dark:text-emerald-400" />
                <span className="text-[11px] font-semibold text-slate-600 dark:text-slate-300">2-Yr Warranty</span>
              </div>
              <div className="p-3 bg-slate-50 dark:bg-slate-800/60 rounded-2xl text-center flex flex-col items-center gap-1.5">
                <RotateCcw className="w-4 h-4 text-emerald-600 dark:text-emerald-400" />
                <span className="text-[11px] font-semibold text-slate-600 dark:text-slate-300">30-Day Returns</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Customer Reviews & Rating Section */}
      <section className="bg-white dark:bg-slate-900 rounded-3xl border border-slate-200/80 dark:border-slate-800 p-6 sm:p-10 space-y-8">
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 border-b border-slate-100 dark:border-slate-800 pb-6">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-2xl bg-amber-50 dark:bg-amber-950/40 text-amber-500 flex items-center justify-center">
              <Star className="w-5 h-5 fill-amber-400 text-amber-400" />
            </div>
            <div>
              <h2 className="text-xl font-bold text-slate-900 dark:text-white">
                {t('customerReviews')}
              </h2>
              <p className="text-xs text-slate-500 dark:text-slate-400">
                {totalReviews} {t('basedOnReviews')}
              </p>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <span className="text-3xl font-extrabold text-slate-900 dark:text-white">
              {avgRating.toFixed(1)}
            </span>
            <div>
              <Rating value={avgRating} size="sm" />
              <p className="text-[11px] text-slate-400 dark:text-slate-500 mt-0.5">out of 5 stars</p>
            </div>
          </div>
        </div>

        {/* Write a Review Box */}
        <div className="bg-slate-50 dark:bg-slate-800/50 rounded-2xl p-6 border border-slate-100 dark:border-slate-800">
          <h3 className="text-base font-bold text-slate-900 dark:text-white mb-2">
            {t('writeReview')}
          </h3>

          {isAuthenticated ? (
            <form onSubmit={handleSubmitReview} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold text-slate-600 dark:text-slate-300 mb-1.5">
                  {t('yourRating')}
                </label>
                <div className="flex items-center gap-1.5">
                  {[1, 2, 3, 4, 5].map((star) => (
                    <button
                      key={star}
                      type="button"
                      onMouseEnter={() => setHoverRating(star)}
                      onMouseLeave={() => setHoverRating(0)}
                      onClick={() => setNewRating(star)}
                      className="p-1 text-slate-300 hover:scale-110 transition"
                    >
                      <Star
                        className={`w-6 h-6 ${
                          star <= (hoverRating || newRating)
                            ? 'fill-amber-400 text-amber-400'
                            : 'text-slate-300 dark:text-slate-600'
                        }`}
                      />
                    </button>
                  ))}
                  <span className="text-xs font-bold text-slate-600 dark:text-slate-300 ml-2">
                    {newRating} / 5
                  </span>
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-600 dark:text-slate-300 mb-1.5">
                  {t('yourReview')}
                </label>
                <textarea
                  rows="3"
                  value={newComment}
                  onChange={(e) => setNewComment(e.target.value)}
                  placeholder={t('reviewPlaceholder')}
                  className="w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900 text-slate-900 dark:text-white text-sm focus:ring-2 focus:ring-emerald-500 outline-none"
                  required
                />
              </div>

              <button
                type="submit"
                disabled={reviewMutation.isPending}
                className="inline-flex items-center gap-2 px-5 py-2.5 bg-emerald-600 hover:bg-emerald-700 text-white font-bold text-xs rounded-xl shadow-sm transition disabled:opacity-50"
              >
                <Send className="w-3.5 h-3.5" />
                {reviewMutation.isPending ? 'Posting...' : t('submitReview')}
              </button>
            </form>
          ) : (
            <div className="flex items-center justify-between gap-4 flex-wrap">
              <p className="text-xs text-slate-500 dark:text-slate-400">
                {t('loginToReview')}
              </p>
              <Link
                to="/login"
                className="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold rounded-xl transition"
              >
                {t('login')}
              </Link>
            </div>
          )}
        </div>

        {/* Existing Reviews List */}
        <div className="space-y-4">
          {reviewsLoading ? (
            <div className="space-y-3">
              {[...Array(3)].map((_, i) => (
                <div key={i} className="h-20 bg-slate-100 dark:bg-slate-800 rounded-2xl animate-pulse" />
              ))}
            </div>
          ) : reviews.length === 0 ? (
            <div className="text-center py-8 text-slate-400 dark:text-slate-500">
              <MessageSquare className="w-8 h-8 mx-auto mb-2 opacity-50" />
              <p className="text-xs">{t('noReviewsYet')}</p>
            </div>
          ) : (
            <div className="divide-y divide-slate-100 dark:divide-slate-800">
              {reviews.map((rev) => (
                <div key={rev.id} className="py-4 first:pt-0 last:pb-0 space-y-1.5">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <div className="w-7 h-7 rounded-full bg-slate-200 dark:bg-slate-800 text-slate-700 dark:text-slate-300 font-bold text-xs flex items-center justify-center">
                        <User className="w-3.5 h-3.5" />
                      </div>
                      <span className="font-bold text-xs text-slate-800 dark:text-slate-200">
                        {rev.userFullName}
                      </span>
                    </div>
                    <span className="text-[11px] text-slate-400 dark:text-slate-500">
                      {formatDate(rev.createdAt)}
                    </span>
                  </div>

                  <Rating value={rev.rating} size="sm" />

                  {rev.comment && (
                    <p className="text-xs text-slate-600 dark:text-slate-300 leading-relaxed pt-1">
                      {rev.comment}
                    </p>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>
      </section>

      {/* Related Products Section */}
      {relatedProducts.length > 0 && (
        <section className="space-y-6 pt-4">
          <div className="flex items-center justify-between">
            <h3 className="text-xl sm:text-2xl font-bold text-slate-900 dark:text-white">
              Related Products in {product.categoryName}
            </h3>
            <Link
              to={`/products?categoryId=${product.categoryId}`}
              className="text-xs font-bold text-emerald-600 dark:text-emerald-400 hover:underline"
            >
              {t('viewAll')}
            </Link>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
            {relatedProducts.slice(0, 4).map((p) => (
              <ProductCard key={p.id} product={p} />
            ))}
          </div>
        </section>
      )}
    </div>
  );
};
