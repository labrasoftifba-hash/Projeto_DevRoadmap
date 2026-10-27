window.roadmapUi = {
    scrollToModule: () => {
        document.getElementById("roadmap-module-reader")?.scrollIntoView({
            behavior: "smooth",
            block: "start"
        });
    }
};
