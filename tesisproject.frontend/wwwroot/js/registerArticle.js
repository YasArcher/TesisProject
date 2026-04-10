window.registerArticleScrollToSection = (targetId) => {
    if (!targetId) {
        return;
    }

    const element = document.getElementById(targetId);
    if (!element) {
        return;
    }

    element.scrollIntoView({
        behavior: "smooth",
        block: "start",
        inline: "nearest"
    });
};
