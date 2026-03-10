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

function autoSelectSingleOptionDropdowns(root = document) {
    const selects = root.querySelectorAll('select:not([multiple]):not([data-no-auto-single-select])');

    selects.forEach((select) => {
        if (select.disabled) {
            return;
        }

        const options = Array.from(select.options);
        const selectableOptions = options.filter((option) => option.value !== '' && !option.disabled);
        if (selectableOptions.length !== 1) {
            return;
        }

        const onlyOption = selectableOptions[0];
        if (!onlyOption) {
            return;
        }

        // Apply the default only when the dropdown is currently empty/unselected.
        if (select.value !== '' && select.value !== onlyOption.value) {
            return;
        }

        if (select.value === onlyOption.value) {
            return;
        }

        select.value = onlyOption.value;
        select.dispatchEvent(new Event('change', { bubbles: true }));
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

    autoSelectSingleOptionDropdowns();
});
