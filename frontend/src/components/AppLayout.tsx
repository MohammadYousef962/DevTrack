import { useEffect, useState } from 'react'
import { Outlet } from 'react-router-dom'
import { Menu, Layers } from 'lucide-react'
import { Sidebar, SidebarContent } from './Sidebar'

export function AppLayout() {
    const [isNavOpen, setIsNavOpen] = useState(false)

    useEffect(() => {
        if (!isNavOpen) return
        function handleKeyDown(e: KeyboardEvent) {
            if (e.key === 'Escape') setIsNavOpen(false)
        }
        window.addEventListener('keydown', handleKeyDown)
        return () => window.removeEventListener('keydown', handleKeyDown)
    }, [isNavOpen])

    return (
        <div className="h-dvh w-full flex bg-app-base overflow-hidden">
            <Sidebar />

            <main className="flex-1 flex flex-col min-w-0 h-full">
                <header className="md:hidden h-14 border-b border-app-border bg-app-surface flex items-center gap-3 px-4 shrink-0">
                    <button
                        onClick={() => setIsNavOpen(true)}
                        aria-label="Open navigation"
                        className="p-1.5 -ml-1.5 text-app-muted hover:text-app-text rounded hover:bg-app-hover transition-colors"
                    >
                        <Menu className="w-5 h-5" />
                    </button>
                    <div className="flex items-center gap-2">
                        <div className="w-6 h-6 rounded bg-app-accent flex items-center justify-center text-white">
                            <Layers className="w-3.5 h-3.5" />
                        </div>
                        <span className="text-sm font-semibold tracking-tight">DevTrack</span>
                    </div>
                </header>

                <div className="flex-1 overflow-y-auto">
                    <Outlet />
                </div>
            </main>

            {isNavOpen && (
                <div className="md:hidden fixed inset-0 z-50">
                    <div className="absolute inset-0 bg-black/60 modal-backdrop" onClick={() => setIsNavOpen(false)} />
                    <div
                        role="dialog"
                        aria-modal="true"
                        aria-label="Navigation"
                        className="slide-in-left absolute inset-y-0 left-0 w-64 max-w-[85%] bg-app-surface border-r border-app-border shadow-2xl flex flex-col"
                    >
                        <SidebarContent onNavigate={() => setIsNavOpen(false)} />
                    </div>
                </div>
            )}
        </div>
    )
}