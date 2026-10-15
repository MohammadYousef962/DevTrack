export const userRoles = ['Admin', 'ProjectManager', 'Developer'] as const
export type UserRoleName = (typeof userRoles)[number]

const roleLabels: Record<UserRoleName, string> = {
    Admin: 'Admin',
    ProjectManager: 'Project Manager',
    Developer: 'Developer',
}

const roleStyles: Record<UserRoleName, string> = {
    Admin: 'border-app-accent/30 text-app-accent bg-app-accent/10',
    ProjectManager: 'border-app-review/30 text-app-review bg-app-review/10',
    Developer: 'border-app-border text-app-muted bg-app-hover',
}

export function roleLabel(role: string): string {
    return roleLabels[role as UserRoleName] ?? role
}

export function roleStyle(role: string): string {
    return roleStyles[role as UserRoleName] ?? roleStyles.Developer
}