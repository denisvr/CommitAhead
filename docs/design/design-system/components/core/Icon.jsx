import React from 'react';

/** Renders one glyph from the bundled Lucide sprite (assets/icons/icons.js). */
export function Icon({ name, size = 16, strokeWidth, style, ...rest }) {
  return (
    <svg
      width={size}
      height={size}
      fill="none"
      stroke="currentColor"
      strokeWidth={strokeWidth}
      aria-hidden="true"
      focusable="false"
      style={{ flex: 'none', display: 'block', ...style }}
      {...rest}
    >
      <use href={'#icon-' + name} />
    </svg>
  );
}
