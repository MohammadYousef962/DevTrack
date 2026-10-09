import { Routes, Route, Navigate } from 'react-router-dom'
import { LoginPage } from './pages/LoginPage'
import { RegisterPage } from './pages/RegisterPage'
import { DashboardPage } from './pages/DashboardPage'
import { ProjectsPage } from './pages/ProjectsPage'
import { ProjectBoardPage } from './pages/ProjectBoardPage'
import { TeamsPage } from './pages/TeamsPage'
import { TeamDetailPage } from './pages/TeamDetailPage'
import { ProtectedRoute } from './components/ProtectedRoute'
import { AppLayout } from './components/AppLayout'

function App() {
    return (
        <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />

            <Route
                element={
                    <ProtectedRoute>
                        <AppLayout />
                    </ProtectedRoute>
                }
            >
                <Route path="/dashboard" element={<DashboardPage />} />
                <Route path="/projects" element={<ProjectsPage />} />
                <Route path="/projects/:projectId" element={<ProjectBoardPage />} />
                <Route path="/teams" element={<TeamsPage />} />
                <Route path="/teams/:teamId" element={<TeamDetailPage />} />
            </Route>

            <Route path="/" element={<Navigate to="/dashboard" replace />} />
        </Routes>
    )
}

export default App