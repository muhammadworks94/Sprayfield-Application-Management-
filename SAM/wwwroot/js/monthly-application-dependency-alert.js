(function (window) {
    function escapeHtml(value) {
        if (value == null) return '';
        return String(value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/\"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function renderMonthlyDependencyAlert(alertEl, guidance) {
        if (!alertEl || !guidance) return;

        const summary = guidance.dependencySummary || guidance.message || 'WWChar chemistry is required before saving this monthly application.';
        const steps = Array.isArray(guidance.dependencySteps) ? guidance.dependencySteps : [];
        const wwCharUrl = guidance.wwCharShortcutUrl || '';
        const permitUrl = guidance.permitVersionsUrl || '';

        let stepsHtml = '';
        if (steps.length > 0) {
            const items = steps.map(step => `<li>${escapeHtml(step)}</li>`).join('');
            stepsHtml = `<ol class="mb-2 ps-3">${items}</ol>`;
        }

        const links = [];
        if (wwCharUrl) {
            links.push(`<a class="link-primary fw-semibold me-3" href="${escapeHtml(wwCharUrl)}" target="_blank" rel="noopener noreferrer">Open WWChar For This Month</a>`);
        }
        if (permitUrl) {
            links.push(`<a class="link-primary fw-semibold" href="${escapeHtml(permitUrl)}" target="_blank" rel="noopener noreferrer">Open Permit Versions</a>`);
        }

        alertEl.innerHTML = `
            <div class="w-100">
                <div class="fw-semibold mb-2">${escapeHtml(summary)}</div>
                ${stepsHtml}
                <div>${links.join('')}</div>
            </div>`;
        alertEl.classList.remove('d-none');
    }

    window.MonthlyDependencyAlert = {
        render: renderMonthlyDependencyAlert
    };
})(window);
