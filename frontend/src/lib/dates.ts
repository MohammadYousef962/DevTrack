
export function parseApiDate(value: string): Date {
    const hasTimezone = /([zZ]|[+-]\d{2}:?\d{2})$/.test(value)
    return new Date(hasTimezone ? value : `${value}Z`)
}

export function formatRelativeTime(date: Date): string {
    const seconds = Math.round((Date.now() - date.getTime()) / 1000)
    if (seconds < 45) return 'just now'

    const minutes = Math.round(seconds / 60)
    if (minutes < 60) return `${minutes} minute${minutes === 1 ? '' : 's'} ago`

    const hours = Math.round(minutes / 60)
    if (hours < 24) return `${hours} hour${hours === 1 ? '' : 's'} ago`

    const days = Math.round(hours / 24)
    if (days < 7) return `${days} day${days === 1 ? '' : 's'} ago`

    return date.toLocaleDateString()
}
function pad(value: number): string {
    return String(value).padStart(2, '0')
}

// A date as YYYY-MM-DD in the user's own timezone (toISOString would give the UTC date).
export function toLocalDateString(date: Date): string {
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`
}

export function todayLocal(): string {
    return toLocalDateString(new Date())
}

export function addDaysLocal(days: number): string {
    const date = new Date()
    date.setDate(date.getDate() + days)
    return toLocalDateString(date)
}