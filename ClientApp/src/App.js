// src/App.jsx
import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { createTheme } from '@mui/material/styles';
import SchedulingPage from './pages/SchedulingPage';

// 鍒涘缓涓婚
const theme = createTheme({
    palette: {
        primary: {
            main: '#1976d2',
        },
        secondary: {
            main: '#dc004e',
        },
    },
});

function App() {
    return (
        <Routes>
            <Route path="/scheduling" element={<SchedulingPage />} />
            {/* 添加其他路由 */}
            <Route path="/" element={<Navigate to="/scheduling" />} />
        </Routes>
    );
}

export default App;

