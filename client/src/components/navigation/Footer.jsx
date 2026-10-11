import React from 'react';
import { Link } from 'react-router-dom';
import { ShoppingBag, Heart } from 'lucide-react';

export const Footer = () => {
  return (
    <footer className="bg-slate-900 text-slate-400 text-sm mt-auto">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-8">
          <div className="space-y-4">
            <div className="flex items-center gap-2">
              <div className="w-8 h-8 rounded-lg bg-emerald-600 flex items-center justify-center text-white">
                <ShoppingBag className="w-4 h-4" />
              </div>
              <span className="font-bold text-lg text-white">
                Shop<span className="text-emerald-500">Hub</span>
              </span>
            </div>
            <p className="text-xs leading-relaxed text-slate-400">
              Modern full-stack e-commerce platform built with ASP.NET Core Web API, Entity Framework Core, and React.
            </p>
          </div>

          <div>
            <h4 className="font-semibold text-white mb-3 text-xs tracking-wider uppercase">Shop</h4>
            <ul className="space-y-2 text-xs">
              <li><Link to="/products" className="hover:text-emerald-400 transition">All Products</Link></li>
              <li><Link to="/categories" className="hover:text-emerald-400 transition">Categories</Link></li>
              <li><Link to="/cart" className="hover:text-emerald-400 transition">Shopping Cart</Link></li>
            </ul>
          </div>

          <div>
            <h4 className="font-semibold text-white mb-3 text-xs tracking-wider uppercase">Account</h4>
            <ul className="space-y-2 text-xs">
              <li><Link to="/login" className="hover:text-emerald-400 transition">Sign In</Link></li>
              <li><Link to="/register" className="hover:text-emerald-400 transition">Register</Link></li>
              <li><Link to="/orders" className="hover:text-emerald-400 transition">Order History</Link></li>
            </ul>
          </div>

          <div>
            <h4 className="font-semibold text-white mb-3 text-xs tracking-wider uppercase">System</h4>
            <p className="text-xs text-slate-400 mb-2">Powered by .NET 8 & React + Vite</p>
            <div className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-emerald-950/60 text-emerald-400 text-xs border border-emerald-800/40">
              <span className="w-2 h-2 rounded-full bg-emerald-400 animate-ping"></span>
              API Online & Seeded
            </div>
          </div>
        </div>

        <div className="border-t border-slate-800 mt-12 pt-6 flex flex-col sm:flex-row items-center justify-between text-xs text-slate-500">
          <p>© {new Date().getFullYear()} ShopHub Store. All rights reserved.</p>
          <p className="flex items-center gap-1 mt-2 sm:mt-0">
            Engineered with <Heart className="w-3.5 h-3.5 text-red-500 fill-red-500" /> Onion Architecture
          </p>
        </div>
      </div>
    </footer>
  );
};
