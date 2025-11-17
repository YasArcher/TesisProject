// sidebarManager.js
let sidebarStateCallbacks = new Map();

function registerSidebarListener(dotNetHelper) {
    const observer = new MutationObserver((mutations) => {
        mutations.forEach((mutation) => {
            if (mutation.type === 'attributes' && mutation.attributeName === 'class') {
                const isCollapsed = mutation.target.classList.contains('is-collapsed');
                dotNetHelper.invokeMethodAsync('OnSidebarStateChanged', isCollapsed);
            }
        });
    });

    const sidebar = document.querySelector('.sidebar');
    if (sidebar) {
        observer.observe(sidebar, { attributes: true });
        // Estado inicial
        const initialCollapsed = sidebar.classList.contains('is-collapsed');
        dotNetHelper.invokeMethodAsync('OnSidebarStateChanged', initialCollapsed);

        sidebarStateCallbacks.set(dotNetHelper, observer);
    }
}

function unregisterSidebarListener(dotNetHelper) {
    const observer = sidebarStateCallbacks.get(dotNetHelper);
    if (observer) {
        observer.disconnect();
        sidebarStateCallbacks.delete(dotNetHelper);
    }
}

// Inicializar automáticamente
document.addEventListener('DOMContentLoaded', function () {
    const sidebar = document.querySelector('.sidebar');
    if (sidebar) {
        // Notificar estado inicial a todos los componentes
        const isCollapsed = sidebar.classList.contains('is-collapsed');
        sidebarStateCallbacks.forEach((obs, dotNetHelper) => {
            dotNetHelper.invokeMethodAsync('OnSidebarStateChanged', isCollapsed);
        });
    }
});