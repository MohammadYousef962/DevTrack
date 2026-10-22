import { NavLink } from 'react-router-dom'

const tabs = [
    { label: 'Board', suffix: '', end: true },
    { label: 'Activity', suffix: '/activity', end: false },
    { label: 'Settings', suffix: '/settings', end: false },
]

export function ProjectTabs({ projectId }: { projectId: number }) {
    return (
        <div className="px-6 flex items-center gap-6 border-b border-app-border bg-app-surface text-sm font-medium">
            {tabs.map((tab) => (
                <NavLink
                    key={tab.label}
                    to={`/projects/${projectId}${tab.suffix}`}
                    end={tab.end}
                    className={({ isActive }) =>
                        `py-3 border-b-2 transition-colors ${isActive
                            ? 'border-app-accent text-app-text'
                            : 'border-transparent text-app-muted hover:text-app-text'
                        }`
                    }
                >
                    {tab.label}
                </NavLink>
            ))}
        </div>
    )
}