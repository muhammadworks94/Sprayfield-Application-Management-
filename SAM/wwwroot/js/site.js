// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function applyTruncationTooltips(root = document) {
    const truncateSelectors = '.u-truncate, .u-truncate-2';
    const truncateElements = root.querySelectorAll(truncateSelectors);

    truncateElements.forEach((element) => {
        if (element.hasAttribute('data-no-tooltip') || element.getAttribute('title')) {
            return;
        }

        const text = (element.textContent || '').trim();
        if (text.length > 0) {
            element.setAttribute('title', text);
        }
    });
}

document.addEventListener('DOMContentLoaded', function () {
    applyTruncationTooltips();

    document.querySelectorAll('select[data-auto-submit=\"true\"]').forEach((element) => {
        element.addEventListener('change', () => {
            const form = element.closest('form');
            if (form) {
                form.submit();
            }
        });
    });
});
