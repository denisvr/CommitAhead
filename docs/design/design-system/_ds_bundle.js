/* @ds-bundle: {"format":4,"namespace":"CommitAheadDesignSystem_80fdcb","components":[{"name":"Badge","sourcePath":"components/core/Badge.jsx"},{"name":"Button","sourcePath":"components/core/Button.jsx"},{"name":"Callout","sourcePath":"components/core/Callout.jsx"},{"name":"Chip","sourcePath":"components/core/Chip.jsx"},{"name":"Dialog","sourcePath":"components/core/Dialog.jsx"},{"name":"EmptyState","sourcePath":"components/core/EmptyState.jsx"},{"name":"Icon","sourcePath":"components/core/Icon.jsx"},{"name":"IconButton","sourcePath":"components/core/IconButton.jsx"},{"name":"Tabs","sourcePath":"components/core/Tabs.jsx"},{"name":"DataTable","sourcePath":"components/domain/DataTable.jsx"},{"name":"ProposalRow","sourcePath":"components/domain/ProposalRow.jsx"},{"name":"QueueRow","sourcePath":"components/domain/QueueRow.jsx"},{"name":"ScoreBreakdown","sourcePath":"components/domain/ScoreBreakdown.jsx"},{"name":"ScoreNumeral","sourcePath":"components/domain/ScoreNumeral.jsx"},{"name":"Checkbox","sourcePath":"components/forms/Checkbox.jsx"},{"name":"Field","sourcePath":"components/forms/Field.jsx"},{"name":"Input","sourcePath":"components/forms/Input.jsx"},{"name":"RatingScale","sourcePath":"components/forms/RatingScale.jsx"},{"name":"Select","sourcePath":"components/forms/Select.jsx"},{"name":"Textarea","sourcePath":"components/forms/Textarea.jsx"},{"name":"Brand","sourcePath":"components/navigation/Brand.jsx"},{"name":"PageHeader","sourcePath":"components/navigation/PageHeader.jsx"},{"name":"NAV_ITEMS","sourcePath":"components/navigation/SidebarNav.jsx"},{"name":"SidebarNav","sourcePath":"components/navigation/SidebarNav.jsx"}],"sourceHashes":{"assets/icons/icons.js":"9fa740152491","components/core/Badge.jsx":"d19366c8ce7b","components/core/Button.jsx":"aec7f11ef87a","components/core/Callout.jsx":"8943a0eb9685","components/core/Chip.jsx":"c0991c46a270","components/core/Dialog.jsx":"b83790147e4b","components/core/EmptyState.jsx":"956ba3ec3eaa","components/core/Icon.jsx":"41df50229dff","components/core/IconButton.jsx":"f7e5f458acaf","components/core/Tabs.jsx":"3af2b2e954fc","components/domain/DataTable.jsx":"f69d34a0a77f","components/domain/ProposalRow.jsx":"e6b2ba3ed6d4","components/domain/QueueRow.jsx":"eb7f2a9c100d","components/domain/ScoreBreakdown.jsx":"a9ff1457257f","components/domain/ScoreNumeral.jsx":"978443b2441b","components/forms/Checkbox.jsx":"e5f47f73ed7f","components/forms/Field.jsx":"e7447c390173","components/forms/Input.jsx":"b15852dd83ca","components/forms/RatingScale.jsx":"a64f5e223fe6","components/forms/Select.jsx":"6e26f076e713","components/forms/Textarea.jsx":"5cc26a7c7d87","components/navigation/Brand.jsx":"ca179dd7363e","components/navigation/PageHeader.jsx":"70122b791ab6","components/navigation/SidebarNav.jsx":"8bdf3f49703e","ui_kits/app/App.jsx":"31bd249bc365","ui_kits/app/CVEditor.jsx":"79654e73a6a5","ui_kits/app/JobAnalysis.jsx":"15bb7e5aaaad","ui_kits/app/Login.jsx":"b4bd3183cb89","ui_kits/app/StudyItemDetail.jsx":"6fcb9b51fe14","ui_kits/app/StudyQueue.jsx":"df6ab980793e","ui_kits/app/data.js":"45db7168ec7b"},"inlinedExternals":[],"unexposedExports":[]} */

