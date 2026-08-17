if (!window.financeAssistantComposerEnterHandler) {
    window.financeAssistantComposerEnterHandler = true;

    document.addEventListener("keydown", event => {
        const target = event.target;
        if (!(target instanceof HTMLTextAreaElement) || !target.matches("textarea[data-enter-submits='true']")) {
            return;
        }

        const submitKey = target.dataset.submitKey ?? "Enter";
        if (event.key !== submitKey || event.shiftKey) {
            return;
        }

        event.preventDefault();
        const form = target.closest("form");
        const sendButtonSelector = target.dataset.sendButtonSelector ?? "button.primary-button";
        const sendButton = form?.querySelector(sendButtonSelector);
        if (sendButton instanceof HTMLButtonElement) {
            sendButton.click();
        }
    }, true);
}
