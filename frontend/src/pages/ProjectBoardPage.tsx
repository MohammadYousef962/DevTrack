import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ChevronRight, Plus, Calendar, Loader2, Circle, CircleDot, Check, type LucideIcon } from 'lucide-react'
import { getProject, getProjectMembers } from '../api/projects'
import { getTasks, createTask } from '../api/tasks'
import { getTeams } from '../api/teams'
import { getErrorMessage } from '../api/errors'
import { useAuth } from '../context/AuthContext'
import { Modal } from '../components/Modal'
import { TaskDetailPanel } from '../components/TaskDetailPanel'
import { projectStatusStyles, taskPriorityStyles, taskPriorityOrder, taskStatusLabels } from '../lib/styles'
import type { ProjectMemberResponse, ProjectResponse } from '../types/project'
import type { TaskResponse, TaskStatus, TaskPriority } from '../types/task'
import type { TeamResponse } from '../types/team'

const columns: { status: TaskStatus; icon: LucideIcon; color: string }[] = [
    { status: 'Backlog', icon: Circle, color: 'text-app-muted' },
    { status: 'ToDo', icon: Circle, color: 'text-app-todo' },
    { status: 'InProgress', icon: CircleDot, color: 'text-app-progress' },
    { status: 'InReview', icon: CircleDot, color: 'text-app-review' },
    { status: 'Done', icon: Check, color: 'text-app-done' },
]

export function ProjectBoardPage() {
    const { projectId } = useParams()
    const id = Number(projectId)
    const { user } = useAuth()
    const [project, setProject] = useState<ProjectResponse | null>(null)
    const [tasks, setTasks] = useState<TaskResponse[]>([])
    const [members, setMembers] = useState<ProjectMemberResponse[]>([])
    const [teams, setTeams] = useState<TeamResponse[]>([])
    const [isLoading, setIsLoading] = useState(true)
    const [loadError, setLoadError] = useState('')
    const [isModalOpen, setIsModalOpen] = useState(false)
    const [selectedTaskId, setSelectedTaskId] = useState<number | null>(null)

    const loadBoard = useCallback(async () => {
        setLoadError('')
        try {
            const [projectData, tasksData, membersData, teamsData] = await Promise.all([
                getProject(id),
                getTasks({ projectId: id, pageSize: 200 }),
                // If members or teams can't be loaded, the board should still work; only the
                // assignee dropdown / the owner-only controls are affected.
                getProjectMembers(id).catch((): ProjectMemberResponse[] => []),
                getTeams().catch((): TeamResponse[] => []),
            ])
            setProject(projectData)
            setTasks(tasksData.items)
            setMembers(membersData)
            setTeams(teamsData)
        } catch (err) {
            setLoadError(getErrorMessage(err))
        } finally {
            setIsLoading(false)
        }
    }, [id])

    useEffect(() => {
        loadBoard()
    }, [loadBoard])

    if (isLoading) {
        return (
            <div className="flex items-center justify-center py-24 text-app-muted">
                <Loader2 className="w-5 h-5 animate-spin mr-2" /> Loading board...
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
                            loadBoard()
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

    const isArchived = project.status === 'Archived'
    const selectedTask = tasks.find((t) => t.id === selectedTaskId) ?? null
    const isTeamOwner = teams.some((t) => t.id === project.teamId && t.ownerId === user?.userId)
    const canManage = user?.role === 'Admin' || isTeamOwner

    function handleTaskUpdated(updated: TaskResponse) {
        setTasks((prev) => prev.map((t) => (t.id === updated.id ? updated : t)))
    }

    function handleTaskDeleted(taskId: number) {
        setTasks((prev) => prev.filter((t) => t.id !== taskId))
        setSelectedTaskId(null)
    }

    return (
        <div className="flex flex-col min-h-full">
            <div className="px-6 py-4 border-b border-app-border bg-app-surface">
                <div className="flex items-center gap-2 text-sm text-app-muted mb-2">
                    <Link to="/projects" className="hover:text-app-text transition-colors">
                        Projects
                    </Link>
                    <ChevronRight className="w-3 h-3" />
                    <span className="text-app-text font-medium">{project.name}</span>
                </div>
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                    <div className="flex items-center gap-3 flex-wrap">
                        <h1 className="text-2xl font-semibold tracking-tight text-app-text">{project.name}</h1>
                        <span className={`px-2 py-0.5 rounded text-[10px] font-semibold border ${projectStatusStyles[project.status]}`}>
                            {project.status}
                        </span>
                        <span className="text-xs text-app-muted">{project.teamName}</span>
                    </div>
                    <button
                        onClick={() => setIsModalOpen(true)}
                        disabled={isArchived}
                        title={isArchived ? 'Archived projects cannot be modified' : undefined}
                        className="bg-app-accent text-white px-3 py-1.5 rounded-md text-sm font-medium hover:bg-app-accentHover transition-colors flex items-center justify-center gap-2 disabled:opacity-40 disabled:cursor-not-allowed"
                    >
                        <Plus className="w-4 h-4" /> New Task
                    </button>
                </div>
            </div>

            <div className="flex-1 overflow-x-auto p-6">
                <div className="flex gap-4 min-w-max items-start">
                    {columns.map((column) => {
                        const columnTasks = tasks.filter((t) => t.status === column.status)
                        return (
                            <div key={column.status} className="w-[300px] shrink-0">
                                <div className="flex items-center gap-2 mb-3 px-1">
                                    <column.icon className={`w-4 h-4 ${column.color}`} />
                                    <h3 className="text-sm font-medium text-app-text">{taskStatusLabels[column.status]}</h3>
                                    <span className="text-sm text-app-muted">{columnTasks.length}</span>
                                </div>
                                <div className="space-y-3">
                                    {columnTasks.length === 0 && (
                                        <div className="border border-dashed border-app-border rounded-lg py-6 text-center text-xs text-app-muted">
                                            No tasks
                                        </div>
                                    )}
                                    {columnTasks.map((task) => (
                                        <TaskCard key={task.id} task={task} onOpen={() => setSelectedTaskId(task.id)} />
                                    ))}
                                </div>
                            </div>
                        )
                    })}
                </div>
            </div>

            <Modal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)} title="New Task">
                <CreateTaskForm
                    projectId={project.id}
                    onCreated={() => {
                        setIsModalOpen(false)
                        loadBoard()
                    }}
                />
            </Modal>

            {selectedTask && (
                <TaskDetailPanel
                    key={selectedTask.id}
                    task={selectedTask}
                    members={members}
                    isArchived={isArchived}
                    canManage={canManage}
                    onClose={() => setSelectedTaskId(null)}
                    onTaskUpdated={handleTaskUpdated}
                    onTaskDeleted={handleTaskDeleted}
                />
            )}
        </div>
    )
}

