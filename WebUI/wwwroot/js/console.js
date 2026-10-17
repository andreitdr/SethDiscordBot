// Keeps the console scrolled to the newest line unless the reader has scrolled up.
const STICK_THRESHOLD_PX = 24;

export function attach(element, dotNetRef) {
    let wasAtBottom = true;
    const onScroll = () => {
        const atBottom = element.scrollHeight - element.scrollTop - element.clientHeight <= STICK_THRESHOLD_PX;
        if (atBottom !== wasAtBottom) {
            wasAtBottom = atBottom;
            dotNetRef.invokeMethodAsync('OnStickToBottomChanged', atBottom).catch(() => { });
        }
    };
    element.addEventListener('scroll', onScroll, { passive: true });
    element._sethConsoleDetach = () => element.removeEventListener('scroll', onScroll);
}

export function detach(element) {
    element?._sethConsoleDetach?.();
}

export function scrollToBottom(element) {
    if (element) element.scrollTop = element.scrollHeight;
}

export async function copyText(text) {
    await navigator.clipboard.writeText(text);
}
