# Dashboard overview

The WebUI is a single page of panels. Each panel covers one job (bot status, console, settings, plugins, …). You can move, resize, collapse and hide them.

![The dashboard](../images/dashboard-overview.jpg)

## Header

From left to right:

| Element | What it does |
| --- | --- |
| **Seth Discord Bot** | Goes back to the dashboard from a plugin page. |
| Status pill | *Offline*, *Connecting*, *Online*, *Reconnecting* or *Stopping*. |
| **Start / Stop / Cancel** | Starts the bot, stops it (after a confirmation; plugins are unloaded), or cancels a connection attempt. **Start** is disabled until a token is saved. |
| **Pages** | Menu of the web pages of loaded plugins, grouped by plugin. Disabled when no plugin page is loaded. See [Plugin web pages](../creating-plugins/web-pages.md). |
| **Panels** | Shows or hides each panel, and **Reset layout** restores the default arrangement. |
| Lock | Locks the layout so panels can't be dragged or resized by accident. |
| Sun / moon | Switches between the light and dark theme. The choice is kept in a cookie for a year. |

## Arranging panels

- **Move**: drag a panel by its title bar.
- **Resize**: drag the bottom-right corner. Each panel has a minimum size.
- **Collapse**: the `^` button in the title bar shrinks a panel to its title; click again to expand.
- **Hide**: the `✕` button removes a panel; bring it back from **Panels**.
- **Lock**: the lock button in the header freezes the layout.
- **Reset**: **Panels → Reset layout**.

The layout (positions, sizes, collapsed and hidden panels, lock) is saved **per browser** in local storage (`seth.dashboard.layout.v1`). Another browser or computer starts from the default layout.

## Shared state

The bot, the plugin lists and the console are shared by every open dashboard. If you start the bot in one tab, every other tab shows it online; an install started in one tab shows its progress in all of them. Two people clicking **Start** at once can't start the bot twice: the second click gets *Another bot operation is already in progress*.

## Notifications

Actions report their result with a toast in the corner (*Bot started*, *Plugins reloaded*, an error message, …). Destructive actions (stopping the bot, deleting a plugin) ask for confirmation first.

---

Next: [Panels reference](panels.md)