(() => {

const __ds_ns = (window.CommitAheadDesignSystem_80fdcb = window.CommitAheadDesignSystem_80fdcb || {});

const __ds_scope = {};

(__ds_ns.__errors = __ds_ns.__errors || []);

// assets/icons/icons.js
try { (() => {
/* CommitAhead icons — injects the Lucide sprite inline so <use href="#icon-name">
   works with currentColor. Local, no network. Load once per page:
   <script src="assets/icons/icons.js"><\/script>  then
   <svg class="icon"><use href="#icon-check"></use></svg> */
(function () {
  function inject() {
    if (document.getElementById('ca-icon-sprite')) return;
    var d = document.createElement('div');
    d.id = 'ca-icon-sprite';
    d.setAttribute('aria-hidden', 'true');
    d.style.cssText = 'position:absolute;width:0;height:0;overflow:hidden';
    d.innerHTML = "<svg xmlns=\"http://www.w3.org/2000/svg\"><symbol id=\"icon-arrow-left\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"m12 19-7-7 7-7\"></path>\n  <path d=\"M19 12H5\"></path></symbol><symbol id=\"icon-arrow-right\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M5 12h14\"></path>\n  <path d=\"m12 5 7 7-7 7\"></path></symbol><symbol id=\"icon-book-marked\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M10 2v8l3-3 3 3V2\"></path>\n  <path d=\"M4 19.5v-15A2.5 2.5 0 0 1 6.5 2H19a1 1 0 0 1 1 1v18a1 1 0 0 1-1 1H6.5a1 1 0 0 1 0-5H20\"></path></symbol><symbol id=\"icon-briefcase\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M16 20V4a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16\"></path>\n  <rect width=\"20\" height=\"14\" x=\"2\" y=\"6\" rx=\"2\"></rect></symbol><symbol id=\"icon-check\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M20 6 9 17l-5-5\"></path></symbol><symbol id=\"icon-chevron-down\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"m6 9 6 6 6-6\"></path></symbol><symbol id=\"icon-chevron-right\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"m9 18 6-6-6-6\"></path></symbol><symbol id=\"icon-circle-alert\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><circle cx=\"12\" cy=\"12\" r=\"10\"></circle>\n  <line x1=\"12\" x2=\"12\" y1=\"8\" y2=\"12\"></line>\n  <line x1=\"12\" x2=\"12.01\" y1=\"16\" y2=\"16\"></line></symbol><symbol id=\"icon-download\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M12 15V3\"></path>\n  <path d=\"M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4\"></path>\n  <path d=\"m7 10 5 5 5-5\"></path></symbol><symbol id=\"icon-eye-off\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M10.733 5.076a10.744 10.744 0 0 1 11.205 6.575 1 1 0 0 1 0 .696 10.747 10.747 0 0 1-1.444 2.49\"></path>\n  <path d=\"M14.084 14.158a3 3 0 0 1-4.242-4.242\"></path>\n  <path d=\"M17.479 17.499a10.75 10.75 0 0 1-15.417-5.151 1 1 0 0 1 0-.696 10.75 10.75 0 0 1 4.446-5.143\"></path>\n  <path d=\"m2 2 20 20\"></path></symbol><symbol id=\"icon-eye\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M2.062 12.348a1 1 0 0 1 0-.696 10.75 10.75 0 0 1 19.876 0 1 1 0 0 1 0 .696 10.75 10.75 0 0 1-19.876 0\"></path>\n  <circle cx=\"12\" cy=\"12\" r=\"3\"></circle></symbol><symbol id=\"icon-link\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71\"></path>\n  <path d=\"M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71\"></path></symbol><symbol id=\"icon-list-ordered\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M11 5h10\"></path>\n  <path d=\"M11 12h10\"></path>\n  <path d=\"M11 19h10\"></path>\n  <path d=\"M4 4h1v5\"></path>\n  <path d=\"M4 9h2\"></path>\n  <path d=\"M6.5 20H3.4c0-1 2.6-1.925 2.6-3.5a1.5 1.5 0 0 0-2.6-1.02\"></path></symbol><symbol id=\"icon-loader-circle\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M21 12a9 9 0 1 1-6.219-8.56\"></path></symbol><symbol id=\"icon-moon\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M20.985 12.486a9 9 0 1 1-9.473-9.472c.405-.022.617.46.402.803a6 6 0 0 0 8.268 8.268c.344-.215.825-.004.803.401\"></path></symbol><symbol id=\"icon-notebook-text\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M2 6h4\"></path>\n  <path d=\"M2 10h4\"></path>\n  <path d=\"M2 14h4\"></path>\n  <path d=\"M2 18h4\"></path>\n  <rect width=\"16\" height=\"20\" x=\"4\" y=\"2\" rx=\"2\"></rect>\n  <path d=\"M9.5 8h5\"></path>\n  <path d=\"M9.5 12H16\"></path>\n  <path d=\"M9.5 16H14\"></path></symbol><symbol id=\"icon-pencil\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M21.174 6.812a1 1 0 0 0-3.986-3.987L3.842 16.174a2 2 0 0 0-.5.83l-1.321 4.352a.5.5 0 0 0 .623.622l4.353-1.32a2 2 0 0 0 .83-.497z\"></path>\n  <path d=\"m15 5 4 4\"></path></symbol><symbol id=\"icon-plus\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M5 12h14\"></path>\n  <path d=\"M12 5v14\"></path></symbol><symbol id=\"icon-search\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"m21 21-4.34-4.34\"></path>\n  <circle cx=\"11\" cy=\"11\" r=\"8\"></circle></symbol><symbol id=\"icon-settings\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M9.671 4.136a2.34 2.34 0 0 1 4.659 0 2.34 2.34 0 0 0 3.319 1.915 2.34 2.34 0 0 1 2.33 4.033 2.34 2.34 0 0 0 0 3.831 2.34 2.34 0 0 1-2.33 4.033 2.34 2.34 0 0 0-3.319 1.915 2.34 2.34 0 0 1-4.659 0 2.34 2.34 0 0 0-3.32-1.915 2.34 2.34 0 0 1-2.33-4.033 2.34 2.34 0 0 0 0-3.831A2.34 2.34 0 0 1 6.35 6.051a2.34 2.34 0 0 0 3.319-1.915\"></path>\n  <circle cx=\"12\" cy=\"12\" r=\"3\"></circle></symbol><symbol id=\"icon-sparkles\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M11.017 2.814a1 1 0 0 1 1.966 0l1.051 5.558a2 2 0 0 0 1.594 1.594l5.558 1.051a1 1 0 0 1 0 1.966l-5.558 1.051a2 2 0 0 0-1.594 1.594l-1.051 5.558a1 1 0 0 1-1.966 0l-1.051-5.558a2 2 0 0 0-1.594-1.594l-5.558-1.051a1 1 0 0 1 0-1.966l5.558-1.051a2 2 0 0 0 1.594-1.594z\"></path>\n  <path d=\"M20 2v4\"></path>\n  <path d=\"M22 4h-4\"></path>\n  <circle cx=\"4\" cy=\"20\" r=\"2\"></circle></symbol><symbol id=\"icon-sun\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><circle cx=\"12\" cy=\"12\" r=\"4\"></circle>\n  <path d=\"M12 2v2\"></path>\n  <path d=\"M12 20v2\"></path>\n  <path d=\"m4.93 4.93 1.41 1.41\"></path>\n  <path d=\"m17.66 17.66 1.41 1.41\"></path>\n  <path d=\"M2 12h2\"></path>\n  <path d=\"M20 12h2\"></path>\n  <path d=\"m6.34 17.66-1.41 1.41\"></path>\n  <path d=\"m19.07 4.93-1.41 1.41\"></path></symbol><symbol id=\"icon-trash-2\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M10 11v6\"></path>\n  <path d=\"M14 11v6\"></path>\n  <path d=\"M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6\"></path>\n  <path d=\"M3 6h18\"></path>\n  <path d=\"M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2\"></path></symbol><symbol id=\"icon-user-round\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><circle cx=\"12\" cy=\"8\" r=\"5\"></circle>\n  <path d=\"M20 21a8 8 0 0 0-16 0\"></path></symbol><symbol id=\"icon-x\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.75\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M18 6 6 18\"></path>\n  <path d=\"m6 6 12 12\"></path></symbol></svg>";
    document.body.insertBefore(d, document.body.firstChild);
  }
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', inject);else inject();
})();
})(); } catch (e) { __ds_ns.__errors.push({ path: "assets/icons/icons.js", error: String((e && e.message) || e) }); }

// components/core/Badge.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const tones = {
  critical: {
    color: 'var(--critical)',
    background: 'var(--critical-wash)'
  },
  caution: {
    color: 'var(--caution)',
    background: 'var(--caution-wash)'
  },
  good: {
    color: 'var(--good)',
    background: 'var(--good-wash)'
  },
  draft: {
    color: 'var(--accent)',
    background: 'var(--accent-wash)'
  },
  neutral: {
    color: 'var(--text-muted)',
    background: 'var(--surface-alt)'
  }
};
function Badge({
  children,
  tone = 'neutral',
  dot = true,
  style,
  ...rest
}) {
  const t = tones[tone];
  return /*#__PURE__*/React.createElement("span", _extends({
    style: {
      display: 'inline-flex',
      alignItems: 'center',
      gap: 'var(--space-3)',
      fontSize: 'var(--text-xs)',
      fontWeight: 'var(--weight-semibold)',
      borderRadius: 'var(--radius-xs)',
      padding: '4px 9px',
      whiteSpace: 'nowrap',
      ...t,
      ...style
    }
  }, rest), dot ? /*#__PURE__*/React.createElement("span", {
    style: {
      width: 6,
      height: 6,
      borderRadius: '50%',
      background: 'currentColor',
      display: 'block'
    }
  }) : null, children);
}
Object.assign(__ds_scope, { Badge });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/core/Badge.jsx", error: String((e && e.message) || e) }); }

// components/core/Chip.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
function Chip({
  children,
  selected,
  as = 'span',
  onClick,
  style,
  ...rest
}) {
  const Tag = onClick ? 'button' : as;
  return /*#__PURE__*/React.createElement(Tag, _extends({
    onClick: onClick,
    style: {
      display: 'inline-flex',
      alignItems: 'center',
      fontFamily: 'var(--font-mono)',
      fontSize: 'var(--text-xs)',
      lineHeight: 1,
      color: selected ? 'var(--accent-contrast)' : 'var(--text-muted)',
      background: selected ? 'var(--accent)' : 'transparent',
      border: '1px solid ' + (selected ? 'var(--accent)' : 'var(--border-strong)'),
      borderRadius: 'var(--radius-xs)',
      padding: '5px 8px',
      whiteSpace: 'nowrap',
      cursor: onClick ? 'pointer' : 'default',
      transition: 'background-color var(--dur-fast) var(--ease-standard), border-color var(--dur-fast) var(--ease-standard)',
      ...style
    }
  }, rest), children);
}
Object.assign(__ds_scope, { Chip });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/core/Chip.jsx", error: String((e && e.message) || e) }); }

// components/core/EmptyState.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
function EmptyState({
  title,
  children,
  action,
  style,
  ...rest
}) {
  return /*#__PURE__*/React.createElement("div", _extends({
    style: {
      padding: 'var(--space-22) var(--space-8)',
      textAlign: 'center',
      ...style
    }
  }, rest), /*#__PURE__*/React.createElement("div", {
    style: {
      fontSize: 'var(--text-lead)',
      fontWeight: 'var(--weight-semibold)',
      letterSpacing: 'var(--track-headline)',
      marginBottom: 'var(--space-3)'
    }
  }, title), /*#__PURE__*/React.createElement("div", {
    style: {
      fontSize: 'var(--text-sm)',
      color: 'var(--text-muted)',
      maxWidth: '46ch',
      margin: '0 auto var(--space-8)',
      lineHeight: 'var(--leading-prose)'
    }
  }, children), action);
}
Object.assign(__ds_scope, { EmptyState });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/core/EmptyState.jsx", error: String((e && e.message) || e) }); }

// components/core/Icon.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
/** Renders one glyph from the bundled Lucide sprite (assets/icons/icons.js). */
function Icon({
  name,
  size = 16,
  strokeWidth,
  style,
  ...rest
}) {
  return /*#__PURE__*/React.createElement("svg", _extends({
    width: size,
    height: size,
    fill: "none",
    stroke: "currentColor",
    strokeWidth: strokeWidth,
    "aria-hidden": "true",
    focusable: "false",
    style: {
      flex: 'none',
      display: 'block',
      ...style
    }
  }, rest), /*#__PURE__*/React.createElement("use", {
    href: '#icon-' + name
  }));
}
Object.assign(__ds_scope, { Icon });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/core/Icon.jsx", error: String((e && e.message) || e) }); }

// components/core/Button.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const base = {
  display: 'inline-flex',
  alignItems: 'center',
  justifyContent: 'center',
  gap: 'var(--space-4)',
  fontFamily: 'var(--font-sans)',
  fontWeight: 'var(--weight-semibold)',
  whiteSpace: 'nowrap',
  borderRadius: 'var(--radius-sm)',
  border: '1px solid transparent',
  cursor: 'pointer',
  transition: 'background-color var(--dur-fast) var(--ease-standard), color var(--dur-fast) var(--ease-standard), border-color var(--dur-fast) var(--ease-standard)'
};
const sizes = {
  md: {
    height: 'var(--control-height)',
    padding: '0 18px',
    fontSize: 'var(--text-sm)'
  },
  sm: {
    height: 'var(--control-height-sm)',
    padding: '0 12px',
    fontSize: 'var(--text-xs)'
  }
};
const variants = {
  primary: {
    background: 'var(--accent)',
    color: 'var(--accent-contrast)'
  },
  secondary: {
    background: 'transparent',
    color: 'var(--text-muted)',
    borderColor: 'var(--border-strong)',
    fontWeight: 'var(--weight-medium)'
  },
  ghost: {
    background: 'transparent',
    color: 'var(--text-muted)',
    fontWeight: 'var(--weight-medium)'
  },
  danger: {
    background: 'transparent',
    color: 'var(--critical)',
    borderColor: 'var(--critical)'
  }
};
const hovers = {
  primary: {
    background: 'var(--accent-hover)'
  },
  secondary: {
    background: 'var(--surface-alt)',
    color: 'var(--text)'
  },
  ghost: {
    background: 'var(--surface-alt)',
    color: 'var(--text)'
  },
  danger: {
    background: 'var(--critical-wash)'
  }
};
function Button({
  children,
  variant = 'primary',
  size = 'md',
  icon,
  iconEnd,
  disabled,
  fullWidth,
  style,
  ...rest
}) {
  const [hover, setHover] = React.useState(false);
  return /*#__PURE__*/React.createElement("button", _extends({
    disabled: disabled,
    onMouseEnter: () => setHover(true),
    onMouseLeave: () => setHover(false),
    style: {
      ...base,
      ...sizes[size],
      ...variants[variant],
      ...(hover && !disabled ? hovers[variant] : null),
      width: fullWidth ? '100%' : undefined,
      opacity: disabled ? 0.45 : 1,
      cursor: disabled ? 'not-allowed' : 'pointer',
      ...style
    }
  }, rest), icon ? /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: icon,
    size: size === 'sm' ? 14 : 16
  }) : null, children, iconEnd ? /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: iconEnd,
    size: size === 'sm' ? 14 : 16
  }) : null);
}
Object.assign(__ds_scope, { Button });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/core/Button.jsx", error: String((e && e.message) || e) }); }

// components/core/Callout.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const tones = {
  info: {
    border: 'var(--accent)',
    icon: 'circle-alert',
    color: 'var(--accent)'
  },
  critical: {
    border: 'var(--critical)',
    icon: 'circle-alert',
    color: 'var(--critical)'
  },
  caution: {
    border: 'var(--caution)',
    icon: 'circle-alert',
    color: 'var(--caution)'
  }
};
function Callout({
  children,
  title,
  tone = 'info',
  style,
  ...rest
}) {
  const t = tones[tone];
  return /*#__PURE__*/React.createElement("div", _extends({
    role: tone === 'critical' ? 'alert' : undefined,
    style: {
      display: 'flex',
      gap: 'var(--space-6)',
      padding: 'var(--space-7) var(--space-8)',
      border: '1px solid var(--border)',
      borderLeft: '3px solid ' + t.border,
      borderRadius: 'var(--radius-sm)',
      background: 'var(--surface)',
      fontSize: 'var(--text-sm)',
      lineHeight: 'var(--leading-prose)',
      color: 'var(--text-muted)',
      ...style
    }
  }, rest), /*#__PURE__*/React.createElement("span", {
    style: {
      color: t.color,
      paddingTop: 2
    }
  }, /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: t.icon,
    size: 16
  })), /*#__PURE__*/React.createElement("div", null, title ? /*#__PURE__*/React.createElement("div", {
    style: {
      color: 'var(--text)',
      fontWeight: 'var(--weight-semibold)',
      marginBottom: 4
    }
  }, title) : null, children));
}
Object.assign(__ds_scope, { Callout });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/core/Callout.jsx", error: String((e && e.message) || e) }); }

// components/core/Dialog.jsx
try { (() => {
function Dialog({
  open,
  title,
  children,
  confirmLabel = 'Confirm',
  cancelLabel = 'Cancel',
  destructive,
  onConfirm,
  onCancel
}) {
  if (!open) return null;
  return /*#__PURE__*/React.createElement("div", {
    role: "dialog",
    "aria-modal": "true",
    "aria-label": title,
    style: {
      position: 'fixed',
      inset: 0,
      background: 'var(--scrim)',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      zIndex: 40,
      padding: 'var(--space-8)'
    },
    onClick: onCancel
  }, /*#__PURE__*/React.createElement("div", {
    onClick: e => e.stopPropagation(),
    style: {
      width: 440,
      maxWidth: '100%',
      background: 'var(--surface)',
      border: '1px solid var(--border)',
      borderRadius: 'var(--radius-md)',
      boxShadow: 'var(--shadow-overlay)',
      padding: 'var(--space-12)'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      fontSize: 'var(--text-lead)',
      fontWeight: 'var(--weight-semibold)',
      letterSpacing: 'var(--track-headline)',
      marginBottom: 'var(--space-4)'
    }
  }, title), /*#__PURE__*/React.createElement("div", {
    style: {
      fontSize: 'var(--text-sm)',
      color: 'var(--text-muted)',
      lineHeight: 'var(--leading-prose)',
      marginBottom: 'var(--space-12)'
    }
  }, children), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      gap: 'var(--space-4)',
      justifyContent: 'flex-end'
    }
  }, /*#__PURE__*/React.createElement(__ds_scope.Button, {
    variant: "secondary",
    onClick: onCancel
  }, cancelLabel), /*#__PURE__*/React.createElement(__ds_scope.Button, {
    variant: destructive ? 'danger' : 'primary',
    onClick: onConfirm
  }, confirmLabel))));
}
Object.assign(__ds_scope, { Dialog });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/core/Dialog.jsx", error: String((e && e.message) || e) }); }

