import { useEffect, useState, type FormEvent, type ReactNode } from 'react'
import { X, Calendar, Loader2, Pencil, Trash2 } from 'lucide-react'
import { addComment, deleteComment, getComments, updateComment } from '../api/comments'
import {
    deleteTask,
    updateTask,
    updateTaskAssignee,
    updateTaskPriority,
    updateTaskStatus,
} from '../api/tasks'
import { getErrorMessage } from '../api/errors'
import { useAuth } from '../context/AuthContext'
import { parseApiDate } from '../lib/dates'
import { taskPriorityOrder, taskStatusLabels, taskStatusOrder } from '../lib/styles'
import { ConfirmDialog } from './ConfirmDialog'
import type { CommentResponse } from '../types/comment'
import type { ProjectMemberResponse } from '../types/project'
import type { TaskPriority, TaskResponse, TaskStatus } from '../types/task'

interface TaskDetailPanelProps {
    task: TaskResponse
    members: ProjectMemberResponse[]
    isArchived: boolean
    canManage: boolean
    onClose: () => void
    onTaskUpdated: (task: TaskResponse) => void
    onTaskDeleted: (taskId: number) => void
}

type UpdatableField = 'status' | 'priority' | 'assignee'
type DeleteTarget = { kind: 'task' } | { kind: 'comment'; comment: CommentResponse }

const selectClass =
    'w-full bg-app-base border border-app-border rounded-lg px-2.5 py-1.5 text-sm text-app-text focus:outline-none focus:border-app-accent focus:ring-1 focus:ring-app-accent disabled:opacity-50 disabled:cursor-not-allowed'

const inputClass =
    'w-full bg-app-base border border-app-border rounded-lg px-3 py-2 text-sm text-app-text focus:outline-none focus:border-app-accent focus:ring-1 focus:ring-app-accent'

