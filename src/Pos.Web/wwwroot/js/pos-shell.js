(function () {
    const toggleBtn = document.getElementById("pos-menu-toggle");
    const closeBtn = document.getElementById("pos-sidebar-close");
    const backdrop = document.getElementById("pos-backdrop");

    function setSidebarOpen(isOpen) {
        document.body.classList.toggle("pos-nav-open", isOpen);
        if (toggleBtn) {
            toggleBtn.setAttribute("aria-expanded", String(isOpen));
        }
    }

    if (toggleBtn) {
        toggleBtn.addEventListener("click", () => {
            const isOpen = document.body.classList.contains("pos-nav-open");
            setSidebarOpen(!isOpen);
        });
    }

    if (closeBtn) {
        closeBtn.addEventListener("click", () => setSidebarOpen(false));
    }

    if (backdrop) {
        backdrop.addEventListener("click", () => setSidebarOpen(false));
    }

    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape" && document.body.classList.contains("pos-nav-open")) {
            setSidebarOpen(false);
        }
    });
})();
