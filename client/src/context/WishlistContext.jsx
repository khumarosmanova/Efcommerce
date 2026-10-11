import React, { createContext, useContext, useEffect, useState } from 'react';
import { wishlistApi } from '../api/endpoints';
import { useAuth } from './AuthContext';
import { useTranslation } from './LanguageContext';
import toast from 'react-hot-toast';

const WishlistContext = createContext({
  wishlistIds: [],
  wishlistCount: 0,
  isInWishlist: () => false,
  toggleWishlist: () => {},
  loading: false,
});

export const WishlistProvider = ({ children }) => {
  const { isAuthenticated, isAdmin } = useAuth();
  const { t } = useTranslation();
  const [wishlistIds, setWishlistIds] = useState(() => {
    try {
      const local = localStorage.getItem('shophub_guest_wishlist');
      return local ? JSON.parse(local) : [];
    } catch {
      return [];
    }
  });
  const [loading, setLoading] = useState(false);

  // Sync with backend when authenticated (non-admin)
  useEffect(() => {
    let isMounted = true;
    if (isAdmin) {
      setWishlistIds([]);
    } else if (isAuthenticated) {
      setLoading(true);
      wishlistApi.getIds()
        .then((res) => {
          if (isMounted && res?.data) {
            setWishlistIds(res.data);
          }
        })
        .catch(() => {
          // silently handle or keep local
        })
        .finally(() => {
          if (isMounted) setLoading(false);
        });
    }
    return () => { isMounted = false; };
  }, [isAuthenticated, isAdmin]);

  const isInWishlist = (productId) => {
    if (isAdmin) return false;
    return wishlistIds.includes(Number(productId));
  };

  const toggleWishlist = async (product) => {
    if (isAdmin) {
      toast.error(t('adminCannotWishlist'));
      return;
    }
    const id = Number(product.id);
    const currentlyIn = isInWishlist(id);

    // Optimistic UI update
    setWishlistIds((prev) =>
      currentlyIn ? prev.filter((item) => item !== id) : [...prev, id]
    );

    if (isAuthenticated) {
      try {
        const res = await wishlistApi.toggle(id);
        if (res.data?.isAdded) {
          toast.success(t('addedToWishlist'));
        } else {
          toast.success(t('removedFromWishlist'));
        }
      } catch (err) {
        // Rollback
        setWishlistIds((prev) =>
          currentlyIn ? [...prev, id] : prev.filter((item) => item !== id)
        );
        toast.error('Failed to update wishlist');
      }
    } else {
      // Guest local storage
      const updated = currentlyIn
        ? wishlistIds.filter((item) => item !== id)
        : [...wishlistIds, id];
      localStorage.setItem('shophub_guest_wishlist', JSON.stringify(updated));
      toast.success(currentlyIn ? t('removedFromWishlist') : t('addedToWishlist'));
    }
  };

  return (
    <WishlistContext.Provider
      value={{
        wishlistIds: isAdmin ? [] : wishlistIds,
        wishlistCount: isAdmin ? 0 : wishlistIds.length,
        isInWishlist,
        toggleWishlist,
        loading,
      }}
    >
      {children}
    </WishlistContext.Provider>
  );
};

export const useWishlist = () => useContext(WishlistContext);
