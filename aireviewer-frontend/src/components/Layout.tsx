import React from 'react';
import { Outlet } from 'react-router-dom';
import { Navbar } from './Navbar';

export const Layout: React.FC = () => {
  return (
    <div className="min-h-screen bg-gradient-to-b from-gray-50 to-white">
      <Navbar />
      <main className="mx-auto max-w-screen-2xl px-4 sm:px-6 lg:px-8 py-8 fade-in">
        <Outlet />
      </main>
    </div>
  );
};