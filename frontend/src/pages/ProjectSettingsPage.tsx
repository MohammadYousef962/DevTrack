import { useEffect, useState, type FormEvent, type ReactNode } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ChevronRight, Loader2, UserPlus } from 'lucide-react'
import {
    addProjectMember,
    deleteProject,
    getProject,
    getProjectMembers,
    removeProjectMember,
    updateProject,
    updateProjectStatus,
} from '../api/projects'
import { getTasks } from '../api/tasks'
import { getTeamMembers, getTeams } from '../api/teams'
import { getErrorMessage } from '../api/errors'
import { useAuth } from '../context/AuthContext'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { ProjectTabs } from '../components/ProjectTabs'
import { projectStatusStyles } from '../lib/styles'
import type { ProjectMemberResponse, ProjectResponse, ProjectStatus } from '../types/project'
import type { TeamMemberResponse, TeamResponse } from '../types/team'

const inputClass =
    'w-full bg-app-base border border-app-border rounded-lg px-3 py-2 text-sm text-app-text focus:outline-none focus:border-app-accent focus:ring-1 focus:ring-app-accent'
const selectClass = `${inputClass} disabled:opacity-60 disabled:cursor-not-allowed`
const statuses: ProjectStatus[] = ['Planning', 'Active', 'Completed', 'Archived']

