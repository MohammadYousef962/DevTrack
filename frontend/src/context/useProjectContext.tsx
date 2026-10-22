import { useOutletContext } from 'react-router-dom'
import type { ProjectResponse } from '../types/project'

export interface ProjectContextValue {
    project: ProjectResponse
    setProject: (project: ProjectResponse) => void
    // An empty spot in the project header that a page can fill, e.g. the Board's "New Task" button.
    headerActions: HTMLElement | null
}

export function useProjectContext() {
    return useOutletContext<ProjectContextValue>()
}