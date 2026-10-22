import { useEffect, useState } from 'react'
import { Link, Outlet, useLocation, useParams } from 'react-router-dom'
import { ChevronRight, Loader2 } from 'lucide-react'
import { getProject } from '../api/projects'
import { getErrorMessage } from '../api/errors'
import type { ProjectContextValue } from '../context/useProjectContext'
import { projectStatusStyles } from '../lib/styles'
import type { ProjectResponse } from '../types/project'
import { ProjectTabs } from './ProjectTabs'

// The frame around a project's Board, Activity and Settings pages. It loads the project once, draws the
// header and tabs, and hands the project to whichever page is open, so switching tabs doesn't reload it.
export function ProjectLayout() {
    const { projectId } = useParams()
    // The key gives each project a fresh start, e.g. when browser history jumps from one project to another.
    return <ProjectFrame key={projectId} id={Number(projectId)} />
}

function ProjectFrame({ id }: { id: number }) {
    const location = useLocation()
    const [project, setProject] = useState<ProjectResponse | null>(null)
    const [isLoading, setIsLoading] = useState(true)
    const [loadError, setLoadError] = useState('')
    const [attempt, setAttempt] = useState(0)
    // An empty spot in the header that pages can fill, e.g. the Board's "New Task" button.
    const [headerActions, setHeaderActions] = useState<HTMLElement | null>(null)

    useEffect(() => {
        let cancelled = false
        getProject(id)
            .then((data) => {
                if (!cancelled) {
                    setProject(data)
                    setLoadError('')
                }
            })
            .catch((err) => {
                if (!cancelled) setLoadError(getErrorMessage(err))
            })
            .finally(() => {
                if (!cancelled) setIsLoading(false)
            })
        return () => {
            cancelled = true
        }
    }, [id, attempt])

    if (isLoading) {
        return (
            <div className="flex items-center justify-center py-24 text-app-muted">
                <Loader2 className="w-5 h-5 animate-spin mr-2" /> Loading project...
            </div>
        )
    }

    if (loadError || !project) {
        return (
            <div className="text-center py-24">
                <p className="text-app-bug text-sm mb-3">{loadError || 'Project not found.'}</p>
                <div className="flex items-center justify-center gap-4 text-sm">
                    <button
                        onClick={() => {
                            setIsLoading(true)
                            setAttempt((n) => n + 1)
                        }}
                        className="text-app-accent hover:underline"
                    >
                        Try again
                    </button>
                    <Link to="/projects" className="text-app-muted hover:text-app-text">
                        Back to projects
                    </Link>
                </div>
            </div>
        )
    }

    const path = location.pathname.replace(/\/$/, '')
    const section = path.endsWith('/activity') ? 'Activity' : path.endsWith('/settings') ? 'Settings' : null
    const context: ProjectContextValue = { project, setProject, headerActions }

    return (
        <div className="flex flex-col min-h-full">
            <div className="px-6 py-4 border-b border-app-border bg-app-surface">
                <div className="flex items-center gap-2 text-sm text-app-muted mb-2">
                    <Link to="/projects" className="hover:text-app-text transition-colors">
                        Projects
                    </Link>
                    <ChevronRight className="w-3 h-3" />
                    {section ? (
                        <>
                            <Link to={`/projects/${project.id}`} className="hover:text-app-text transition-colors">
                                {project.name}
                            </Link>
                            <ChevronRight className="w-3 h-3" />
                            <span className="text-app-text font-medium">{section}</span>
                        </>
                    ) : (
                        <span className="text-app-text font-medium">{project.name}</span>
                    )}
                </div>
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                    <div className="flex items-center gap-3 flex-wrap">
                        <h1 className="text-2xl font-semibold tracking-tight text-app-text">{project.name}</h1>
                        <span className={`px-2 py-0.5 rounded text-[10px] font-semibold border ${projectStatusStyles[project.status]}`}>
                            {project.status}
                        </span>
                        <span className="text-xs text-app-muted">{project.teamName}</span>
                    </div>
                    <div ref={setHeaderActions} className="flex items-center empty:hidden" />
                </div>
            </div>

            <ProjectTabs projectId={project.id} />

            <Outlet context={context} />
        </div>
    )
}