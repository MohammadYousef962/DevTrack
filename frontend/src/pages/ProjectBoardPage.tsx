import { useEffect, useState, type FormEvent } from 'react'
import { createPortal } from 'react-dom'
import { useSearchParams } from 'react-router-dom'
import { Plus, Calendar, Loader2, Circle, CircleDot, Check, type LucideIcon } from 'lucide-react'
import { getProjectMembers } from '../api/projects'
import { getTasks, createTask } from '../api/tasks'
import { getTeams } from '../api/teams'
import { getErrorMessage } from '../api/errors'
import { useAuth } from '../context/AuthContext'
import { useProjectContext } from '../context/useProjectContext'
import { todayLocal } from '../lib/dates'
import { Modal } from '../components/Modal'
import { TaskDetailPanel } from '../components/TaskDetailPanel'
import { taskPriorityStyles, taskPriorityOrder, taskStatusLabels } from '../lib/styles'
import type { ProjectMemberResponse } from '../types/project'
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
    const { project, headerActions } = useProjectContext()
    const id = project.id
    const { user } = useAuth()
    const [searchParams, setSearchParams] = useSearchParams()
    const [tasks, setTasks] = useState<TaskResponse[]>([])
    const [totalTaskCount, setTotalTaskCount] = useState(0)
    const [members, setMembers] = useState<ProjectMemberResponse[]>([])
    const [teams, setTeams] = useState<TeamResponse[]>([])
    const [isLoading, setIsLoading] = useState(true)
    const [loadError, setLoadError] = useState('')
    const [isModalOpen, setIsModalOpen] = useState(false)
    const [attempt, setAttempt] = useState(0)

    // The open task lives in the URL (?task=12), so a task can be linked to and survives a refresh.
    const taskParam = Number(searchParams.get('task'))
    const selectedTaskId = Number.isInteger(taskParam) && taskParam > 0 ? taskParam : null

    useEffect(() => {
        let cancelled = false
        Promise.all([
            getTasks({ projectId: id, pageSize: 200 }),
            // If members or teams can't be loaded, the board should still work; only the
            // assignee dropdown / the owner-only controls are affected.
            getProjectMembers(id).catch((): ProjectMemberResponse[] => []),
            getTeams().catch((): TeamResponse[] => []),
        ])
            .then(([tasksData, membersData, teamsData]) => {
                if (cancelled) return
                setTasks(tasksData.items)
                setTotalTaskCount(tasksData.totalCount)
                setMembers(membersData)
                setTeams(teamsData)
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
    }, [id, attempt])

    function openTask(taskId: number) {
        const next = new URLSearchParams(searchParams)
        next.set('task', String(taskId))
        setSearchParams(next, { replace: true })
    }

    function closeTask() {
        const next = new URLSearchParams(searchParams)
        next.delete('task')
        setSearchParams(next, { replace: true })
    }

    if (isLoading) {
        return (
            <div className="flex items-center justify-center py-24 text-app-muted">
                <Loader2 className="w-5 h-5 animate-spin mr-2" /> Loading board...
            </div>
        )
    }

    if (loadError) {
        return (
            <div className="text-center py-24">
                <p className="text-app-bug text-sm mb-3">{loadError}</p>
                <button
                    onClick={() => {
                        setIsLoading(true)
                        setAttempt((n) => n + 1)
                    }}
                    className="text-sm text-app-accent hover:underline"
                >
                    Try again
                </button>
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
        setTotalTaskCount((n) => Math.max(0, n - 1))
        closeTask()
    }

    return (
        <div className="flex flex-col flex-1">
            {/* The button lives in the layout's header; the portal draws it there. */}
            {headerActions &&
                createPortal(
                    <button
                        onClick={() => setIsModalOpen(true)}
                        disabled={isArchived}
                        title={isArchived ? 'Archived projects cannot be modified' : undefined}
                        className="bg-app-accent text-white px-3 py-1.5 rounded-md text-sm font-medium hover:bg-app-accentHover transition-colors flex items-center justify-center gap-2 disabled:opacity-40 disabled:cursor-not-allowed"
                    >
                        <Plus className="w-4 h-4" /> New Task
                    </button>,
                    headerActions,
                )}

            {selectedTaskId !== null && !selectedTask && (
                <div className="mx-6 mt-4 flex items-center justify-between gap-3 rounded-lg border border-app-progress/30 bg-app-progress/10 px-3 py-2 text-sm text-app-progress">
                    <span>Task #{selectedTaskId} wasn't found on this board.</span>
                    <button onClick={closeTask} className="shrink-0 text-xs underline hover:no-underline">
                        Dismiss
                    </button>
                </div>
            )}

            {totalTaskCount > tasks.length && (
                <div className="mx-6 mt-4 rounded-lg border border-app-border bg-app-surface px-3 py-2 text-sm text-app-muted">
                    Showing the newest {tasks.length} of {totalTaskCount} tasks. Older tasks aren't shown on this board yet.
                </div>
            )}

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
                                        <TaskCard key={task.id} task={task} onOpen={() => openTask(task.id)} />
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
                        setAttempt((n) => n + 1)
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
                    onClose={closeTask}
                    onTaskUpdated={handleTaskUpdated}
                    onTaskDeleted={handleTaskDeleted}
                />
            )}
        </div>
    )
}

function TaskCard({ task, onOpen }: { task: TaskResponse; onOpen: () => void }) {
    const today = todayLocal()
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