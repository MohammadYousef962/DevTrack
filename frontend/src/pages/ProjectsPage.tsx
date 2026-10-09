import { useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { Plus, Calendar, Loader2 } from 'lucide-react'
import { getProjects, createProject } from '../api/projects'
import { getTeams } from '../api/teams'
import { getErrorMessage } from '../api/errors'
import { useAuth } from '../context/AuthContext'
import { Modal } from '../components/Modal'
import { projectStatusStyles } from '../lib/styles'
import type { ProjectResponse } from '../types/project'
import type { TeamResponse } from '../types/team'

export function ProjectsPage() {
    const { user } = useAuth()
    const [projects, setProjects] = useState<ProjectResponse[]>([])
    const [teams, setTeams] = useState<TeamResponse[]>([])
    const [isLoading, setIsLoading] = useState(true)
    const [loadError, setLoadError] = useState('')
    const [isModalOpen, setIsModalOpen] = useState(false)

    useEffect(() => {
        loadData()
    }, [])

    async function loadData() {
        setIsLoading(true)
        setLoadError('')
        try {
            const [projectsData, teamsData] = await Promise.all([getProjects(), getTeams()])
            setProjects(projectsData)
            setTeams(teamsData)
        } catch (err) {
            setLoadError(getErrorMessage(err))
        } finally {
            setIsLoading(false)
        }
    }

    const eligibleTeams =
        user?.role === 'Admin' ? teams : teams.filter((t) => t.ownerId === user?.userId)

    return (
        <div className="p-6 md:p-8 max-w-6xl mx-auto space-y-6">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-app-border pb-4">
                <div>
                    <h1 className="text-2xl font-semibold tracking-tight text-app-text">Projects</h1>
                    <p className="text-sm text-app-muted mt-1">Every project you own or belong to.</p>
                </div>
                <button
                    onClick={() => setIsModalOpen(true)}
                    disabled={eligibleTeams.length === 0}
                    title={eligibleTeams.length === 0 ? 'You need to own a team before creating a project' : undefined}
                    className="bg-app-text text-app-base px-3 py-1.5 rounded-md text-sm font-medium hover:bg-gray-200 transition-colors flex items-center justify-center gap-2 disabled:opacity-40 disabled:cursor-not-allowed"
                >
                    <Plus className="w-4 h-4" /> New Project
                </button>
            </div>

            {isLoading && (
                <div className="flex items-center justify-center py-16 text-app-muted">
                    <Loader2 className="w-5 h-5 animate-spin mr-2" /> Loading projects...
                </div>
            )}

            {!isLoading && loadError && (
                <div className="text-center py-16">
                    <p className="text-app-bug text-sm mb-3">{loadError}</p>
                    <button onClick={loadData} className="text-sm text-app-accent hover:underline">
                        Try again
                    </button>
                </div>
            )}

            {!isLoading && !loadError && projects.length === 0 && (
                <div className="text-center py-16 border border-dashed border-app-border rounded-xl">
                    <p className="text-app-text font-medium mb-1">No projects yet</p>
                    <p className="text-app-muted text-sm">
                        {eligibleTeams.length === 0
                            ? 'Own a team first, then create your first project.'
                            : 'Create your first project to get started.'}
                    </p>
                </div>
            )}

            {!isLoading && !loadError && projects.length > 0 && (
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                    {projects.map((project) => (
                        <Link
                            key={project.id}
                            to={`/projects/${project.id}`}
                            className="bg-app-surface border border-app-border rounded-xl p-5 flex flex-col h-full hover:border-app-borderLighter transition-colors"
                        >
                            <div className="flex justify-between items-start mb-3">
                                <span className="text-xs font-medium text-app-muted">{project.teamName}</span>
                                <span className={`px-2 py-0.5 rounded text-[10px] font-semibold border ${projectStatusStyles[project.status]}`}>
                                    {project.status}
                                </span>
                            </div>
                            <h3 className="text-base font-semibold text-app-text mb-1">{project.name}</h3>
                            <p className="text-sm text-app-muted line-clamp-2 mb-4 flex-1">
                                {project.description || 'No description.'}
                            </p>
                            {project.dueDate && (
                                <div className="flex items-center gap-1 text-xs text-app-muted pt-3 border-t border-app-border/50">
                                    <Calendar className="w-3 h-3" />
                                    Due {new Date(project.dueDate).toLocaleDateString()}
                                </div>
                            )}
                        </Link>
                    ))}
                </div>
            )}

            <Modal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)} title="New Project">
                <CreateProjectForm
                    teams={eligibleTeams}
                    onCreated={() => {
                        setIsModalOpen(false)
                        loadData()
                    }}
                />
            </Modal>
        </div>
    )
}

