/* eslint-disable react-refresh/only-export-components */
import type { ReactNode } from 'react'
import { taskStatusLabels } from './styles'
import type { ActivityLogResponse } from '../types/activity'
import type { TaskStatus } from '../types/task'

function Emphasis({ children }: { children: ReactNode }) {
    return <span className="font-medium text-app-text">{children}</span>
}

function statusLabel(value: string): string {
    return taskStatusLabels[value as TaskStatus] ?? value
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
        case 'Created task':
            return (
                <>
                    created {task}
                    {details && <> &ldquo;{details}&rdquo;</>}
                </>
            )
        case 'Changed task status': {
            const [from, to] = (details ?? '').split(' \u2192 ')
            if (from && to) {
                return (
                    <>
                        changed {task} from <Emphasis>{statusLabel(from)}</Emphasis> to <Emphasis>{statusLabel(to)}</Emphasis>
                    </>
                )
            }
            return <>changed the status of {task}</>
        }
        case 'Assigned task':
            return details && details !== 'Unassigned' ? (
                <>
                    assigned {task} to <Emphasis>{details}</Emphasis>
                </>
            ) : (
                <>unassigned {task}</>
            )
        default:
            return (
                <>
                    {entry.action.toLowerCase()} {entry.entityType.toLowerCase()} #{entry.entityId}
                    {details ? `: ${details}` : ''}
                </>
            )
    }
}