/**
 * Filter functionality for dynamic search and filtering
 */

(function() {
    'use strict';

    /**
     * Performs AJAX search
     * @param {string} url - Search endpoint URL
     * @param {string} searchTerm - Search term
     * @param {object} additionalParams - Additional query parameters
     * @returns {Promise} Promise that resolves with search results
     */
    window.performSearch = function(url, searchTerm, additionalParams = {}) {
        const params = new URLSearchParams();
        params.append('searchTerm', searchTerm);
        
        for (const [key, value] of Object.entries(additionalParams)) {
            if (value !== null && value !== undefined && value !== '') {
                params.append(key, value);
            }
        }

        return fetch(`${url}?${params.toString()}`, {
            method: 'GET',
            headers: {
                'Accept': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            }
        })
        .then(response => {
            if (!response.ok) {
                throw new Error('Search request failed');
            }
            return response.json();
        })
        .catch(error => {
            console.error('Search error:', error);
            throw error;
        });
    };

    /**
     * Updates the results table with new data
     * @param {Array} results - Array of result objects
     * @param {Function} renderRow - Function to render a single row
     * @param {string} tableBodyId - ID of the tbody element to update
     */
    window.updateResultsTable = function(results, renderRow, tableBodyId) {
        const tbody = document.getElementById(tableBodyId);
        if (!tbody) {
            console.error(`Table body with ID '${tableBodyId}' not found`);
            return;
        }

        tbody.innerHTML = '';
        
        if (results.length === 0) {
            const tr = document.createElement('tr');
            tr.innerHTML = '<td colspan="100%" class="text-center text-muted py-5">No results found</td>';
            tbody.appendChild(tr);
            return;
        }

        results.forEach(result => {
            const row = renderRow(result);
            if (row) {
                tbody.appendChild(row);
            }
        });
    };

    /**
     * Shows loading indicator
     * @param {string} containerId - ID of container to show loading in
     */
    window.showLoading = function(containerId) {
        const container = document.getElementById(containerId);
        if (container) {
            container.innerHTML = '<div class="text-center py-5"><div class="spinner-border" role="status"><span class="visually-hidden">Loading...</span></div></div>';
        }
    };

    /**
     * Shows error message
     * @param {string} containerId - ID of container to show error in
     * @param {string} message - Error message
     */
    window.showError = function(containerId, message) {
        const container = document.getElementById(containerId);
        if (container) {
            container.innerHTML = `<div class="alert alert-danger">${message}</div>`;
        }
    };

})();

