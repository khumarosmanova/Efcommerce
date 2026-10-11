import React from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate, Link } from 'react-router-dom';
import { ordersApi } from '../../api/endpoints';
import { useCart } from '../../context/CartContext';
import { useAuth } from '../../context/AuthContext';
import { useTranslation } from '../../context/LanguageContext';
import { formatCurrency } from '../../utils/storage';
import {
  MapPin,
  CreditCard,
  ShoppingBag,
  ShieldCheck,
  ArrowRight,
  ArrowLeft,
} from 'lucide-react';
import toast from 'react-hot-toast';

const checkoutSchema = z.object({
  fullName: z.string().min(3, 'Full name is required'),
  email: z.string().email('Valid email required'),
  address: z.string().min(5, 'Street address is required'),
  city: z.string().min(2, 'City is required'),
  postalCode: z.string().min(2, 'Postal code is required'),
  country: z.string().min(2, 'Country is required'),
});

export const CheckoutPage = () => {
  const { cart, itemCount, totalAmount } = useCart();
  const { user, isAdmin } = useAuth();
  const { t } = useTranslation();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(checkoutSchema),
    defaultValues: {
      fullName: user ? `${user.firstName} ${user.lastName}` : '',
      email: user?.email || '',
      address: '',
      city: '',
      postalCode: '',
      country: 'Azerbaijan',
    },
  });

  const checkoutMutation = useMutation({
    mutationFn: (shippingAddress) => ordersApi.checkout({ shippingAddress }),
    onSuccess: (res) => {
      queryClient.invalidateQueries({ queryKey: ['cart'] });
      queryClient.invalidateQueries({ queryKey: ['my-orders'] });
      toast.success(t('orderSuccess'));
      const orderId = res.data?.id;
      navigate(`/orders/${orderId}`, { replace: true });
    },
    onError: (err) => {
      const msg = err.response?.data?.message || 'Failed to place order';
      toast.error(msg);
    },
  });

  const onSubmit = (data) => {
    const formattedAddress = `${data.fullName}, ${data.address}, ${data.city}, ${data.postalCode}, ${data.country} (Email: ${data.email})`;
    checkoutMutation.mutate(formattedAddress);
  };

  if (isAdmin) {
    return (
      <div className="max-w-md mx-auto px-4 py-16 text-center space-y-4">
        <ShieldCheck className="w-12 h-12 text-emerald-600 mx-auto" />
        <h2 className="text-xl font-bold text-slate-900 dark:text-white">{t('adminNoticeTitle')}</h2>
        <p className="text-xs text-slate-500 dark:text-slate-400">{t('adminNoticeDesc')}</p>
        <Link to="/admin/statistics" className="inline-block px-5 py-2.5 bg-emerald-600 text-white rounded-xl text-xs font-bold">
          {t('goToDashboard')}
        </Link>
      </div>
    );
  }

  if (itemCount === 0) {
    return (
      <div className="max-w-md mx-auto px-4 py-16 text-center space-y-4">
        <ShoppingBag className="w-12 h-12 text-slate-300 dark:text-slate-600 mx-auto" />
        <h2 className="text-xl font-bold text-slate-900 dark:text-white">{t('emptyCart')}</h2>
        <p className="text-xs text-slate-500 dark:text-slate-400">{t('emptyCartDesc')}</p>
        <Link to="/products" className="inline-block px-5 py-2.5 bg-emerald-600 text-white rounded-xl text-xs font-bold">
          {t('shopNow')}
        </Link>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-8">
      <Link to="/cart" className="inline-flex items-center gap-2 text-xs font-bold text-slate-600 dark:text-slate-400 hover:text-emerald-600 dark:hover:text-emerald-400 transition">
        <ArrowLeft className="w-4 h-4" /> {t('cart')}
      </Link>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-10 items-start">
        {/* Shipping Form */}
        <div className="lg:col-span-2 space-y-6">
          <div className="bg-white dark:bg-slate-900 p-6 sm:p-8 rounded-3xl border border-slate-200/80 dark:border-slate-800 shadow-xs space-y-6">
            <div className="flex items-center gap-3 pb-4 border-b border-slate-100 dark:border-slate-800">
              <div className="w-10 h-10 rounded-xl bg-emerald-50 dark:bg-emerald-950/40 text-emerald-600 dark:text-emerald-400 flex items-center justify-center font-bold">
                <MapPin className="w-5 h-5" />
              </div>
              <div>
                <h2 className="text-lg font-bold text-slate-900 dark:text-white">{t('shippingAddress')}</h2>
                <p className="text-xs text-slate-400 dark:text-slate-500">{t('addressPlaceholder')}</p>
              </div>
            </div>

            <form id="checkout-form" onSubmit={handleSubmit(onSubmit)} className="space-y-4">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-bold uppercase tracking-wider text-slate-700 dark:text-slate-300 mb-1">
                    {t('firstName')} & {t('lastName')}
                  </label>
                  <input
                    type="text"
                    {...register('fullName')}
                    className={`w-full px-3.5 py-2.5 bg-white dark:bg-slate-800 border rounded-xl text-sm text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-emerald-500/20 ${
                      errors.fullName ? 'border-red-400' : 'border-slate-200 dark:border-slate-700 focus:border-emerald-500'
                    }`}
                  />
                  {errors.fullName && <p className="text-[11px] text-red-500 mt-1">{errors.fullName.message}</p>}
                </div>

                <div>
                  <label className="block text-xs font-bold uppercase tracking-wider text-slate-700 dark:text-slate-300 mb-1">
                    {t('email')}
                  </label>
                  <input
                    type="email"
                    {...register('email')}
                    className={`w-full px-3.5 py-2.5 bg-white dark:bg-slate-800 border rounded-xl text-sm text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-emerald-500/20 ${
                      errors.email ? 'border-red-400' : 'border-slate-200 dark:border-slate-700 focus:border-emerald-500'
                    }`}
                  />
                  {errors.email && <p className="text-[11px] text-red-500 mt-1">{errors.email.message}</p>}
                </div>
              </div>

              <div>
                <label className="block text-xs font-bold uppercase tracking-wider text-slate-700 dark:text-slate-300 mb-1">
                  {t('shippingAddress')}
                </label>
                <input
                  type="text"
                  placeholder="123 Nizami Street, Apt 14"
                  {...register('address')}
                  className={`w-full px-3.5 py-2.5 bg-white dark:bg-slate-800 border rounded-xl text-sm text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-emerald-500/20 ${
                    errors.address ? 'border-red-400' : 'border-slate-200 dark:border-slate-700 focus:border-emerald-500'
                  }`}
                />
                {errors.address && <p className="text-[11px] text-red-500 mt-1">{errors.address.message}</p>}
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                <div>
                  <label className="block text-xs font-bold uppercase tracking-wider text-slate-700 dark:text-slate-300 mb-1">
                    City
                  </label>
                  <input
                    type="text"
                    {...register('city')}
                    className={`w-full px-3.5 py-2.5 bg-white dark:bg-slate-800 border rounded-xl text-sm text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-emerald-500/20 ${
                      errors.city ? 'border-red-400' : 'border-slate-200 dark:border-slate-700 focus:border-emerald-500'
                    }`}
                  />
                  {errors.city && <p className="text-[11px] text-red-500 mt-1">{errors.city.message}</p>}
                </div>

                <div>
                  <label className="block text-xs font-bold uppercase tracking-wider text-slate-700 dark:text-slate-300 mb-1">
                    Postal Code
                  </label>
                  <input
                    type="text"
                    {...register('postalCode')}
                    className={`w-full px-3.5 py-2.5 bg-white dark:bg-slate-800 border rounded-xl text-sm text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-emerald-500/20 ${
                      errors.postalCode ? 'border-red-400' : 'border-slate-200 dark:border-slate-700 focus:border-emerald-500'
                    }`}
                  />
                  {errors.postalCode && <p className="text-[11px] text-red-500 mt-1">{errors.postalCode.message}</p>}
                </div>

                <div>
                  <label className="block text-xs font-bold uppercase tracking-wider text-slate-700 dark:text-slate-300 mb-1">
                    Country
                  </label>
                  <input
                    type="text"
                    {...register('country')}
                    className={`w-full px-3.5 py-2.5 bg-white dark:bg-slate-800 border rounded-xl text-sm text-slate-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-emerald-500/20 ${
                      errors.country ? 'border-red-400' : 'border-slate-200 dark:border-slate-700 focus:border-emerald-500'
                    }`}
                  />
                  {errors.country && <p className="text-[11px] text-red-500 mt-1">{errors.country.message}</p>}
                </div>
              </div>
            </form>
          </div>

          {/* Payment Method */}
          <div className="bg-white dark:bg-slate-900 p-6 sm:p-8 rounded-3xl border border-slate-200/80 dark:border-slate-800 shadow-xs flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-blue-50 dark:bg-blue-950/40 text-blue-600 dark:text-blue-400 flex items-center justify-center font-bold">
                <CreditCard className="w-5 h-5" />
              </div>
              <div>
                <h3 className="text-sm font-bold text-slate-900 dark:text-white">{t('paymentMethod')}</h3>
                <p className="text-xs text-slate-500 dark:text-slate-400">{t('cashOnDelivery')}</p>
              </div>
            </div>
            <span className="text-xs font-bold text-emerald-600 dark:text-emerald-400 bg-emerald-50 dark:bg-emerald-950/40 px-3 py-1 rounded-full">
              Standard
            </span>
          </div>
        </div>

        {/* Order Summary Sidebar */}
        <div className="bg-white dark:bg-slate-900 p-6 sm:p-8 rounded-3xl border border-slate-200/80 dark:border-slate-800 shadow-xs space-y-6 sticky top-24">
          <h3 className="font-extrabold text-slate-900 dark:text-white text-lg">{t('subtotal')}</h3>

          <div className="divide-y divide-slate-100 dark:divide-slate-800 max-h-60 overflow-y-auto pr-1">
            {cart.items.map((item) => (
              <div key={item.id} className="py-2.5 flex items-center justify-between text-xs">
                <div className="truncate max-w-[170px]">
                  <p className="font-bold text-slate-800 dark:text-slate-200 truncate">{item.productName}</p>
                  <p className="text-slate-400 dark:text-slate-500">{t('quantity')}: {item.quantity}</p>
                </div>
                <span className="font-extrabold text-slate-900 dark:text-white">
                  {formatCurrency(item.unitPrice * item.quantity)}
                </span>
              </div>
            ))}
          </div>

          <div className="border-t border-slate-100 dark:border-slate-800 pt-4 space-y-2 text-xs">
            <div className="flex justify-between text-slate-600 dark:text-slate-300">
              <span>{t('subtotal')}</span>
              <span className="font-bold text-slate-900 dark:text-white">{formatCurrency(totalAmount)}</span>
            </div>
            <div className="flex justify-between text-slate-600 dark:text-slate-300">
              <span>{t('shipping')}</span>
              <span className="font-bold text-emerald-600 dark:text-emerald-400">{t('freeShipping')}</span>
            </div>
            <div className="flex justify-between text-base pt-2 border-t border-slate-100 dark:border-slate-800">
              <span className="font-black text-slate-900 dark:text-white">{t('total')}</span>
              <span className="font-black text-xl text-emerald-600 dark:text-emerald-400">{formatCurrency(totalAmount)}</span>
            </div>
          </div>

          <button
            type="submit"
            form="checkout-form"
            disabled={checkoutMutation.isPending}
            className="w-full py-4 px-6 bg-emerald-600 hover:bg-emerald-500 text-white font-bold text-sm rounded-2xl shadow-lg shadow-emerald-600/25 transition flex items-center justify-center gap-2 disabled:opacity-60 disabled:cursor-not-allowed"
          >
            {checkoutMutation.isPending ? (
              <div className="w-5 h-5 border-2 border-white border-t-transparent rounded-full animate-spin" />
            ) : (
              <>
                {t('placeOrder')} <ArrowRight className="w-4 h-4" />
              </>
            )}
          </button>

          <div className="flex items-center justify-center gap-2 text-[11px] text-slate-400 dark:text-slate-500 pt-1">
            <ShieldCheck className="w-4 h-4 text-emerald-600 dark:text-emerald-400" />
            <span>Encrypted transactional checkout</span>
          </div>
        </div>
      </div>
    </div>
  );
};
