// Gridstack glue for the dashboard. Blazor renders the panel elements; this module only lets gridstack move and
// resize them, and reports the new positions back to DashboardLayoutService.
const GridStackClass = window.GridStack?.GridStack ?? window.GridStack;

let grid = null;
let dotNet = null;

function itemById(id) {
    return grid?.el.querySelector(`:scope > .grid-stack-item[gs-id="${CSS.escape(id)}"]`) ?? null;
}

function reportLayout() {
    if (!grid || !dotNet) return;
    const positions = grid.engine.nodes.map(n => ({ id: n.id, x: n.x, y: n.y, w: n.w, h: n.h }));
    dotNet.invokeMethodAsync('OnLayoutChanged', positions).catch(() => { /* circuit gone */ });
}

export function init(element, dotNetRef, options) {
    destroy();
    dotNet = dotNetRef;
    grid = GridStackClass.init({
        column: 12,
        cellHeight: 72,
        margin: 6,
        float: false,
        animate: true,
        handle: '.panel-drag-handle',
        resizable: { handles: 'e,se,s,sw,w' },
        draggable: { cancel: 'button, input, a, .panel-actions' },
        staticGrid: !!options?.locked,
    }, element);

    grid.on('change', reportLayout);
    // Positions may shift on init (compaction, auto-positioned panels), so save what gridstack settled on.
    reportLayout();
}

export function adoptWidget(id) {
    const el = itemById(id);
    if (grid && el && !el.gridstackNode) {
        grid.makeWidget(el);
        reportLayout();
    }
}

export function releaseWidget(id) {
    const el = itemById(id);
    if (grid && el) {
        // Keep the DOM node: Blazor removes it on the next render.
        grid.removeWidget(el, false);
        reportLayout();
    }
}

export function updateWidget(id, options) {
    const el = itemById(id);
    if (grid && el) {
        grid.update(el, options);
        reportLayout();
    }
}

export function setLocked(locked) {
    grid?.setStatic(!!locked);
}

export function destroy() {
    if (grid) {
        grid.offAll();
        grid.destroy(false);
        grid = null;
    }
}

export function loadLayout(key) {
    try { return localStorage.getItem(key); } catch { return null; }
}

export function saveLayout(key, json) {
    try { localStorage.setItem(key, json); } catch { /* storage unavailable */ }
}