export function TaskDetailPanel({
    task,
    members,
    isArchived,
    canManage,
    onClose,
    onTaskUpdated,
    onTaskDeleted,
}: TaskDetailPanelProps) {
    const { user } = useAuth()

    // dropdown updates
    const [pendingField, setPendingField] = useState<UpdatableField | null>(null)
    const [actionError, setActionError] = useState('')

    // comments
    const [comments, setComments] = useState<CommentResponse[]>([])
    const [isLoadingComments, setIsLoadingComments] = useState(true)
    const [commentsError, setCommentsError] = useState('')
    const [newComment, setNewComment] = useState('')
    const [commentError, setCommentError] = useState('')
    const [isPostingComment, setIsPostingComment] = useState(false)

    // editing the task
    const [isEditing, setIsEditing] = useState(false)
    const [draftTitle, setDraftTitle] = useState('')
    const [draftDescription, setDraftDescription] = useState('')
    const [draftDueDate, setDraftDueDate] = useState('')
    const [editError, setEditError] = useState('')
    const [isSavingEdit, setIsSavingEdit] = useState(false)

    // editing a comment
    const [editingCommentId, setEditingCommentId] = useState<number | null>(null)
    const [editingContent, setEditingContent] = useState('')
    const [commentEditError, setCommentEditError] = useState('')
    const [isSavingComment, setIsSavingComment] = useState(false)

    // delete confirmation (task or comment)
    const [deleteTarget, setDeleteTarget] = useState<DeleteTarget | null>(null)
    const [deleteError, setDeleteError] = useState('')
    const [isDeleting, setIsDeleting] = useState(false)

    const canEditTask = !isArchived && (canManage || task.creatorId === user?.userId)
    const canDeleteTask = !isArchived && canManage
    const canEditComment = (c: CommentResponse) => !isArchived && c.authorId === user?.userId
    const canDeleteComment = (c: CommentResponse) =>
        !isArchived && (c.authorId === user?.userId || canManage)

    // Escape closes the topmost thing: confirm dialog (handles itself) > open edit > the panel.
    useEffect(() => {
        function handleKeyDown(e: globalThis.KeyboardEvent) {
            if (e.key !== 'Escape' || deleteTarget !== null) return
            if (isEditing) setIsEditing(false)
            else if (editingCommentId !== null) setEditingCommentId(null)
            else onClose()
        }
        window.addEventListener('keydown', handleKeyDown)
        return () => window.removeEventListener('keydown', handleKeyDown)
    }, [onClose, deleteTarget, isEditing, editingCommentId])

    useEffect(() => {
        let cancelled = false
        getComments(task.id)
            .then((data) => {
                if (!cancelled) setComments(data)
            })
            .catch((err) => {
                if (!cancelled) setCommentsError(getErrorMessage(err))
            })
            .finally(() => {
                if (!cancelled) setIsLoadingComments(false)
            })
        return () => {
            cancelled = true
        }
    }, [task.id])

    async function runUpdate(field: UpdatableField, action: () => Promise<TaskResponse>) {
        setActionError('')
        setPendingField(field)
        try {
            onTaskUpdated(await action())
        } catch (err) {
            setActionError(getErrorMessage(err))
        } finally {
            setPendingField(null)
        }
    }

    function startEdit() {
        setDraftTitle(task.title)
        setDraftDescription(task.description ?? '')
        setDraftDueDate(task.dueDate ? task.dueDate.slice(0, 10) : '')
        setEditError('')
        setIsEditing(true)
    }

    async function handleSaveEdit(e: FormEvent) {
        e.preventDefault()
        const title = draftTitle.trim()
        if (!title) return
        setEditError('')
        setIsSavingEdit(true)
        try {
            const updated = await updateTask(task.id, {
                title,
                description: draftDescription.trim() || null,
                dueDate: draftDueDate || null,
            })
            onTaskUpdated(updated)
            setIsEditing(false)
        } catch (err) {
            setEditError(getErrorMessage(err))
        } finally {
            setIsSavingEdit(false)
        }
    }

    async function handleAddComment(e: FormEvent) {
        e.preventDefault()
        const content = newComment.trim()
        if (!content) return
        setCommentError('')
        setIsPostingComment(true)
        try {
            const created = await addComment(task.id, content)
            setComments((prev) => [...prev, created])
            setNewComment('')
        } catch (err) {
            setCommentError(getErrorMessage(err))
        } finally {
            setIsPostingComment(false)
        }
    }

    function startCommentEdit(comment: CommentResponse) {
        setEditingCommentId(comment.id)
        setEditingContent(comment.content)
        setCommentEditError('')
    }

    async function handleSaveComment(e: FormEvent) {
        e.preventDefault()
        if (editingCommentId === null) return
        const content = editingContent.trim()
        if (!content) return
        setCommentEditError('')
        setIsSavingComment(true)
        try {
            const updated = await updateComment(editingCommentId, content)
            setComments((prev) => prev.map((c) => (c.id === updated.id ? updated : c)))
            setEditingCommentId(null)
        } catch (err) {
            setCommentEditError(getErrorMessage(err))
        } finally {
            setIsSavingComment(false)
        }
    }

    async function handleConfirmDelete() {
        if (!deleteTarget) return
        setDeleteError('')
        setIsDeleting(true)
        try {
            if (deleteTarget.kind === 'task') {
                await deleteTask(task.id)
                onTaskDeleted(task.id) // the board removes the task and closes this panel
                return
            }
            const commentId = deleteTarget.comment.id
            await deleteComment(commentId)
            setComments((prev) => prev.filter((c) => c.id !== commentId))
            setDeleteTarget(null)
        } catch (err) {
            setDeleteError(getErrorMessage(err))
        } finally {
            setIsDeleting(false)
        }
    }

    const assigneeOptions = [...members]
    if (task.assigneeId && !members.some((m) => m.userId === task.assigneeId)) {
        assigneeOptions.unshift({
            userId: task.assigneeId,
            fullName: task.assigneeName ?? `User ${task.assigneeId}`,
            email: '',
            joinedAt: '',
        })
    }

    const commentsNote = commentsError
        ? 'Any comments on it will be deleted too.'
        : comments.length > 0
            ? `Its ${comments.length} comment${comments.length === 1 ? '' : 's'} will be deleted too.`
            : ''

    const deleteMessage =
        deleteTarget?.kind === 'task'
            ? [`Delete #${task.id} "${task.title}"?`, commentsNote, "This can't be undone."].filter(Boolean).join(' ')
            : "Delete this comment? This can't be undone."

    const controlsDisabled = isArchived || pendingField !== null

    return (
        <div className="fixed inset-0 z-50">
            <div className="absolute inset-0 bg-black/60 modal-backdrop" onClick={onClose} />

            <div className="slide-in-right absolute inset-y-0 right-0 w-full md:w-[720px] bg-app-surface border-l border-app-border shadow-2xl flex flex-col">
                <div className="h-14 border-b border-app-border flex items-center justify-between px-6 shrink-0">
                    <div className="flex items-center gap-3 text-sm">
                        <span className="font-medium text-app-muted">#{task.id}</span>
                        <div className="h-4 w-px bg-app-border" />
                        <span className="text-app-muted">{task.projectName}</span>
                    </div>
                    <button
                        onClick={onClose}
                        aria-label="Close"
                        className="p-1.5 text-app-muted hover:text-app-text rounded hover:bg-app-hover transition-colors"
                    >
                        <X className="w-4 h-4" />
                    </button>
                </div>

                <div className="flex-1 overflow-y-auto flex flex-col md:flex-row">
                    <div className="flex-1 p-6 md:p-8 md:border-r border-app-border min-w-0">
                        {isEditing ? (
                            <form onSubmit={handleSaveEdit} className="space-y-4 mb-8">
                                <div>
                                    <label className="block text-xs font-medium text-app-muted mb-1.5">Title</label>
                                    <input
                                        type="text"
                                        required
                                        maxLength={200}
                                        value={draftTitle}
                                        onChange={(e) => setDraftTitle(e.target.value)}
                                        className={inputClass}
                                    />
                                </div>
                                <div>
                                    <label className="block text-xs font-medium text-app-muted mb-1.5">Description</label>
                                    <textarea
                                        rows={6}
                                        maxLength={4000}
                                        value={draftDescription}
                                        onChange={(e) => setDraftDescription(e.target.value)}
                                        className={`${inputClass} resize-none`}
                                    />
                                </div>
                                <div>
                                    <label className="block text-xs font-medium text-app-muted mb-1.5">Due date</label>
                                    <input
                                        type="date"
                                        value={draftDueDate}
                                        onChange={(e) => setDraftDueDate(e.target.value)}
                                        className={inputClass}
                                    />
                                </div>
                                {editError && <p className="text-sm text-app-bug">{editError}</p>}
                                <div className="flex gap-2">
                                    <button
                                        type="submit"
                                        disabled={isSavingEdit || !draftTitle.trim()}
                                        className="bg-app-accent text-white px-3 py-1.5 rounded-md text-sm font-medium hover:bg-app-accentHover transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                                    >
                                        {isSavingEdit ? 'Saving...' : 'Save'}
                                    </button>
                                    <button
                                        type="button"
                                        onClick={() => setIsEditing(false)}
                                        disabled={isSavingEdit}
                                        className="px-3 py-1.5 rounded-md text-sm text-app-text border border-app-border hover:bg-app-hover transition-colors disabled:opacity-50"
                                    >
                                        Cancel
                                    </button>
                                </div>
                            </form>
                        ) : (
                            <>
                                <div className="flex items-start justify-between gap-3 mb-4">
                                    <h2 className="text-2xl font-semibold text-app-text break-words min-w-0">{task.title}</h2>
                                    {canEditTask && (
                                        <button
                                            onClick={startEdit}
                                            className="shrink-0 flex items-center gap-1.5 px-2.5 py-1.5 rounded-md text-xs font-medium text-app-muted border border-app-border hover:text-app-text hover:bg-app-hover transition-colors"
                                        >
                                            <Pencil className="w-3.5 h-3.5" /> Edit
                                        </button>
                                    )}
                                </div>
                                <p className="text-sm text-app-muted whitespace-pre-wrap break-words mb-8">
                                    {task.description || 'No description.'}
                                </p>
                            </>
                        )}

                        <hr className="border-app-border mb-6" />

                        <h3 className="text-sm font-semibold text-app-text mb-4">Comments ({comments.length})</h3>

                        {isLoadingComments && (
                            <div className="flex items-center text-sm text-app-muted">
                                <Loader2 className="w-4 h-4 animate-spin mr-2" /> Loading comments...
                            </div>
                        )}

                        {commentsError && <p className="text-sm text-app-bug">{commentsError}</p>}

                        {!isLoadingComments && !commentsError && comments.length === 0 && (
                            <p className="text-sm text-app-muted">No comments yet.</p>
                        )}

                        <div className="space-y-5">
                            {comments.map((c) => {
                                const isEditingThis = editingCommentId === c.id
                                const showEdit = canEditComment(c)
                                const showDelete = canDeleteComment(c)
                                return (
                                    <div key={c.id} className="flex gap-3">
                                        <div className="w-8 h-8 rounded-full bg-app-accent flex items-center justify-center text-white text-xs font-semibold shrink-0">
                                            {c.authorName.charAt(0)}
                                        </div>
                                        <div className="min-w-0 flex-1">
                                            <div className="flex items-baseline gap-2 mb-1 flex-wrap">
                                                <span className="text-sm font-medium text-app-text">
                                                    {c.authorName}
                                                    {c.authorId === user?.userId ? ' (you)' : ''}
                                                </span>
                                                <span className="text-xs text-app-muted">{parseApiDate(c.createdAt).toLocaleString()}</span>
                                                {c.updatedAt && (
                                                    <span
                                                        className="text-xs text-app-muted"
                                                        title={`Edited ${parseApiDate(c.updatedAt).toLocaleString()}`}
                                                    >
                                                        (edited)
                                                    </span>
                                                )}
                                                {!isEditingThis && (showEdit || showDelete) && (
                                                    <span className="ml-auto flex items-center gap-3">
                                                        {showEdit && (
                                                            <button
                                                                onClick={() => startCommentEdit(c)}
                                                                className="text-xs text-app-muted hover:text-app-text transition-colors"
                                                            >
                                                                Edit
                                                            </button>
                                                        )}
                                                        {showDelete && (
                                                            <button
                                                                onClick={() => {
                                                                    setDeleteError('')
                                                                    setDeleteTarget({ kind: 'comment', comment: c })
                                                                }}
                                                                className="text-xs text-app-muted hover:text-app-bug transition-colors"
                                                            >
                                                                Delete
                                                            </button>
                                                        )}
                                                    </span>
                                                )}
                                            </div>

                                            {isEditingThis ? (
                                                <form onSubmit={handleSaveComment}>
                                                    <textarea
                                                        value={editingContent}
                                                        onChange={(e) => setEditingContent(e.target.value)}
                                                        rows={3}
                                                        maxLength={2000}
                                                        autoFocus
                                                        className={`${inputClass} resize-none`}
                                                    />
                                                    {commentEditError && <p className="text-sm text-app-bug mt-2">{commentEditError}</p>}
                                                    <div className="flex gap-2 mt-2">
                                                        <button
                                                            type="submit"
                                                            disabled={isSavingComment || !editingContent.trim()}
                                                            className="bg-app-accent text-white px-3 py-1 rounded text-xs font-medium hover:bg-app-accentHover transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                                                        >
                                                            {isSavingComment ? 'Saving...' : 'Save'}
                                                        </button>
                                                        <button
                                                            type="button"
                                                            onClick={() => setEditingCommentId(null)}
                                                            disabled={isSavingComment}
                                                            className="px-3 py-1 rounded text-xs text-app-text border border-app-border hover:bg-app-hover transition-colors disabled:opacity-50"
                                                        >
                                                            Cancel
                                                        </button>
                                                    </div>
                                                </form>
                                            ) : (
                                                <div className="text-sm text-app-muted bg-app-base border border-app-border rounded-lg p-3 whitespace-pre-wrap break-words">
                                                    {c.content}
                                                </div>
                                            )}
                                        </div>
                                    </div>
                                )
                            })}
                        </div>

                        <form onSubmit={handleAddComment} className="mt-6">
                            <div className="bg-app-base border border-app-border rounded-lg focus-within:border-app-accent focus-within:ring-1 focus-within:ring-app-accent overflow-hidden">
                                <textarea
                                    value={newComment}
                                    onChange={(e) => setNewComment(e.target.value)}
                                    rows={3}
                                    maxLength={2000}
                                    disabled={isArchived}
                                    placeholder={isArchived ? 'Comments are locked on archived projects' : 'Leave a comment...'}
                                    className="w-full bg-transparent p-3 text-sm text-app-text placeholder-app-muted outline-none resize-none disabled:opacity-50"
                                />
                                <div className="px-3 py-2 flex items-center justify-between border-t border-app-border">
                                    <span className="text-xs text-app-muted">{newComment.length}/2000</span>
                                    <button
                                        type="submit"
                                        disabled={isArchived || isPostingComment || !newComment.trim()}
                                        className="bg-app-accent text-white px-3 py-1 rounded text-xs font-medium hover:bg-app-accentHover transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                                    >
                                        {isPostingComment ? 'Posting...' : 'Comment'}
                                    </button>
                                </div>
                            </div>
                            {commentError && <p className="text-sm text-app-bug mt-2">{commentError}</p>}
                        </form>
                    </div>

                    <div className="w-full md:w-60 p-6 space-y-5 shrink-0 bg-app-base/30">
                        {isArchived && (
                            <p className="text-xs text-app-muted border border-app-border rounded-lg p-2.5">
                                This project is archived, so its tasks are read-only.
                            </p>
                        )}

                        <Field label="Status" saving={pendingField === 'status'}>
                            <select
                                value={task.status}
                                disabled={controlsDisabled}
                                onChange={(e) => runUpdate('status', () => updateTaskStatus(task.id, e.target.value as TaskStatus))}
                                className={selectClass}
                            >
                                {taskStatusOrder.map((s) => (
                                    <option key={s} value={s}>
                                        {taskStatusLabels[s]}
                                    </option>
                                ))}
                            </select>
                        </Field>

                        <Field label="Priority" saving={pendingField === 'priority'}>
                            <select
                                value={task.priority}
                                disabled={controlsDisabled}
                                onChange={(e) =>
                                    runUpdate('priority', () => updateTaskPriority(task.id, e.target.value as TaskPriority))
                                }
                                className={selectClass}
                            >
                                {taskPriorityOrder.map((p) => (
                                    <option key={p} value={p}>
                                        {p}
                                    </option>
                                ))}
                            </select>
                        </Field>

                        <Field label="Assignee" saving={pendingField === 'assignee'}>
                            <select
                                value={task.assigneeId ?? ''}
                                disabled={controlsDisabled}
                                onChange={(e) => {
                                    const value = e.target.value
                                    runUpdate('assignee', () => updateTaskAssignee(task.id, value === '' ? null : Number(value)))
                                }}
                                className={selectClass}
                            >
                                <option value="">Unassigned</option>
                                {assigneeOptions.map((m) => (
                                    <option key={m.userId} value={m.userId}>
                                        {m.fullName}
                                    </option>
                                ))}
                            </select>
                        </Field>

                        {actionError && <p className="text-sm text-app-bug">{actionError}</p>}

                        <Field label="Due date">
                            <div className="flex items-center gap-2 text-sm text-app-text">
                                <Calendar className="w-4 h-4 text-app-muted" />
                                {task.dueDate ? new Date(task.dueDate).toLocaleDateString() : 'No due date'}
                            </div>
                        </Field>

                        <div className="pt-4 border-t border-app-border space-y-1">
                            <p className="text-xs text-app-muted">Created by {task.creatorName}</p>
                            <p className="text-xs text-app-muted">{parseApiDate(task.createdAt).toLocaleString()}</p>
                        </div>

                        {canDeleteTask && (
                            <div className="pt-4 border-t border-app-border">
                                <button
                                    onClick={() => {
                                        setDeleteError('')
                                        setDeleteTarget({ kind: 'task' })
                                    }}
                                    className="w-full flex items-center gap-2 px-2.5 py-1.5 rounded-lg text-sm text-app-bug hover:bg-app-bug/10 transition-colors"
                                >
                                    <Trash2 className="w-4 h-4" /> Delete task
                                </button>
                            </div>
                        )}
                    </div>
                </div>
            </div>

            <ConfirmDialog
                isOpen={deleteTarget !== null}
                title={deleteTarget?.kind === 'task' ? 'Delete task' : 'Delete comment'}
                message={deleteMessage}
                confirmLabel="Delete"
                isProcessing={isDeleting}
                error={deleteError}
                onConfirm={handleConfirmDelete}
                onCancel={() => setDeleteTarget(null)}
            />
        </div>
    )
}

function Field({ label, saving, children }: { label: string; saving?: boolean; children: ReactNode }) {
    return (
        <div>
            <div className="flex items-center gap-1.5 mb-1.5">
                <span className="text-xs font-medium text-app-muted">{label}</span>
                {saving && <Loader2 className="w-3 h-3 animate-spin text-app-muted" />}
            </div>
            {children}
        </div>
    )
}