function TaskCard({ task, onOpen }: { task: TaskResponse; onOpen: () => void }) {
    const today = new Date().toISOString().slice(0, 10)
    const isDone = task.status === 'Done'
    const isOverdue = !!task.dueDate && !isDone && task.dueDate.slice(0, 10) < today

    return (
        <div
            role="button"
            tabIndex={0}
            onClick={onOpen}
            onKeyDown={(e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault()
                    onOpen()
                }
            }}
            className={`task-card cursor-pointer bg-app-surface border border-app-border rounded-lg p-3.5 focus:outline-none focus:border-app-accent ${isDone ? 'opacity-60' : ''
                }`}
        >
            <div className="flex justify-between items-start gap-2 mb-2">
                <span className="text-xs font-medium text-app-muted">#{task.id}</span>
                <span className={`px-1.5 py-0.5 rounded text-[10px] font-semibold border ${taskPriorityStyles[task.priority]}`}>
                    {task.priority}
                </span>
            </div>
            <p className={`text-sm font-medium text-app-text mb-3 leading-snug ${isDone ? 'line-through' : ''}`}>
                {task.title}
            </p>
            <div className="flex items-center justify-between gap-2 text-xs">
                <div className="flex items-center gap-1.5 min-w-0">
                    {task.assigneeName ? (
                        <>
                            <div className="w-5 h-5 rounded-full bg-app-accent flex items-center justify-center text-white text-[10px] font-semibold shrink-0">
                                {task.assigneeName.charAt(0)}
                            </div>
                            <span className="text-app-muted truncate">{task.assigneeName}</span>
                        </>
                    ) : (
                        <span className="text-app-muted">Unassigned</span>
                    )}
                </div>
                {task.dueDate && (
                    <span className={`flex items-center gap-1 shrink-0 ${isOverdue ? 'text-app-bug' : 'text-app-muted'}`}>
                        <Calendar className="w-3 h-3" />
                        {new Date(task.dueDate).toLocaleDateString()}
                    </span>
                )}
            </div>
        </div>
    )
}

function CreateTaskForm({ projectId, onCreated }: { projectId: number; onCreated: () => void }) {
    const [title, setTitle] = useState('')
    const [description, setDescription] = useState('')
    const [priority, setPriority] = useState<TaskPriority>('Medium')
    const [dueDate, setDueDate] = useState('')
    const [error, setError] = useState('')
    const [isSubmitting, setIsSubmitting] = useState(false)

    async function handleSubmit(e: FormEvent) {
        e.preventDefault()
        setError('')
        setIsSubmitting(true)
        try {
            await createTask({
                title,
                description: description || undefined,
                projectId,
                priority,
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
                <label className="block text-xs font-medium text-app-muted mb-1.5">Title</label>
                <input
                    type="text"
                    required
                    maxLength={200}
                    value={title}
                    onChange={(e) => setTitle(e.target.value)}
                    className="w-full bg-app-base border border-app-border rounded-lg px-3 py-2 text-sm text-app-text focus:outline-none focus:border-app-accent focus:ring-1 focus:ring-app-accent"
                />
            </div>
            <div>
                <label className="block text-xs font-medium text-app-muted mb-1.5">Description</label>
                <textarea
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    rows={3}
                    maxLength={4000}
                    className="w-full bg-app-base border border-app-border rounded-lg px-3 py-2 text-sm text-app-text focus:outline-none focus:border-app-accent focus:ring-1 focus:ring-app-accent resize-none"
                />
            </div>
            <div className="grid grid-cols-2 gap-3">
                <div>
                    <label className="block text-xs font-medium text-app-muted mb-1.5">Priority</label>
                    <select
                        value={priority}
                        onChange={(e) => setPriority(e.target.value as TaskPriority)}
                        className="w-full bg-app-base border border-app-border rounded-lg px-3 py-2 text-sm text-app-text focus:outline-none focus:border-app-accent focus:ring-1 focus:ring-app-accent"
                    >
                        {taskPriorityOrder.map((p) => (
                            <option key={p} value={p}>
                                {p}
                            </option>
                        ))}
                    </select>
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
                {isSubmitting ? 'Creating...' : 'Create Task'}
            </button>
        </form>
    )
}