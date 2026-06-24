(function (global) {
    'use strict';

    global.SAM = global.SAM || {};

    function isSelectDropdownOpen(select) {
        return select && select.tagName === 'SELECT' && select.dataset.gridSelectOpen === 'true';
    }

    function buildCellMatrix(root, rowSelector, cellSelector, skipRow) {
        return Array.from(root.querySelectorAll(rowSelector))
            .filter(function (tr) { return !skipRow || !skipRow(tr); })
            .map(function (tr) {
                return Array.from(tr.querySelectorAll(cellSelector))
                    .filter(function (el) { return !el.disabled; });
            })
            .filter(function (cells) { return cells.length > 0; });
    }

    function findCellPosition(matrix, element) {
        for (var r = 0; r < matrix.length; r++) {
            var col = matrix[r].indexOf(element);
            if (col >= 0) {
                return { row: r, col: col };
            }
        }
        return null;
    }

    function movePosition(matrix, row, col, direction, wrap) {
        var rowCount = matrix.length;
        if (rowCount === 0) {
            return null;
        }

        var nr = row;
        var nc = col;

        switch (direction) {
            case 'up':
                nr = row - 1;
                if (nr < 0) {
                    nr = wrap ? rowCount - 1 : row;
                }
                nc = Math.min(col, matrix[nr].length - 1);
                break;
            case 'down':
                nr = row + 1;
                if (nr >= rowCount) {
                    nr = wrap ? 0 : row;
                }
                nc = Math.min(col, matrix[nr].length - 1);
                break;
            case 'left':
                if (col > 0) {
                    nc = col - 1;
                } else if (wrap) {
                    nr = row - 1;
                    if (nr < 0) {
                        nr = rowCount - 1;
                    }
                    nc = matrix[nr].length - 1;
                }
                break;
            case 'right':
                if (col < matrix[row].length - 1) {
                    nc = col + 1;
                } else if (wrap) {
                    nr = row + 1;
                    if (nr >= rowCount) {
                        nr = 0;
                    }
                    nc = 0;
                }
                break;
            default:
                return null;
        }

        return { row: nr, col: nc };
    }

    function directionFromKey(event) {
        switch (event.key) {
            case 'ArrowUp':
                return 'up';
            case 'ArrowDown':
                return 'down';
            case 'ArrowLeft':
                return 'left';
            case 'ArrowRight':
                return 'right';
            case 'Enter':
                return event.shiftKey ? 'up' : 'down';
            case 'Tab':
                return event.shiftKey ? 'left' : 'right';
            default:
                return null;
        }
    }

    function markSelectOpen(select) {
        select.dataset.gridSelectOpen = 'true';
    }

    function clearSelectOpen(select) {
        delete select.dataset.gridSelectOpen;
    }

    function placeCaretAtEnd(cell) {
        if (cell.tagName === 'SELECT') {
            return;
        }

        if (cell.tagName !== 'INPUT' && cell.tagName !== 'TEXTAREA') {
            return;
        }

        var value = cell.value == null ? '' : String(cell.value);
        var len = value.length;

        if (typeof cell.setSelectionRange !== 'function') {
            return;
        }

        try {
            cell.setSelectionRange(len, len);
            return;
        } catch (_) {
            // type="number" and some other input types reject setSelectionRange.
        }

        if (cell.type === 'number' || cell.type === 'time') {
            var inputType = cell.type;
            cell.type = 'text';
            try {
                cell.setSelectionRange(len, len);
            } catch (_) { }
            cell.type = inputType;
        }
    }

    function focusCellWithCaretAtEnd(cell) {
        cell.focus({ preventScroll: true });
        cell.scrollIntoView({ block: 'nearest', inline: 'nearest' });
        placeCaretAtEnd(cell);
        requestAnimationFrame(function () {
            placeCaretAtEnd(cell);
        });
    }

    function init(options) {
        var root = options.root;
        var cellSelector = options.cellSelector;
        var rowSelector = options.rowSelector || 'tbody tr';
        var skipRow = options.skipRow || function () { return false; };
        var wrap = options.wrap !== false;

        if (!root || !cellSelector) {
            return;
        }

        root.addEventListener('mousedown', function (e) {
            var select = e.target.closest('select');
            if (select && select.matches(cellSelector)) {
                markSelectOpen(select);
            }
        }, true);

        root.addEventListener('keydown', function (e) {
            var select = e.target.closest('select');
            if (!select || !select.matches(cellSelector)) {
                return;
            }

            if (e.key === ' ' || e.key === 'Enter' || e.key === 'F4' ||
                (e.altKey && (e.key === 'ArrowDown' || e.key === 'ArrowUp'))) {
                markSelectOpen(select);
            }
        }, true);

        root.addEventListener('change', function (e) {
            var select = e.target.closest('select');
            if (select && select.matches(cellSelector)) {
                clearSelectOpen(select);
            }
        }, true);

        root.addEventListener('blur', function (e) {
            var select = e.target;
            if (select.tagName === 'SELECT' && select.matches(cellSelector)) {
                clearSelectOpen(select);
            }
        }, true);

        root.addEventListener('keydown', function (e) {
            if (e.ctrlKey || e.metaKey || e.altKey) {
                return;
            }

            var target = e.target;
            if (!target.matches || !target.matches(cellSelector) || target.disabled) {
                return;
            }

            if (target.tagName === 'SELECT' && isSelectDropdownOpen(target)) {
                if (e.key === 'ArrowUp' || e.key === 'ArrowDown') {
                    return;
                }
            }

            var direction = directionFromKey(e);
            if (!direction) {
                return;
            }

            var matrix = buildCellMatrix(root, rowSelector, cellSelector, skipRow);
            var position = findCellPosition(matrix, target);
            if (!position) {
                return;
            }

            var nextPosition = movePosition(matrix, position.row, position.col, direction, wrap);
            if (!nextPosition) {
                return;
            }

            var nextCell = matrix[nextPosition.row][nextPosition.col];
            if (!nextCell || nextCell === target) {
                return;
            }

            e.preventDefault();
            focusCellWithCaretAtEnd(nextCell);
        });
    }

    global.SAM.gridKeyboardNavigation = {
        init: init
    };
}(window));
