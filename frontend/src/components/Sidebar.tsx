import { Link, useLocation } from 'react-router-dom'
import { LayoutDashboard, FolderKanban, CheckSquare, Users, Activity, Settings, Layers } from 'lucide-react'
import { useAuth } from '../context/AuthContext'

const navItems = [
    { label: 'Dashboard', icon: LayoutDashboard, to: '/dashboard' },
    { label: 'Projects', icon: FolderKanban, to: '/projects' },
    { label: 'Teams', icon: Users, to: '/teams' },
]

const comingSoonItems = [
    { label: 'My Tasks', icon: CheckSquare },
    { label: 'Activity', icon: Activity },
]

export function Sidebar() {
  const location = useLocation()
  const { user, logout } = useAuth()

  return (
    <aside className="w-64 flex-shrink-0 border-r border-app-border bg-app-surface hidden md:flex flex-col justify-between">
      <div>
        <div className="h-14 flex items-center px-4 border-b border-app-border">
          <div className="flex items-center gap-2">
            <div className="w-6 h-6 rounded bg-app-accent flex items-center justify-center text-white">
              <Layers className="w-3.5 h-3.5" />
            </div>
            <span className="text-sm font-semibold tracking-tight">DevTrack</span>
          </div>
        </div>

        <nav className="p-3 space-y-0.5">
          {navItems.map((item) => {
              const isActive = location.pathname.startsWith(item.to)
              return (
              <Link
                key={item.to}
                to={item.to}
                className={`flex items-center gap-2.5 px-3 py-1.5 text-sm font-medium rounded-md transition-colors ${
                  isActive ? 'text-app-text bg-app-hover' : 'text-app-muted hover:text-app-text hover:bg-app-hover'
                }`}
              >
                <item.icon className="w-4 h-4" />
                {item.label}
              </Link>
            )
          })}

          {comingSoonItems.map((item) => (
            <div
              key={item.label}
              className="flex items-center gap-2.5 px-3 py-1.5 text-sm font-medium rounded-md text-app-muted/50 cursor-not-allowed"
              title="Coming soon"
            >
              <item.icon className="w-4 h-4" />
              {item.label}
            </div>
          ))}
        </nav>
      </div>

      <div className="p-3 border-t border-app-border">
        <div
          className="flex items-center gap-2.5 px-3 py-1.5 text-sm font-medium rounded-md text-app-muted/50 cursor-not-allowed mb-1"
          title="Coming soon"
        >
          <Settings className="w-4 h-4" />
          Settings
        </div>
        <div className="flex items-center gap-3 px-3 py-2 rounded-md">
          <div className="w-7 h-7 rounded-full bg-app-accent flex items-center justify-center text-white text-xs font-semibold shrink-0">
            {user?.fullName?.charAt(0) ?? '?'}
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-sm font-medium text-app-text truncate">{user?.fullName}</p>
            <p className="text-xs text-app-muted truncate">{user?.role}</p>
          </div>
        </div>
        <button
          onClick={logout}
          className="w-full mt-1 flex items-center gap-2.5 px-3 py-1.5 text-sm font-medium rounded-md text-app-muted hover:text-app-text hover:bg-app-hover transition-colors"
        >
          Log out
        </button>
      </div>
    </aside>
  )
}