// components/core/IconButton.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
function IconButton({
  icon,
  label,
  size = 'md',
  tone = 'default',
  disabled,
  style,
  ...rest
}) {
  const [hover, setHover] = React.useState(false);
  const dim = size === 'sm' ? 'var(--control-height-sm)' : 'var(--control-height)';
  const color = tone === 'danger' ? 'var(--critical)' : 'var(--text-muted)';
  return /*#__PURE__*/React.createElement("button", _extends({
    "aria-label": label,
    title: label,
    disabled: disabled,
    onMouseEnter: () => setHover(true),
    onMouseLeave: () => setHover(false),
    style: {
      width: dim,
      height: dim,
      display: 'inline-flex',
      alignItems: 'center',
      justifyContent: 'center',
      borderRadius: 'var(--radius-sm)',
      border: '1px solid transparent',
      background: hover && !disabled ? tone === 'danger' ? 'var(--critical-wash)' : 'var(--surface-alt)' : 'transparent',
      color: hover && !disabled && tone !== 'danger' ? 'var(--text)' : color,
      cursor: disabled ? 'not-allowed' : 'pointer',
      opacity: disabled ? 0.45 : 1,
      transition: 'background-color var(--dur-fast) var(--ease-standard), color var(--dur-fast) var(--ease-standard)',
      ...style
    }
  }, rest), /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: icon,
    size: size === 'sm' ? 15 : 17
  }));
}
Object.assign(__ds_scope, { IconButton });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/core/IconButton.jsx", error: String((e && e.message) || e) }); }

// components/core/Tabs.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
function Tabs({
  items,
  value,
  onChange,
  style,
  ...rest
}) {
  return /*#__PURE__*/React.createElement("div", _extends({
    role: "tablist",
    style: {
      display: 'flex',
      gap: 'var(--space-12)',
      borderBottom: '1px solid var(--border-soft)',
      ...style
    }
  }, rest), items.map(it => {
    const on = it.value === value;
    return /*#__PURE__*/React.createElement("button", {
      key: it.value,
      role: "tab",
      "aria-selected": on,
      onClick: () => onChange && onChange(it.value),
      style: {
        appearance: 'none',
        background: 'none',
        border: 0,
        cursor: 'pointer',
        padding: '0 0 10px',
        whiteSpace: 'nowrap',
        fontFamily: 'var(--font-sans)',
        fontSize: 'var(--text-sm)',
        fontWeight: on ? 'var(--weight-semibold)' : 'var(--weight-regular)',
        color: on ? 'var(--text)' : 'var(--text-muted)',
        borderBottom: '3px solid ' + (on ? 'var(--accent)' : 'transparent'),
        marginBottom: -1
      }
    }, it.label);
  }));
}
Object.assign(__ds_scope, { Tabs });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/core/Tabs.jsx", error: String((e && e.message) || e) }); }

// components/domain/DataTable.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
function DataTable({
  columns,
  rows,
  onRowClick,
  style,
  ...rest
}) {
  const grid = columns.map(c => c.width || '1fr').join(' ');
  return /*#__PURE__*/React.createElement("div", _extends({
    "data-density": "dense",
    style: {
      ...style
    }
  }, rest), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'grid',
      gridTemplateColumns: grid,
      gap: 12,
      padding: '8px 10px',
      borderBottom: '1px solid var(--border)',
      fontFamily: 'var(--font-mono)',
      fontWeight: 'var(--weight-medium)',
      fontSize: 'var(--text-micro)',
      letterSpacing: 'var(--track-label)',
      textTransform: 'uppercase',
      color: 'var(--text-faint)'
    }
  }, columns.map(c => /*#__PURE__*/React.createElement("span", {
    key: c.key,
    style: {
      textAlign: c.align || 'left'
    }
  }, c.label))), rows.map((r, i) => /*#__PURE__*/React.createElement("div", {
    key: r.id || i,
    onClick: onRowClick ? () => onRowClick(r) : undefined,
    style: {
      display: 'grid',
      gridTemplateColumns: grid,
      gap: 12,
      alignItems: 'center',
      padding: '7px 10px',
      borderBottom: '1px solid var(--border-soft)',
      fontSize: 'var(--text-sm)',
      cursor: onRowClick ? 'pointer' : 'default',
      background: i % 2 ? 'var(--surface-alt)' : 'transparent'
    }
  }, columns.map(c => /*#__PURE__*/React.createElement("span", {
    key: c.key,
    style: {
      textAlign: c.align || 'left',
      fontFamily: c.mono ? 'var(--font-mono)' : 'inherit',
      fontVariantNumeric: c.mono ? 'tabular-nums' : undefined,
      color: c.muted ? 'var(--text-muted)' : 'var(--text)',
      overflow: 'hidden',
      textOverflow: 'ellipsis',
      whiteSpace: 'nowrap'
    }
  }, r[c.key])))));
}
Object.assign(__ds_scope, { DataTable });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/domain/DataTable.jsx", error: String((e && e.message) || e) }); }

// components/domain/ProposalRow.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
function ProposalRow({
  kind,
  children,
  rationale,
  status = 'pending',
  onAccept,
  onReject,
  style,
  ...rest
}) {
  return /*#__PURE__*/React.createElement("div", _extends({
    style: {
      display: 'flex',
      alignItems: 'flex-start',
      justifyContent: 'space-between',
      gap: 'var(--space-8)',
      padding: 'var(--space-7) 0',
      borderBottom: '1px solid var(--border-soft)',
      ...style
    }
  }, rest), /*#__PURE__*/React.createElement("div", {
    style: {
      minWidth: 0
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 'var(--space-4)',
      marginBottom: 'var(--space-3)'
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: 'var(--font-mono)',
      fontWeight: 'var(--weight-medium)',
      fontSize: 'var(--text-micro)',
      letterSpacing: 'var(--track-label)',
      textTransform: 'uppercase',
      color: 'var(--text-faint)'
    }
  }, kind), status !== 'pending' ? /*#__PURE__*/React.createElement(__ds_scope.Badge, {
    tone: status === 'accepted' ? 'good' : 'neutral',
    dot: false
  }, status === 'accepted' ? 'Accepted' : 'Rejected') : null), /*#__PURE__*/React.createElement("div", {
    style: {
      fontSize: 'var(--text-md)',
      color: 'var(--text)'
    }
  }, children), rationale ? /*#__PURE__*/React.createElement("div", {
    style: {
      fontSize: 'var(--text-xs)',
      color: 'var(--text-muted)',
      marginTop: 'var(--space-3)',
      lineHeight: 'var(--leading-prose)',
      maxWidth: '62ch'
    }
  }, rationale) : null), status === 'pending' ? /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      gap: 'var(--space-4)',
      flex: 'none'
    }
  }, /*#__PURE__*/React.createElement(__ds_scope.Button, {
    size: "sm",
    icon: "check",
    onClick: onAccept
  }, "Accept"), /*#__PURE__*/React.createElement(__ds_scope.Button, {
    size: "sm",
    variant: "secondary",
    icon: "x",
    onClick: onReject
  }, "Reject")) : null);
}
Object.assign(__ds_scope, { ProposalRow });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/domain/ProposalRow.jsx", error: String((e && e.message) || e) }); }

// components/domain/ScoreBreakdown.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const PARTS = [{
  key: 'importance',
  label: 'Importance',
  opacity: 1
}, {
  key: 'demand',
  label: 'Demand',
  opacity: 0.62
}, {
  key: 'masteryGap',
  label: 'Mastery gap',
  opacity: 0.32
}];
function ScoreBreakdown({
  importance,
  demand,
  masteryGap,
  variant = 'rows',
  width = 104,
  style,
  ...rest
}) {
  const vals = {
    importance,
    demand,
    masteryGap
  };
  const total = importance + demand + masteryGap;
  if (variant === 'bar') {
    return /*#__PURE__*/React.createElement("div", _extends({
      role: "img",
      "aria-label": 'Effective score ' + total + ': importance ' + importance + ', demand ' + demand + ', mastery gap ' + masteryGap,
      style: {
        width,
        height: 4,
        display: 'flex',
        overflow: 'hidden',
        background: 'var(--border-soft)',
        ...style
      }
    }, rest), PARTS.map(p => /*#__PURE__*/React.createElement("i", {
      key: p.key,
      style: {
        display: 'block',
        height: '100%',
        width: vals[p.key] / 100 * width,
        background: 'var(--accent)',
        opacity: p.opacity
      }
    })));
  }
  return /*#__PURE__*/React.createElement("div", _extends({
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 'var(--space-3)',
      ...style
    }
  }, rest), PARTS.map(p => /*#__PURE__*/React.createElement("span", {
    key: p.key,
    style: {
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'flex-end',
      gap: 'var(--space-4)',
      fontFamily: 'var(--font-mono)',
      fontSize: 'var(--text-micro)',
      color: 'var(--text-faint)',
      fontVariantNumeric: 'tabular-nums'
    }
  }, p.label, /*#__PURE__*/React.createElement("i", {
    style: {
      display: 'block',
      height: 3,
      width: vals[p.key],
      background: 'var(--accent)',
      opacity: p.opacity
    }
  }), vals[p.key])));
}
Object.assign(__ds_scope, { ScoreBreakdown });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/domain/ScoreBreakdown.jsx", error: String((e && e.message) || e) }); }

// components/domain/QueueRow.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
function QueueRow({
  rank,
  title,
  meta,
  category,
  score,
  breakdown,
  dense,
  onClick,
  style,
  ...rest
}) {
  const [hover, setHover] = React.useState(false);
  return /*#__PURE__*/React.createElement("div", _extends({
    role: onClick ? 'button' : undefined,
    tabIndex: onClick ? 0 : undefined,
    onClick: onClick,
    onMouseEnter: () => setHover(true),
    onMouseLeave: () => setHover(false),
    style: {
      display: 'grid',
      gridTemplateColumns: dense ? '28px 1fr 120px 52px' : '28px 1fr 116px 52px',
      alignItems: dense ? 'center' : 'baseline',
      gap: dense ? 12 : 20,
      padding: dense ? '7px 10px' : '15px 10px',
      margin: '0 -10px',
      borderBottom: '1px solid var(--border-soft)',
      background: hover && onClick ? 'var(--surface-alt)' : 'transparent',
      cursor: onClick ? 'pointer' : 'default',
      transition: 'background-color var(--dur-fast) var(--ease-standard)',
      ...style
    }
  }, rest), /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: 'var(--font-mono)',
      fontSize: 'var(--text-xs)',
      color: 'var(--text-faint)',
      fontVariantNumeric: 'tabular-nums'
    }
  }, rank), /*#__PURE__*/React.createElement("div", {
    style: {
      minWidth: 0
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      fontSize: dense ? 'var(--text-sm)' : 'var(--text-md)',
      fontWeight: 'var(--weight-medium)',
      color: 'var(--text)',
      overflow: 'hidden',
      textOverflow: 'ellipsis',
      whiteSpace: 'nowrap'
    }
  }, title), !dense && meta ? /*#__PURE__*/React.createElement("div", {
    style: {
      fontSize: 'var(--text-xs)',
      color: 'var(--text-faint)',
      marginTop: 3
    }
  }, meta) : null, !dense && breakdown ? /*#__PURE__*/React.createElement("div", {
    style: {
      marginTop: 8
    }
  }, /*#__PURE__*/React.createElement(__ds_scope.ScoreBreakdown, _extends({
    variant: "bar"
  }, breakdown))) : null), /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: 'var(--font-mono)',
      fontSize: 'var(--text-xs)',
      color: 'var(--text-muted)'
    }
  }, category), /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: 'var(--font-mono)',
      fontSize: 'var(--text-md)',
      textAlign: 'right',
      fontVariantNumeric: 'tabular-nums',
      color: 'var(--text-muted)'
    }
  }, score));
}
Object.assign(__ds_scope, { QueueRow });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/domain/QueueRow.jsx", error: String((e && e.message) || e) }); }

