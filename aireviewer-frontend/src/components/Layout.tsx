import React from 'react';
import { Outlet } from 'react-router-dom';
import { Navbar } from './Navbar';

export const Layout: React.FC = () => {
  return (
    <div className="min-h-screen bg-gradient-to-b from-gray-50 to-white dark:from-gray-900 dark:to-gray-950">
      <Navbar />
      <main className="mx-auto max-w-screen-2xl [max-width:var(--max-w)] px-4 sm:px-6 lg:px-8 [padding-left:var(--pad-x)] [padding-right:var(--pad-x)] py-8 fade-in max-container">
        <Outlet />
      </main>
    </div>
  );
};