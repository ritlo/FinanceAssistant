const composer = document.querySelector("textarea[data-enter-submits='true']");
if (composer) {
    composer.addEventListener("keydown", event => {
        const submitKey = composer.dataset.submitKey ?? "Enter";
        if (event.key !== submitKey || event.shiftKey) {
            return;
        }

        event.preventDefault();
        const form = composer.closest("form");
        if (!form) {
            return;
        }

        if (typeof form.requestSubmit === "function") {
            form.requestSubmit();
            return;
        }

        form.dispatchEvent(new SubmitEvent("submit", { bubbles: true, cancelable: true }));
    });
}
