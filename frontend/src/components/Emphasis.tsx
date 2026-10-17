import type { ReactNode } from 'react'

// Highlights the key words inside an activity sentence.
export function Emphasis({ children }: { children: ReactNode }) {
    return <span className="font-medium text-app-text">{children}</span>
}