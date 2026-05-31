import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import LoginPage from './pages/LoginPage';
import HrDashboard from './pages/hr/HrDashboard';
import TaxProfileEditor from './pages/hr/TaxProfileEditor';
import SalarySlip from './pages/employee/SalarySlip';
import RetirementSimulator from './pages/employee/RetirementSimulator';
import KnowledgeBase from './pages/employee/KnowledgeBase';
import PortfolioRegistry from './pages/employee/PortfolioRegistry';
import UserManagement from './pages/admin/UserManagement';

const PrivateRoute: React.FC<{ children: React.ReactNode; rola: string }> = ({ children, rola }) => {
  const token = localStorage.getItem('token');
  const userRola = localStorage.getItem('rola');
  if (!token) return <Navigate to="/login" />;
  if (userRola !== rola) return <Navigate to="/login" />;
  return <>{children}</>;
};

const App: React.FC = () => {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />

        {/* Panel HR */}
        <Route path="/hr" element={
          <PrivateRoute rola="HR"><HrDashboard /></PrivateRoute>
        } />
        <Route path="/hr/tax-profile" element={
          <PrivateRoute rola="HR"><TaxProfileEditor /></PrivateRoute>
        } />

        {/* Panel Pracownika */}
        <Route path="/employee" element={
          <PrivateRoute rola="PRACOWNIK"><SalarySlip /></PrivateRoute>
        } />
        <Route path="/employee/retirement" element={
          <PrivateRoute rola="PRACOWNIK"><RetirementSimulator /></PrivateRoute>
        } />
        <Route path="/employee/portfolio" element={
          <PrivateRoute rola="PRACOWNIK"><PortfolioRegistry /></PrivateRoute>
        } />
        <Route path="/employee/knowledge" element={
          <PrivateRoute rola="PRACOWNIK"><KnowledgeBase /></PrivateRoute>
        } />

        {/* Panel Admin IT */}
        <Route path="/admin" element={
          <PrivateRoute rola="ADMIN_IT"><UserManagement /></PrivateRoute>
        } />

        <Route path="/" element={<Navigate to="/login" />} />
        <Route path="*" element={<Navigate to="/login" />} />
      </Routes>
    </BrowserRouter>
  );
};

export default App;