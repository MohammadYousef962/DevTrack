import { Routes, Route, Navigate } from 'react-router-dom'
import { LoginPage } from './pages/LoginPage'
import { RegisterPage } from './pages/RegisterPage'
import { DashboardPage } from './pages/DashboardPage'
import { ProjectsPage } from './pages/ProjectsPage'
import { ProjectBoardPage } from './pages/ProjectBoardPage'
import { ProjectActivityPage } from './pages/ProjectActivityPage'
import { ProjectSettingsPage } from './pages/ProjectSettingsPage'
import { MyTasksPage } from './pages/MyTasksPage'
import { ActivityPage } from './pages/ActivityPage'
import { TeamsPage } from './pages/TeamsPage'
import { TeamDetailPage } from './pages/TeamDetailPage'
import { ProfilePage } from './pages/ProfilePage'
import { AdminPage } from './pages/AdminPage'
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
                <Route path="/projects/:projectId/activity" element={<ProjectActivityPage />} />
                <Route path="/projects/:projectId/settings" element={<ProjectSettingsPage />} />
                <Route path="/my-tasks" element={<MyTasksPage />} />
                <Route path="/activity" element={<ActivityPage />} />
                <Route path="/teams" element={<TeamsPage />} />
                <Route path="/teams/:teamId" element={<TeamDetailPage />} />
                <Route path="/profile" element={<ProfilePage />} />
                <Route path="/admin" element={<AdminPage />} />
            </Route>

            <Route path="/" element={<Navigate to="/dashboard" replace />} />
        </Routes>
    )
}

export default App