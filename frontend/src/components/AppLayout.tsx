import { Outlet } from 'react-router-dom'
import { Bell } from 'lucide-react'
import { Sidebar } from './Sidebar'

export function AppLayout() {
  return (
    <div className="h-screen w-full flex bg-app-base overflow-hidden">
      <Sidebar />
      <main className="flex-1 flex flex-col min-w-0 h-full">
        <header className="h-14 border-b border-app-border flex items-center justify-end px-4 shrink-0">
          <button
            className="text-app-muted hover:text-app-text p-1.5 rounded hover:bg-app-hover transition-colors"
            title="Coming soon"
          >
            <Bell className="w-4 h-4" />
          </button>
        </header>
        <div className="flex-1 overflow-y-auto">
          <Outlet />
        </div>
      </main>
    </div>
  )
}