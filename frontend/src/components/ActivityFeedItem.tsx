import { Link } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { describeActivity } from '../lib/activity'
import { formatRelativeTime, parseApiDate } from '../lib/dates'
import type { ActivityLogResponse, RecentActivityResponse } from '../types/activity'

export function ActivityFeedItem({ entry }: { entry: ActivityLogResponse | RecentActivityResponse }) {
    const { user } = useAuth()
    const actor = entry.userId === user?.userId ? 'You' : entry.userName
    const when = parseApiDate(entry.timestamp)
    const projectName = 'projectName' in entry ? entry.projectName : null

    return (
        <div className="p-4 flex gap-3">
            <div className="w-7 h-7 rounded-full bg-app-accent flex items-center justify-center text-white text-xs font-semibold shrink-0">
                {entry.userName.charAt(0)}
            </div>
            <div className="min-w-0 flex-1">
                <p className="text-sm text-app-muted break-words">
                    <span className="font-medium text-app-text">{actor}</span> {describeActivity(entry)}
                </p>
                <p className="text-xs text-app-muted mt-0.5">
                    <span title={when.toLocaleString()}>{formatRelativeTime(when)}</span>
                    {entry.projectId !== null && projectName && (
                        <>
                            {' '}
                            &middot;{' '}
                            <Link to={`/projects/${entry.projectId}`} className="hover:text-app-text hover:underline">
                                {projectName}
                            </Link>
                        </>
                    )}
                </p>
            </div>
        </div>
    )
}