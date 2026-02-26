# Frontend UI Conventions

## Styling ownership
- `SAM/wwwroot/css/custom.scss` and SCSS partials are the source of truth.
- `SAM/wwwroot/css/custom.css` is a compiled artifact and must stay aligned with SCSS updates.
- `SAM/wwwroot/css/site.css` is limited to compatibility/reset only.

## Token contract
- Use semantic tokens for new UI work: `--surface-1`, `--surface-2`, `--text-1`, `--text-2`, `--border-1`, `--accent-1`.
- Use 4/8 spacing rhythm.
- Use the standard radius scale: `--radius-sm`, `--radius-md`, `--radius-lg`, `--radius-xl`.

## Shared component usage
- Cards: `.card`, `.dashboard-chart-card`, `.dashboard-kpi-card`.
- Filters: `.filter-card`, `.filter-form`, `.filter-control`.
- Tables: `.table`, `.table-responsive`, `.table-actions`.
- Status and empty states: `.dashboard-status-tag`, `.app-empty-state`, `.dashboard-empty-state`.

## Accessibility and interaction
- Keep visible focus styles for links, controls, and nav items.
- Respect `prefers-reduced-motion` for non-essential animations.
- Avoid adding new inline styles in Razor views unless it is a short-lived prototype.
- Keep layout/nav JS in `SAM/wwwroot/js/*.js` rather than inline in `_Layout.cshtml`.