function CreateProjectForm({ teams, onCreated }: { teams: TeamResponse[]; onCreated: () => void }) {
    const [name, setName] = useState('')
    const [description, setDescription] = useState('')
    const [teamId, setTeamId] = useState(teams[0]?.id ?? 0)
    const [startDate, setStartDate] = useState(() => new Date().toISOString().slice(0, 10))
    const [dueDate, setDueDate] = useState('')
    const [error, setError] = useState('')
    const [isSubmitting, setIsSubmitting] = useState(false)

    async function handleSubmit(e: FormEvent) {
        e.preventDefault()
        setError('')
        setIsSubmitting(true)
        try {
            await createProject({
                name,
                description: description || undefined,
                teamId,
                startDate,
                dueDate: dueDate || undefined,
            })
            onCreated()
        } catch (err) {
            setError(getErrorMessage(err))
        } finally {
            setIsSubmitting(false)
        }
    }

    return (
        <form onSubmit={handleSubmit} className="space-y-4">
            <div>
                <label className="block text-xs font-medium text-app-muted mb-1.5">Name</label>
                <input
                    type="text"
                    required
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    className="w-full bg-app-base border border-app-border rounded-lg px-3 py-2 text-sm text-app-text focus:outline-none focus:border-app-accent focus:ring-1 focus:ring-app-accent"
                />
            </div>
            <div>
                <label className="block text-xs font-medium text-app-muted mb-1.5">Description</label>
                <textarea
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    rows={2}
                    className="w-full bg-app-base border border-app-border rounded-lg px-3 py-2 text-sm text-app-text focus:outline-none focus:border-app-accent focus:ring-1 focus:ring-app-accent resize-none"
                />
            </div>
            <div>
                <label className="block text-xs font-medium text-app-muted mb-1.5">Team</label>
                <select
                    required
                    value={teamId}
                    onChange={(e) => setTeamId(Number(e.target.value))}
                    className="w-full bg-app-base border border-app-border rounded-lg px-3 py-2 text-sm text-app-text focus:outline-none focus:border-app-accent focus:ring-1 focus:ring-app-accent"
                >
                    {teams.map((t) => (
                        <option key={t.id} value={t.id}>{t.name}</option>
                    ))}
                </select>
            </div>
            <div className="grid grid-cols-2 gap-3">
                <div>
                    <label className="block text-xs font-medium text-app-muted mb-1.5">Start date</label>
                    <input
                        type="date"
                        required
                        value={startDate}
                        onChange={(e) => setStartDate(e.target.value)}
                        className="w-full bg-app-base border border-app-border rounded-lg px-3 py-2 text-sm text-app-text focus:outline-none focus:border-app-accent focus:ring-1 focus:ring-app-accent"
                    />
                </div>
                <div>
                    <label className="block text-xs font-medium text-app-muted mb-1.5">Due date</label>
                    <input
                        type="date"
                        value={dueDate}
                        onChange={(e) => setDueDate(e.target.value)}
                        className="w-full bg-app-base border border-app-border rounded-lg px-3 py-2 text-sm text-app-text focus:outline-none focus:border-app-accent focus:ring-1 focus:ring-app-accent"
                    />
                </div>
            </div>

            {error && <p className="text-sm text-app-bug">{error}</p>}

            <button
                type="submit"
                disabled={isSubmitting}
                className="w-full bg-app-accent text-white font-medium text-sm py-2.5 rounded-lg hover:bg-app-accentHover transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
                {isSubmitting ? 'Creating...' : 'Create Project'}
            </button>
        </form>
    )
}