// Picks the starting theme from the OS when the reader hasn't chosen one yet.
export function needsSystemTheme(cookieName) {
    const hasChoice = document.cookie.split(';').some(c => c.trim().startsWith(cookieName + '='));
    if (hasChoice) return null;
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}