export function ProjectSettingsPage() {
    const { projectId } = useParams()
    const id = Number(projectId)
    const { user } = useAuth()
    const navigate = useNavigate()

    const [project, setProject] = useState<ProjectResponse | null>(null)
    const [members, setMembers] = useState<ProjectMemberResponse[]>([])
    const [teamMembers, setTeamMembers] = useState<TeamMemberResponse[] | null>(null)
    const [taskCount, setTaskCount] = useState<number | null>(null)
    const [canManage, setCanManage] = useState(false)
    const [isLoading, setIsLoading] = useState(true)
    const [loadError, setLoadError] = useState('')
    const [attempt, setAttempt] = useState(0)

    const [isDeleteOpen, setIsDeleteOpen] = useState(false)
    const [deleteError, setDeleteError] = useState('')
    const [isDeleting, setIsDeleting] = useState(false)

    const role = user?.role
    const userId = user?.userId

    useEffect(() => {
        let cancelled = false
        Promise.all([
            getProject(id),
            getProjectMembers(id),
            getTeams().catch((): TeamResponse[] => []),
            getTasks({ projectId: id, pageSize: 1 }).catch(() => null),
        ])
            .then(async ([projectData, membersData, teamsData, tasksData]) => {
                const manage =
                    role === 'Admin' || teamsData.some((t) => t.id === projectData.teamId && t.ownerId === userId)
                // Only managers can add members, so only they need the team's member list.
                const teamMembersData = manage ? await getTeamMembers(projectData.teamId).catch(() => null) : null
                if (cancelled) return
                setProject(projectData)
                setMembers(membersData)
                setTaskCount(tasksData ? tasksData.totalCount : null)
                setCanManage(manage)
                setTeamMembers(teamMembersData)
                setLoadError('')
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
    }, [id, role, userId, attempt])

    async function handleDeleteProject() {
        setDeleteError('')
        setIsDeleting(true)
        try {
            await deleteProject(id)
            navigate('/projects')
        } catch (err) {
            setDeleteError(getErrorMessage(err))
            setIsDeleting(false)
        }
    }

    if (isLoading) {
        return (
            <div className="flex items-center justify-center py-24 text-app-muted">
                <Loader2 className="w-5 h-5 animate-spin mr-2" /> Loading settings...
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

    const taskNote =
        taskCount === null
            ? 'All of its tasks and their comments will be deleted too.'
            : taskCount === 0
                ? 'It has no tasks.'
                : `Its ${taskCount} task${taskCount === 1 ? '' : 's'} and their comments will be deleted too.`

    return (
        <div className="flex flex-col min-h-full">
            <div className="px-6 py-4 border-b border-app-border bg-app-surface">
                <div className="flex items-center gap-2 text-sm text-app-muted mb-2">
                    <Link to="/projects" className="hover:text-app-text transition-colors">
                        Projects
                    </Link>
                    <ChevronRight className="w-3 h-3" />
                    <Link to={`/projects/${project.id}`} className="hover:text-app-text transition-colors">
                        {project.name}
                    </Link>
                    <ChevronRight className="w-3 h-3" />
                    <span className="text-app-text font-medium">Settings</span>
                </div>
                <div className="flex items-center gap-3 flex-wrap">
                    <h1 className="text-2xl font-semibold tracking-tight text-app-text">{project.name}</h1>
                    <span className={`px-2 py-0.5 rounded text-[10px] font-semibold border ${projectStatusStyles[project.status]}`}>
                        {project.status}
                    </span>
                    <span className="text-xs text-app-muted">{project.teamName}</span>
                </div>
            </div>

            <ProjectTabs projectId={project.id} active="settings" />

            <div className="p-6 md:p-8 max-w-3xl w-full mx-auto space-y-10">
                <Section title="Details">
                    <DetailsForm project={project} canManage={canManage} onSaved={setProject} />
                </Section>

                <Section
                    title="Status"
                    description="Archiving makes a project's details, members, tasks and comments read-only. You can change the status back at any time."
                >
                    <StatusControl project={project} canManage={canManage} onChanged={setProject} />
                </Section>

                <Section
                    title={`Members (${members.length})`}
                    description={`Members can be assigned tasks. Only people on ${project.teamName} can be added.`}
                >
                    <MembersSection
                        project={project}
                        canManage={canManage}
                        members={members}
                        teamMembers={teamMembers}
                        onChange={setMembers}
                    />
                </Section>

                {canManage && (
                    <Section title="Danger zone">
                        <div className="border border-app-bug/30 rounded-xl p-4 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
                            <div>
                                <p className="text-sm font-medium text-app-text">Delete this project</p>
                                <p className="text-xs text-app-muted mt-0.5">
                                    Permanently removes the project, its tasks and their comments.
                                </p>
                            </div>
                            <button
                                onClick={() => {
                                    setDeleteError('')
                                    setIsDeleteOpen(true)
                                }}
                                className="shrink-0 px-3 py-1.5 rounded-md text-sm font-medium text-app-bug border border-app-bug/40 hover:bg-app-bug/10 transition-colors"
                            >
                                Delete project
                            </button>
                        </div>
                    </Section>
                )}
            </div>

            <ConfirmDialog
                isOpen={isDeleteOpen}
                title="Delete project"
                message={`Delete "${project.name}"? ${taskNote} This can't be undone.`}
                confirmLabel="Delete"
                isProcessing={isDeleting}
                error={deleteError}
                onConfirm={handleDeleteProject}
                onCancel={() => setIsDeleteOpen(false)}
            />
        </div>
    )
}

function Section({ title, description, children }: { title: string; description?: string; children: ReactNode }) {
    return (
        <section className="space-y-3">
            <div>
                <h2 className="text-sm font-semibold tracking-wide uppercase text-app-muted">{title}</h2>
                {description && <p className="text-xs text-app-muted mt-1">{description}</p>}
            </div>
            {children}
        </section>
    )
}

function DetailsForm({
    project,
    canManage,
    onSaved,
}: {
    project: ProjectResponse
    canManage: boolean
    onSaved: (project: ProjectResponse) => void
}) {
    const [name, setName] = useState(project.name)
    const [description, setDescription] = useState(project.description ?? '')
    const [startDate, setStartDate] = useState(project.startDate.slice(0, 10))
    const [dueDate, setDueDate] = useState(project.dueDate ? project.dueDate.slice(0, 10) : '')
    const [error, setError] = useState('')
    const [saved, setSaved] = useState(false)
    const [isSaving, setIsSaving] = useState(false)

    const isArchived = project.status === 'Archived'
    const locked = !canManage || isArchived

    async function handleSubmit(e: FormEvent) {
        e.preventDefault()
        const trimmedName = name.trim()
        if (!trimmedName) return
        setError('')
        setSaved(false)
        setIsSaving(true)
        try {
            const updated = await updateProject(project.id, {
                name: trimmedName,
                description: description.trim() || null,
                startDate,
                dueDate: dueDate || null,
            })
            onSaved(updated)
            setSaved(true)
        } catch (err) {
            setError(getErrorMessage(err))
        } finally {
            setIsSaving(false)
        }
    }

    return (
        <form onSubmit={handleSubmit} className="space-y-4">
            <fieldset disabled={locked || isSaving} className="space-y-4 disabled:opacity-60">
                <div>
                    <label className="block text-xs font-medium text-app-muted mb-1.5">Name</label>
                    <input
                        type="text"
                        required
                        maxLength={150}
                        value={name}
                        onChange={(e) => {
                            setName(e.target.value)
                            setSaved(false)
                        }}
                        className={inputClass}
                    />
                </div>
                <div>
                    <label className="block text-xs font-medium text-app-muted mb-1.5">Description</label>
                    <textarea
                        rows={3}
                        maxLength={2000}
                        value={description}
                        onChange={(e) => {
                            setDescription(e.target.value)
                            setSaved(false)
                        }}
                        className={`${inputClass} resize-none`}
                    />
                </div>
                <div className="grid grid-cols-2 gap-3">
                    <div>
                        <label className="block text-xs font-medium text-app-muted mb-1.5">Start date</label>
                        <input
                            type="date"
                            required
                            value={startDate}
                            onChange={(e) => {
                                setStartDate(e.target.value)
                                setSaved(false)
                            }}
                            className={inputClass}
                        />
                    </div>
                    <div>
                        <label className="block text-xs font-medium text-app-muted mb-1.5">Due date</label>
                        <input
                            type="date"
                            value={dueDate}
                            onChange={(e) => {
                                setDueDate(e.target.value)
                                setSaved(false)
                            }}
                            className={inputClass}
                        />
                    </div>
                </div>
            </fieldset>

            {!canManage && (
                <p className="text-xs text-app-muted">Only the team owner or an admin can edit project details.</p>
            )}
            {canManage && isArchived && (
                <p className="text-xs text-app-muted">
                    Archived projects can't be edited. Change the status below to unarchive it first.
                </p>
            )}
            {error && <p className="text-sm text-app-bug">{error}</p>}
            {saved && <p className="text-sm text-app-done">Changes saved.</p>}

            {!locked && (
                <button
                    type="submit"
                    disabled={isSaving || !name.trim()}
                    className="bg-app-accent text-white px-3 py-1.5 rounded-md text-sm font-medium hover:bg-app-accentHover transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                    {isSaving ? 'Saving...' : 'Save changes'}
                </button>
            )}
        </form>
    )
}

function StatusControl({
    project,
    canManage,
    onChanged,
}: {
    project: ProjectResponse
    canManage: boolean
    onChanged: (project: ProjectResponse) => void
}) {
    const [error, setError] = useState('')
    const [isSaving, setIsSaving] = useState(false)
    const [confirmArchive, setConfirmArchive] = useState(false)

    async function apply(status: ProjectStatus) {
        setError('')
        setIsSaving(true)
        try {
            onChanged(await updateProjectStatus(project.id, status))
            setConfirmArchive(false)
        } catch (err) {
            setError(getErrorMessage(err))
        } finally {
            setIsSaving(false)
        }
    }

    function handleChange(next: ProjectStatus) {
        if (next === 'Archived') {
            setError('')
            setConfirmArchive(true)
        } else {
            apply(next)
        }
    }

    return (
        <div className="space-y-2">
            <div className="flex items-center gap-3">
                <select
                    value={project.status}
                    disabled={!canManage || isSaving}
                    onChange={(e) => handleChange(e.target.value as ProjectStatus)}
                    className={`${selectClass} max-w-xs`}
                >
                    {statuses.map((s) => (
                        <option key={s} value={s}>
                            {s}
                        </option>
                    ))}
                </select>
                {isSaving && !confirmArchive && <Loader2 className="w-4 h-4 animate-spin text-app-muted" />}
            </div>

            {!canManage && (
                <p className="text-xs text-app-muted">Only the team owner or an admin can change a project's status.</p>
            )}
            {error && !confirmArchive && <p className="text-sm text-app-bug">{error}</p>}

            <ConfirmDialog
                isOpen={confirmArchive}
                title="Archive project"
                message={`Archive "${project.name}"? While it's archived, its details, members, tasks and comments are read-only. You can change the status back at any time.`}
                confirmLabel="Archive"
                isProcessing={isSaving}
                error={error}
                onConfirm={() => apply('Archived')}
                onCancel={() => {
                    setConfirmArchive(false)
                    setError('')
                }}
            />
        </div>
    )
}

function MembersSection({
    project,
    canManage,
    members,
    teamMembers,
    onChange,
}: {
    project: ProjectResponse
    canManage: boolean
    members: ProjectMemberResponse[]
    teamMembers: TeamMemberResponse[] | null
    onChange: (members: ProjectMemberResponse[]) => void
}) {
    const { user } = useAuth()
    const [selectedUserId, setSelectedUserId] = useState('')
    const [addError, setAddError] = useState('')
    const [isAdding, setIsAdding] = useState(false)
    const [toRemove, setToRemove] = useState<ProjectMemberResponse | null>(null)
    const [removeError, setRemoveError] = useState('')
    const [isRemoving, setIsRemoving] = useState(false)

    const isArchived = project.status === 'Archived'
    const canEdit = canManage && !isArchived
    const available = (teamMembers ?? []).filter((tm) => !members.some((m) => m.userId === tm.userId))

    async function handleAdd(e: FormEvent) {
        e.preventDefault()
        if (!selectedUserId) return
        setAddError('')
        setIsAdding(true)
        try {
            const added = await addProjectMember(project.id, Number(selectedUserId))
            onChange([...members, added])
            setSelectedUserId('')
        } catch (err) {
            setAddError(getErrorMessage(err))
        } finally {
            setIsAdding(false)
        }
    }

    async function handleRemove() {
        if (!toRemove) return
        setRemoveError('')
        setIsRemoving(true)
        try {
            await removeProjectMember(project.id, toRemove.userId)
            onChange(members.filter((m) => m.userId !== toRemove.userId))
            setToRemove(null)
        } catch (err) {
            setRemoveError(getErrorMessage(err))
        } finally {
            setIsRemoving(false)
        }
    }

    return (
        <div className="space-y-3">
            <div className="bg-app-surface border border-app-border rounded-xl overflow-hidden">
                {members.length === 0 ? (
                    <p className="p-6 text-sm text-app-muted text-center">No one is on this project yet.</p>
                ) : (
                    <div className="divide-y divide-app-border">
                        {members.map((m) => (
                            <div key={m.userId} className="p-3 flex items-center gap-3">
                                <div className="w-8 h-8 rounded-full bg-app-accent flex items-center justify-center text-white text-xs font-semibold shrink-0">
                                    {m.fullName.charAt(0)}
                                </div>
                                <div className="min-w-0 flex-1">
                                    <p className="text-sm font-medium text-app-text truncate">
                                        {m.fullName}
                                        {m.userId === user?.userId ? ' (you)' : ''}
                                    </p>
                                    <p className="text-xs text-app-muted truncate">{m.email}</p>
                                </div>
                                {canEdit && (
                                    <button
                                        onClick={() => {
                                            setRemoveError('')
                                            setToRemove(m)
                                        }}
                                        className="text-xs text-app-muted hover:text-app-bug transition-colors"
                                    >
                                        Remove
                                    </button>
                                )}
                            </div>
                        ))}
                    </div>
                )}

                {canEdit && teamMembers !== null && available.length > 0 && (
                    <form
                        onSubmit={handleAdd}
                        className={`p-3 flex gap-2 ${members.length > 0 ? 'border-t border-app-border' : ''}`}
                    >
                        <select
                            value={selectedUserId}
                            onChange={(e) => setSelectedUserId(e.target.value)}
                            className={`${selectClass} flex-1 min-w-0 py-1.5`}
                        >
                            <option value="">Add a team member...</option>
                            {available.map((tm) => (
                                <option key={tm.userId} value={tm.userId}>
                                    {tm.fullName} ({tm.email})
                                </option>
                            ))}
                        </select>
                        <button
                            type="submit"
                            disabled={isAdding || !selectedUserId}
                            className="flex items-center gap-1.5 bg-app-accent text-white px-3 py-1.5 rounded-lg text-sm font-medium hover:bg-app-accentHover transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                        >
                            {isAdding ? <Loader2 className="w-4 h-4 animate-spin" /> : <UserPlus className="w-4 h-4" />}
                            Add
                        </button>
                    </form>
                )}
            </div>

            {addError && <p className="text-sm text-app-bug">{addError}</p>}

            {canEdit && teamMembers === null && (
                <p className="text-xs text-app-muted">
                    Couldn't load the team's members, so people can't be added right now.
                </p>
            )}
            {canEdit && teamMembers !== null && available.length === 0 && (
                <p className="text-xs text-app-muted">
                    Everyone on {project.teamName} is already on this project. To add someone else,{' '}
                    <Link to={`/teams/${project.teamId}`} className="text-app-accent hover:underline">
                        add them to the team
                    </Link>{' '}
                    first.
                </p>
            )}
            {canManage && isArchived && (
                <p className="text-xs text-app-muted">Members can't be changed while the project is archived.</p>
            )}

            <ConfirmDialog
                isOpen={toRemove !== null}
                title="Remove member"
                message={
                    toRemove
                        ? `Remove ${toRemove.fullName} from this project? Any tasks assigned to them in this project will become unassigned.`
                        : ''
                }
                confirmLabel="Remove"
                isProcessing={isRemoving}
                error={removeError}
                onConfirm={handleRemove}
                onCancel={() => setToRemove(null)}
            />
        </div>
    )
}