// components/domain/ScoreNumeral.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
function ScoreNumeral({
  value,
  label = 'Effective score',
  size = 52,
  align = 'right',
  style,
  ...rest
}) {
  return /*#__PURE__*/React.createElement("div", _extends({
    style: {
      textAlign: align,
      ...style
    }
  }, rest), /*#__PURE__*/React.createElement("div", {
    style: {
      fontFamily: 'var(--font-mono)',
      fontSize: size,
      lineHeight: 1,
      letterSpacing: '-0.03em',
      fontVariantNumeric: 'tabular-nums',
      color: 'var(--text)'
    }
  }, value), label ? /*#__PURE__*/React.createElement("div", {
    style: {
      marginTop: 'var(--space-4)',
      fontFamily: 'var(--font-mono)',
      fontWeight: 'var(--weight-medium)',
      fontSize: 'var(--text-micro)',
      letterSpacing: 'var(--track-label)',
      textTransform: 'uppercase',
      color: 'var(--text-faint)'
    }
  }, label) : null);
}
Object.assign(__ds_scope, { ScoreNumeral });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/domain/ScoreNumeral.jsx", error: String((e && e.message) || e) }); }

// components/forms/Checkbox.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
function Checkbox({
  checked,
  onChange,
  label,
  disabled,
  style,
  ...rest
}) {
  return /*#__PURE__*/React.createElement("label", {
    style: {
      display: 'inline-flex',
      alignItems: 'center',
      gap: 'var(--space-4)',
      cursor: disabled ? 'not-allowed' : 'pointer',
      opacity: disabled ? 0.45 : 1,
      fontSize: 'var(--text-sm)',
      ...style
    }
  }, /*#__PURE__*/React.createElement("input", _extends({
    type: "checkbox",
    checked: !!checked,
    onChange: onChange,
    disabled: disabled,
    style: {
      position: 'absolute',
      opacity: 0,
      width: 18,
      height: 18,
      margin: 0,
      cursor: 'inherit'
    }
  }, rest)), /*#__PURE__*/React.createElement("span", {
    "aria-hidden": "true",
    style: {
      width: 18,
      height: 18,
      flex: 'none',
      display: 'inline-flex',
      alignItems: 'center',
      justifyContent: 'center',
      borderRadius: 'var(--radius-xs)',
      border: '1px solid ' + (checked ? 'var(--accent)' : 'var(--border-strong)'),
      background: checked ? 'var(--accent)' : 'var(--surface)',
      color: 'var(--accent-contrast)'
    }
  }, checked ? /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: "check",
    size: 13,
    strokeWidth: 2.5
  }) : null), label);
}
Object.assign(__ds_scope, { Checkbox });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/Checkbox.jsx", error: String((e && e.message) || e) }); }

// components/forms/Field.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
function Field({
  label,
  hint,
  error,
  htmlFor,
  children,
  style,
  ...rest
}) {
  return /*#__PURE__*/React.createElement("div", _extends({
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 'var(--space-3)',
      ...style
    }
  }, rest), label ? /*#__PURE__*/React.createElement("label", {
    htmlFor: htmlFor,
    style: {
      fontSize: 'var(--text-xs)',
      color: 'var(--text-muted)',
      fontWeight: 'var(--weight-medium)'
    }
  }, label) : null, children, error ? /*#__PURE__*/React.createElement("span", {
    style: {
      fontSize: 'var(--text-xs)',
      color: 'var(--critical)'
    }
  }, error) : hint ? /*#__PURE__*/React.createElement("span", {
    style: {
      fontSize: 'var(--text-xs)',
      color: 'var(--text-faint)'
    }
  }, hint) : null);
}
Object.assign(__ds_scope, { Field });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/Field.jsx", error: String((e && e.message) || e) }); }

// components/forms/Input.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
function Input({
  invalid,
  style,
  ...rest
}) {
  return /*#__PURE__*/React.createElement("input", _extends({
    style: {
      width: '100%',
      height: 'var(--control-height)',
      padding: '0 12px',
      fontFamily: 'var(--font-sans)',
      fontSize: 'var(--text-sm)',
      color: 'var(--text)',
      background: 'var(--surface)',
      border: '1px solid ' + (invalid ? 'var(--critical)' : 'var(--border-strong)'),
      borderRadius: 'var(--radius-sm)',
      ...style
    }
  }, rest));
}
Object.assign(__ds_scope, { Input });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/Input.jsx", error: String((e && e.message) || e) }); }

// components/forms/RatingScale.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
/** 1–5 control used for Importance, InitialMastery and StudyReview confidence. */
function RatingScale({
  value,
  onChange,
  name = 'rating',
  min = 1,
  max = 5,
  disabled,
  style,
  ...rest
}) {
  const items = [];
  for (let i = min; i <= max; i++) items.push(i);
  return /*#__PURE__*/React.createElement("div", _extends({
    role: "radiogroup",
    "aria-label": name,
    style: {
      display: 'flex',
      gap: 'var(--space-3)',
      ...style
    }
  }, rest), items.map(i => {
    const on = value === i;
    return /*#__PURE__*/React.createElement("button", {
      key: i,
      role: "radio",
      "aria-checked": on,
      disabled: disabled,
      onClick: () => onChange && onChange(i),
      style: {
        width: 40,
        height: 40,
        borderRadius: 'var(--radius-sm)',
        fontFamily: 'var(--font-mono)',
        fontSize: 'var(--text-sm)',
        border: '1px solid ' + (on ? 'var(--accent)' : 'var(--border-strong)'),
        background: on ? 'var(--accent)' : 'var(--surface)',
        color: on ? 'var(--accent-contrast)' : 'var(--text-muted)',
        cursor: disabled ? 'not-allowed' : 'pointer',
        transition: 'background-color var(--dur-fast) var(--ease-standard), border-color var(--dur-fast) var(--ease-standard)'
      }
    }, i);
  }));
}
Object.assign(__ds_scope, { RatingScale });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/RatingScale.jsx", error: String((e && e.message) || e) }); }

// components/forms/Select.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
function Select({
  options = [],
  style,
  ...rest
}) {
  return /*#__PURE__*/React.createElement("span", {
    style: {
      position: 'relative',
      display: 'block'
    }
  }, /*#__PURE__*/React.createElement("select", _extends({
    style: {
      width: '100%',
      height: 'var(--control-height)',
      padding: '0 34px 0 12px',
      appearance: 'none',
      fontFamily: 'var(--font-sans)',
      fontSize: 'var(--text-sm)',
      color: 'var(--text)',
      background: 'var(--surface)',
      border: '1px solid var(--border-strong)',
      borderRadius: 'var(--radius-sm)',
      ...style
    }
  }, rest), options.map(o => /*#__PURE__*/React.createElement("option", {
    key: o.value,
    value: o.value
  }, o.label))), /*#__PURE__*/React.createElement("span", {
    style: {
      position: 'absolute',
      right: 11,
      top: '50%',
      transform: 'translateY(-50%)',
      color: 'var(--text-faint)',
      pointerEvents: 'none'
    }
  }, /*#__PURE__*/React.createElement(__ds_scope.Icon, {
    name: "chevron-down",
    size: 16
  })));
}
Object.assign(__ds_scope, { Select });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/Select.jsx", error: String((e && e.message) || e) }); }

// components/forms/Textarea.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
function Textarea({
  invalid,
  rows = 6,
  mono,
  style,
  ...rest
}) {
  return /*#__PURE__*/React.createElement("textarea", _extends({
    rows: rows,
    style: {
      width: '100%',
      padding: '10px 12px',
      resize: 'vertical',
      fontFamily: mono ? 'var(--font-mono)' : 'var(--font-sans)',
      fontSize: 'var(--text-sm)',
      lineHeight: 'var(--leading-prose)',
      color: 'var(--text)',
      background: 'var(--surface)',
      border: '1px solid ' + (invalid ? 'var(--critical)' : 'var(--border-strong)'),
      borderRadius: 'var(--radius-sm)',
      ...style
    }
  }, rest));
}
Object.assign(__ds_scope, { Textarea });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/Textarea.jsx", error: String((e && e.message) || e) }); }

// components/navigation/Brand.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const MARK = 'M2 0h28a2 2 0 0 1 2 2v44l-9.6-11.4h-4L0 46V2a2 2 0 0 1 2-2Z';
const SLOTS = 'M6 11h20v3H6z M6 18h13v3H6z';
const SLOT_SM = 'M6 11.5h20v3.5H6z';

/** Wordmark lockup for UI chrome: the outlined bookmark symbol + live type. */
function Brand({
  size = 17,
  symbol = true,
  style,
  ...rest
}) {
  const h = Math.round(size * 0.82);
  const cuts = h >= 22 ? SLOTS : h >= 14 ? SLOT_SM : '';
  return /*#__PURE__*/React.createElement("span", _extends({
    style: {
      display: 'inline-flex',
      alignItems: 'center',
      gap: Math.round(size * 0.55),
      ...style
    }
  }, rest), symbol ? /*#__PURE__*/React.createElement("svg", {
    viewBox: "0 0 32 46",
    height: h,
    width: h * 32 / 46,
    fill: "var(--accent)",
    fillRule: "evenodd",
    "aria-hidden": "true",
    style: {
      display: 'block',
      flex: 'none'
    }
  }, /*#__PURE__*/React.createElement("path", {
    d: MARK + ' ' + cuts
  })) : null, /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: 'var(--font-sans)',
      fontWeight: 'var(--weight-bold)',
      fontSize: size,
      letterSpacing: 'var(--track-title)',
      color: 'var(--text)',
      lineHeight: 1
    }
  }, "Commit", /*#__PURE__*/React.createElement("span", {
    style: {
      fontWeight: 'var(--weight-regular)',
      color: 'var(--accent)'
    }
  }, "Ahead")));
}
Object.assign(__ds_scope, { Brand });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/navigation/Brand.jsx", error: String((e && e.message) || e) }); }

// components/navigation/PageHeader.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
function PageHeader({
  kicker,
  title,
  summary,
  actions,
  style,
  ...rest
}) {
  return /*#__PURE__*/React.createElement("header", _extends({
    style: {
      marginBottom: 'var(--space-18)',
      ...style
    }
  }, rest), kicker ? /*#__PURE__*/React.createElement("p", {
    style: {
      margin: '0 0 var(--space-5)',
      fontFamily: 'var(--font-mono)',
      fontWeight: 'var(--weight-medium)',
      fontSize: 'var(--text-micro)',
      letterSpacing: 'var(--track-label)',
      textTransform: 'uppercase',
      color: 'var(--text-faint)'
    }
  }, kicker) : null, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'flex-start',
      justifyContent: 'space-between',
      gap: 'var(--space-8)'
    }
  }, /*#__PURE__*/React.createElement("h1", {
    style: {
      margin: 0,
      fontFamily: 'var(--font-sans)',
      fontWeight: 'var(--weight-bold)',
      fontSize: 'var(--text-title)',
      lineHeight: 'var(--leading-title)',
      letterSpacing: 'var(--track-title)'
    }
  }, title), actions ? /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      gap: 'var(--space-4)',
      flex: 'none'
    }
  }, actions) : null), summary ? /*#__PURE__*/React.createElement("p", {
    style: {
      margin: 'var(--space-3) 0 0',
      fontSize: 'var(--text-base)',
      color: 'var(--text-muted)',
      maxWidth: '58ch',
      textWrap: 'pretty'
    }
  }, summary) : null);
}
Object.assign(__ds_scope, { PageHeader });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/navigation/PageHeader.jsx", error: String((e && e.message) || e) }); }

