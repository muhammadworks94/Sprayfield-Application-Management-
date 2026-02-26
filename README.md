# Sprayfield-Application-Management-

## Frontend Styling Conventions

### Source of truth
- `SAM/wwwroot/css/custom.scss` and SCSS partials under `SAM/wwwroot/css/**` are the canonical styling source.
- `SAM/wwwroot/css/custom.css` is generated output and should mirror SCSS updates.
- `SAM/wwwroot/css/site.css` should remain minimal for compatibility/reset concerns only.

### Component usage
- Use shared component classes for consistency:
`card`, `dashboard-chart-card`, `table`, `form-control`, `table-actions`, `dashboard-kpi-card`.
- Avoid introducing page-specific hardcoded colors when a token exists.

### Token usage
- Prefer semantic tokens over raw values:
`--surface-1`, `--surface-2`, `--text-1`, `--text-2`, `--border-1`, `--accent-1`.
- Preserve 4/8 spacing rhythm and standard radius scale (`sm/md/lg/xl`).

### View hygiene
- Avoid inline styles in Razor views except temporary prototypes.
- Shared interaction logic should live in `wwwroot/js/*.js`, not inline in layout views.

See also: `docs/frontend-ui-conventions.md`.
