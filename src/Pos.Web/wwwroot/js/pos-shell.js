(function () {
    const toggleBtn = document.getElementById("pos-menu-toggle");
    const closeBtn = document.getElementById("pos-sidebar-close");
    const backdrop = document.getElementById("pos-backdrop");
    const sidebar = document.getElementById("pos-sidebar");
    const content = document.querySelector(".pos-main");
    const mobile = window.matchMedia("(max-width: 1024px)");

    if (!toggleBtn || !closeBtn || !backdrop || !sidebar || !content) return;

    let open = false;
    function setSidebarOpen(nextOpen) {
        const focusWasInSidebar = sidebar.contains(document.activeElement);
        open = mobile.matches && nextOpen;
        document.body.classList.toggle("pos-nav-open", open);
        toggleBtn.setAttribute("aria-expanded", String(open));
        if (open) {
            sidebar.inert = false;
            sidebar.setAttribute("role", "dialog");
            sidebar.setAttribute("aria-modal", "true");
            closeBtn.focus();
            content.inert = true;
        } else {
            content.inert = false;
            if (mobile.matches && focusWasInSidebar) toggleBtn.focus();
            if (!mobile.matches && document.activeElement === closeBtn) sidebar.querySelector("a[href]")?.focus();
            sidebar.inert = mobile.matches;
            sidebar.removeAttribute("role");
            sidebar.removeAttribute("aria-modal");
        }
    }

    toggleBtn.addEventListener("click", () => setSidebarOpen(!open));
    closeBtn.addEventListener("click", () => setSidebarOpen(false));
    backdrop.addEventListener("click", () => setSidebarOpen(false));
    mobile.addEventListener("change", () => setSidebarOpen(false));

    document.addEventListener("keydown", (event) => {
        if (!open) return;
        if (event.key === "Escape") {
            event.preventDefault();
            setSidebarOpen(false);
        }
        if (event.key !== "Tab") return;
        const links = sidebar.querySelectorAll("a[href], button:not(:disabled)");
        const first = links[0];
        const last = links[links.length - 1];
        if (event.shiftKey && document.activeElement === first) {
            event.preventDefault();
            last.focus();
        } else if (!event.shiftKey && document.activeElement === last) {
            event.preventDefault();
            first.focus();
        }
    });
    setSidebarOpen(false);
})();