// components/navigation/SidebarNav.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
const NAV_ITEMS = [{
  id: 'queue',
  label: 'Study Queue',
  icon: 'list-ordered'
}, {
  id: 'items',
  label: 'Study Items',
  icon: 'book-marked'
}, {
  id: 'profile',
  label: 'Profile & CVs',
  icon: 'user-round'
}, {
  id: 'jobs',
  label: 'Job Analyses',
  icon: 'briefcase'
}, {
  id: 'notes',
  label: 'Interview Notes',
  icon: 'notebook-text'
}, {
  id: 'settings',
  label: 'Settings',
  icon: 'settings'
}];
function SidebarNav({
  items = NAV_ITEMS,
  active,
  onNavigate,
  footer,
  style,
  ...rest
}) {
  return /*#__PURE__*/React.createElement("aside", _extends({
    style: {
      width: 'var(--sidebar-width)',
      flex: 'none',
      padding: 'var(--space-14) var(--space-6)',
      display: 'flex',
      flexDirection: 'column',
      gap: 'var(--space-14)',
      background: 'var(--bg)',
      ...style
    }
  }, rest), /*#__PURE__*/React.createElement("div", {
    style: {
      padding: '0 var(--space-5)'
    }
  }, /*#__PURE__*/React.createElement(__ds_scope.Brand, null)), /*#__PURE__*/React.createElement("nav", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 2
    }
  }, items.map(it => {
    const on = it.id === active;
    return /*#__PURE__*/React.createElement("button", {
      key: it.id,
      onClick: () => onNavigate && onNavigate(it.id),
      "aria-current": on ? 'page' : undefined,
      style: {
        display: 'flex',
        alignItems: 'center',
        gap: 'var(--space-5)',
        padding: '9px var(--space-5)',
        minHeight: 38,
        border: 0,
        cursor: 'pointer',
        textAlign: 'left',
        borderRadius: 'var(--radius-sm)',
        fontFamily: 'var(--font-sans)',
        fontSize: 'var(--text-sm)',
        background: on ? 'var(--surface-alt)' : 'transparent',
        color: on ? 'var(--text)' : 'var(--text-muted)',
        fontWeight: on ? 'var(--weight-semibold)' : 'var(--weight-regular)'
      }
    }, /*#__PURE__*/React.createElement("span", {
      style: {
        color: on ? 'var(--accent)' : 'inherit',
        opacity: on ? 1 : 0.75
      }
    }, /*#__PURE__*/React.createElement(__ds_scope.Icon, {
      name: it.icon,
      size: 16
    })), it.label);
  })), footer ? /*#__PURE__*/React.createElement("div", {
    style: {
      marginTop: 'auto'
    }
  }, footer) : null);
}
Object.assign(__ds_scope, { NAV_ITEMS, SidebarNav });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/navigation/SidebarNav.jsx", error: String((e && e.message) || e) }); }

// ui_kits/app/App.jsx
try { (() => {
// Design-system primitives are read inside each component: this file is also swept
// into _ds_bundle.js, where module-top-level reads would resolve before the namespace is populated.

function ThemeToggle() {
  const {
    IconButton
  } = window.CommitAheadDesignSystem_80fdcb;
  const [dark, setDark] = React.useState(false);
  React.useEffect(() => {
    document.documentElement.setAttribute('data-theme', dark ? 'dark' : 'light');
  }, [dark]);
  return /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'space-between',
      padding: '0 var(--space-5)',
      borderTop: '1px solid var(--border-soft)',
      paddingTop: 'var(--space-6)'
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: 'var(--font-mono)',
      fontSize: 'var(--text-micro)',
      letterSpacing: 'var(--track-label)',
      textTransform: 'uppercase',
      color: 'var(--text-faint)'
    }
  }, window.CA.budget.used.toFixed(2), " / ", window.CA.budget.cap.toFixed(2), " ", window.CA.budget.currency), /*#__PURE__*/React.createElement(IconButton, {
    size: "sm",
    icon: dark ? 'sun' : 'moon',
    label: dark ? 'Switch to light theme' : 'Switch to dark theme',
    onClick: () => setDark(!dark)
  }));
}
function App() {
  const {
    SidebarNav,
    EmptyState,
    Button
  } = window.CommitAheadDesignSystem_80fdcb;
  const [signedIn, setSignedIn] = React.useState(false);
  const [screen, setScreen] = React.useState('queue');
  const [item, setItem] = React.useState(null);
  const [reviewing, setReviewing] = React.useState(false);
  if (!signedIn) return /*#__PURE__*/React.createElement(Login, {
    onSignIn: () => setSignedIn(true)
  });
  const go = id => {
    setScreen(id);
    setItem(null);
  };
  let body;
  if (item) body = /*#__PURE__*/React.createElement(StudyItemDetail, {
    itemId: item,
    reviewing: reviewing,
    onBack: () => {
      setItem(null);
      setReviewing(false);
    }
  });else if (screen === 'queue') body = /*#__PURE__*/React.createElement(StudyQueue, {
    onOpenItem: (id, r) => {
      setItem(id);
      setReviewing(!!r);
    }
  });else if (screen === 'jobs') body = /*#__PURE__*/React.createElement(JobAnalysis, null);else if (screen === 'profile') body = /*#__PURE__*/React.createElement(CVEditor, null);else if (screen === 'items') body = /*#__PURE__*/React.createElement(StudyQueue, {
    onOpenItem: (id, r) => {
      setItem(id);
      setReviewing(!!r);
    }
  });else body = /*#__PURE__*/React.createElement(EmptyState, {
    title: screen === 'notes' ? 'No interview notes yet' : 'Settings',
    action: screen === 'notes' ? /*#__PURE__*/React.createElement(Button, {
      icon: "plus"
    }, "New interview note") : null
  }, screen === 'notes' ? 'Record what was actually asked after each round. Notes become evidence, and evidence is what moves items up your queue.' : 'Scoring weights, theme, AI budget and account. Not part of this UI kit.');
  return /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      minHeight: '100vh',
      background: 'var(--bg)'
    }
  }, /*#__PURE__*/React.createElement(SidebarNav, {
    active: item ? 'queue' : screen,
    onNavigate: go,
    footer: /*#__PURE__*/React.createElement(ThemeToggle, null)
  }), /*#__PURE__*/React.createElement("main", {
    style: {
      flex: 1,
      background: 'var(--surface)',
      borderLeft: '1px solid var(--border-soft)',
      padding: 'var(--page-pad-y) var(--page-pad-x) var(--space-32)',
      minWidth: 0
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      maxWidth: 'var(--content-max)'
    }
  }, body)));
}
// Mounted from an inline block in index.html — inline scripts are not swept into the bundle.
window.CommitAheadApp = App;
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/app/App.jsx", error: String((e && e.message) || e) }); }

// ui_kits/app/CVEditor.jsx
try { (() => {
// Design-system primitives are read inside each component: this file is also swept
// into _ds_bundle.js, where module-top-level reads would resolve before the namespace is populated.

function Preview({
  cv
}) {
  const {
    Chip
  } = window.CommitAheadDesignSystem_80fdcb;
  return /*#__PURE__*/React.createElement("div", {
    style: {
      border: '1px solid var(--border)',
      borderRadius: 'var(--radius-sm)',
      background: 'var(--surface)',
      padding: 'var(--space-16) var(--space-18)',
      fontSize: 'var(--text-sm)',
      lineHeight: 'var(--leading-prose)'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      fontSize: 'var(--text-headline)',
      fontWeight: 'var(--weight-bold)',
      letterSpacing: 'var(--track-headline)'
    }
  }, "Denis Silva"), /*#__PURE__*/React.createElement("div", {
    style: {
      color: 'var(--text-muted)',
      marginTop: 2
    }
  }, "Senior Backend Engineer"), /*#__PURE__*/React.createElement("div", {
    style: {
      fontFamily: 'var(--font-mono)',
      fontSize: 'var(--text-xs)',
      color: 'var(--text-faint)',
      marginTop: 'var(--space-4)'
    }
  }, [cv.include.email && 'denis@example.com', cv.include.phone && '+44 7700 900000', cv.include.address && 'London, United Kingdom'].filter(Boolean).join(' · ')), /*#__PURE__*/React.createElement("p", {
    style: {
      margin: 'var(--space-10) 0 0',
      color: 'var(--text-muted)'
    }
  }, cv.summary), /*#__PURE__*/React.createElement("div", {
    style: {
      marginTop: 'var(--space-14)',
      fontFamily: 'var(--font-mono)',
      fontWeight: 'var(--weight-medium)',
      fontSize: 'var(--text-micro)',
      letterSpacing: 'var(--track-label)',
      textTransform: 'uppercase',
      color: 'var(--text-faint)',
      paddingBottom: 'var(--space-3)',
      borderBottom: '1px solid var(--border-soft)'
    }
  }, "Experience"), cv.experience.filter(e => e.on).map(e => /*#__PURE__*/React.createElement("div", {
    key: e.id,
    style: {
      padding: 'var(--space-8) 0',
      borderBottom: '1px solid var(--border-soft)'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      justifyContent: 'space-between',
      gap: 'var(--space-8)',
      alignItems: 'baseline',
      flexWrap: 'wrap'
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      fontWeight: 'var(--weight-semibold)',
      minWidth: 0
    }
  }, e.role), /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: 'var(--font-mono)',
      fontSize: 'var(--text-xs)',
      color: 'var(--text-faint)',
      whiteSpace: 'nowrap'
    }
  }, e.dates)), /*#__PURE__*/React.createElement("div", {
    style: {
      color: 'var(--text-muted)',
      fontSize: 'var(--text-xs)',
      margin: '2px 0 var(--space-4)'
    }
  }, e.company), /*#__PURE__*/React.createElement("div", {
    style: {
      color: 'var(--text-muted)'
    }
  }, e.summary))), /*#__PURE__*/React.createElement("div", {
    style: {
      marginTop: 'var(--space-12)',
      display: 'flex',
      gap: 'var(--space-4)',
      flexWrap: 'wrap'
    }
  }, cv.skills.map(s => /*#__PURE__*/React.createElement(Chip, {
    key: s
  }, s))));
}
function CVEditor() {
  const {
    PageHeader,
    Button,
    Tabs,
    Field,
    Input,
    Select,
    Checkbox,
    Textarea
  } = window.CommitAheadDesignSystem_80fdcb;
  const [cv, setCv] = React.useState(window.CA.cv);
  const [tab, setTab] = React.useState('content');
  const toggleInc = k => setCv({
    ...cv,
    include: {
      ...cv.include,
      [k]: !cv.include[k]
    }
  });
  const toggleExp = id => setCv({
    ...cv,
    experience: cv.experience.map(e => e.id === id ? {
      ...e,
      on: !e.on
    } : e)
  });
  const included = cv.experience.filter(e => e.on).length;
  return /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(PageHeader, {
    kicker: "CV presentation \xB7 1 of 2",
    title: cv.label,
    summary: 'Curated from your professional profile for ' + cv.market + '. ' + included + ' of ' + cv.experience.length + ' experience entries included, ' + cv.pageLimit + '-page limit.',
    actions: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Button, {
      size: "sm",
      variant: "secondary",
      icon: "sparkles"
    }, "Analyse with AI"), /*#__PURE__*/React.createElement(Button, {
      size: "sm",
      icon: "download"
    }, "Export"))
  }), /*#__PURE__*/React.createElement(Tabs, {
    value: tab,
    onChange: setTab,
    style: {
      marginBottom: 'var(--space-14)'
    },
    items: [{
      value: 'content',
      label: 'Content'
    }, {
      value: 'preview',
      label: 'Preview'
    }, {
      value: 'settings',
      label: 'Presentation settings'
    }]
  }), tab === 'preview' ? /*#__PURE__*/React.createElement(Preview, {
    cv: cv
  }) : null, tab === 'content' ? /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'grid',
      gridTemplateColumns: '1fr 320px',
      gap: 'var(--space-20)',
      alignItems: 'start'
    }
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement(Field, {
    label: "Summary override",
    hint: "Leave empty to use the profile summary."
  }, /*#__PURE__*/React.createElement(Textarea, {
    rows: 4,
    value: cv.summary,
    onChange: e => setCv({
      ...cv,
      summary: e.target.value
    })
  })), /*#__PURE__*/React.createElement("div", {
    style: {
      margin: 'var(--space-16) 0 var(--space-6)',
      fontFamily: 'var(--font-mono)',
      fontWeight: 'var(--weight-medium)',
      fontSize: 'var(--text-micro)',
      letterSpacing: 'var(--track-label)',
      textTransform: 'uppercase',
      color: 'var(--text-faint)'
    }
  }, "Experience \u2014 select and order"), cv.experience.map(e => /*#__PURE__*/React.createElement("div", {
    key: e.id,
    style: {
      display: 'flex',
      gap: 'var(--space-6)',
      padding: 'var(--space-7) 0',
      borderBottom: '1px solid var(--border-soft)',
      opacity: e.on ? 1 : 0.55
    }
  }, /*#__PURE__*/React.createElement(Checkbox, {
    checked: e.on,
    onChange: () => toggleExp(e.id)
  }), /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      gap: 'var(--space-6)',
      alignItems: 'baseline'
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      fontSize: 'var(--text-md)',
      fontWeight: 'var(--weight-medium)'
    }
  }, e.role), /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: 'var(--font-mono)',
      fontSize: 'var(--text-xs)',
      color: 'var(--text-faint)'
    }
  }, e.dates)), /*#__PURE__*/React.createElement("div", {
    style: {
      fontSize: 'var(--text-xs)',
      color: 'var(--text-muted)',
      marginTop: 2
    }
  }, e.company))))), /*#__PURE__*/React.createElement("aside", {
    style: {
      borderLeft: '1px solid var(--border-soft)',
      paddingLeft: 'var(--space-14)'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      marginBottom: 'var(--space-8)',
      fontFamily: 'var(--font-mono)',
      fontWeight: 'var(--weight-medium)',
      fontSize: 'var(--text-micro)',
      letterSpacing: 'var(--track-label)',
      textTransform: 'uppercase',
      color: 'var(--text-faint)'
    }
  }, "Live preview"), /*#__PURE__*/React.createElement("div", {
    style: {
      transform: 'scale(0.7)',
      transformOrigin: 'top left',
      width: '143%'
    }
  }, /*#__PURE__*/React.createElement(Preview, {
    cv: cv
  })))) : null, tab === 'settings' ? /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'grid',
      gridTemplateColumns: '1fr 1fr',
      gap: 'var(--space-12) var(--space-16)',
      maxWidth: 620
    }
  }, /*#__PURE__*/React.createElement(Field, {
    label: "Label",
    htmlFor: "lbl"
  }, /*#__PURE__*/React.createElement(Input, {
    id: "lbl",
    defaultValue: cv.label
  })), /*#__PURE__*/React.createElement(Field, {
    label: "Target market",
    htmlFor: "mkt"
  }, /*#__PURE__*/React.createElement(Input, {
    id: "mkt",
    defaultValue: cv.market
  })), /*#__PURE__*/React.createElement(Field, {
    label: "Locale",
    htmlFor: "loc"
  }, /*#__PURE__*/React.createElement(Select, {
    id: "loc",
    defaultValue: cv.locale,
    options: [{
      value: 'en-GB',
      label: 'en-GB — United Kingdom'
    }, {
      value: 'en-US',
      label: 'en-US — United States'
    }, {
      value: 'pt-PT',
      label: 'pt-PT — Portugal'
    }]
  })), /*#__PURE__*/React.createElement(Field, {
    label: "Export template",
    htmlFor: "tpl"
  }, /*#__PURE__*/React.createElement(Select, {
    id: "tpl",
    options: [{
      value: 'rc',
      label: 'Reverse chronological'
    }, {
      value: 'sk',
      label: 'Skills first'
    }]
  })), /*#__PURE__*/React.createElement(Field, {
    label: "Page limit",
    htmlFor: "pl"
  }, /*#__PURE__*/React.createElement(Input, {
    id: "pl",
    type: "number",
    defaultValue: cv.pageLimit
  })), /*#__PURE__*/React.createElement(Field, {
    label: "Date format",
    htmlFor: "df"
  }, /*#__PURE__*/React.createElement(Select, {
    id: "df",
    options: [{
      value: 'my',
      label: 'March 2022'
    }, {
      value: 'sn',
      label: '03/2022'
    }]
  })), /*#__PURE__*/React.createElement("div", {
    style: {
      gridColumn: '1 / -1'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      marginBottom: 'var(--space-6)',
      fontFamily: 'var(--font-mono)',
      fontWeight: 'var(--weight-medium)',
      fontSize: 'var(--text-micro)',
      letterSpacing: 'var(--track-label)',
      textTransform: 'uppercase',
      color: 'var(--text-faint)'
    }
  }, "Personal details shown on this presentation"), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      gap: 'var(--space-14)',
      flexWrap: 'wrap'
    }
  }, /*#__PURE__*/React.createElement(Checkbox, {
    checked: cv.include.photo,
    onChange: () => toggleInc('photo'),
    label: "Photo"
  }), /*#__PURE__*/React.createElement(Checkbox, {
    checked: cv.include.email,
    onChange: () => toggleInc('email'),
    label: "Email"
  }), /*#__PURE__*/React.createElement(Checkbox, {
    checked: cv.include.phone,
    onChange: () => toggleInc('phone'),
    label: "Phone"
  }), /*#__PURE__*/React.createElement(Checkbox, {
    checked: cv.include.address,
    onChange: () => toggleInc('address'),
    label: "Address"
  })), /*#__PURE__*/React.createElement("p", {
    style: {
      marginTop: 'var(--space-6)',
      fontSize: 'var(--text-xs)',
      color: 'var(--text-faint)',
      maxWidth: '58ch',
      lineHeight: 'var(--leading-prose)'
    }
  }, "These control rendering only. Contact details always live on the professional profile and are never duplicated onto a presentation."))) : null);
}
window.CVEditor = CVEditor;
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/app/CVEditor.jsx", error: String((e && e.message) || e) }); }

