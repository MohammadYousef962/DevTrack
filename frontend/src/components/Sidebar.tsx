import { Link, useLocation } from 'react-router-dom'
import { LayoutDashboard, FolderKanban, CheckSquare, Users, Activity, Shield, Layers } from 'lucide-react'
import { useAuth } from '../context/AuthContext'
import { roleLabel } from '../lib/roles'
import { Avatar } from './Avatar'

const navItems = [
    { label: 'Dashboard', icon: LayoutDashboard, to: '/dashboard', adminOnly: false },
    { label: 'Projects', icon: FolderKanban, to: '/projects', adminOnly: false },
    { label: 'My Tasks', icon: CheckSquare, to: '/my-tasks', adminOnly: false },
    { label: 'Teams', icon: Users, to: '/teams', adminOnly: false },
    { label: 'Activity', icon: Activity, to: '/activity', adminOnly: false },
    { label: 'Admin', icon: Shield, to: '/admin', adminOnly: true },
]

// The navigation itself, shared by the desktop sidebar and the mobile drawer.
// onNavigate lets the drawer close itself when a link is tapped.
export function SidebarContent({ onNavigate }: { onNavigate?: () => void }) {
    const location = useLocation()
    const { user, logout } = useAuth()
    const visibleItems = navItems.filter((item) => !item.adminOnly || user?.role === 'Admin')
    const onProfile = location.pathname === '/profile'

    return (
        <div className="flex-1 min-h-0 flex flex-col justify-between">
            <div className="overflow-y-auto">
                <div className="h-14 flex items-center px-4 border-b border-app-border">
                    <div className="flex items-center gap-2">
                        <div className="w-6 h-6 rounded bg-app-accent flex items-center justify-center text-white">
                            <Layers className="w-3.5 h-3.5" />
                        </div>
                        <span className="text-sm font-semibold tracking-tight">DevTrack</span>
                    </div>
                </div>

                <nav className="p-3 space-y-0.5">
                    {visibleItems.map((item) => {
                        const isActive = location.pathname.startsWith(item.to)
                        return (
                            <Link
                                key={item.to}
                                to={item.to}
                                onClick={onNavigate}
                                className={`flex items-center gap-2.5 px-3 py-1.5 text-sm font-medium rounded-md transition-colors ${isActive ? 'text-app-text bg-app-hover' : 'text-app-muted hover:text-app-text hover:bg-app-hover'
                                    }`}
                            >
                                <item.icon className="w-4 h-4" />
                                {item.label}
                            </Link>
                        )
                    })}
                </nav>
            </div>

            <div className="p-3 border-t border-app-border">
                <Link
                    to="/profile"
                    onClick={onNavigate}
                    className={`flex items-center gap-3 px-3 py-2 rounded-md transition-colors ${onProfile ? 'bg-app-hover' : 'hover:bg-app-hover'
                        }`}
                >
                    <Avatar name={user?.fullName ?? '?'} imageUrl={user?.profileImageUrl} size="sm" />
                    <div className="flex-1 min-w-0">
                        <p className="text-sm font-medium text-app-text truncate">{user?.fullName}</p>
                        <p className="text-xs text-app-muted truncate">{user ? roleLabel(user.role) : ''}</p>
                    </div>
                </Link>
                <button
                    onClick={logout}
                    className="w-full mt-1 flex items-center gap-2.5 px-3 py-1.5 text-sm font-medium rounded-md text-app-muted hover:text-app-text hover:bg-app-hover transition-colors"
                >
                    Log out
                </button>
            </div>
        </div>
    )
}

export function Sidebar() {
    return (
        <aside className="w-64 flex-shrink-0 border-r border-app-border bg-app-surface hidden md:flex flex-col">
            <SidebarContent />
        </aside>
    )
}