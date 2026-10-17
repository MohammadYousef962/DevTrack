import type { ReactNode } from 'react'
import { Emphasis } from '../components/Emphasis'
import { taskStatusLabels } from './styles'
import type { ActivityLogResponse } from '../types/activity'
import type { TaskStatus } from '../types/task'

function statusLabel(value: string): string {
    return taskStatusLabels[value as TaskStatus] ?? value
}

// Details like "Backlog → InProgress" come as one string. Returns [from, to], or null if it isn't in that shape.
function parseChange(details: string | null): [string, string] | null {
    const [from, to] = (details ?? '').split(' \u2192 ')
    return from && to ? [from, to] : null
}

// Turns a raw log entry into a sentence fragment like: created task #12 "Fix login"
export function describeActivity(entry: ActivityLogResponse): ReactNode {
    const task = <Emphasis>task #{entry.entityId}</Emphasis>
    const details = entry.details

    switch (entry.action) {
        case 'Created project':
            return (
                <>
                    created project <Emphasis>{details ?? `#${entry.entityId}`}</Emphasis>
                </>
            )
        case 'Deleted project':
            return (
                <>
                    deleted project <Emphasis>{details ?? `#${entry.entityId}`}</Emphasis>
                </>
            )
        case 'Updated project':
            return (
                <>
                    edited the project details
                    {details && <> ({details})</>}
                </>
            )
        case 'Changed project status': {
            const change = parseChange(details)
            return change ? (
                <>
                    changed the project status from <Emphasis>{change[0]}</Emphasis> to <Emphasis>{change[1]}</Emphasis>
                </>
            ) : (
                <>changed the project status</>
            )
        }
        case 'Added project member':
            return (
                <>
                    added <Emphasis>{details ?? 'someone'}</Emphasis> to the project
                </>
            )
        case 'Removed project member':
            return (
                <>
                    removed <Emphasis>{details ?? 'someone'}</Emphasis> from the project
                </>
            )
        case 'Created task':
            return (
                <>
                    created {task}
                    {details && <> &ldquo;{details}&rdquo;</>}
                </>
            )
        case 'Updated task':
            return (
                <>
                    edited {task}
                    {details && <> ({details})</>}
                </>
            )
        case 'Changed task status': {
            const change = parseChange(details)
            return change ? (
                <>
                    changed {task} from <Emphasis>{statusLabel(change[0])}</Emphasis> to{' '}
                    <Emphasis>{statusLabel(change[1])}</Emphasis>
                </>
            ) : (
                <>changed the status of {task}</>
            )
        }
        case 'Changed task priority': {
            const change = parseChange(details)
            return change ? (
                <>
                    changed the priority of {task} from <Emphasis>{change[0]}</Emphasis> to <Emphasis>{change[1]}</Emphasis>
                </>
            ) : (
                <>changed the priority of {task}</>
            )
        }
        case 'Assigned task':
            return details && details !== 'Unassigned' ? (
                <>
                    assigned {task} to <Emphasis>{details}</Emphasis>
                </>
            ) : (
                <>unassigned {task}</>
            )
        case 'Deleted task':
            return (
                <>
                    deleted {task}
                    {details && <> &ldquo;{details}&rdquo;</>}
                </>
            )
        case 'Added comment':
            return <>commented on {task}</>
        default:
            return (
                <>
                    {entry.action.toLowerCase()} {entry.entityType.toLowerCase()} #{entry.entityId}
                    {details ? `: ${details}` : ''}
                </>
            )
    }
}