// ui_kits/app/JobAnalysis.jsx
try { (() => {
// Design-system primitives are read inside each component: this file is also swept
// into _ds_bundle.js, where module-top-level reads would resolve before the namespace is populated.

function JobAnalysis() {
  const {
    PageHeader,
    Button,
    Badge,
    DataTable,
    ProposalRow,
    Callout,
    Dialog
  } = window.CommitAheadDesignSystem_80fdcb;
  const job = window.CA.job;
  const b = window.CA.budget;
  const [decisions, setDecisions] = React.useState({});
  const [applied, setApplied] = React.useState(false);
  const [confirmRun, setConfirmRun] = React.useState(false);
  const pending = job.proposals.filter(p => !decisions[p.id]).length;
  const accepted = job.proposals.filter(p => decisions[p.id] === 'accepted').length;
  const rows = job.requirements.map(r => ({
    id: String(r.id),
    priority: r.priority,
    text: r.text,
    match: r.match,
    gap: r.severity ? /*#__PURE__*/React.createElement(Badge, {
      tone: r.severity === 'High' ? 'critical' : r.severity === 'Medium' ? 'caution' : 'good'
    }, r.severity) : /*#__PURE__*/React.createElement(Badge, {
      tone: "good",
      dot: false
    }, "Matched")
  }));
  return /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(PageHeader, {
    kicker: "Job analysis \xB7 added 2 days ago",
    title: job.title,
    summary: "Six requirements extracted from the posting, each matched against your professional profile.",
    actions: /*#__PURE__*/React.createElement(Button, {
      size: "sm",
      icon: "sparkles",
      onClick: () => setConfirmRun(true)
    }, "Analyse with AI")
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      gap: 'var(--space-14)',
      marginBottom: 'var(--space-14)',
      fontFamily: 'var(--font-mono)',
      fontSize: 'var(--text-xs)',
      color: 'var(--text-faint)'
    }
  }, /*#__PURE__*/React.createElement("span", null, job.source), /*#__PURE__*/React.createElement("span", null, "AI budget ", b.used.toFixed(2), " of ", b.cap.toFixed(2), " ", b.currency, " this month")), /*#__PURE__*/React.createElement(DataTable, {
    columns: [{
      key: 'priority',
      label: 'Priority',
      width: '86px',
      muted: true
    }, {
      key: 'text',
      label: 'Requirement'
    }, {
      key: 'match',
      label: 'Match',
      width: '82px',
      muted: true
    }, {
      key: 'gap',
      label: 'Gap',
      width: '104px'
    }],
    rows: rows
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      marginTop: 'var(--space-20)'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'baseline',
      justifyContent: 'space-between',
      gap: 'var(--space-8)',
      paddingBottom: 'var(--space-6)',
      borderBottom: '1px solid var(--border)'
    }
  }, /*#__PURE__*/React.createElement("h2", {
    style: {
      margin: 0,
      fontSize: 'var(--text-lead)',
      fontWeight: 'var(--weight-semibold)',
      letterSpacing: 'var(--track-headline)'
    }
  }, "Analysis draft"), /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: 'var(--font-mono)',
      fontSize: 'var(--text-xs)',
      color: 'var(--text-faint)'
    }
  }, applied ? 'Applied · ' + accepted + ' of 3 accepted' : pending + ' of 3 undecided')), applied ? null : /*#__PURE__*/React.createElement(Callout, {
    title: "Nothing changes until you apply this draft",
    style: {
      margin: 'var(--space-8) 0 var(--space-6)'
    }
  }, "Every proposal needs an explicit accept or reject. Accepted link proposals become evidence links, which is what raises demand in your queue."), job.proposals.map(p => /*#__PURE__*/React.createElement(ProposalRow, {
    key: p.id,
    kind: p.kind,
    rationale: p.rationale,
    status: decisions[p.id] || 'pending',
    onAccept: () => setDecisions({
      ...decisions,
      [p.id]: 'accepted'
    }),
    onReject: () => setDecisions({
      ...decisions,
      [p.id]: 'rejected'
    })
  }, p.text)), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      gap: 'var(--space-5)',
      marginTop: 'var(--space-10)',
      alignItems: 'center'
    }
  }, /*#__PURE__*/React.createElement(Button, {
    disabled: pending > 0 || applied,
    onClick: () => setApplied(true)
  }, applied ? 'Draft applied' : 'Apply draft'), /*#__PURE__*/React.createElement(Button, {
    variant: "secondary",
    disabled: applied
  }, "Discard draft"), pending > 0 ? /*#__PURE__*/React.createElement("span", {
    style: {
      fontSize: 'var(--text-xs)',
      color: 'var(--text-faint)'
    }
  }, "Decide every proposal first \u2014 ", pending, " left.") : null)), /*#__PURE__*/React.createElement(Dialog, {
    open: confirmRun,
    title: "Analyse this job posting?",
    confirmLabel: "Run analysis",
    onCancel: () => setConfirmRun(false),
    onConfirm: () => setConfirmRun(false)
  }, "This sends the extracted posting text to the AI provider once and produces a new draft. Estimated cost 0.18 ", b.currency, ", charged against your monthly budget. It replaces any pending draft for this analysis."));
}
window.JobAnalysis = JobAnalysis;
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/app/JobAnalysis.jsx", error: String((e && e.message) || e) }); }

