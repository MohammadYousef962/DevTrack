import { Link } from 'react-router-dom'

interface ProjectTabsProps {
    projectId: number
    active: 'board' | 'activity' | 'settings'
}

const tabs = [
    { key: 'board', label: 'Board', suffix: '' },
    { key: 'activity', label: 'Activity', suffix: '/activity' },
    { key: 'settings', label: 'Settings', suffix: '/settings' },
] as const

export function ProjectTabs({ projectId, active }: ProjectTabsProps) {
    return (
        <div className="px-6 flex items-center gap-6 border-b border-app-border bg-app-surface text-sm font-medium">
            {tabs.map((tab) => (
                <Link
                    key={tab.key}
                    to={`/projects/${projectId}${tab.suffix}`}
                    className={`py-3 border-b-2 transition-colors ${active === tab.key
                            ? 'border-app-accent text-app-text'
                            : 'border-transparent text-app-muted hover:text-app-text'
                        }`}
                >
                    {tab.label}
                </Link>
            ))}
        </div>
    )
}