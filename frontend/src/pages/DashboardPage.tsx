import { useAuth } from '../context/AuthContext'

export function DashboardPage() {
    const { user, logout } = useAuth()

    return (
        <div className="h-screen w-full flex flex-col items-center justify-center gap-4 bg-app-base">
            <span className="text-2xl font-bold text-app-text">Welcome, {user?.fullName}</span>
            <span className="text-app-muted text-sm">Role: {user?.role}</span>
            <button
                onClick={logout}
                className="px-4 py-2 rounded-md bg-app-surface border border-app-border text-app-text text-sm hover:bg-app-hover transition-colors"
            >
                Log out
            </button>
        </div>
    )
}