// ui_kits/app/Login.jsx
try { (() => {
// Design-system primitives are read inside each component: this file is also swept
// into _ds_bundle.js, where module-top-level reads would resolve before the namespace is populated.

function Login({
  onSignIn
}) {
  const {
    Brand,
    Field,
    Input,
    Button,
    Callout
  } = window.CommitAheadDesignSystem_80fdcb;
  const [sent, setSent] = React.useState(false);
  const [email, setEmail] = React.useState('denis@example.com');
  return /*#__PURE__*/React.createElement("div", {
    style: {
      minHeight: '100%',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      background: 'var(--bg)',
      padding: 'var(--space-20)'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      width: 340
    }
  }, /*#__PURE__*/React.createElement(Brand, {
    size: 30,
    style: {
      marginBottom: 'var(--space-8)'
    }
  }), /*#__PURE__*/React.createElement("p", {
    style: {
      margin: '0 0 var(--space-18)',
      fontSize: 'var(--text-lg)',
      lineHeight: 'var(--leading-prose)',
      color: 'var(--text-muted)'
    }
  }, "Know what to study next, and why."), sent ? /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Callout, {
    title: "Check your inbox"
  }, "A sign-in link is on its way to ", /*#__PURE__*/React.createElement("strong", {
    style: {
      color: 'var(--text)'
    }
  }, email), ". It is valid for 15 minutes and works once."), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      gap: 'var(--space-4)',
      marginTop: 'var(--space-8)'
    }
  }, /*#__PURE__*/React.createElement(Button, {
    onClick: onSignIn
  }, "Continue"), /*#__PURE__*/React.createElement(Button, {
    variant: "ghost",
    onClick: () => setSent(false)
  }, "Use a different address"))) : /*#__PURE__*/React.createElement("form", {
    onSubmit: e => {
      e.preventDefault();
      setSent(true);
    }
  }, /*#__PURE__*/React.createElement(Field, {
    label: "Email",
    htmlFor: "login-email"
  }, /*#__PURE__*/React.createElement(Input, {
    id: "login-email",
    type: "email",
    value: email,
    onChange: e => setEmail(e.target.value),
    autoComplete: "email"
  })), /*#__PURE__*/React.createElement(Button, {
    type: "submit",
    fullWidth: true,
    style: {
      marginTop: 'var(--space-8)'
    }
  }, "Send sign-in link")), /*#__PURE__*/React.createElement("p", {
    style: {
      marginTop: 'var(--space-14)',
      paddingTop: 'var(--space-8)',
      borderTop: '1px solid var(--border-soft)',
      fontSize: 'var(--text-xs)',
      color: 'var(--text-faint)',
      lineHeight: 'var(--leading-prose)'
    }
  }, "Invite-only. Ask for access if you don't have one. There is no password to forget and no public sign-up.")));
}
window.Login = Login;
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/app/Login.jsx", error: String((e && e.message) || e) }); }

// ui_kits/app/StudyItemDetail.jsx
try { (() => {
// Design-system primitives are read inside each component: this file is also swept
// into _ds_bundle.js, where module-top-level reads would resolve before the namespace is populated.

function Sparkline({
  values
}) {
  return /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'flex-end',
      gap: 5,
      height: 34
    }
  }, values.map((v, i) => /*#__PURE__*/React.createElement("div", {
    key: i,
    title: 'Confidence ' + v + ' of 5',
    style: {
      width: 12,
      height: v / 5 * 34,
      background: i === values.length - 1 ? 'var(--accent)' : 'var(--border)'
    }
  })));
}
function Meta({
  label,
  children
}) {
  return /*#__PURE__*/React.createElement("div", {
    style: {
      marginBottom: 'var(--space-10)'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      fontFamily: 'var(--font-mono)',
      fontWeight: 'var(--weight-medium)',
      fontSize: 'var(--text-micro)',
      letterSpacing: 'var(--track-label)',
      textTransform: 'uppercase',
      color: 'var(--text-faint)',
      marginBottom: 'var(--space-3)'
    }
  }, label), /*#__PURE__*/React.createElement("div", {
    style: {
      fontSize: 'var(--text-md)',
      lineHeight: 'var(--leading-prose)',
      color: 'var(--text)'
    }
  }, children));
}
function StudyItemDetail({
  itemId,
  onBack,
  reviewing
}) {
  const {
    Button,
    Chip,
    Field,
    Textarea,
    RatingScale,
    Tabs,
    ScoreBreakdown,
    Dialog
  } = window.CommitAheadDesignSystem_80fdcb;
  const item = window.CA.queue.find(i => i.id === itemId) || window.CA.queue[0];
  const [tab, setTab] = React.useState('details');
  const [rating, setRating] = React.useState(reviewing ? 4 : null);
  const [saved, setSaved] = React.useState(false);
  const [confirm, setConfirm] = React.useState(false);
  return /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Button, {
    variant: "ghost",
    size: "sm",
    icon: "arrow-left",
    onClick: onBack,
    style: {
      marginLeft: -12,
      marginBottom: 'var(--space-6)'
    }
  }, "Back to queue"), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'flex-start',
      justifyContent: 'space-between',
      gap: 'var(--space-8)'
    }
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("h1", {
    style: {
      margin: '0 0 var(--space-5)',
      fontSize: 'var(--text-title)',
      fontWeight: 'var(--weight-bold)',
      letterSpacing: 'var(--track-title)',
      lineHeight: 'var(--leading-title)'
    }
  }, item.title), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      gap: 'var(--space-4)'
    }
  }, /*#__PURE__*/React.createElement(Chip, null, item.category), item.difficulty ? /*#__PURE__*/React.createElement(Chip, null, item.difficulty) : null, /*#__PURE__*/React.createElement(Chip, null, `Importance ${item.importance} of 5`))), /*#__PURE__*/React.createElement(Button, {
    variant: "danger",
    size: "sm",
    icon: "trash-2",
    onClick: () => setConfirm(true)
  }, "Delete")), /*#__PURE__*/React.createElement(Tabs, {
    value: tab,
    onChange: setTab,
    style: {
      margin: 'var(--space-14) 0 var(--space-14)'
    },
    items: [{
      value: 'details',
      label: 'Details'
    }, {
      value: 'reviews',
      label: 'Review history'
    }, {
      value: 'evidence',
      label: 'Evidence links'
    }]
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'grid',
      gridTemplateColumns: '1fr 264px',
      gap: 'var(--space-20)',
      alignItems: 'start'
    }
  }, /*#__PURE__*/React.createElement("div", null, tab === 'details' ? /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Meta, {
    label: "Patterns"
  }, item.patterns), /*#__PURE__*/React.createElement(Meta, {
    label: "Expected complexity"
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: 'var(--font-mono)'
    }
  }, item.complexity)), /*#__PURE__*/React.createElement(Meta, {
    label: "Approach"
  }, item.approach)) : tab === 'reviews' ? /*#__PURE__*/React.createElement("div", null, [['11 days ago', 3, 'Got the merge right but fumbled the touching-interval edge case.'], ['3 weeks ago', 2, 'Needed the hint. Sorting step was not obvious under time pressure.'], ['6 weeks ago', 3, null]].map(([when, score, note], i) => /*#__PURE__*/React.createElement("div", {
    key: i,
    style: {
      padding: 'var(--space-7) 0',
      borderBottom: '1px solid var(--border-soft)'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      justifyContent: 'space-between',
      gap: 'var(--space-8)'
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      fontSize: 'var(--text-sm)',
      color: 'var(--text-muted)'
    }
  }, when), /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: 'var(--font-mono)',
      fontSize: 'var(--text-sm)'
    }
  }, score, " of 5")), note ? /*#__PURE__*/React.createElement("p", {
    style: {
      margin: 'var(--space-3) 0 0',
      fontSize: 'var(--text-sm)',
      color: 'var(--text-muted)',
      lineHeight: 'var(--leading-prose)'
    }
  }, note) : null))) : /*#__PURE__*/React.createElement("div", null, [['Ledgerline — Senior Backend Engineer', 'Job analysis', 4, 'Interval scheduling named under required algorithms.'], ['Northwind, technical round 2', 'Interview note', 3, 'Asked to merge overlapping booking windows.']].map(([t, kind, w, why], i) => /*#__PURE__*/React.createElement("div", {
    key: i,
    style: {
      padding: 'var(--space-7) 0',
      borderBottom: '1px solid var(--border-soft)'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      justifyContent: 'space-between',
      gap: 'var(--space-8)'
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      fontSize: 'var(--text-md)',
      fontWeight: 'var(--weight-medium)'
    }
  }, t), /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: 'var(--font-mono)',
      fontSize: 'var(--text-xs)',
      color: 'var(--text-muted)',
      whiteSpace: 'nowrap'
    }
  }, "weight ", w, " of 5")), /*#__PURE__*/React.createElement("p", {
    style: {
      margin: 'var(--space-3) 0 0',
      fontSize: 'var(--text-xs)',
      color: 'var(--text-faint)'
    }
  }, kind, " \xB7 ", why))))), /*#__PURE__*/React.createElement("aside", {
    style: {
      borderLeft: '1px solid var(--border-soft)',
      paddingLeft: 'var(--space-14)'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      fontFamily: 'var(--font-mono)',
      fontWeight: 'var(--weight-medium)',
      fontSize: 'var(--text-micro)',
      letterSpacing: 'var(--track-label)',
      textTransform: 'uppercase',
      color: 'var(--text-faint)'
    }
  }, "Mastery"), /*#__PURE__*/React.createElement("div", {
    style: {
      fontFamily: 'var(--font-mono)',
      fontSize: 42,
      lineHeight: 1,
      margin: 'var(--space-5) 0 var(--space-3)',
      fontVariantNumeric: 'tabular-nums'
    }
  }, item.mastery.toFixed(1)), /*#__PURE__*/React.createElement("p", {
    style: {
      margin: '0 0 var(--space-8)',
      fontSize: 'var(--text-xs)',
      color: 'var(--text-faint)'
    }
  }, "Average of your last three reviews"), /*#__PURE__*/React.createElement(Sparkline, {
    values: item.reviews || [3, 2, 3]
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      margin: 'var(--space-14) 0 var(--space-5)',
      fontFamily: 'var(--font-mono)',
      fontWeight: 'var(--weight-medium)',
      fontSize: 'var(--text-micro)',
      letterSpacing: 'var(--track-label)',
      textTransform: 'uppercase',
      color: 'var(--text-faint)'
    }
  }, "Effective score ", item.score), /*#__PURE__*/React.createElement(ScoreBreakdown, item.breakdown || {
    importance: 30,
    demand: 20,
    masteryGap: 20
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      marginTop: 'var(--space-16)',
      paddingTop: 'var(--space-12)',
      borderTop: '1px solid var(--border-soft)'
    }
  }, /*#__PURE__*/React.createElement(Field, {
    label: "Confidence after this session",
    hint: "1 = could not start \xB7 5 = could teach it"
  }, /*#__PURE__*/React.createElement(RatingScale, {
    name: "Confidence rating",
    value: rating,
    onChange: v => {
      setRating(v);
      setSaved(false);
    }
  })), /*#__PURE__*/React.createElement(Textarea, {
    rows: 3,
    placeholder: "Optional notes",
    style: {
      margin: 'var(--space-6) 0'
    }
  }), /*#__PURE__*/React.createElement(Button, {
    fullWidth: true,
    disabled: !rating,
    onClick: () => setSaved(true)
  }, saved ? 'Review saved' : 'Save review'), saved ? /*#__PURE__*/React.createElement("p", {
    style: {
      margin: 'var(--space-5) 0 0',
      fontSize: 'var(--text-xs)',
      color: 'var(--text-faint)'
    }
  }, "Mastery and effective score recalculate on save.") : null))), /*#__PURE__*/React.createElement(Dialog, {
    open: confirm,
    destructive: true,
    title: "Delete this study item?",
    confirmLabel: "Delete item",
    onCancel: () => setConfirm(false),
    onConfirm: () => setConfirm(false)
  }, item.title, " has three reviews and two evidence links. Deleting it removes the review history and the demand those links contribute to other rankings."));
}
window.StudyItemDetail = StudyItemDetail;
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/app/StudyItemDetail.jsx", error: String((e && e.message) || e) }); }

