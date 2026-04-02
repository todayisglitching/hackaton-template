import type { SVGProps } from 'react';

type BrandLogoProps = SVGProps<SVGSVGElement> & {
  title?: string;
};

const BrandLogo = ({ title = 'USmart logo', ...props }: BrandLogoProps) => {
  return (
    <svg viewBox="0 0 96 96" fill="none" aria-label={title} role="img" {...props}>
      <path
        d="M16 44L48 16L80 44"
        stroke="#ff5a1f"
        strokeWidth="10"
        strokeLinecap="square"
        strokeLinejoin="miter"
      />
      <path
        d="M24 44V64C24 74 31 82 48 82C65 82 72 74 72 64V44"
        stroke="url(#brandLogoGradient)"
        strokeWidth="10"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <defs>
        <linearGradient id="brandLogoGradient" x1="48" y1="82" x2="48" y2="44" gradientUnits="userSpaceOnUse">
          <stop offset="0" stopColor="#7000ff" />
          <stop offset="1" stopColor="#9b14ff" />
        </linearGradient>
      </defs>
    </svg>
  );
};

export default BrandLogo;
