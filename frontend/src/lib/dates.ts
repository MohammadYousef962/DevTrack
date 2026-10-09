export function parseApiDate(value: string): Date {
    const hasTimezone = /([zZ]|[+-]\d{2}:?\d{2})$/.test(value)
    return new Date(hasTimezone ? value : `${value}Z`)
}