// ui_kits/app/StudyQueue.jsx
try { (() => {
function _extends() { return _extends = Object.assign ? Object.assign.bind() : function (n) { for (var e = 1; e < arguments.length; e++) { var t = arguments[e]; for (var r in t) ({}).hasOwnProperty.call(t, r) && (n[r] = t[r]); } return n; }, _extends.apply(null, arguments); }
// Design-system primitives are read inside each component: this file is also swept
// into _ds_bundle.js, where module-top-level reads would resolve before the namespace is populated.

function StudyQueue({
  onOpenItem
}) {
  const {
    PageHeader,
    Chip,
    Button,
    QueueRow,
    ScoreNumeral,
    ScoreBreakdown
  } = window.CommitAheadDesignSystem_80fdcb;
  const [filter, setFilter] = React.useState('All');
  const cats = ['All', 'Theory', 'LeetCode', 'System Design', 'Behavioral'];
  const all = window.CA.queue;
  const next = all[0];
  const rest = all.slice(1).filter(i => filter === 'All' || i.category === filter);
  return /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(PageHeader, {
    kicker: "Tuesday \xB7 18 active items",
    title: "Study Queue",
    summary: "Ranked by importance, evidence of demand, and how long ago you last proved you knew it.",
    actions: /*#__PURE__*/React.createElement(Button, {
      size: "sm",
      icon: "plus"
    }, "New study item")
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'grid',
      gridTemplateColumns: '1fr 176px',
      gap: 'var(--space-18)',
      alignItems: 'start',
      paddingBottom: 'var(--space-14)',
      borderBottom: '1px solid var(--border-soft)'
    }
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("p", {
    style: {
      margin: '0 0 var(--space-5)',
      fontFamily: 'var(--font-mono)',
      fontWeight: 'var(--weight-medium)',
      fontSize: 'var(--text-micro)',
      letterSpacing: 'var(--track-label)',
      textTransform: 'uppercase',
      color: 'var(--accent)'
    }
  }, "Next \xB7 ", next.category), /*#__PURE__*/React.createElement("h2", {
    style: {
      margin: '0 0 var(--space-6)',
      fontSize: 'var(--text-headline)',
      fontWeight: 'var(--weight-semibold)',
      letterSpacing: 'var(--track-headline)',
      lineHeight: 1.25
    }
  }, next.title), /*#__PURE__*/React.createElement("p", {
    style: {
      margin: '0 0 var(--space-10)',
      fontSize: 'var(--text-md)',
      lineHeight: 'var(--leading-prose)',
      color: 'var(--text-muted)',
      maxWidth: '52ch',
      textWrap: 'pretty'
    }
  }, next.why), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      gap: 'var(--space-5)'
    }
  }, /*#__PURE__*/React.createElement(Button, {
    onClick: () => onOpenItem(next.id, true)
  }, "Start review"), /*#__PURE__*/React.createElement(Button, {
    variant: "secondary",
    onClick: () => onOpenItem(next.id)
  }, "Open item"))), /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement(ScoreNumeral, {
    value: next.score
  }), /*#__PURE__*/React.createElement(ScoreBreakdown, _extends({}, next.breakdown, {
    style: {
      marginTop: 'var(--space-8)'
    }
  })))), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 'var(--space-4)',
      margin: 'var(--space-14) 0 var(--space-4)',
      flexWrap: 'wrap'
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      fontFamily: 'var(--font-mono)',
      fontWeight: 'var(--weight-medium)',
      fontSize: 'var(--text-micro)',
      letterSpacing: 'var(--track-label)',
      textTransform: 'uppercase',
      color: 'var(--text-faint)',
      marginRight: 'var(--space-3)'
    }
  }, "Then"), cats.map(c => /*#__PURE__*/React.createElement(Chip, {
    key: c,
    selected: c === filter,
    onClick: () => setFilter(c)
  }, c))), /*#__PURE__*/React.createElement("div", null, rest.map(i => /*#__PURE__*/React.createElement(QueueRow, {
    key: i.id,
    rank: i.rank,
    title: i.title,
    category: i.category,
    score: i.score,
    meta: i.meta,
    onClick: () => onOpenItem(i.id)
  }))), /*#__PURE__*/React.createElement("p", {
    style: {
      marginTop: 'var(--space-10)'
    }
  }, /*#__PURE__*/React.createElement(Button, {
    variant: "ghost",
    iconEnd: "arrow-right"
  }, "Show all 18 items")));
}
window.StudyQueue = StudyQueue;
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/app/StudyQueue.jsx", error: String((e && e.message) || e) }); }

// ui_kits/app/data.js
try { (() => {
window.CA = {
  user: {
    name: 'Denis Silva',
    email: 'denis@example.com'
  },
  weights: {
    importance: 40,
    demand: 35,
    masteryGap: 25
  },
  budget: {
    used: 3.1,
    cap: 8,
    currency: 'EUR'
  },
  queue: [{
    id: 'merge-intervals',
    rank: '01',
    title: 'Merge Intervals',
    category: 'LeetCode',
    score: 92,
    breakdown: {
      importance: 37,
      demand: 32,
      masteryGap: 23
    },
    meta: 'Linked from two job analyses · reviewed 11 days ago',
    why: 'First because two open job analyses ask for interval and scheduling work, you rated yourself 3 of 5 eleven days ago, and you set importance to 5 yourself.',
    importance: 5,
    demand: 4.5,
    mastery: 2.7,
    reviews: [3, 2, 3],
    difficulty: 'Medium',
    patterns: 'interval-merge, sorting',
    complexity: 'O(n log n) time · O(n) space',
    approach: 'Sort by start time, then walk once, merging with the last interval on the stack whenever the next one overlaps. The only real trap is treating touching intervals as disjoint.'
  }, {
    id: 'rate-limiter',
    rank: '02',
    title: 'Design a Rate Limiter',
    category: 'System Design',
    score: 88,
    breakdown: {
      importance: 36,
      demand: 30,
      masteryGap: 22
    },
    meta: 'Linked from Ledgerline — Senior Backend · reviewed 6 days ago',
    importance: 5,
    demand: 4.0,
    mastery: 3.0
  }, {
    id: 'disagreement',
    rank: '03',
    title: 'Disagreed with a technical decision',
    category: 'Behavioral',
    score: 81,
    breakdown: {
      importance: 30,
      demand: 24,
      masteryGap: 27
    },
    meta: 'Never reviewed · STAR story has no result yet',
    importance: 4,
    demand: 3.5,
    mastery: 2.0
  }, {
    id: 'cap',
    rank: '04',
    title: 'CAP theorem trade-offs',
    category: 'Theory',
    score: 74,
    breakdown: {
      importance: 32,
      demand: 21,
      masteryGap: 21
    },
    meta: 'Reviewed 11 days ago · mastery 3.3 of 5',
    importance: 4,
    demand: 3.0,
    mastery: 3.3
  }, {
    id: 'lru',
    rank: '05',
    title: 'LRU Cache',
    category: 'LeetCode',
    score: 69,
    breakdown: {
      importance: 32,
      demand: 21,
      masteryGap: 16
    },
    meta: 'Reviewed yesterday · mastery 4.0 of 5',
    importance: 4,
    demand: 3.0,
    mastery: 4.0
  }, {
    id: 'idempotency',
    rank: '06',
    title: 'Idempotency in payment flows',
    category: 'System Design',
    score: 63,
    breakdown: {
      importance: 24,
      demand: 18,
      masteryGap: 21
    },
    meta: 'Reviewed 18 days ago · mastery 3.7 of 5',
    importance: 3,
    demand: 2.5,
    mastery: 3.7
  }, {
    id: 'solid',
    rank: '07',
    title: 'SOLID in practice, not in slogans',
    category: 'Theory',
    score: 58,
    breakdown: {
      importance: 24,
      demand: 14,
      masteryGap: 20
    },
    meta: 'Reviewed 24 days ago · mastery 4.0 of 5',
    importance: 3,
    demand: 2.0,
    mastery: 4.0
  }],
  job: {
    title: 'Ledgerline — Senior Backend Engineer',
    source: 'Pasted text · 1 240 words · added 2 days ago',
    requirements: [{
      id: 1,
      priority: 'Required',
      text: 'Distributed transaction handling at scale',
      match: 'Missing',
      severity: 'High'
    }, {
      id: 2,
      priority: 'Required',
      text: 'Rate limiting and backpressure design',
      match: 'Partial',
      severity: 'Medium'
    }, {
      id: 3,
      priority: 'Required',
      text: 'PostgreSQL query optimisation',
      match: 'Matched',
      severity: null
    }, {
      id: 4,
      priority: 'Preferred',
      text: 'Event sourcing in a payments domain',
      match: 'Matched',
      severity: null
    }, {
      id: 5,
      priority: 'Preferred',
      text: 'Kubernetes operational ownership',
      match: 'Partial',
      severity: 'Low'
    }, {
      id: 6,
      priority: 'Required',
      text: 'Mentoring engineers through design review',
      match: 'Unknown',
      severity: 'Medium'
    }],
    proposals: [{
      id: 'p1',
      kind: 'Link proposal',
      text: 'Link this analysis to Design a Rate Limiter — weight 4 of 5',
      rationale: 'The posting names rate limiting and backpressure twice, both under required responsibilities.'
    }, {
      id: 'p2',
      kind: 'Study item proposal',
      text: 'New study item — Distributed transactions and the saga pattern (System Design)',
      rationale: 'No active study item covers distributed transactions, and it is the only Missing requirement marked High.'
    }, {
      id: 'p3',
      kind: 'Suggestion',
      text: 'Add a mentoring example to the UK CV presentation',
      rationale: 'Design-review mentoring is a required item and your profile has no achievement that evidences it. Advisory only — nothing is changed automatically.'
    }]
  },
  cv: {
    label: 'UK — Senior Backend Engineer',
    market: 'United Kingdom',
    locale: 'en-GB',
    template: 'Reverse chronological',
    pageLimit: 2,
    include: {
      photo: false,
      email: true,
      phone: true,
      address: false
    },
    summary: 'Backend engineer with six years on payments and telemetry systems, working in C#, .NET and PostgreSQL. Comfortable owning a service end to end, from schema to on-call.',
    experience: [{
      id: 'e1',
      on: true,
      role: 'Senior Backend Engineer',
      company: 'Ledgerline',
      dates: '2022 — present',
      summary: 'Owned the ledger reconciliation service; cut settlement discrepancies from 40 a week to near zero by rebuilding the matching engine around idempotent event sourcing.'
    }, {
      id: 'e2',
      on: true,
      role: 'Backend Engineer',
      company: 'Anchor Systems',
      dates: '2019 — 2022',
      summary: 'Built the ingestion pipeline handling 40 000 events a second from field devices, and the on-call rotation that held it at 99.95% for two years.'
    }, {
      id: 'e3',
      on: false,
      role: 'Junior Developer',
      company: 'Northgate Software',
      dates: '2017 — 2019',
      summary: 'Internal tooling in C# and SQL Server for a logistics customer.'
    }],
    skills: ['C#', '.NET', 'PostgreSQL', 'Distributed Systems', 'Event Sourcing', 'Docker', 'React']
  }
};
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/app/data.js", error: String((e && e.message) || e) }); }

__ds_ns.Badge = __ds_scope.Badge;

__ds_ns.Button = __ds_scope.Button;

__ds_ns.Callout = __ds_scope.Callout;

__ds_ns.Chip = __ds_scope.Chip;

__ds_ns.Dialog = __ds_scope.Dialog;

__ds_ns.EmptyState = __ds_scope.EmptyState;

__ds_ns.Icon = __ds_scope.Icon;

__ds_ns.IconButton = __ds_scope.IconButton;

__ds_ns.Tabs = __ds_scope.Tabs;

__ds_ns.DataTable = __ds_scope.DataTable;

__ds_ns.ProposalRow = __ds_scope.ProposalRow;

__ds_ns.QueueRow = __ds_scope.QueueRow;

__ds_ns.ScoreBreakdown = __ds_scope.ScoreBreakdown;

__ds_ns.ScoreNumeral = __ds_scope.ScoreNumeral;

__ds_ns.Checkbox = __ds_scope.Checkbox;

__ds_ns.Field = __ds_scope.Field;

__ds_ns.Input = __ds_scope.Input;

__ds_ns.RatingScale = __ds_scope.RatingScale;

__ds_ns.Select = __ds_scope.Select;

__ds_ns.Textarea = __ds_scope.Textarea;

__ds_ns.Brand = __ds_scope.Brand;

__ds_ns.PageHeader = __ds_scope.PageHeader;

__ds_ns.NAV_ITEMS = __ds_scope.NAV_ITEMS;

__ds_ns.SidebarNav = __ds_scope.SidebarNav;

})();
